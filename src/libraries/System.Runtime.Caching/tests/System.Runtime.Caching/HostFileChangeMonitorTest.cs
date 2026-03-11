// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
// Authors:
//      Marek Habersack <mhabersack@novell.com>
//
// Copyright (C) 2010 Novell, Inc. (http://novell.com/)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Runtime.Caching;
using System.Runtime.Caching.Hosting;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MonoTests.Common;
using Xunit;

namespace MonoTests.System.Runtime.Caching
{
    [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "HostFileChangeMonitor is not supported on Browser/iOS/tvOS")]
    public class HostFileChangeMonitorTest
    {
        [Fact]
        public void Constructor_Exceptions()
        {
            string relPath = Path.Combine("relative", "file", "path");
            var paths = new List<string> {
                relPath
            };

            Assert.Throws<ArgumentException>(() =>
            {
                new HostFileChangeMonitor(paths);
            });

            paths.Clear();
            paths.Add(null);
            Assert.Throws<ArgumentException>(() =>
            {
                new HostFileChangeMonitor(paths);
            });

            paths.Clear();
            paths.Add(string.Empty);
            Assert.Throws<ArgumentException>(() =>
            {
                new HostFileChangeMonitor(paths);
            });

            Assert.Throws<ArgumentNullException>(() =>
            {
                new HostFileChangeMonitor(null);
            });

            paths.Clear();
            Assert.Throws<ArgumentException>(() =>
            {
                new HostFileChangeMonitor(paths);
            });
        }

        [Fact]
        public static void Constructor_MissingFiles_Handler()
        {
            HostFileChangeMonitor monitor;
            string missingFile = Path.GetFullPath(Path.Combine(Guid.NewGuid().ToString("N"), "file", "path"));

            var paths = new List<string> {
                missingFile
            };

            // Actually thrown by FileSystemWatcher constructor - note that the exception message suggests the file's
            // parent directory is being watched, not the file itself:
            //
            // MonoTests.System.Runtime.Caching.HostFileChangeMonitorTest.Constructor_MissingFiles:
            // System.ArgumentException : The directory name c:\missing\file is invalid.
            // at System.IO.FileSystemWatcher..ctor(String path, String filter)
            // at System.IO.FileSystemWatcher..ctor(String path)
            // at System.Runtime.Caching.FileChangeNotificationSystem.System.Runtime.Caching.Hosting.IFileChangeNotificationSystem.StartMonitoring(String filePath, OnChangedCallback onChangedCallback, Object& state, DateTimeOffset& lastWriteTime, Int64& fileSize)
            // at System.Runtime.Caching.HostFileChangeMonitor.InitDisposableMembers()
            // at System.Runtime.Caching.HostFileChangeMonitor..ctor(IList`1 filePaths)
            // at MonoTests.System.Runtime.Caching.HostFileChangeMonitorTest.Constructor_MissingFiles() in c:\users\grendel\documents\visual studio 2010\Projects\System.Runtime.Caching.Test\System.Runtime.Caching.Test\System.Runtime.Caching\HostFileChangeMonitorTest.cs:line 68
            Assert.Throws<ArgumentException>(() =>
            {
                new HostFileChangeMonitor(paths);
            });

            missingFile = Path.GetFullPath(Guid.NewGuid().ToString("N"));

            paths.Clear();
            paths.Add(missingFile);
            monitor = new HostFileChangeMonitor(paths);
            Assert.Equal(1, monitor.FilePaths.Count);
            Assert.Equal(missingFile, monitor.FilePaths[0]);
            //??
            Assert.Equal(missingFile + "701CE1722770000FFFFFFFFFFFFFFFF", monitor.UniqueId);
            monitor.Dispose();

            paths.Add(missingFile);
            monitor = new HostFileChangeMonitor(paths);
            Assert.Equal(2, monitor.FilePaths.Count);
            Assert.Equal(missingFile, monitor.FilePaths[0]);
            Assert.Equal(missingFile, monitor.FilePaths[1]);
            //??
            Assert.Equal(missingFile + "701CE1722770000FFFFFFFFFFFFFFFF", monitor.UniqueId);
            monitor.Dispose();
        }

        [Fact]
        public void Constructor_Duplicates()
        {
            HostFileChangeMonitor monitor;
            string missingFile = Path.GetFullPath(Guid.NewGuid().ToString("N"));

            var paths = new List<string> {
                missingFile,
                missingFile
            };

            // Just checks if it doesn't throw any exception for dupes
            monitor = new HostFileChangeMonitor(paths);
            monitor.Dispose();
        }

        private static Tuple<string, string, string, IList<string>> SetupMonitoring(string uniqueId)
        {
            string testPath = Path.Combine(Path.GetTempPath(), "HostFileChangeMonitorTest", uniqueId);
            if (!Directory.Exists(testPath))
                Directory.CreateDirectory(testPath);

            string firstFile = Path.Combine(testPath, "FirstFile.txt");
            string secondFile = Path.Combine(testPath, "SecondFile.txt");

            File.WriteAllText(firstFile, "I am the first file.");
            File.WriteAllText(secondFile, "I am the second file.");

            var paths = new List<string> {
                firstFile,
                secondFile
            };

            return new Tuple<string, string, string, IList<string>>(testPath, firstFile, secondFile, paths);
        }

        private static void CleanupMonitoring(Tuple<string, string, string, IList<string>> setup)
        {
            string testPath = setup != null ? setup.Item1 : null;
            if (string.IsNullOrEmpty(testPath) || !Directory.Exists(testPath))
                return;

            foreach (string f in Directory.EnumerateFiles(testPath))
            {
                try
                {
                    File.Delete(f);
                }
                catch
                {
                    // ignore
                }
            }

            try
            {
                // 2 nested folders were created by SetupMonitoring, so we'll delete both
                var dirInfo = new DirectoryInfo(testPath);
                var parentDirInfo = dirInfo.Parent;
                dirInfo.Delete(recursive: true);
                parentDirInfo.Delete(recursive: true);
            }
            catch
            {
                // ignore
            }
        }

        [Fact]
        public void UniqueId()
        {
            Tuple<string, string, string, IList<string>> setup = null;
            try
            {
                setup = SetupMonitoring(nameof(UniqueId));
                FileInfo fi;
                var monitor = new HostFileChangeMonitor(setup.Item4);
                var sb = new StringBuilder();

                fi = new FileInfo(setup.Item2);
                sb.AppendFormat("{0}{1:X}{2:X}",
                    setup.Item2,
                    fi.LastWriteTimeUtc.Ticks,
                    fi.Length);

                fi = new FileInfo(setup.Item3);
                sb.AppendFormat("{0}{1:X}{2:X}",
                    setup.Item3,
                    fi.LastWriteTimeUtc.Ticks,
                    fi.Length);

                Assert.Equal(sb.ToString(), monitor.UniqueId);

                var list = new List<string>(setup.Item4);
                list.Add(setup.Item1);

                monitor = new HostFileChangeMonitor(list);
                var di = new DirectoryInfo(setup.Item1);
                sb.AppendFormat("{0}{1:X}{2:X}",
                    setup.Item1,
                    di.LastWriteTimeUtc.Ticks,
                    -1L);
                Assert.Equal(sb.ToString(), monitor.UniqueId);

                list.Add(setup.Item1);
                monitor = new HostFileChangeMonitor(list);
                Assert.Equal(sb.ToString(), monitor.UniqueId);
                monitor.Dispose();
            }
            finally
            {
                CleanupMonitoring(setup);
            }
        }

        [OuterLoop]
        [Fact]
        public async Task Reasonable_Delay()
        {
            Tuple<string, string, string, IList<string>> setup = null;
            try
            {
                setup = SetupMonitoring(nameof(Reasonable_Delay));

                using var monitor = new HostFileChangeMonitor(setup.Item4);
                var policy = new CacheItemPolicy
                {
                    ChangeMonitors = { monitor }
                };
                var config = new NameValueCollection();
                var mc = new PokerMemoryCache("MyCache", config);

                mc.Set("key", "value", policy);

                // Verify the cache item is set
                Assert.Equal("value", mc["key"]);

                // Update the file dependency
                File.WriteAllText(setup.Item2, "I am the first file. Updated.");

                // Wait for the monitor to detect the change - 5s should be more than enough
                var stop = DateTime.UtcNow.AddMilliseconds(5000);
                while (DateTime.UtcNow < stop)
                {
                    if (!mc.Contains("key"))
                        break;
                    await Task.Delay(50);
                }

                Assert.Null(mc["key"]);
            }
            finally
            {
                CleanupMonitoring(setup);
            }
        }
    }
}
