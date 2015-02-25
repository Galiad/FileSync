using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Permissions;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace QXS.FileSync
{
    /// <summary>
    /// String Extensions
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Compares the string against a given pattern.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="wildcard">The wildcard, where "*" means any sequence of characters, and "?" means any single character.</param>
        /// <returns><c>true</c> if the string matches the given pattern; otherwise <c>false</c>.</returns>
        public static bool GlobMatch(this string str, string wildcard)
        {
            return new Regex(
                "^" + Regex.Escape(wildcard).Replace(@"\*", ".*").Replace(@"\?", ".") + "$",
                RegexOptions.IgnoreCase | RegexOptions.Singleline
            ).IsMatch(str);
        }
    }


    /// <summary>
    /// Syncs files from one folder to another folder using the <c>FileSystemWatcher</c>
    /// </summary>
    public class FileSync
    {
        /// <summary>
        /// Path to listen for changes
        /// </summary>
        protected string fromPath = "";
        /// <summary>
        /// Path, where to apply the changes
        /// </summary>
        protected string toPath = "";

        /// <summary>
        /// Watcher for the <c>fromPath</c>
        /// </summary>
        protected FileSystemWatcher fromWatcher = null;
        /// <summary>
        /// Watcher for the <c>toPath</c>, just set when using 2 way sync
        /// </summary>
        protected FileSystemWatcher toWatcher = null;

        /// <summary>
        /// Delegate for renamed events
        /// </summary>
        /// <param name="sender">Object, where the event occured</param>
        /// <param name="oldFileName">the old filename</param>
        /// <param name="newFileName">the new filename</param>
        public delegate void FileRenamedHandler(object sender, string oldFileName, string newFileName);
        /// <summary>
        /// Delegate for file creations, changes or deletions
        /// </summary>
        /// <param name="sender">Object, where the event occured</param>
        /// <param name="fileName">the filename</param>
        public delegate void FileChangeHandler(object sender, string fileName);

        /// <summary>
        /// Destination file was renamed
        /// </summary>
        public event FileRenamedHandler Renamed;
        /// <summary>
        /// Destination file was created
        /// </summary>
        public event FileChangeHandler Created;
        /// <summary>
        /// Destination file was deleted
        /// </summary>
        public event FileChangeHandler Deleted;
        /// <summary>
        /// Destination file was changed
        /// </summary>
        public event FileChangeHandler Changed;

        /// <summary>
        /// Is the <c>FileSync</c> <c>Running</c>?
        /// </summary>
        public bool Running
        {
            get
            {
                return fromWatcher != null;
            }
            private set { }
        }

        /// <summary>
        /// Is the <c>FileSync</c> <c>Running</c> in 2 way mode?
        /// </summary>
        public bool IsTwoWaySyncRunning
        {
            get
            {
                return fromWatcher != null && toWatcher != null;
            }
            private set { }
        }

        /// <summary>
        /// Glob filters, that should be ignored
        /// </summary>
        /// <example> 
        /// This sample shows how to use the <see cref="IgnoreGlobFilters"/> property.
        /// <code>
        /// FileSync sync = new FileSync(@"c:\testdir1\", @"c:\testdir2\");
        /// sync.IgnoreGlobFilters = new string[] {
        ///    "*/tmp/*", // ignore all tmp directories
        ///    "*.exe",   // ignore all exe files
        /// };
        /// sync.Start();
        /// </code>
        /// </example>
        public string[] IgnoreGlobFilters = new string[] { };

        /// <summary>
        /// The Constructor
        /// </summary>
        /// <param name="fromPath">Source path, where to listen for changes</param>
        /// <param name="toPath">Destination path, where to apply changes</param>
        public FileSync(string fromPath, string toPath)
        {
            this.fromPath = fromPath;
            this.toPath = toPath;
            if (this.fromPath.Last() != '\\')
            {
                this.fromPath += "\\";
            }
            if (this.toPath.Last() != '\\')
            {
                this.toPath += "\\";
            }
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~FileSync()
        {
            if (Running)
            {
                Stop();
            }

        }

        /// <summary>
        /// Attaches a logger to the FileSsync
        /// </summary>
        /// <param name="logger">the logger</param>
        public void AttachLogger(ILogger logger)
        {
            this.Deleted += logger.OnSyncDeleted;
            this.Changed += logger.OnSyncChanged;
            this.Created += logger.OnSyncCreated;
            this.Renamed += logger.OnSyncRenamed;
        }

        /// <summary>
        /// Start the file synchronization
        /// </summary>
        /// <param name="twoWaySync">when <c>true</c>, apply changes in the destination path back to the source path. when <c>false</c>, just apply any source changes to the destination</param>
        public void Start(bool twoWaySync = false)
        {
            if (Running)
            {
                throw new FileSyncException("Filesync is already running.");
            }
            fromWatcher = new FileSystemWatcher(fromPath);
            fromWatcher.InternalBufferSize = 65536;
            fromWatcher.IncludeSubdirectories = true;
            fromWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.CreationTime;
            // Add event handlers.
            fromWatcher.Changed += new FileSystemEventHandler(OnChanged);
            fromWatcher.Created += new FileSystemEventHandler(OnChanged);
            fromWatcher.Deleted += new FileSystemEventHandler(OnChanged);
            fromWatcher.Renamed += new RenamedEventHandler(OnRenamed);

            // start watching
            fromWatcher.EnableRaisingEvents = true;

            if (twoWaySync)
            {
                toWatcher = new FileSystemWatcher(toPath);
                toWatcher.InternalBufferSize = 65536;
                toWatcher.IncludeSubdirectories = true;
                toWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.CreationTime;
                // Add event handlers.
                toWatcher.Changed += new FileSystemEventHandler(OnChanged);
                toWatcher.Created += new FileSystemEventHandler(OnChanged);
                toWatcher.Deleted += new FileSystemEventHandler(OnChanged);
                toWatcher.Renamed += new RenamedEventHandler(OnRenamed);

                // start watching
                toWatcher.EnableRaisingEvents = true;
            }
        }

        /// <summary>
        /// Stop the file synchronization
        /// </summary>
        public void Stop()
        {
            if (!Running)
            {
                throw new FileSyncException("Filesync is not running.");
            }
            if (fromWatcher != null)
            {
                fromWatcher.EnableRaisingEvents = false;
                fromWatcher = null;
            }
            if (toWatcher != null)
            {
                toWatcher.EnableRaisingEvents = false;
                toWatcher = null;
            }
        }

        /// <summary>
        /// Trys to set normal file attributes
        /// </summary>
        /// <param name="fullNewPath">The path to the file</param>
        protected void tryToSetNormalPermissions(string fullNewPath)
        {
            try
            {
                File.SetAttributes(fullNewPath, FileAttributes.Normal);
            }
            catch { }
        }

        /// <summary>
        /// Method for the the <c>FileSystemEventHandler</c>
        /// </summary>
        /// <param name="source">Object where the event occured</param>
        /// <param name="e">Event data</param>
        protected void OnChanged(object source, FileSystemEventArgs e)
        {
            // any excludes?
            foreach (string filter in IgnoreGlobFilters)
            {
                if (e.FullPath.GlobMatch(filter))
                {
                    return;
                }
            }

            string fullNewPath = "";
            if (e.FullPath.StartsWith(fromPath))
            {
                fullNewPath = toPath + e.FullPath.Substring(fromPath.Length);
            }
            else if (e.FullPath.StartsWith(toPath))
            {
                fullNewPath = fromPath + e.FullPath.Substring(toPath.Length);
            }
            else
            {
                return;
            }


            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Created:
                    try
                    {
                        if ((File.GetAttributes(e.FullPath) & FileAttributes.Directory) == FileAttributes.Directory)
                        {
                            if (!Directory.Exists(fullNewPath))
                            {
                                Directory.CreateDirectory(fullNewPath);
                                Created(this, fullNewPath);
                            }

                        }
                        else
                        {
                            try
                            {
                                FileInfo fromFile = new FileInfo(e.FullPath);
                                FileInfo toFile = new FileInfo(fullNewPath);
                                if (fromFile.LastWriteTime >= toFile.LastWriteTime.AddSeconds(2) && fromFile.Length != toFile.Length)
                                {
                                    tryToSetNormalPermissions(fullNewPath);
                                    File.Copy(e.FullPath, fullNewPath, true);
                                    Changed(this, fullNewPath);
                                }

                            }
                            catch
                            {
                                File.Copy(e.FullPath, fullNewPath, true);
                                tryToSetNormalPermissions(fullNewPath);
                                Created(this, fullNewPath);
                            }
                        }
                    }
                    catch { }
                    break;
                case WatcherChangeTypes.Changed:
                    try
                    {
                        if ((File.GetAttributes(e.FullPath) & FileAttributes.Directory) != FileAttributes.Directory)
                        {
                            try
                            {
                                FileInfo fromFile = new FileInfo(e.FullPath);
                                FileInfo toFile = new FileInfo(fullNewPath);
                                if (fromFile.LastWriteTime >= toFile.LastWriteTime.AddSeconds(2) && fromFile.Length != toFile.Length)
                                {
                                    tryToSetNormalPermissions(fullNewPath);
                                    File.Copy(e.FullPath, fullNewPath, true);
                                    Changed(this, fullNewPath);
                                }
                            }
                            catch
                            {
                                tryToSetNormalPermissions(fullNewPath);
                                File.Copy(e.FullPath, fullNewPath, true);
                                Created(this, fullNewPath);
                            }
                        }
                    }
                    catch { }
                    break;
                case WatcherChangeTypes.Deleted:
                    try
                    {
                        if ((File.GetAttributes(fullNewPath) & FileAttributes.Directory) == FileAttributes.Directory)
                        {
                            if (Directory.Exists(fullNewPath))
                            {
                                Directory.Delete(fullNewPath);
                                Deleted(this, fullNewPath);
                            }

                        }
                        else
                        {
                            if (File.Exists(fullNewPath))
                            {
                                tryToSetNormalPermissions(fullNewPath);
                                File.Delete(fullNewPath);
                                Deleted(this, fullNewPath);
                            }
                        }

                    }
                    catch { }
                    break;

            }

            //Console.WriteLine("File: " + e.FullPath + " " + e.ChangeType);
            //Console.WriteLine("  ==> " + fullNewPath);
        }

        /// <summary>
        /// Method for the the <c>RenamedEventHandler</c>
        /// </summary>
        /// <param name="source">Object where the event occured</param>
        /// <param name="e">Event data</param>
        protected void OnRenamed(object source, RenamedEventArgs e)
        {
            // any excludes?
            foreach (string filter in IgnoreGlobFilters)
            {
                if (e.OldFullPath.GlobMatch(filter) || e.FullPath.GlobMatch(filter))
                {
                    return;
                }
            }

            string oldFullPath = "";
            string newFullPath = "";
            if (e.OldFullPath.StartsWith(fromPath))
            {
                oldFullPath = toPath + e.OldFullPath.Substring(fromPath.Length);
            }
            else if (e.OldFullPath.StartsWith(toPath))
            {
                oldFullPath = fromPath + e.OldFullPath.Substring(toPath.Length);
            }
            else
            {
                return;
            }

            if (e.FullPath.StartsWith(fromPath))
            {
                newFullPath = toPath + e.FullPath.Substring(fromPath.Length);
            }
            else if (e.FullPath.StartsWith(toPath))
            {
                newFullPath = fromPath + e.FullPath.Substring(toPath.Length);
            }
            else
            {
                return;
            }

            try
            {
                if ((File.GetAttributes(oldFullPath) & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    Directory.Move(oldFullPath, newFullPath);
                    Renamed(this, oldFullPath, newFullPath);
                }
                else
                {
                    if (File.Exists(oldFullPath))
                    {
                        File.Move(oldFullPath, newFullPath);
                        Renamed(this, oldFullPath, newFullPath);
                    }
                    else
                    {
                        File.Copy(e.FullPath, newFullPath, true);
                        tryToSetNormalPermissions(newFullPath);
                        Created(this, newFullPath);

                    }
                }
            }
            catch { }

            //Console.WriteLine("File: {0} renamed to {1}", e.OldFullPath, e.FullPath);
            //Console.WriteLine("  ==> {0} renamed to {1}", oldFullPath, newFullPath);
        }
    }
}
