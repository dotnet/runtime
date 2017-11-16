// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Configuration.ConfigurationLockCollectionTest.cs - Unit
// tests for System.Configuration.ConfigurationLockCollection.
//
// Author:
//	Chris Toshok  <toshok@ximian.com>
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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

using System;
using System.Configuration;
using System.Collections;
using Xunit;
using SysConfig = System.Configuration.Configuration;

namespace MonoTests.System.Configuration
{
    public class ConfigurationLockCollectionTest
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetNativeRunningAsConsoleApp))]
        public void InitialState()
        {
            SysConfig cfg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            ConfigurationLockCollection col;

            col = cfg.AppSettings.LockAttributes;
            Assert.Equal(0, col.Count);
            Assert.False(col.Contains("file"), "A2");
            Assert.False(col.HasParentElements, "A4");
            Assert.False(col.IsModified, "A5");
            Assert.False(col.IsSynchronized);
            Assert.Equal(col, col.SyncRoot);

            col = cfg.AppSettings.LockElements;
            Assert.Equal(0, col.Count);
            Assert.False(col.HasParentElements, "A11");
            Assert.False(col.IsModified, "A12");
            Assert.False(col.IsSynchronized, "A13");
            Assert.Equal(col, col.SyncRoot);

            col = cfg.ConnectionStrings.LockAttributes;
            Assert.Equal(0, col.Count);
            Assert.False(col.HasParentElements, "A11");
            Assert.False(col.IsModified, "A12");
            Assert.False(col.IsSynchronized, "A13");
            Assert.Equal(col, col.SyncRoot);

            col = cfg.ConnectionStrings.LockElements;
            Assert.Equal(0, col.Count);
            Assert.False(col.HasParentElements, "A11");
            Assert.False(col.IsModified, "A12");
            Assert.False(col.IsSynchronized, "A13");
            Assert.Equal(col, col.SyncRoot);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetNativeRunningAsConsoleApp))]
        public void NonExistentItem()
        {
            SysConfig cfg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            ConfigurationLockCollection col;

            col = cfg.AppSettings.LockAttributes;

            Assert.Throws<ConfigurationErrorsException>(() => col.IsReadOnly("file"));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetNativeRunningAsConsoleApp))]
        public void Populate()
        {
            SysConfig cfg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            ConfigurationLockCollection col = cfg.AppSettings.LockAttributes;

            col.Add("file");

            Assert.Equal(1, col.Count);
            Assert.False(col.HasParentElements, "A2");
            Assert.True(col.IsModified, "A3");
            Assert.True(col.Contains("file"), "A4");
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetNativeRunningAsConsoleApp))]
        public void Populate_Error()
        {
            SysConfig cfg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            ConfigurationLockCollection col = cfg.AppSettings.LockAttributes;

            Assert.Throws<ConfigurationErrorsException>(() => col.Add("boo"));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetNativeRunningAsConsoleApp))]
        public void Enumerator()
        {
            SysConfig cfg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            ConfigurationLockCollection col = cfg.AppSettings.LockAttributes;

            col.Add("file");

            IEnumerator e = col.GetEnumerator();
            Assert.True(e.MoveNext(), "A1");
            Assert.Equal("file", (string)e.Current);
            Assert.False(e.MoveNext(), "A3");
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetNativeRunningAsConsoleApp))]
        public void SetFromList()
        {
            SysConfig cfg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            ConfigurationLockCollection col = cfg.AppSettings.LockAttributes;

            col.SetFromList("file");
            Assert.Equal(1, col.Count);
            Assert.True(col.Contains("file"), "A2");

            col.Clear();
            Assert.Equal(0, col.Count);

            col.SetFromList(" file ");
            Assert.Equal(1, col.Count);
            Assert.True(col.Contains("file"), "A2");
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetNativeRunningAsConsoleApp))]
        [ActiveIssue("dotnet/corefx #18195", TargetFrameworkMonikers.NetFramework)]
        public void DuplicateAdd()
        {
            SysConfig cfg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            AppSettingsSection app = cfg.AppSettings;

            app.LockAttributes.Clear();

            app.LockAttributes.Add("file");
            app.LockAttributes.Add("file");

            Assert.Equal(1, app.LockAttributes.Count);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetNativeRunningAsConsoleApp))]
        public void IsReadOnly()
        {
            SysConfig cfg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            AppSettingsSection app = cfg.AppSettings;

            app.LockAttributes.Clear();
            app.LockAllAttributesExcept.Clear();

            app.LockAttributes.Add("file");
            Assert.False(app.LockAttributes.IsReadOnly("file"), "A1");

            app.LockAllAttributesExcept.Add("file");
            Assert.False(app.LockAllAttributesExcept.IsReadOnly("file"), "A2");
        }
    }
}

