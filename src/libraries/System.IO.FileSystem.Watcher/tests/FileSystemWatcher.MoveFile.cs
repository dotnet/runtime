// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;
using Xunit;

public class MoveFileTests
{
    #region WindowsTests

    [Theory]
    [InlineData(WatcherChangeTypes.Changed, false)]
    [InlineData(WatcherChangeTypes.Created, false)]
    [InlineData(WatcherChangeTypes.Deleted, false)]
    [InlineData(WatcherChangeTypes.Renamed, true)]
    [PlatformSpecific(PlatformID.Windows)]
    public static void Windows_File_Move_To_Same_Directory_Triggers_Event(WatcherChangeTypes eventType, bool moveRaisesEvent)
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
    public static void Windows_File_Move_To_Different_Directory_Triggers_Event(WatcherChangeTypes eventType, bool moveRaisesEvent)
    {
        MoveAndCheck_DifferentDirectory(eventType, moveRaisesEvent);
    }

    [Theory]
    [InlineData(WatcherChangeTypes.Changed, true)]
    [InlineData(WatcherChangeTypes.Created, true)]
    [InlineData(WatcherChangeTypes.Deleted, false)]
    [InlineData(WatcherChangeTypes.Renamed, true)]
    [PlatformSpecific(PlatformID.Windows)]
    public static void Windows_File_Move_In_Nested_Directory_Triggers_Event(WatcherChangeTypes eventType, bool moveRaisesEvent)
    {
        MoveAndCheck_NestedDirectory(eventType, moveRaisesEvent);
    }

    [Theory]
    [InlineData(WatcherChangeTypes.Changed, false)]
    [InlineData(WatcherChangeTypes.Created, false)]
    [InlineData(WatcherChangeTypes.Deleted, false)]
    [InlineData(WatcherChangeTypes.Renamed, true)]
    [PlatformSpecific(PlatformID.Windows)]
    public static void Windows_File_Move_With_Set_NotifyFilter_Triggers_Event(WatcherChangeTypes eventType, bool moveRaisesEvent)
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
    public static void Unix_File_Move_To_Same_Directory_Triggers_Event(WatcherChangeTypes eventType, bool moveRaisesEvent)
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
    public static void Unix_File_Move_To_Different_Directory_Triggers_Event(WatcherChangeTypes eventType, bool moveRaisesEvent)
    {
        MoveAndCheck_DifferentDirectory(eventType, moveRaisesEvent);

    }

    [Theory]
    [InlineData(WatcherChangeTypes.Changed, false)]
    [InlineData(WatcherChangeTypes.Created, true)]
    [InlineData(WatcherChangeTypes.Deleted, true)]
    [InlineData(WatcherChangeTypes.Renamed, false)]
    [PlatformSpecific(PlatformID.AnyUnix)]
    public static void Unix_File_Move_In_Nested_Directory_Triggers_Event(WatcherChangeTypes eventType, bool moveRaisesEvent)
    {
        MoveAndCheck_NestedDirectory(eventType, moveRaisesEvent);
    }

    [Theory]
    [InlineData(WatcherChangeTypes.Changed, false)]
    [InlineData(WatcherChangeTypes.Created, false)]
    [InlineData(WatcherChangeTypes.Deleted, true)]
    [InlineData(WatcherChangeTypes.Renamed, false)]
    [PlatformSpecific(PlatformID.AnyUnix)]
    public static void Unix_File_Move_With_Set_NotifyFilter_Triggers_Event(WatcherChangeTypes eventType, bool moveRaisesEvent)
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
    private static void MoveAndCheck_SameDirectory(WatcherChangeTypes eventType, bool moveRaisesEvent)
    {
        using (var dir = Utility.CreateTestDirectory(Guid.NewGuid().ToString()))
        using (var watcher = new FileSystemWatcher())
        {
            // put everything in our own directory to avoid collisions
            watcher.Path = Path.GetFullPath(dir.Path);
            watcher.Filter = "*.*";

            // create a file
            using (var testFile = new TemporaryTestFile(Path.Combine(dir.Path, "file")))
            {
                watcher.EnableRaisingEvents = true;
                AutoResetEvent eventOccured = Utility.WatchForEvents(watcher, eventType);

                // Move the testFile to a different name in the same directory
                testFile.Move(testFile.Path + "_" + eventType.ToString());

                // Test that the event is observed or not observed
                if (moveRaisesEvent)
                    Utility.ExpectEvent(eventOccured, eventType.ToString());
                else
                    Utility.ExpectNoEvent(eventOccured, eventType.ToString());
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
    private static void MoveAndCheck_DifferentDirectory(WatcherChangeTypes eventType, bool moveRaisesEvent)
    {
        using (var dir = Utility.CreateTestDirectory(Guid.NewGuid().ToString()))
        using (var dir_unwatched = new TemporaryTestDirectory(Path.GetRandomFileName()))
        using (var watcher = new FileSystemWatcher())
        {
            // put everything in our own directory to avoid collisions
            watcher.Path = Path.GetFullPath(dir.Path);
            watcher.Filter = "*.*";

            // create a file
            using (var testFile = new TemporaryTestFile(Path.Combine(dir.Path, "file")))
            {
                watcher.EnableRaisingEvents = true;
                AutoResetEvent eventOccured = Utility.WatchForEvents(watcher, eventType);

                // Move the testFile to a different name in the same directory
                testFile.Move(Path.Combine(dir_unwatched.Path, testFile.Name + "_" + eventType.ToString()));

                // Test which events are thrown
                if (moveRaisesEvent)
                    Utility.ExpectEvent(eventOccured, eventType.ToString());
                else
                    Utility.ExpectNoEvent(eventOccured, eventType.ToString());
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
    private static void MoveAndCheck_NestedDirectory(WatcherChangeTypes eventType, bool moveRaisesEvent)
    {
        Utility.TestNestedDirectoriesHelper(eventType, (AutoResetEvent eventOccured, TemporaryTestDirectory ttd) =>
        {
            using (var nestedFile = new TemporaryTestFile(Path.Combine(ttd.Path, "nestedFile" + eventType.ToString())))
            {
                nestedFile.Move(nestedFile.Path + "_2");
                if (moveRaisesEvent)
                    Utility.ExpectEvent(eventOccured, eventType.ToString());
                else
                    Utility.ExpectNoEvent(eventOccured, eventType.ToString());
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
    private static void MoveAndCheck_WithNotifyFilter(WatcherChangeTypes eventType, bool moveRaisesEvent)
    {
        using (var file = Utility.CreateTestFile(Guid.NewGuid().ToString()))
        using (var watcher = new FileSystemWatcher("."))
        {
            watcher.NotifyFilter = NotifyFilters.FileName;
            watcher.Filter = Path.GetFileName(file.Path);
            AutoResetEvent eventOccured = Utility.WatchForEvents(watcher, eventType);

            string newName = file.Path + "_" + eventType.ToString();
            Utility.EnsureDelete(newName);

            watcher.EnableRaisingEvents = true;

            file.Move(newName);

            if (moveRaisesEvent)
                Utility.ExpectEvent(eventOccured, eventType.ToString());
            else
                Utility.ExpectNoEvent(eventOccured, eventType.ToString());
        }
    }

    #endregion
}
