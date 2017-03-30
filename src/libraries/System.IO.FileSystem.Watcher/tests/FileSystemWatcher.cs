// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;

namespace System.IO.Tests
{
    public class FileSystemWatcherTests_netstandard17 : FileSystemWatcherTest
    {
        public class TestSite : ISite
        {
            public bool designMode;
            public bool DesignMode => designMode;
            public IComponent Component => null;
            public IContainer Container => null;
            public string Name { get; set; }
            public object GetService(Type serviceType) => null;
        }

        [Fact]
        public void Site_GetSetRoundtrips()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var watcher = new TestFileSystemWatcher(testDirectory.Path, "*"))
            {
                TestSite site = new TestSite();
                Assert.Null(watcher.Site);
                watcher.Site = site;
                Assert.Equal(site, watcher.Site);
                watcher.Site = null;
                Assert.Null(watcher.Site);
                Assert.False(watcher.EnableRaisingEvents);
            }
        }

        /// <summary>
        /// When the FSW Site is set to a nonnull Site with DesignMode enabled, Event raising will be set to true
        /// </summary>
        [Fact]
        public void Site_NonNullSetEnablesRaisingEvents()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var watcher = new TestFileSystemWatcher(testDirectory.Path, "*"))
            {
                TestSite site = new TestSite() { designMode = true };
                watcher.Site = site;
                Assert.True(watcher.EnableRaisingEvents);
            }
        }

        internal class TestISynchronizeInvoke : ISynchronizeInvoke
        {
            public bool BeginInvoke_Called;
            public Delegate ExpectedDelegate;

            public IAsyncResult BeginInvoke(Delegate method, object[] args)
            {
                Assert.Equal(ExpectedDelegate, method);
                BeginInvoke_Called = true;
                return null;
            }

            public bool InvokeRequired => true;
            public object EndInvoke(IAsyncResult result) => null;
            public object Invoke(Delegate method, object[] args) => null;
        }

        [Fact]
        public void SynchronizingObject_GetSetRoundtrips()
        {
            TestISynchronizeInvoke invoker = new TestISynchronizeInvoke() { };
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var watcher = new TestFileSystemWatcher(testDirectory.Path, "*"))
            {
                Assert.Null(watcher.SynchronizingObject);
                watcher.SynchronizingObject = invoker;
                Assert.Equal(invoker, watcher.SynchronizingObject);
                watcher.SynchronizingObject = null;
                Assert.Null(watcher.SynchronizingObject);
            }
        }

        /// <summary>
        /// Ensure that the SynchronizeObject is invoked when an event occurs
        /// </summary>
        [Theory]
        [InlineData(WatcherChangeTypes.Changed)]
        [InlineData(WatcherChangeTypes.Deleted)]
        [InlineData(WatcherChangeTypes.Created)]
        public void SynchronizingObject_CalledOnEvent(WatcherChangeTypes expectedChangeType)
        {
            FileSystemEventHandler dele = (sender, e) => { Assert.Equal(expectedChangeType, e.ChangeType); };
            TestISynchronizeInvoke invoker = new TestISynchronizeInvoke() { ExpectedDelegate = dele };
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var watcher = new TestFileSystemWatcher(testDirectory.Path, "*"))
            {
                watcher.SynchronizingObject = invoker;
                if (expectedChangeType == WatcherChangeTypes.Created)
                {
                    watcher.Created += dele;
                    watcher.CallOnCreated(new FileSystemEventArgs(WatcherChangeTypes.Created, "test", "name"));
                }
                else if (expectedChangeType == WatcherChangeTypes.Deleted)
                {
                    watcher.Deleted += dele;
                    watcher.CallOnDeleted(new FileSystemEventArgs(WatcherChangeTypes.Deleted, "test", "name"));
                }
                else if (expectedChangeType == WatcherChangeTypes.Changed)
                {
                    watcher.Changed += dele;
                    watcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, "test", "name"));
                }
                Assert.True(invoker.BeginInvoke_Called);
            }
        }

        /// <summary>
        /// Ensure that the SynchronizeObject is invoked when an Renamed event occurs
        /// </summary>
        [Fact]
        public void SynchronizingObject_CalledOnRenamed()
        {
            RenamedEventHandler dele = (sender, e) => { Assert.Equal(WatcherChangeTypes.Renamed, e.ChangeType); };
            TestISynchronizeInvoke invoker = new TestISynchronizeInvoke() { ExpectedDelegate = dele };
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var watcher = new TestFileSystemWatcher(testDirectory.Path, "*"))
            {
                watcher.SynchronizingObject = invoker;
                watcher.Renamed += dele;
                watcher.CallOnRenamed(new RenamedEventArgs(WatcherChangeTypes.Changed, "test", "name", "oldname"));
                Assert.True(invoker.BeginInvoke_Called);
            }
        }

        /// <summary>
        /// Ensure that the SynchronizeObject is invoked when an Error event occurs
        /// </summary>
        [Fact]
        public void SynchronizingObject_CalledOnError()
        {
            ErrorEventHandler dele = (sender, e) => { Assert.IsType<FileNotFoundException>(e.GetException()); };
            TestISynchronizeInvoke invoker = new TestISynchronizeInvoke() { ExpectedDelegate = dele };
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var watcher = new TestFileSystemWatcher(testDirectory.Path, "*"))
            {
                watcher.SynchronizingObject = invoker;
                watcher.Error += dele;
                watcher.CallOnError(new ErrorEventArgs(new FileNotFoundException()));
                Assert.True(invoker.BeginInvoke_Called);
            }
        }

        /// <summary>
        /// Calling BeginInit and EndInit in a loop is fine. If events are enabled, they will start and stop in the loop as well.
        /// </summary>
        [Fact]
        public void BeginEndInit_Repeated()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var watcher = new TestFileSystemWatcher(testDirectory.Path, "*"))
            {
                watcher.BeginInit();
                watcher.EndInit();
                watcher.BeginInit();
                watcher.EndInit();
                Assert.False(watcher.EnableRaisingEvents);
            }
        }

        /// <summary>
        /// BeginInit followed by a EnableRaisingEvents=true does not cause the watcher to begin.
        /// </summary>
        [Fact]
        public void BeginInit_PausesEnableRaisingEvents()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var watcher = new TestFileSystemWatcher(testDirectory.Path, "*"))
            {
                watcher.Created += (obj, e) => { Assert.False(true, "Created event should not occur"); };
                watcher.Deleted += (obj, e) => { Assert.False(true, "Deleted event should not occur"); };
                watcher.BeginInit();
                watcher.EnableRaisingEvents = true;
                new TempFile(Path.Combine(testDirectory.Path, GetTestFileName())).Dispose();
                Thread.Sleep(WaitForExpectedEventTimeout);
            }
        }

        /// <summary>
        /// EndInit will begin EnableRaisingEvents if we previously set EnableRaisingEvents=true
        /// </summary>
        [Fact]
        public void EndInit_ResumesPausedEnableRaisingEvents()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var watcher = new TestFileSystemWatcher(testDirectory.Path, "*"))
            {
                watcher.BeginInit();
                watcher.EnableRaisingEvents = true;
                watcher.EndInit();
                ExpectEvent(watcher, WatcherChangeTypes.Created | WatcherChangeTypes.Deleted, () => new TempFile(Path.Combine(testDirectory.Path, GetTestFileName())).Dispose(), null);
            }
        }

        /// <summary>
        /// EndInit will begin EnableRaisingEvents if we previously set EnableRaisingEvents=true
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void EndInit_ResumesPausedEnableRaisingEvents(bool setBeforeBeginInit)
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var watcher = new TestFileSystemWatcher(testDirectory.Path, "*"))
            {
                if (setBeforeBeginInit)
                    watcher.EnableRaisingEvents = true;
                watcher.BeginInit();
                if (!setBeforeBeginInit)
                    watcher.EnableRaisingEvents = true;
                watcher.EndInit();
                ExpectEvent(watcher, WatcherChangeTypes.Created | WatcherChangeTypes.Deleted, () => new TempFile(Path.Combine(testDirectory.Path, GetTestFileName())).Dispose(), null);
            }
        }

        /// <summary>
        /// Stopping events during the initialization period will prevent the watcher from restarting after EndInit()
        /// </summary>
        [Fact]
        public void EndRaisingEventsDuringPause()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var watcher = new TestFileSystemWatcher(testDirectory.Path, "*"))
            {
                watcher.EnableRaisingEvents = true;
                watcher.BeginInit();
                watcher.EnableRaisingEvents = false;
                watcher.EndInit();
                new TempFile(Path.Combine(testDirectory.Path, GetTestFileName())).Dispose();
                Thread.Sleep(WaitForExpectedEventTimeout);
            }
        }

        /// <summary>
        /// EndInit will not start event raising unless EnableRaisingEvents was set to true.
        /// </summary>
        [Fact]
        public void EndInit_DoesNotEnableEventRaisedEvents()
        {
            using (var testDirectory = new TempDirectory(GetTestFilePath()))
            using (var watcher = new TestFileSystemWatcher(testDirectory.Path, "*"))
            {
                watcher.Created += (obj, e) => { Assert.False(true, "Created event should not occur"); };
                watcher.Deleted += (obj, e) => { Assert.False(true, "Deleted event should not occur"); };
                watcher.BeginInit();
                watcher.EndInit();
                new TempFile(Path.Combine(testDirectory.Path, GetTestFileName())).Dispose();
                Thread.Sleep(WaitForExpectedEventTimeout);
            }
        }
    }
}
