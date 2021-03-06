﻿using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using JavaScriptEngineSwitcher.Core.Utilities;

namespace JavaScriptEngineSwitcher.ChakraCore
{
    /// <summary>
    /// Provides services for managing the queue of script tasks on the thread with modified stack size
    /// </summary>
    public sealed class ScriptDispatcher : IDisposable
    {
        private AutoResetEvent _queueEnqeued = new AutoResetEvent(false);
        private ConcurrentQueue<ScriptTask> _queue = new ConcurrentQueue<ScriptTask>();

        private readonly ChakraCoreJsEngine _refToEngine;

        public ConcurrentQueue<Action<ChakraCoreJsEngine>> _sharedQueue { get; set; }
        public AutoResetEvent _sharedQueueEnqeued { get; set; }

        /// <summary>
        /// Constructs an instance of script dispatcher
        /// </summary>
        /// <param name="maxStackSize">The maximum stack size, in bytes, to be used by the thread,
        /// or 0 to use the default maximum stack size specified in the header for the executable.</param>
        public ScriptDispatcher(int maxStackSize, ChakraCoreJsEngine refToEngine)
        {
            _refToEngine = refToEngine;
            var thread = new Thread(StartThread, maxStackSize)
            {
                IsBackground = true,
                Name = "ChakraCore Thread"
            };
            thread.Start();
        }

        [MethodImpl((MethodImplOptions)256 /* AggressiveInlining */)]
        private void VerifyNotDisposed()
        {
            if (_disposedFlag.IsSet())
            {
                throw new ObjectDisposedException(ToString());
            }
        }

        /// <summary>
        /// Starts a thread with modified stack size.
        /// Loops forever, processing script tasks from the queue.
        /// </summary>
        private void StartThread()
        {
            while (true)
            {
                if (_sharedQueueEnqeued != null && _sharedQueue != null)
                {
                    WaitHandle.WaitAny(new WaitHandle[] { _queueEnqeued, _sharedQueueEnqeued }); //todo: optimize
                }
                else if (_queue != null)
                {
                    _queueEnqeued.WaitOne();
                }
                else
                {
                    return;
                }

                while (_queue != null && _queue.TryDequeue(out var next))
                {
                    try
                    {
                        var result = next.Delegate();

                        next.TaskCompletionSource.SetResult(result);
                    }
                    catch (Exception e)
                    {
                        next.TaskCompletionSource.SetException(e);
                    }
                }

                if (_sharedQueueEnqeued != null && _sharedQueue != null)
                {
                    while (_queue != null && _queue.IsEmpty && _sharedQueue.TryDequeue(out var next))
                    {
                        next(_refToEngine);
                    }
                }
            }
        }

        /// <summary>
        /// Adds a script task to the end of the queue
        /// </summary>
        /// <param name="task">Script task</param>
        private void EnqueueTask(ScriptTask task)
        {
            _queue.Enqueue(task);
            _queueEnqeued.Set();
        }

        /// <summary>
        /// Runs a specified delegate on the thread with modified stack size,
        /// and returns its result as an <see cref="System.Object"/>.
        /// Blocks until the invocation of delegate is completed.
        /// </summary>
        /// <param name="del">Delegate to invocation</param>
        /// <returns>Result of the delegate invocation</returns>
        private Task<object> InnnerInvokeAsync(Func<object> del)
        {
            ScriptTask task = new ScriptTask(del);

            EnqueueTask(task);

            return task.TaskCompletionSource.Task;
        }

        /// <summary>
        /// Runs a specified delegate on the thread with modified stack size,
        /// and returns its result as an <typeparamref name="T" />.
        /// Blocks until the invocation of delegate is completed.
        /// </summary>
        /// <typeparam name="T">The type of the return value of the method,
        /// that specified delegate encapsulates</typeparam>
        /// <param name="func">Delegate to invocation</param>
        /// <returns>Result of the delegate invocation</returns>
        public T Invoke<T>(Func<T> func)
        {
            VerifyNotDisposed();

            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            return (T)InnnerInvokeAsync(() => func()).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Runs a specified delegate on the thread with modified stack size.
        /// Blocks until the invocation of delegate is completed.
        /// </summary>
        /// <param name="action">Delegate to invocation</param>
        public void Invoke(Action action)
        {
            VerifyNotDisposed();

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            InnnerInvokeAsync(() =>
            {
                action();
                return null;
            }).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        #region IDisposable implementation


        private InterlockedStatedFlag _disposedFlag = new InterlockedStatedFlag();

        /// <summary>
        /// Destroys object
        /// </summary>
        public void Dispose()
        {
            if (_disposedFlag.Set())
            {
                InnnerInvokeAsync(
                    () =>
                    {
                        _queue.Clear();
                        _queue = null;

                        return null;
                    }).ConfigureAwait(false).GetAwaiter().GetResult();

                if (_queueEnqeued != null)
                {
                    _queueEnqeued.Dispose();
                    _queueEnqeued = null;
                }
            }
        }

        #endregion

        /// <summary>
        /// Represents a script task, that must be executed on separate thread
        /// </summary>
        public readonly struct ScriptTask
        {
            /// <summary>
            /// Gets a delegate to invocation
            /// </summary>
            public readonly Func<object> Delegate;


            public readonly TaskCompletionSource<object> TaskCompletionSource;

            /// <summary>
            /// Constructs an instance of script task
            /// </summary>
            /// <param name="del">Delegate to invocation</param>
            /// <param name="waitHandle">Event to signal when the invocation of delegate has completed</param>
            public ScriptTask(Func<object> del)
            {
                Delegate = del;
                TaskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }
    }
}