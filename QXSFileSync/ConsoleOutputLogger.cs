using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QXS.FileSync
{
    /// <summary>
    /// Console Output Logger
    /// </summary>
    public class ConsoleOutputLogger : ILogger
    {
        /// <summary>
        /// Triggers on <c>FileSync.Changed</c> events
        /// </summary>
        /// <param name="source">The sender</param>
        /// <param name="path">Path to the destination file, that was changed</param>
        public void OnSyncChanged(object source, string path)
        {
            Console.WriteLine("Changed " + path);
        }

        /// <summary>
        /// Triggers on <c>FileSync.Created</c> events
        /// </summary>
        /// <param name="source">The sender</param>
        /// <param name="path">Path to the destination file, that was changed</param>
        public void OnSyncCreated(object source, string path)
        {
            Console.WriteLine("Created " + path);
        }
        /// <summary>
        /// Triggers on <c>FileSync.Deleted</c> events
        /// </summary>
        /// <param name="source">The sender</param>
        /// <param name="path">Path to the destination file, that was changed</param>
        public void OnSyncDeleted(object source, string path)
        {
            Console.WriteLine("Deleted " + path);
        }
        /// <summary>
        /// Triggers on <c>FileSync.Renamed</c> events
        /// </summary>
        /// <param name="source">The sender</param>
        /// <param name="oldpath">Old Path to the destination file, that was changed</param>
        /// <param name="newpath">New Path to the destination file, that was changed</param>
        public void OnSyncRenamed(object source, string oldpath, string newpath)
        {
            Console.WriteLine("Renamed " + oldpath + " to " + newpath);
        }

    }
}
