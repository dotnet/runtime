// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading;
using Xunit;

namespace System.IO.Tests
{
    public class MoveFileTests : FileSystemWatcherTest
    {
        #region WindowsTests

        [Theory]
        [InlineData(WatcherChangeTypes.Changed, false)]
        [InlineData(WatcherChangeTypes.Created, false)]
        [InlineData(WatcherChangeTypes.Deleted, false)]
        [InlineData(WatcherChangeTypes.Renamed, true)]
        [PlatformSpecific(PlatformID.Windows)]
        public void Windows_File_Move_To_Same_Directory_Triggers_Event(WatcherChangeTypes eventType, bool moveRaisesEvent)
        {
            MoveAndCheck_SameDirectory(eventType, moveRaisesEvent);
        }

        [Theory]
        [OuterLoop]
        [InlineData(WatcherChangeTypes.Changed, false)]
        [InlineData(WatcherChangeTypes.Created, false)]
        [InlineData(WatcherChangeTypes.Deleted, false)]
        [InlineData(WatcherChangeTypes.Renamed, true)]
        [PlatformSpecific(PlatformID.Windows)]
        public void Windows_File_Move_To_Different_Directory_Triggers_Event(WatcherChangeTypes eventType, bool moveRaisesEvent)
        {
            MoveAndCheck_DifferentDirectory(eventType, moveRaisesEvent);
        }

        [Theory]
        [InlineData(WatcherChangeTypes.Changed, true)]
        [InlineData(WatcherChangeTypes.Created, true)]
        [InlineData(WatcherChangeTypes.Deleted, false)]
        [InlineData(WatcherChangeTypes.Renamed, true)]
        [PlatformSpecific(PlatformID.Windows)]
        public void Windows_File_Move_In_Nested_Directory_Triggers_Event(WatcherChangeTypes eventType, bool moveRaisesEvent)
        {
            MoveAndCheck_NestedDirectory(eventType, moveRaisesEvent);
        }

        [Theory]
        [InlineData(WatcherChangeTypes.Changed, false)]
        [InlineData(WatcherChangeTypes.Created, false)]
        [InlineData(WatcherChangeTypes.Deleted, false)]
        [InlineData(WatcherChangeTypes.Renamed, true)]
        [PlatformSpecific(PlatformID.Windows)]
        public void Windows_File_Move_With_Set_NotifyFilter_Triggers_Event(WatcherChangeTypes eventType, bool moveRaisesEvent)
        {
            MoveAndCheck_WithNotifyFilter(eventType, moveRaisesEvent);
        }

        #endregion

        #region UnixTests

        [Theory]
        [InlineData(WatcherChangeTypes.Changed, false)]
        [InlineData(WatcherChangeTypes.Created, true)]
        [InlineData(WatcherChangeTypes.Deleted, true)]
        [InlineData(WatcherChangeTypes.Renamed, false)]
        [PlatformSpecific(PlatformID.AnyUnix)]
        public void Unix_File_Move_To_Same_Directory_Triggers_Event(WatcherChangeTypes eventType, bool moveRaisesEvent)
        {
            MoveAndCheck_SameDirectory(eventType, moveRaisesEvent);
        }

        [Theory]
        [OuterLoop]
        [InlineData(WatcherChangeTypes.Changed, false)]
        [InlineData(WatcherChangeTypes.Created, true)]
        [InlineData(WatcherChangeTypes.Deleted, true)]
        [InlineData(WatcherChangeTypes.Renamed, false)]
        [PlatformSpecific(PlatformID.AnyUnix)]
        public void Unix_File_Move_To_Different_Directory_Triggers_Event(WatcherChangeTypes eventType, bool moveRaisesEvent)
        {
            MoveAndCheck_DifferentDirectory(eventType, moveRaisesEvent);

        }

        [Theory, OuterLoop]
        [InlineData(WatcherChangeTypes.Changed, false)]
        [InlineData(WatcherChangeTypes.Created, true)]
        [InlineData(WatcherChangeTypes.Deleted, true)]
        [InlineData(WatcherChangeTypes.Renamed, false)]
        [PlatformSpecific(PlatformID.AnyUnix)]
        [ActiveIssue(3215, PlatformID.OSX)] // failing for Changed, false
        public void Unix_File_Move_In_Nested_Directory_Triggers_Event(WatcherChangeTypes eventType, bool moveRaisesEvent)
        {
            MoveAndCheck_NestedDirectory(eventType, moveRaisesEvent);
        }

        [Theory]
        [InlineData(WatcherChangeTypes.Changed, false)]
        [InlineData(WatcherChangeTypes.Created, false)]
        [InlineData(WatcherChangeTypes.Deleted, true)]
        [InlineData(WatcherChangeTypes.Renamed, false)]
        [PlatformSpecific(PlatformID.AnyUnix)]
        public void Unix_File_Move_With_Set_NotifyFilter_Triggers_Event(WatcherChangeTypes eventType, bool moveRaisesEvent)
        {
            MoveAndCheck_WithNotifyFilter(eventType, moveRaisesEvent);
        }

        #endregion

        #region TestHelpers

