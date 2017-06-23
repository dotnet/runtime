// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Configuration.ExeConfigurationFileMapTest.cs - Unit tests
// for System.Configuration.ExeConfigurationFileMap.
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
using Xunit;

namespace MonoTests.System.Configuration
{
    using Util;

    public class ExeConfigurationFileMapTest
    {
        [Fact]
        public void Properties()
        {
            ExeConfigurationFileMap map = new ExeConfigurationFileMap();

            /* defaults */
            Assert.Equal("", map.ExeConfigFilename);
            Assert.Equal("", map.LocalUserConfigFilename);
            Assert.Equal("", map.RoamingUserConfigFilename);

            /* setter */
            map.ExeConfigFilename = "foo";
            Assert.Equal("foo", map.ExeConfigFilename);
            map.LocalUserConfigFilename = "bar";
            Assert.Equal("bar", map.LocalUserConfigFilename);
            map.RoamingUserConfigFilename = "baz";
            Assert.Equal("baz", map.RoamingUserConfigFilename);

            /* null setter */
            map.ExeConfigFilename = null;
            Assert.Null(map.ExeConfigFilename);
            map.LocalUserConfigFilename = null;
            Assert.Null(map.LocalUserConfigFilename);
            map.RoamingUserConfigFilename = null;
            Assert.Null(map.RoamingUserConfigFilename);
        }

        [Fact]
        public void MissingRoamingFilename()
        {
            TestUtil.RunWithTempFile(filename =>
            {
                var map = new ExeConfigurationFileMap();
                map.ExeConfigFilename = filename;

                AssertExtensions.Throws<ArgumentException>("fileMap.RoamingUserConfigFilename", () =>
                    ConfigurationManager.OpenMappedExeConfiguration(
                        map, ConfigurationUserLevel.PerUserRoaming));
            });
        }

        [Fact]
        public void MissingRoamingFilename2()
        {
            TestUtil.RunWithTempFile(filename =>
            {
                var map = new ExeConfigurationFileMap();
                map.LocalUserConfigFilename = filename;

                AssertExtensions.Throws<ArgumentException>("fileMap.RoamingUserConfigFilename", () =>
                    ConfigurationManager.OpenMappedExeConfiguration(
                        map, ConfigurationUserLevel.PerUserRoamingAndLocal));
            });
        }

        [Fact]
        public void MissingLocalFilename()
        {
            TestUtil.RunWithTempFile(filename =>
            {
                var map = new ExeConfigurationFileMap();
                map.ExeConfigFilename = filename;
                map.RoamingUserConfigFilename = filename;

                AssertExtensions.Throws<ArgumentException>("fileMap.LocalUserConfigFilename", () =>
                    ConfigurationManager.OpenMappedExeConfiguration(
                        map, ConfigurationUserLevel.PerUserRoamingAndLocal));
            });
        }

        [Fact]
        public void MissingExeFilename()
        {
            TestUtil.RunWithTempFiles((roaming, local) =>
            {
                var map = new ExeConfigurationFileMap();
                map.RoamingUserConfigFilename = roaming;
                map.LocalUserConfigFilename = local;

                AssertExtensions.Throws<ArgumentException>("fileMap.ExeConfigFilename", () =>
                    ConfigurationManager.OpenMappedExeConfiguration(
                        map, ConfigurationUserLevel.PerUserRoamingAndLocal));
            });
        }

        [Fact]
        public void MissingExeFilename2()
        {
            TestUtil.RunWithTempFile((machine) =>
            {
                var map = new ExeConfigurationFileMap();
                map.MachineConfigFilename = machine;

                AssertExtensions.Throws<ArgumentException>("fileMap.ExeConfigFilename", () =>
                    ConfigurationManager.OpenMappedExeConfiguration(
                        map, ConfigurationUserLevel.None));
            });
        }
    }
}

