// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public abstract class RegistryTestsBase : IDisposable
    {
        protected string TestRegistryKeyName { get; private set; }
        protected RegistryKey TestRegistryKey { get; private set; }

        protected RegistryTestsBase()
        {
            // Create a unique name for this test class
            TestRegistryKeyName = CreateUniqueKeyName();

            // Cleanup the key in case a previous run of this test crashed and left
            // the key behind.  The key name is specific enough to corefx that we don't
            // need to worry about it being a real key on the user's system used
            // for another purpose.
            RemoveKeyIfExists(TestRegistryKeyName);

            // Then create the key.
            TestRegistryKey = Registry.CurrentUser.CreateSubKey(TestRegistryKeyName, true);
            Assert.NotNull(TestRegistryKey);
        }

        public void Dispose()
        {
            TestRegistryKey.Dispose();
            RemoveKeyIfExists(TestRegistryKeyName);
        }

        private static void RemoveKeyIfExists(string keyName)
        {
            RegistryKey rk = Registry.CurrentUser;
            if (rk.OpenSubKey(keyName) != null)
            {
                rk.DeleteSubKeyTree(keyName);
                Assert.Null(rk.OpenSubKey(keyName));
            }
        }

        private string CreateUniqueKeyName()
        {
            // Create a name to use for this class of tests. The name includes:
            // - A "corefxtest" prefix to help make it clear to anyone looking at the registry
            //   that these keys are test-only and can be deleted, in case the tests crash and 
            //   we end up leaving some keys behind.
            // - The name of this test class, so as to avoid problems with tests on different test 
            //   classes running concurrently
            return "corefxtest_" + GetType().Name;
        }

        public static readonly object[][] TestRegistrySubKeyNames =
        {
            new object[] { @"Foo", @"Foo" },
            new object[] { @"Foo\Bar", @"Foo\Bar" },

            // Multiple/trailing slashes should be removed.
            new object[] { @"Foo", @"Foo\" },
            new object[] { @"Foo", @"Foo\\" },
            new object[] { @"Foo", @"Foo\\\" },
            new object[] { @"Foo", @"Foo\\\\" },
            new object[] { @"Foo\Bar", @"Foo\\Bar" },
            new object[] { @"Foo\Bar", @"Foo\\\Bar" },
            new object[] { @"Foo\Bar", @"Foo\\\\Bar" },
            new object[] { @"Foo\Bar", @"Foo\Bar\" },
            new object[] { @"Foo\Bar", @"Foo\Bar\\" },
            new object[] { @"Foo\Bar", @"Foo\Bar\\\" },
            new object[] { @"Foo\Bar", @"Foo\\Bar\" },
            new object[] { @"Foo\Bar", @"Foo\\Bar\\" },
            new object[] { @"Foo\Bar", @"Foo\\Bar\\\" },
            new object[] { @"Foo\Bar", @"Foo\\\Bar\\\" },
            new object[] { @"Foo\Bar", @"Foo\\\\Bar\\\\" },

            // The name fix-up implementation uses a mark-and-sweep approach.
            // If there are multiple slashes, any extra slash chars will be
            // replaced with a marker char ('\uffff'), and then all '\uffff'
            // chars will be removed, including any pre-existing '\uffff' chars.
            InsertMarkerChar(@"Foo", @"{0}Foo\\"),
            InsertMarkerChar(@"Foo", @"Foo{0}\\"),
            InsertMarkerChar(@"Foo", @"Foo\\{0}"),
            InsertMarkerChar(@"Foo", @"Fo{0}o\\"),
            InsertMarkerChar(@"Foo", @"{0}Fo{0}o{0}\\{0}"),
            InsertMarkerChar(@"Foo", @"{0}Foo\\\"),
            InsertMarkerChar(@"Foo", @"Foo{0}\\\"),
            InsertMarkerChar(@"Foo", @"Foo\\\{0}"),
            InsertMarkerChar(@"Foo", @"Fo{0}o\\\"),
            InsertMarkerChar(@"Foo", @"{0}Fo{0}o{0}\\\{0}"),
            InsertMarkerChar(@"Foo\Bar", @"{0}Foo\\Bar"),
            InsertMarkerChar(@"Foo\Bar", @"Foo{0}\\Bar"),
            InsertMarkerChar(@"Foo\Bar", @"Foo\\{0}Bar"),
            InsertMarkerChar(@"Foo\Bar", @"Foo\\Bar{0}"),
            InsertMarkerChar(@"Foo\Bar", @"Fo{0}o\\Bar"),
            InsertMarkerChar(@"Foo\Bar", @"Foo\\B{0}ar"),
            InsertMarkerChar(@"Foo\Bar", @"Fo{0}o\\B{0}ar"),
            InsertMarkerChar(@"Foo\Bar", @"{0}Fo{0}o{0}\\{0}B{0}ar{0}"),
            InsertMarkerChar(@"Foo\Bar", @"{0}Foo\\\Bar"),
            InsertMarkerChar(@"Foo\Bar", @"Foo{0}\\\Bar"),
            InsertMarkerChar(@"Foo\Bar", @"Foo\\\{0}Bar"),
            InsertMarkerChar(@"Foo\Bar", @"Foo\\\Bar{0}"),
            InsertMarkerChar(@"Foo\Bar", @"Fo{0}o\\\Bar"),
            InsertMarkerChar(@"Foo\Bar", @"Foo\\\B{0}ar"),
            InsertMarkerChar(@"Foo\Bar", @"Fo{0}o\\\B{0}ar"),
            InsertMarkerChar(@"Foo\Bar", @"{0}Fo{0}o{0}\\\{0}B{0}ar{0}"),
            InsertMarkerChar(@"Foo\Bar", @"{0}Foo\Bar\\"),
            InsertMarkerChar(@"Foo\Bar", @"Foo{0}\Bar\\"),
            InsertMarkerChar(@"Foo\Bar", @"Foo\{0}Bar\\"),
            InsertMarkerChar(@"Foo\Bar", @"Foo\Bar{0}\\"),
            InsertMarkerChar(@"Foo\Bar", @"Foo\Bar\\{0}"),
            InsertMarkerChar(@"Foo\Bar", @"Fo{0}o\B{0}ar\\"),
            InsertMarkerChar(@"Foo\Bar", @"{0}Fo{0}o{0}\{0}B{0}ar{0}\\{0}"),

            // If there aren't multiple slashes, any '\uffff' chars should remain.
            InsertMarkerChar(@"{0}Foo"),
            InsertMarkerChar(@"Foo{0}"),
            InsertMarkerChar(@"Fo{0}o"),
            InsertMarkerChar(@"{0}Fo{0}o{0}"),
            InsertMarkerChar(@"{0}Foo\"),
            InsertMarkerChar(@"Foo{0}\"),
            InsertMarkerChar(@"Fo{0}o\"),
            InsertMarkerChar(@"{0}Fo{0}o{0}\"),
            InsertMarkerChar(@"{0}Foo\Bar"),
            InsertMarkerChar(@"Foo{0}\Bar"),
            InsertMarkerChar(@"Foo\{0}Bar"),
            InsertMarkerChar(@"Foo\Bar{0}"),
            InsertMarkerChar(@"Fo{0}o\Bar"),
            InsertMarkerChar(@"Foo\B{0}ar"),
            InsertMarkerChar(@"Fo{0}o\B{0}ar"),
            InsertMarkerChar(@"{0}Fo{0}o{0}\{0}B{0}ar{0}"),
            InsertMarkerChar(@"{0}Foo\Bar\"),
            InsertMarkerChar(@"Foo{0}\Bar\"),
            InsertMarkerChar(@"Foo\{0}Bar\"),
            InsertMarkerChar(@"Foo\Bar{0}\"),
            InsertMarkerChar(@"Fo{0}o\Bar\"),
            InsertMarkerChar(@"Foo\B{0}ar\"),
            InsertMarkerChar(@"Fo{0}o\B{0}ar\"),
            InsertMarkerChar(@"{0}Fo{0}o{0}\{0}B{0}ar{0}\"),
        };

        private const char MarkerChar = '\uffff';

        private static object[] InsertMarkerChar(string expected, string format)
        {
            string result = string.Format(format, MarkerChar);
            return new object[] { expected, result };
        }

        private static object[] InsertMarkerChar(string format)
        {
            string result = string.Format(format, MarkerChar);
            string expected = result.TrimEnd('\\');
            return new object[] { expected, result };
        }

        protected void CreateTestRegistrySubKey(string expected)
        {
            Assert.Equal(0, TestRegistryKey.SubKeyCount);

            using (RegistryKey key = TestRegistryKey.CreateSubKey(expected))
            {
                Assert.NotNull(key);
                Assert.Equal(1, TestRegistryKey.SubKeyCount);
                Assert.Equal(TestRegistryKey.Name + @"\" + expected, key.Name);
            }
        }
    }
}
