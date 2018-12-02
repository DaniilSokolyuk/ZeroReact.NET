using System.Collections.Generic;

namespace ZeroReact
{
    public interface IFileSystem
    {
        /// <summary>
        /// Converts a path from a full filesystem path to anan application relative path (~/...)
        /// </summary>
        /// <param name="absolutePath">Full path of the file</param>
        /// <returns>App-relative path of the file</returns>
        string ToRelativePath(string absolutePath);

        /// <summary>
        /// Reads the contents of a file as a string.
        /// </summary>
        /// <param name="relativePath">App-relative path of the file</param>
        /// <returns>Contents of the file</returns>
        string ReadAsString(string relativePath);

        /// <summary>
        /// Writes a string to a file
        /// </summary>
        /// <param name="relativePath">App-relative path of the file</param>
        /// <param name="contents">Contents of the file</param>
        void WriteAsString(string relativePath, string contents);

        /// <summary>
        /// Determines if the specified file exists
        /// </summary>
        /// <param name="relativePath">App-relative path of the file</param>
        /// <returns><c>true</c> if the file exists</returns>
        bool FileExists(string relativePath);

        /// <summary>
        /// Gets all the file paths that match the specified pattern
        /// </summary>
        /// <param name="glob">Pattern to search for (eg. "~/Scripts/*.js")</param>
        /// <returns>File paths that match the pattern</returns>
        IEnumerable<string> Glob(string glob);

        /// <summary>
        /// Converts a path from an application relative path (~/...) to a full filesystem path
        /// </summary>
        /// <param name="relativePath">App-relative path of the file</param>
        /// <returns>Full path of the file</returns>
        string MapPath(string relativePath);
    }

}
