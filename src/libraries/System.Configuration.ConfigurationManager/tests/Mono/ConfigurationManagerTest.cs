// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Configuration.ConfigurationManagerTest.cs - Unit tests
// for System.Configuration.ConfigurationManager.
//
// Author:
//	Chris Toshok  <toshok@ximian.com>
//	Atsushi Enomoto  <atsushi@ximian.com>
//
// Copyright (C) 2005-2006 Novell, Inc (http://www.novell.com)
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
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using Xunit;
using SysConfig = System.Configuration.Configuration;

namespace MonoTests.System.Configuration
{
    using Util;

    public class ConfigurationManagerTest
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetNativeRunningAsConsoleApp))] // OpenExeConfiguration (ConfigurationUserLevel)
        [ActiveIssue("dotnet/corefx #19384", TargetFrameworkMonikers.NetFramework)]
        public void OpenExeConfiguration1_UserLevel_None()
        {
            SysConfig config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            FileInfo fi = new FileInfo(config.FilePath);
            Assert.Equal(TestUtil.ThisConfigFileName, fi.Name);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetNativeRunningAsConsoleApp))]
        public void OpenExeConfiguration1_UserLevel_PerUserRoaming()
        {
            string applicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            // If there is not ApplicationData folder PerUserRoaming won't work
            if (string.IsNullOrEmpty(applicationData)) return;


            SysConfig config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoaming);
            Assert.False(string.IsNullOrEmpty(config.FilePath), "should have some file path");
            FileInfo fi = new FileInfo(config.FilePath);
            Assert.Equal("user.config", fi.Name);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetNativeRunningAsConsoleApp))]
        [ActiveIssue(15065, TestPlatforms.AnyUnix)]
        public void OpenExeConfiguration1_UserLevel_PerUserRoamingAndLocal()
        {
            SysConfig config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
            FileInfo fi = new FileInfo(config.FilePath);
            Assert.Equal("user.config", fi.Name);
        }

        [Fact] // OpenExeConfiguration (String)
        public void OpenExeConfiguration2()
        {
            using (var temp = new TempDirectory())
            {
                string exePath;
                SysConfig config;

                exePath = Path.Combine(temp.Path, "DoesNotExist.whatever");
                File.Create(exePath).Close();

                config = ConfigurationManager.OpenExeConfiguration(exePath);
                Assert.Equal(exePath + ".config", config.FilePath);

                exePath = Path.Combine(temp.Path, "SomeExecutable.exe");
                File.Create(exePath).Close();

                config = ConfigurationManager.OpenExeConfiguration(exePath);
                Assert.Equal(exePath + ".config", config.FilePath);

                exePath = Path.Combine(temp.Path, "Foo.exe.config");
                File.Create(exePath).Close();

                config = ConfigurationManager.OpenExeConfiguration(exePath);
                Assert.Equal(exePath + ".config", config.FilePath);
            }
        }

        [Fact] // OpenExeConfiguration (String)
        public void OpenExeConfiguration2_ExePath_DoesNotExist()
        {
            using (var temp = new TempDirectory())
            {
                string exePath = Path.Combine(temp.Path, "DoesNotExist.exe");

                ConfigurationErrorsException ex = Assert.Throws<ConfigurationErrorsException>(
                    () => ConfigurationManager.OpenExeConfiguration(exePath));

                // An error occurred loading a configuration file:
                // The parameter 'exePath' is invalid
                Assert.Equal(typeof(ConfigurationErrorsException), ex.GetType());
                Assert.Null(ex.Filename);
                Assert.NotNull(ex.InnerException);
                Assert.Equal(0, ex.Line);
                Assert.NotNull(ex.Message);

                // The parameter 'exePath' is invalid
                ArgumentException inner = ex.InnerException as ArgumentException;
                Assert.NotNull(inner);
                Assert.Equal(typeof(ArgumentException), inner.GetType());
                Assert.Null(inner.InnerException);
                Assert.NotNull(inner.Message);
                Assert.Equal("exePath", inner.ParamName);
            }
        }

        [Fact]
        [ActiveIssue("dotnet/corefx #18831", TargetFrameworkMonikers.NetFramework)]
        public void exePath_UserLevelNone()
        {
            string name = TestUtil.ThisApplicationPath;
            SysConfig config = ConfigurationManager.OpenExeConfiguration(name);
            Assert.Equal(TestUtil.ThisApplicationPath + ".config", config.FilePath);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetNativeRunningAsConsoleApp))]
        public void exePath_UserLevelPerRoaming()
        {
            string applicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            // If there is not ApplicationData folder PerUserRoaming won't work
            if (string.IsNullOrEmpty(applicationData))
                return;

            SysConfig config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoaming);
            string filePath = config.FilePath;
            Assert.False(string.IsNullOrEmpty(filePath), "should have some file path");
            Assert.Equal("user.config", Path.GetFileName(filePath));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetNativeRunningAsConsoleApp))]
        [ActiveIssue(15066, TestPlatforms.AnyUnix)]
        public void exePath_UserLevelPerRoamingAndLocal()
        {
            SysConfig config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
            string filePath = config.FilePath;
            string applicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            Assert.True(filePath.StartsWith(applicationData), "#1:" + filePath);
            Assert.Equal("user.config", Path.GetFileName(filePath));
        }

        [Fact]
        public void mapped_UserLevelNone()
        {
            ExeConfigurationFileMap map = new ExeConfigurationFileMap();
            map.ExeConfigFilename = "execonfig";

            SysConfig config = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None);
            FileInfo fi = new FileInfo(config.FilePath);
            Assert.Equal("execonfig", fi.Name);

        }

        [Fact]
        public void mapped_UserLevelPerRoaming()
        {
            ExeConfigurationFileMap map = new ExeConfigurationFileMap();
            map.ExeConfigFilename = "execonfig";
            map.RoamingUserConfigFilename = "roaminguser";

            SysConfig config = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.PerUserRoaming);
            FileInfo fi = new FileInfo(config.FilePath);
            Assert.Equal("roaminguser", fi.Name);
        }

        [Fact]
        // Doesn't pass on Mono
        // [Category("NotWorking")]
        public void mapped_UserLevelPerRoaming_no_execonfig()
        {
            ExeConfigurationFileMap map = new ExeConfigurationFileMap();
            map.RoamingUserConfigFilename = "roaminguser";

            AssertExtensions.Throws<ArgumentException>("fileMap.ExeConfigFilename", () => ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.PerUserRoaming));
        }

        [Fact]
        public void mapped_UserLevelPerRoamingAndLocal()
        {
            ExeConfigurationFileMap map = new ExeConfigurationFileMap();
            map.ExeConfigFilename = "execonfig";
            map.RoamingUserConfigFilename = "roaminguser";
            map.LocalUserConfigFilename = "localuser";

            SysConfig config = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.PerUserRoamingAndLocal);
            FileInfo fi = new FileInfo(config.FilePath);
            Assert.Equal("localuser", fi.Name);
        }

        [Fact]
        // Doesn't pass on Mono
        // [Category("NotWorking")]
        public void mapped_UserLevelPerRoamingAndLocal_no_execonfig()
        {
            ExeConfigurationFileMap map = new ExeConfigurationFileMap();
            map.RoamingUserConfigFilename = "roaminguser";
            map.LocalUserConfigFilename = "localuser";

            AssertExtensions.Throws<ArgumentException>("fileMap.ExeConfigFilename", () => ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.PerUserRoamingAndLocal));
        }

        [Fact]
        // Doesn't pass on Mono
        // [Category("NotWorking")]
        public void mapped_UserLevelPerRoamingAndLocal_no_roaminguser()
        {
            ExeConfigurationFileMap map = new ExeConfigurationFileMap();
            map.ExeConfigFilename = "execonfig";
            map.LocalUserConfigFilename = "localuser";

            AssertExtensions.Throws<ArgumentException>("fileMap.RoamingUserConfigFilename", () => ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.PerUserRoamingAndLocal));
        }

        [Fact]
        public void MachineConfig()
        {
            SysConfig config = ConfigurationManager.OpenMachineConfiguration();
            FileInfo fi = new FileInfo(config.FilePath);
            Assert.Equal("machine.config", fi.Name);
        }

        [Fact]
        public void mapped_MachineConfig()
        {
            ConfigurationFileMap map = new ConfigurationFileMap();
            map.MachineConfigFilename = "machineconfig";

            SysConfig config = ConfigurationManager.OpenMappedMachineConfiguration(map);

            FileInfo fi = new FileInfo(config.FilePath);
            Assert.Equal("machineconfig", fi.Name);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetNativeRunningAsConsoleApp))]
        // Doesn't pass on Mono
        // [Category("NotWorking")]
        [ActiveIssue("dotnet/corefx #19384", TargetFrameworkMonikers.NetFramework)]
        public void mapped_ExeConfiguration_null()
        {
            SysConfig config = ConfigurationManager.OpenMappedExeConfiguration(null, ConfigurationUserLevel.None);

            FileInfo fi = new FileInfo(config.FilePath);
            Assert.Equal(TestUtil.ThisConfigFileName, fi.Name);
        }

        [Fact]
        // Doesn't pass on Mono
        // [Category("NotWorking")]
        public void mapped_MachineConfig_null()
        {
            SysConfig config = ConfigurationManager.OpenMappedMachineConfiguration(null);

            FileInfo fi = new FileInfo(config.FilePath);
            Assert.Equal("machine.config", fi.Name);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetNativeRunningAsConsoleApp))]
        public void GetSectionReturnsNativeObject()
        {
            Assert.True(ConfigurationManager.GetSection("appSettings") is NameValueCollection);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetNativeRunningAsConsoleApp))] // Test for bug #3412
        // Doesn't pass on Mono
        // [Category("NotWorking")]
        public void TestAddRemoveSection()
        {
            const string name = "testsection";
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            // ensure not present
            if (config.Sections.Get(name) != null)
            {
                config.Sections.Remove(name);
            }

            // add
            config.Sections.Add(name, new TestSection());

            // remove
            var section = config.Sections.Get(name);
            Assert.NotNull(section);
            Assert.NotNull(section as TestSection);
            config.Sections.Remove(name);

            // add
            config.Sections.Add(name, new TestSection());

            // remove
            section = config.Sections.Get(name);
            Assert.NotNull(section);
            Assert.NotNull(section as TestSection);
            config.Sections.Remove(name);
        }

        [Fact]
        public void TestFileMap()
        {
            using (var temp = new TempDirectory())
            {
                string configPath = Path.Combine(temp.Path, Path.GetRandomFileName() + ".config");
                Assert.False(File.Exists(configPath));

                var map = new ExeConfigurationFileMap();
                map.ExeConfigFilename = configPath;

                var config = ConfigurationManager.OpenMappedExeConfiguration(
                    map, ConfigurationUserLevel.None);

                config.Sections.Add("testsection", new TestSection());

                config.Save();

                Assert.True(File.Exists(configPath), "#1");
                Assert.True(File.Exists(Path.GetFullPath(configPath)), "#2");
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetNativeRunningAsConsoleApp))]
        public void TestContext()
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            const string name = "testsection";

            // ensure not present
            if (config.GetSection(name) != null)
                config.Sections.Remove(name);

            var section = new TestContextSection();

            // Can't access EvaluationContext ....
            Assert.Throws<ConfigurationErrorsException>(() => section.TestContext(null));

            // ... until it's been added to a section.
            config.Sections.Add(name, section);
            section.TestContext("#2");

            // Remove ...
            config.Sections.Remove(name);

            // ... and it doesn't lose its context
            section.TestContext(null);
        }

        [Fact]
        public void TestContext2()
        {
            using (var temp = new TempDirectory())
            {
                string configPath = Path.Combine(temp.Path, Path.GetRandomFileName() + ".config");
                Assert.False(File.Exists(configPath));

                var map = new ExeConfigurationFileMap();
                map.ExeConfigFilename = configPath;

                var config = ConfigurationManager.OpenMappedExeConfiguration(
                    map, ConfigurationUserLevel.None);

                config.Sections.Add("testsection", new TestSection());
                config.Sections.Add("testcontext", new TestContextSection());

                config.Save();

                Assert.True(File.Exists(configPath), "#1");
            }
        }

        class TestSection : ConfigurationSection { }

        class TestContextSection : ConfigurationSection
        {
            public void TestContext(string label)
            {
                Assert.NotNull(EvaluationContext);
            }
        }

        [Fact]
        public void BadConfig()
        {
            using (var temp = new TempDirectory())
            {
                string xml = @" badXml";

                var file = Path.Combine(temp.Path, "badConfig.config");
                File.WriteAllText(file, xml);

                var fileMap = new ConfigurationFileMap(file);
                Assert.Equal(file,
                    Assert.Throws<ConfigurationErrorsException>(() => ConfigurationManager.OpenMappedMachineConfiguration(fileMap)).Filename);
            }
        }
    }
}
