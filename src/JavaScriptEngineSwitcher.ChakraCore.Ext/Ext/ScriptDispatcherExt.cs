using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
namespace JavaScriptEngineSwitcher.ChakraCore
{
    /// <summary>
    /// Provides services for managing the queue of script tasks on the thread with modified stack size
    /// </summary>
    internal sealed class ScriptDispatcher : IDisposable
    {
        /// <summary>
        /// Queue of script tasks
        /// </summary>
        private readonly Queue<ScriptTask> _queue = new Queue<ScriptTask>();

        /// <summary>
        /// Constructs an instance of script dispatcher
        /// </summary>
        /// <param name="maxStackSize">The maximum stack size, in bytes, to be used by the thread,
        /// or 0 to use the default maximum stack size specified in the header for the executable.</param>
        public ScriptDispatcher(int maxStackSize)
        {
            var thread = new Thread(StartThread, maxStackSize)
            {
                Name = "ChakraCore Thread",
                IsBackground = true
            };
            thread.Start();
        }


        [MethodImpl((MethodImplOptions)256 /* AggressiveInlining */)]
        private void VerifyNotDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(ToString());
            }
        }

        private int _availableCount;
        /// <summary>
        /// The number of workers currently actively engaged in work
        /// </summary>
        public int AvailableCount => Thread.VolatileRead(ref _availableCount);

        /// <summary>
        /// Starts a thread with modified stack size.
        /// Loops forever, processing script tasks from the queue.
        /// </summary>
        private void StartThread()
        {
            while (true)
            {
                ScriptTask next;

                lock (_queue)
                {
                    if (_queue.Count == 0)
                    {
                        do
                        {
                            if (_disposed)
                                break;

                            _availableCount++;
                            Monitor.Wait(_queue);
                            _availableCount--;

                        } while (_queue.Count == 0);
                    }

                    if (_queue.Count == 0)
                    {
                        if (_disposed)
                            break;
                        else
                            continue;
                    }

                    next = _queue.Dequeue();
                }

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
        }

        /// <summary>
        /// Adds a script task to the end of the queue
        /// </summary>
        /// <param name="task">Script task</param>
        private void EnqueueTask(ScriptTask task)
        {
            lock (_queue)
            {
                _queue.Enqueue(task);
                if (_availableCount != 0)
                {
                    Monitor.Pulse(_queue); // wake up someone
                }
            }
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

            bool restoreFlow = false;
            try
            {
                if (!ExecutionContext.IsFlowSuppressed())
                {
                    ExecutionContext.SuppressFlow();
                    restoreFlow = true;
                }

                EnqueueTask(task);
            }
            finally
            {
                // Restore the current ExecutionContext
                if (restoreFlow)
                    ExecutionContext.RestoreFlow();
            }



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

        public async Task<T> InvokeAsync<T>(Func<T> func)
        {
            VerifyNotDisposed();

            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            return (T)await InnnerInvokeAsync(() => func());
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


        private volatile bool _disposed;

        /// <summary>
        /// Destroys object
        /// </summary>
        public void Dispose()
        {
            _disposed = true;
            lock (_queue)
            {
                Monitor.PulseAll(_queue);
            }
        }

        #endregion

        #region Internal types

        /// <summary>
        /// Represents a script task, that must be executed on separate thread
        /// </summary>
        private readonly struct ScriptTask
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

        #endregion
    }
}