        /// <summary>
        /// Sets up watchers for the type given before performing a File.Move operation and checking for
        /// events. If moveRaisesEvent is true, we make sure that the given event type is observed. If false,
        /// we ensure that it is not observed.
        /// 
        /// This test will move the source file to a destination file in the same directory i.e. rename it
        /// </summary>
        private void MoveAndCheck_SameDirectory(WatcherChangeTypes eventType, bool moveRaisesEvent)
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, GetTestFileName())))
            using (var watcher = new FileSystemWatcher(testDirectory.Path))
            {
                // put everything in our own directory to avoid collisions
                watcher.Path = Path.GetFullPath(dir.Path);
                watcher.Filter = "*.*";

                // create a file
                using (var testFile = new TempFile(Path.Combine(dir.Path, "file")))
                {
                    watcher.EnableRaisingEvents = true;
                    AutoResetEvent eventOccurred = WatchForEvents(watcher, eventType);

                    // Move the testFile to a different name in the same directory
                    File.Move(testFile.Path, testFile.Path + "_" + eventType.ToString());

                    // Test that the event is observed or not observed
                    if (moveRaisesEvent)
                        ExpectEvent(eventOccurred, eventType.ToString());
                    else
                        ExpectNoEvent(eventOccurred, eventType.ToString());
                }
            }
        }

        /// <summary>
        /// Sets up watchers for the type given before performing a File.Move operation and checking for
        /// events. If moveRaisesEvent is true, we make sure that the given event type is observed. If false,
        /// we ensure that it is not observed.
        /// 
        /// This test checks for when the file being moved has a destination directory that is outside of
        /// the path of the FileSystemWatcher.
        /// </summary>
        private void MoveAndCheck_DifferentDirectory(WatcherChangeTypes eventType, bool moveRaisesEvent)
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var dir = new TempDirectory(Path.Combine(testDirectory.Path, GetTestFileName())))
            using (var dir_unwatched = new TempDirectory(Path.Combine(testDirectory.Path, GetTestFileName())))
            using (var watcher = new FileSystemWatcher(testDirectory.Path))
            {
                // put everything in our own directory to avoid collisions
                watcher.Path = Path.GetFullPath(dir.Path);
                watcher.Filter = "*.*";

                // create a file
                using (var testFile = new TempFile(Path.Combine(dir.Path, "file")))
                {
                    watcher.EnableRaisingEvents = true;
                    AutoResetEvent eventOccurred = WatchForEvents(watcher, eventType);

                    // Move the testFile to a different name in the same directory
                    File.Move(testFile.Path, Path.Combine(dir_unwatched.Path, Path.GetFileName(testFile.Path) + "_" + eventType.ToString()));

                    // Test which events are thrown
                    if (moveRaisesEvent)
                        ExpectEvent(eventOccurred, eventType.ToString());
                    else
                        ExpectNoEvent(eventOccurred, eventType.ToString());
                }
            }
        }

        /// <summary>
        /// Sets up watchers for the type given before performing a File.Move operation and checking for
        /// events. If moveRaisesEvent is true, we make sure that the given event type is observed. If false,
        /// we ensure that it is not observed.
        /// 
        /// This test will move the source file of a file within a nested directory
        /// </summary>
        private void MoveAndCheck_NestedDirectory(WatcherChangeTypes eventType, bool moveRaisesEvent)
        {
            TestNestedDirectoriesHelper(GetTestFilePath(), eventType, (AutoResetEvent eventOccurred, TempDirectory ttd) =>
            {
                using (var nestedFile = new TempFile(Path.Combine(ttd.Path, "nestedFile" + eventType.ToString())))
                {
                    File.Move(nestedFile.Path, nestedFile.Path + "_2");
                    if (moveRaisesEvent)
                        ExpectEvent(eventOccurred, eventType.ToString());
                    else
                        ExpectNoEvent(eventOccurred, eventType.ToString());
                }
            });
        }

        /// <summary>
        /// Sets up watchers for the type given before performing a File.Move operation and checking for
        /// events. If moveRaisesEvent is true, we make sure that the given event type is observed. If false,
        /// we ensure that it is not observed.
        /// 
        /// This test will use the NotifyFilter attribute of the FileSystemWatcher before the move and subsequent
        /// checks are made.
        /// </summary>
        private void MoveAndCheck_WithNotifyFilter(WatcherChangeTypes eventType, bool moveRaisesEvent)
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var file = new TempFile(Path.Combine(testDirectory.Path, GetTestFileName())))
            using (var watcher = new FileSystemWatcher(testDirectory.Path))
            {
                watcher.NotifyFilter = NotifyFilters.FileName;
                watcher.Filter = Path.GetFileName(file.Path);
                AutoResetEvent eventOccurred = WatchForEvents(watcher, eventType);

                string newName = Path.Combine(testDirectory.Path, GetTestFileName());
                watcher.EnableRaisingEvents = true;

                File.Move(file.Path, newName);

                if (moveRaisesEvent)
                    ExpectEvent(eventOccurred, eventType.ToString());
                else
                    ExpectNoEvent(eventOccurred, eventType.ToString());
            }
        }

        #endregion
    }
}