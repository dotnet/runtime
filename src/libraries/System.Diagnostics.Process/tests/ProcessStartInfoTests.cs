// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Xunit;
using System.Text;
using System.ComponentModel;
using System.Security;
using System.Threading;

namespace System.Diagnostics.Tests
{
    public partial class ProcessStartInfoTests : ProcessTestBase
    {
        [Fact]
        public void TestEnvironmentProperty()
        {
            Assert.NotEqual(0, new Process().StartInfo.Environment.Count);

            ProcessStartInfo psi = new ProcessStartInfo();

            // Creating a detached ProcessStartInfo will pre-populate the environment
            // with current environmental variables.

            IDictionary<string, string> environment = psi.Environment;

            Assert.NotEqual(0, environment.Count);

            int countItems = environment.Count;

            environment.Add("NewKey", "NewValue");
            environment.Add("NewKey2", "NewValue2");

            Assert.Equal(countItems + 2, environment.Count);
            environment.Remove("NewKey");
            Assert.Equal(countItems + 1, environment.Count);

            environment.Add("NewKey2", "NewValue2Overridden");
            Assert.Equal("NewValue2Overridden", environment["NewKey2"]);

            //Clear
            environment.Clear();
            Assert.Equal(0, environment.Count);

            //ContainsKey 
            environment.Add("NewKey", "NewValue");
            environment.Add("NewKey2", "NewValue2");
            Assert.True(environment.ContainsKey("NewKey"));

            Assert.Equal(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), environment.ContainsKey("newkey"));
            Assert.False(environment.ContainsKey("NewKey99"));

            //Iterating
            string result = null;
            int index = 0;
            foreach (string e1 in environment.Values.OrderBy(p => p))
            {
                index++;
                result += e1;
            }
            Assert.Equal(2, index);
            Assert.Equal("NewValueNewValue2", result);

            result = null;
            index = 0;
            foreach (string e1 in environment.Keys.OrderBy(p => p))
            {
                index++;
                result += e1;
            }
            Assert.Equal("NewKeyNewKey2", result);
            Assert.Equal(2, index);

            result = null;
            index = 0;
            foreach (KeyValuePair<string, string> e1 in environment.OrderBy(p => p.Key))
            {
                index++;
                result += e1.Key;
            }
            Assert.Equal("NewKeyNewKey2", result);
            Assert.Equal(2, index);

            //Contains
            Assert.True(environment.Contains(new KeyValuePair<string, string>("NewKey", "NewValue")));
            Assert.Equal(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), environment.Contains(new KeyValuePair<string, string>("nEwKeY", "NewValue")));
            Assert.False(environment.Contains(new KeyValuePair<string, string>("NewKey99", "NewValue99")));

            //Exception not thrown with invalid key
            Assert.Throws<ArgumentNullException>(() => environment.Contains(new KeyValuePair<string, string>(null, "NewValue99")));

            environment.Add(new KeyValuePair<string, string>("NewKey98", "NewValue98"));

            //Indexed
            string newIndexItem = environment["NewKey98"];
            Assert.Equal("NewValue98", newIndexItem);

            //TryGetValue
            string stringout = null;
            Assert.True(environment.TryGetValue("NewKey", out stringout));
            Assert.Equal("NewValue", stringout);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.True(environment.TryGetValue("NeWkEy", out stringout));
                Assert.Equal("NewValue", stringout);
            }

            stringout = null;
            Assert.False(environment.TryGetValue("NewKey99", out stringout));
            Assert.Equal(null, stringout);

            //Exception not thrown with invalid key
            Assert.Throws<ArgumentNullException>(() =>
            {
                string stringout1 = null;
                environment.TryGetValue(null, out stringout1);
            });

            //Exception not thrown with invalid key
            Assert.Throws<ArgumentNullException>(() => environment.Add(null, "NewValue2"));

            environment.Add("NewKey2", "NewValue2OverriddenAgain");
            Assert.Equal("NewValue2OverriddenAgain", environment["NewKey2"]);

            //Remove Item
            environment.Remove("NewKey98");
            environment.Remove("NewKey98");   //2nd occurrence should not assert

            //Exception not thrown with null key
            Assert.Throws<ArgumentNullException>(() => { environment.Remove(null); });

            //"Exception not thrown with null key"
            Assert.Throws<KeyNotFoundException>(() => environment["1bB"]);

            Assert.True(environment.Contains(new KeyValuePair<string, string>("NewKey2", "NewValue2OverriddenAgain")));
            Assert.Equal(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), environment.Contains(new KeyValuePair<string, string>("NEWKeY2", "NewValue2OverriddenAgain")));

            Assert.False(environment.Contains(new KeyValuePair<string, string>("NewKey2", "newvalue2Overriddenagain")));
            Assert.False(environment.Contains(new KeyValuePair<string, string>("newkey2", "newvalue2Overriddenagain")));

            //Use KeyValuePair Enumerator
            string[] results = new string[2];
            var x = environment.GetEnumerator();
            x.MoveNext();
            results[0] = x.Current.Key + " " + x.Current.Value;
            x.MoveNext();
            results[1] = x.Current.Key + " " + x.Current.Value;

            Assert.Equal(new string[] { "NewKey NewValue", "NewKey2 NewValue2OverriddenAgain" }, results.OrderBy(s => s));

            //IsReadonly
            Assert.False(environment.IsReadOnly);

            environment.Add(new KeyValuePair<string, string>("NewKey3", "NewValue3"));
            environment.Add(new KeyValuePair<string, string>("NewKey4", "NewValue4"));


            //CopyTo - the order is undefined.
            KeyValuePair<string, string>[] kvpa = new KeyValuePair<string, string>[10];
            environment.CopyTo(kvpa, 0);

            KeyValuePair<string, string>[] kvpaOrdered = kvpa.OrderByDescending(k => k.Value).ToArray();
            Assert.Equal("NewKey4", kvpaOrdered[0].Key);
            Assert.Equal("NewKey2", kvpaOrdered[2].Key);

            environment.CopyTo(kvpa, 6);
            Assert.Equal(default(KeyValuePair<string, string>), kvpa[5]);
            Assert.StartsWith("NewKey", kvpa[6].Key);
            Assert.NotEqual(kvpa[6].Key, kvpa[7].Key);
            Assert.StartsWith("NewKey", kvpa[7].Key);
            Assert.NotEqual(kvpa[7].Key, kvpa[8].Key);
            Assert.StartsWith("NewKey", kvpa[8].Key);

            //Exception not thrown with null key
            Assert.Throws<ArgumentOutOfRangeException>(() => { environment.CopyTo(kvpa, -1); });

            //Exception not thrown with null key
            AssertExtensions.Throws<ArgumentException>(null, () => { environment.CopyTo(kvpa, 9); });

            //Exception not thrown with null key
            Assert.Throws<ArgumentNullException>(() =>
            {
                KeyValuePair<string, string>[] kvpanull = null;
                environment.CopyTo(kvpanull, 0);
            });
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapNotUapAot, "Retrieving information about local processes is not supported on uap")]
        public void TestEnvironmentOfChildProcess()
        {
            const string ItemSeparator = "CAFF9451396B4EEF8A5155A15BDC2080"; // random string that shouldn't be in any env vars; used instead of newline to separate env var strings
            const string ExtraEnvVar = "TestEnvironmentOfChildProcess_SpecialStuff";
            Environment.SetEnvironmentVariable(ExtraEnvVar, "\x1234" + Environment.NewLine + "\x5678"); // ensure some Unicode characters and newlines are in the output
            try
            {
                // Schedule a process to see what env vars it gets.  Have it write out those variables
                // to its output stream so we can read them.
                Process p = CreateProcess(() =>
                {
                    Console.Write(string.Join(ItemSeparator, Environment.GetEnvironmentVariables().Cast<DictionaryEntry>().Select(e => Convert.ToBase64String(Encoding.UTF8.GetBytes(e.Key + "=" + e.Value)))));
                    return SuccessExitCode;
                });
                p.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                p.StartInfo.RedirectStandardOutput = true;
                p.Start();
                string output = p.StandardOutput.ReadToEnd();
                Assert.True(p.WaitForExit(WaitInMS));

                // Parse the env vars from the child process
                var actualEnv = new HashSet<string>(output.Split(new[] { ItemSeparator }, StringSplitOptions.None).Select(s => Encoding.UTF8.GetString(Convert.FromBase64String(s))));

                // Validate against StartInfo.Environment.
                var startInfoEnv = new HashSet<string>(p.StartInfo.Environment.Select(e => e.Key + "=" + e.Value));
                Assert.True(startInfoEnv.SetEquals(actualEnv),
                    string.Format("Expected: {0}{1}Actual: {2}",
                        string.Join(", ", startInfoEnv.Except(actualEnv)),
                        Environment.NewLine,
                        string.Join(", ", actualEnv.Except(startInfoEnv))));

                // Validate against current process. (Profilers / code coverage tools can add own environment variables 
                // but we start child process without them. Thus the set of variables from the child process could
                // be a subset of variables from current process.)
                var envEnv = new HashSet<string>(Environment.GetEnvironmentVariables().Cast<DictionaryEntry>().Select(e => e.Key + "=" + e.Value));
                Assert.True(envEnv.IsSupersetOf(actualEnv),
                    string.Format("Expected: {0}{1}Actual: {2}",
                        string.Join(", ", envEnv.Except(actualEnv)),
                        Environment.NewLine,
                        string.Join(", ", actualEnv.Except(envEnv))));
            }
            finally
            {
                Environment.SetEnvironmentVariable(ExtraEnvVar, null);
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.Uap, "Only UAP blocks setting ShellExecute to true")]
        public void UseShellExecute_GetSetWindows_Success_Uap()
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            Assert.False(psi.UseShellExecute);

            // Calling the setter
            Assert.Throws<PlatformNotSupportedException>(() => { psi.UseShellExecute = true; });
            psi.UseShellExecute = false;

            // Calling the getter
            Assert.False(psi.UseShellExecute, "UseShellExecute=true is not supported on onecore.");
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework, "Desktop UseShellExecute is set to true by default")]
        public void UseShellExecute_GetSetWindows_Success_Netfx()
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            Assert.True(psi.UseShellExecute);

            psi.UseShellExecute = false;
            Assert.False(psi.UseShellExecute);

            psi.UseShellExecute = true;
            Assert.True(psi.UseShellExecute);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap | TargetFrameworkMonikers.NetFramework)]
        public void TestUseShellExecuteProperty_SetAndGet_NotUapOrNetFX()
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            Assert.False(psi.UseShellExecute);

            psi.UseShellExecute = true;
            Assert.True(psi.UseShellExecute);

            psi.UseShellExecute = false;
            Assert.False(psi.UseShellExecute);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap)]
        public void TestUseShellExecuteProperty_Redirects_NotSupported(int std)
        {
            Process p = CreateProcessLong();
            p.StartInfo.UseShellExecute = true;

            switch (std)
            {
                case 0: p.StartInfo.RedirectStandardInput = true; break;
                case 1: p.StartInfo.RedirectStandardOutput = true; break;
                case 2: p.StartInfo.RedirectStandardError = true; break;
            }

            Assert.Throws<InvalidOperationException>(() => p.Start());
        }

        [Fact]
        public void TestArgumentsProperty()
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            Assert.Equal(string.Empty, psi.Arguments);

            psi = new ProcessStartInfo("filename", "-arg1 -arg2");
            Assert.Equal("-arg1 -arg2", psi.Arguments);

            psi.Arguments = "-arg3 -arg4";
            Assert.Equal("-arg3 -arg4", psi.Arguments);
        }

        [Theory, InlineData(true), InlineData(false)]
        public void TestCreateNoWindowProperty(bool value)
        {
            Process testProcess = CreateProcessLong();
            try
            {
                testProcess.StartInfo.CreateNoWindow = value;
                testProcess.Start();

                Assert.Equal(value, testProcess.StartInfo.CreateNoWindow);
            }
            finally
            {
                if (!testProcess.HasExited)
                    testProcess.Kill();

                Assert.True(testProcess.WaitForExit(WaitInMS));
            }
        }

        [Fact]
        public void TestWorkingDirectoryProperty()
        {
            CreateDefaultProcess();
            
            // check defaults
            Assert.Equal(string.Empty, _process.StartInfo.WorkingDirectory);

            Process p = CreateProcessLong();
            p.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();

            try
            {
                p.Start();
                Assert.Equal(Directory.GetCurrentDirectory(), p.StartInfo.WorkingDirectory);
            }
            finally
            {
                if (!p.HasExited)
                    p.Kill();

                Assert.True(p.WaitForExit(WaitInMS));
            }
        }

        [ActiveIssue(12696)]
        [Fact, PlatformSpecific(TestPlatforms.Windows), OuterLoop] // Uses P/Invokes, Requires admin privileges
        public void TestUserCredentialsPropertiesOnWindows()
        {
            string username = "test", password = "PassWord123!!";
            try
            {
                Interop.NetUserAdd(username, password);
            }
            catch (Exception exc)
            {
                Console.Error.WriteLine("TestUserCredentialsPropertiesOnWindows: NetUserAdd failed: {0}", exc.Message);
                return; // test is irrelevant if we can't add a user
            }

            bool hasStarted = false;
            SafeProcessHandle handle = null;
            Process p = null;

            try
            {
                p = CreateProcessLong();

                p.StartInfo.LoadUserProfile = true;
                p.StartInfo.UserName = username;
                p.StartInfo.PasswordInClearText = password;

                hasStarted = p.Start();

                if (Interop.OpenProcessToken(p.SafeHandle, 0x8u, out handle))
                {
                    SecurityIdentifier sid;
                    if (Interop.ProcessTokenToSid(handle, out sid))
                    {
                        string actualUserName = sid.Translate(typeof(NTAccount)).ToString();
                        int indexOfDomain = actualUserName.IndexOf('\\');
                        if (indexOfDomain != -1)
                            actualUserName = actualUserName.Substring(indexOfDomain + 1);

                        bool isProfileLoaded = GetNamesOfUserProfiles().Any(profile => profile.Equals(username));

                        Assert.Equal(username, actualUserName);
                        Assert.True(isProfileLoaded);
                    }
                }
            }
            finally
            {
                IEnumerable<uint> collection = new uint[] { 0 /* NERR_Success */, 2221 /* NERR_UserNotFound */ };
                Assert.Contains<uint>(Interop.NetUserDel(null, username), collection);

                if (handle != null)
                    handle.Dispose();

                if (hasStarted)
                {
                    if (!p.HasExited)
                        p.Kill();

                    Assert.True(p.WaitForExit(WaitInMS));
                }
            }
        }

        private static List<string> GetNamesOfUserProfiles()
        {
            List<string> userNames = new List<string>();

            string[] names = Registry.Users.GetSubKeyNames();
            for (int i = 1; i < names.Length; i++)
            {
                try
                {
                    SecurityIdentifier sid = new SecurityIdentifier(names[i]);
                    string userName = sid.Translate(typeof(NTAccount)).ToString();
                    int indexofDomain = userName.IndexOf('\\');
                    if (indexofDomain != -1)
                    {
                        userName = userName.Substring(indexofDomain + 1);
                        userNames.Add(userName);
                    }
                }
                catch (Exception) { }
            }

            return userNames;
        }

        [Fact]
        public void TestEnvironmentVariables_Environment_DataRoundTrips()
        {
            ProcessStartInfo psi = new ProcessStartInfo();

            // Creating a detached ProcessStartInfo will pre-populate the environment
            // with current environmental variables.
            psi.Environment.Clear();

            psi.EnvironmentVariables.Add("NewKey", "NewValue");
            psi.Environment.Add("NewKey2", "NewValue2");
            Assert.Equal(psi.Environment["NewKey"], psi.EnvironmentVariables["NewKey"]);
            Assert.Equal(psi.Environment["NewKey2"], psi.EnvironmentVariables["NewKey2"]);

            Assert.Equal(2, psi.EnvironmentVariables.Count);
            Assert.Equal(psi.Environment.Count, psi.EnvironmentVariables.Count);

            AssertExtensions.Throws<ArgumentException>(null, () => psi.EnvironmentVariables.Add("NewKey2", "NewValue2"));
            psi.EnvironmentVariables.Add("NewKey3", "NewValue3");

            psi.Environment.Add("NewKey3", "NewValue3Overridden");
            Assert.Equal("NewValue3Overridden", psi.Environment["NewKey3"]);

            psi.EnvironmentVariables.Clear();
            Assert.Equal(0, psi.Environment.Count);

            psi.EnvironmentVariables.Add("NewKey", "NewValue");
            psi.EnvironmentVariables.Add("NewKey2", "NewValue2");

            // Environment and EnvironmentVariables should be equal, but have different enumeration types.
            IEnumerable<KeyValuePair<string, string>> allEnvironment = psi.Environment.OrderBy(k => k.Key);
            IEnumerable<DictionaryEntry> allDictionary = psi.EnvironmentVariables.Cast<DictionaryEntry>().OrderBy(k => k.Key);
            Assert.Equal(allEnvironment.Select(k => new DictionaryEntry(k.Key, k.Value)), allDictionary);

            psi.EnvironmentVariables.Add("NewKey3", "NewValue3");
            KeyValuePair<string, string>[] kvpa = new KeyValuePair<string, string>[5];
            psi.Environment.CopyTo(kvpa, 0);

            KeyValuePair<string, string>[] kvpaOrdered = kvpa.OrderByDescending(k => k.Key).ToArray();
            Assert.Equal("NewKey", kvpaOrdered[2].Key);
            Assert.Equal("NewValue", kvpaOrdered[2].Value);

            psi.EnvironmentVariables.Remove("NewKey3");
            Assert.False(psi.Environment.Contains(new KeyValuePair<string,string>("NewKey3", "NewValue3")));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Test case is specific to Windows
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Retrieving information about local processes is not supported on uap")]
        public void Verbs_GetWithExeExtension_ReturnsExpected()
        {
            var psi = new ProcessStartInfo { FileName = $"{Process.GetCurrentProcess().ProcessName}.exe" };

            if (PlatformDetection.IsNetNative)
            {
                // UapAot doesn't have RegistryKey apis available so ProcessStartInfo.Verbs returns Array<string>.Empty().
                Assert.Equal(0, psi.Verbs.Length);
                return;
            }

            Assert.Contains("open", psi.Verbs, StringComparer.OrdinalIgnoreCase);
            if (PlatformDetection.IsNotWindowsNanoServer)
            {
                Assert.Contains("runas", psi.Verbs, StringComparer.OrdinalIgnoreCase);
                Assert.Contains("runasuser", psi.Verbs, StringComparer.OrdinalIgnoreCase);
            }
            Assert.DoesNotContain("printto", psi.Verbs, StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain("closed", psi.Verbs, StringComparer.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("")]
        [InlineData("nofileextension")]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void Verbs_GetWithNoExtension_ReturnsEmpty(string fileName)
        {
            var info = new ProcessStartInfo { FileName = fileName };
            Assert.Empty(info.Verbs);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void Verbs_GetWithNoRegisteredExtension_ReturnsEmpty()
        {
            var info = new ProcessStartInfo { FileName = "file.nosuchextension" };
            Assert.Empty(info.Verbs);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void Verbs_GetWithNoEmptyStringKey_ReturnsEmpty()
        {
            const string Extension = ".noemptykeyextension";
            const string FileName = "file" + Extension;

            using (TempRegistryKey tempKey = new TempRegistryKey(Registry.ClassesRoot, Extension))
            {
                if (tempKey.Key == null)
                {
                    // Skip this test if the user doesn't have permission to
                    // modify the registry.
                    return;
                }

                var info = new ProcessStartInfo { FileName = FileName };
                Assert.Empty(info.Verbs);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void Verbs_GetWithEmptyStringValue_ReturnsEmpty()
        {
            const string Extension = ".emptystringextension";
            const string FileName = "file" + Extension;

            using (TempRegistryKey tempKey = new TempRegistryKey(Registry.ClassesRoot, Extension))
            {
                if (tempKey.Key == null)
                {
                    // Skip this test if the user doesn't have permission to
                    // modify the registry.
                    return;
                }

                tempKey.Key.SetValue("", "");

                var info = new ProcessStartInfo { FileName = FileName };
                Assert.Empty(info.Verbs);
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "The full .NET Framework throws an InvalidCastException for non-string keys. See https://github.com/dotnet/corefx/issues/18187.")]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void Verbs_GetWithNonStringValue_ReturnsEmpty()
        {
            const string Extension = ".nonstringextension";
            const string FileName = "file" + Extension;

            using (TempRegistryKey tempKey = new TempRegistryKey(Registry.ClassesRoot, Extension))
            {
                if (tempKey.Key == null)
                {
                    // Skip this test if the user doesn't have permission to
                    // modify the registry.
                    return;
                }
                
                tempKey.Key.SetValue("", 123);

                var info = new ProcessStartInfo { FileName = FileName };
                Assert.Empty(info.Verbs);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void Verbs_GetWithNoShellSubKey_ReturnsEmpty()
        {
            const string Extension = ".noshellsubkey";
            const string FileName = "file" + Extension;

            using (TempRegistryKey tempKey = new TempRegistryKey(Registry.ClassesRoot, Extension))
            {
                if (tempKey.Key == null)
                {
                    // Skip this test if the user doesn't have permission to
                    // modify the registry.
                    return;
                }
                
                tempKey.Key.SetValue("", "nosuchshell");

                var info = new ProcessStartInfo { FileName = FileName };
                Assert.Empty(info.Verbs);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void Verbs_GetWithSubkeys_ReturnsEmpty()
        {
            const string Extension = ".customregistryextension";
            const string FileName = "file" + Extension;
            const string SubKeyValue = "customregistryextensionshell";

            using (TempRegistryKey extensionKey = new TempRegistryKey(Registry.ClassesRoot, Extension))
            using (TempRegistryKey shellKey = new TempRegistryKey(Registry.ClassesRoot, SubKeyValue + "\\shell"))
            {
                if (extensionKey.Key == null)
                {
                    // Skip this test if the user doesn't have permission to
                    // modify the registry.
                    return;
                }

                extensionKey.Key.SetValue("", SubKeyValue);
                
                shellKey.Key.CreateSubKey("verb1");
                shellKey.Key.CreateSubKey("NEW");
                shellKey.Key.CreateSubKey("new");
                shellKey.Key.CreateSubKey("verb2");

                var info = new ProcessStartInfo { FileName = FileName };
                Assert.Equal(new string[] { "verb1", "verb2" }, info.Verbs);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void Verbs_GetUnix_ReturnsEmpty()
        {
            var info = new ProcessStartInfo();
            Assert.Empty(info.Verbs);
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Test case is specific to Unix
        [Fact]
        public void TestEnvironmentVariablesPropertyUnix()
        {
            ProcessStartInfo psi = new ProcessStartInfo();

            // Creating a detached ProcessStartInfo will pre-populate the environment
            // with current environmental variables.

            StringDictionary environmentVariables = psi.EnvironmentVariables;

            Assert.NotEqual(0, environmentVariables.Count);

            int CountItems = environmentVariables.Count;

            environmentVariables.Add("NewKey", "NewValue");
            environmentVariables.Add("NewKey2", "NewValue2");

            Assert.Equal(CountItems + 2, environmentVariables.Count);
            environmentVariables.Remove("NewKey");
            Assert.Equal(CountItems + 1, environmentVariables.Count);

            //Exception not thrown with invalid key
            AssertExtensions.Throws<ArgumentException>(null, () => { environmentVariables.Add("NewKey2", "NewValue2"); });
            Assert.False(environmentVariables.ContainsKey("NewKey"));

            environmentVariables.Add("newkey2", "newvalue2");
            Assert.True(environmentVariables.ContainsKey("newkey2"));
            Assert.Equal("newvalue2", environmentVariables["newkey2"]);
            Assert.Equal("NewValue2", environmentVariables["NewKey2"]);

            environmentVariables.Clear();

            Assert.Equal(0, environmentVariables.Count);

            environmentVariables.Add("NewKey", "newvalue");
            environmentVariables.Add("newkey2", "NewValue2");
            Assert.False(environmentVariables.ContainsKey("newkey"));
            Assert.False(environmentVariables.ContainsValue("NewValue"));

            string result = null;
            int index = 0;
            foreach (string e1 in environmentVariables.Values)
            {
                index++;
                result += e1;
            }
            Assert.Equal(2, index);
            Assert.Equal("newvalueNewValue2", result);

            result = null;
            index = 0;
            foreach (string e1 in environmentVariables.Keys)
            {
                index++;
                result += e1;
            }
            Assert.Equal("NewKeynewkey2", result);
            Assert.Equal(2, index);

            result = null;
            index = 0;
            foreach (DictionaryEntry e1 in environmentVariables)
            {
                index++;
                result += e1.Key;
            }
            Assert.Equal("NewKeynewkey2", result);
            Assert.Equal(2, index);

            //Key not found
            Assert.Throws<KeyNotFoundException>(() =>
            {
                string stringout = environmentVariables["NewKey99"];
            });            

            //Exception not thrown with invalid key
            Assert.Throws<ArgumentNullException>(() =>
            {
                string stringout = environmentVariables[null];
            });

            //Exception not thrown with invalid key
            Assert.Throws<ArgumentNullException>(() => environmentVariables.Add(null, "NewValue2"));

            AssertExtensions.Throws<ArgumentException>(null, () => environmentVariables.Add("newkey2", "NewValue2"));

            //Use DictionaryEntry Enumerator
            var x = environmentVariables.GetEnumerator() as IEnumerator;
            x.MoveNext();
            var y1 = (DictionaryEntry)x.Current;
            Assert.Equal("NewKey newvalue", y1.Key + " " + y1.Value);
            x.MoveNext();
            y1 = (DictionaryEntry)x.Current;
            Assert.Equal("newkey2 NewValue2", y1.Key + " " + y1.Value);

            environmentVariables.Add("newkey3", "newvalue3");

            KeyValuePair<string, string>[] kvpa = new KeyValuePair<string, string>[10];
            environmentVariables.CopyTo(kvpa, 0);
            Assert.Equal("NewKey", kvpa[0].Key);
            Assert.Equal("newkey3", kvpa[2].Key);
            Assert.Equal("newvalue3", kvpa[2].Value);

            string[] kvp = new string[10];
            AssertExtensions.Throws<ArgumentException>(null, () => { environmentVariables.CopyTo(kvp, 6); });
            environmentVariables.CopyTo(kvpa, 6);
            Assert.Equal("NewKey", kvpa[6].Key);
            Assert.Equal("newvalue", kvpa[6].Value);

            Assert.Throws<ArgumentOutOfRangeException>(() => { environmentVariables.CopyTo(kvpa, -1); });

            AssertExtensions.Throws<ArgumentException>(null, () => { environmentVariables.CopyTo(kvpa, 9); });

            Assert.Throws<ArgumentNullException>(() =>
            {
                KeyValuePair<string, string>[] kvpanull = null;
                environmentVariables.CopyTo(kvpanull, 0);
            });
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("domain")]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void Domain_SetWindows_GetReturnsExpected(string domain)
        {
            var info = new ProcessStartInfo { Domain = domain };
            Assert.Equal(domain ?? string.Empty, info.Domain);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void Domain_GetSetUnix_ThrowsPlatformNotSupportedException()
        {
            var info = new ProcessStartInfo();
            Assert.Throws<PlatformNotSupportedException>(() => info.Domain);
            Assert.Throws<PlatformNotSupportedException>(() => info.Domain = "domain");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("filename")]
        public void FileName_Set_GetReturnsExpected(string fileName)
        {
            var info = new ProcessStartInfo { FileName = fileName };
            Assert.Equal(fileName ?? string.Empty, info.FileName);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void LoadUserProfile_SetWindows_GetReturnsExpected(bool loadUserProfile)
        {
            var info = new ProcessStartInfo { LoadUserProfile = loadUserProfile };
            Assert.Equal(loadUserProfile, info.LoadUserProfile);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void LoadUserProfile_GetSetUnix_ThrowsPlatformNotSupportedException()
        {
            var info = new ProcessStartInfo();
            Assert.Throws<PlatformNotSupportedException>(() => info.LoadUserProfile);
            Assert.Throws<PlatformNotSupportedException>(() => info.LoadUserProfile = false);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("passwordInClearText")]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void PasswordInClearText_SetWindows_GetReturnsExpected(string passwordInClearText)
        {
            var info = new ProcessStartInfo { PasswordInClearText = passwordInClearText };
            Assert.Equal(passwordInClearText, info.PasswordInClearText);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void PasswordInClearText_GetSetUnix_ThrowsPlatformNotSupportedException()
        {
            var info = new ProcessStartInfo();
            Assert.Throws<PlatformNotSupportedException>(() => info.PasswordInClearText);
            Assert.Throws<PlatformNotSupportedException>(() => info.PasswordInClearText = "passwordInClearText");
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void Password_SetWindows_GetReturnsExpected()
        {
            using (SecureString password = new SecureString())
            {
                password.AppendChar('a');
                var info = new ProcessStartInfo { Password = password };
                Assert.Equal(password, info.Password);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void Password_GetSetUnix_ThrowsPlatformNotSupportedException()
        {
            var info = new ProcessStartInfo();
            Assert.Throws<PlatformNotSupportedException>(() => info.Password);
            Assert.Throws<PlatformNotSupportedException>(() => info.Password = new SecureString());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("domain")]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void UserName_SetWindows_GetReturnsExpected(string userName)
        {
            var info = new ProcessStartInfo { UserName = userName };
            Assert.Equal(userName ?? string.Empty, info.UserName);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void UserName_GetSetUnix_ThrowsPlatformNotSupportedException()
        {
            var info = new ProcessStartInfo();
            Assert.Throws<PlatformNotSupportedException>(() => info.UserName);
            Assert.Throws<PlatformNotSupportedException>(() => info.UserName = "username");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("verb")]
        public void Verb_Set_GetReturnsExpected(string verb)
        {
            var info = new ProcessStartInfo { Verb = verb };
            Assert.Equal(verb ?? string.Empty, info.Verb);
        }

        [Theory]
        [InlineData(ProcessWindowStyle.Normal - 1)]
        [InlineData(ProcessWindowStyle.Maximized + 1)]
        public void WindowStyle_SetNoSuchWindowStyle_ThrowsInvalidEnumArgumentException(ProcessWindowStyle style)
        {
            var info = new ProcessStartInfo();
            Assert.Throws<InvalidEnumArgumentException>(() => info.WindowStyle = style);
        }

        [Theory]
        [InlineData(ProcessWindowStyle.Hidden)]
        [InlineData(ProcessWindowStyle.Maximized)]
        [InlineData(ProcessWindowStyle.Minimized)]
        [InlineData(ProcessWindowStyle.Normal)]
        public void WindowStyle_Set_GetReturnsExpected(ProcessWindowStyle style)
        {
            var info = new ProcessStartInfo { WindowStyle = style };
            Assert.Equal(style, info.WindowStyle);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("workingdirectory")]
        public void WorkingDirectory_Set_GetReturnsExpected(string workingDirectory)
        {
            var info = new ProcessStartInfo { WorkingDirectory = workingDirectory };
            Assert.Equal(workingDirectory ?? string.Empty, info.WorkingDirectory);
        }

        [Fact(Skip = "Manual test")]
        [PlatformSpecific(TestPlatforms.Windows)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap)]
        public void StartInfo_WebPage()
        {
            ProcessStartInfo info = new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = @"http://www.microsoft.com"
            };

            Process.Start(info); // Returns null after navigating browser
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))] // No Notepad on Nano
        [MemberData(nameof(UseShellExecute))]
        [OuterLoop("Launches notepad")]
        [PlatformSpecific(TestPlatforms.Windows)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "WaitForInputIdle, ProcessName, and MainWindowTitle are not supported on UAP")]
        public void StartInfo_NotepadWithContent(bool useShellExecute)
        {
            string tempFile = GetTestFilePath() + ".txt";
            File.WriteAllText(tempFile, $"StartInfo_NotepadWithContent({useShellExecute})");

            ProcessStartInfo info = new ProcessStartInfo
            {
                UseShellExecute = useShellExecute,
                FileName = @"notepad.exe",
                Arguments = tempFile,
                WindowStyle = ProcessWindowStyle.Minimized
            };

            using (var process = Process.Start(info))
            {
                Assert.True(process != null, $"Could not start {info.FileName} {info.Arguments} UseShellExecute={info.UseShellExecute}");

                try
                {
                    process.WaitForInputIdle(); // Give the file a chance to load
                    Assert.Equal("notepad", process.ProcessName);

                    // On some Windows versions, the file extension is not included in the title
                    Assert.StartsWith(Path.GetFileNameWithoutExtension(tempFile), process.MainWindowTitle);
                }
                finally
                {
                    if (process != null && !process.HasExited)
                        process.Kill();
                }
            }
        }
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer), // Nano does not support UseShellExecute
                                                    nameof(PlatformDetection.IsNotWindows8x))] // https://github.com/dotnet/corefx/issues/20388
        [OuterLoop("Launches notepad")]
        [PlatformSpecific(TestPlatforms.Windows)]
        // Re-enabling with extra diagnostic info
        // [ActiveIssue("https://github.com/dotnet/corefx/issues/20388")]
        // We don't have the ability yet for UseShellExecute in UAP
        [ActiveIssue("https://github.com/dotnet/corefx/issues/20204", TargetFrameworkMonikers.Uap | TargetFrameworkMonikers.UapAot)]
        public void StartInfo_TextFile_ShellExecute()
        {
            string tempFile = GetTestFilePath() + ".txt";
            File.WriteAllText(tempFile, $"StartInfo_TextFile_ShellExecute");

            ProcessStartInfo info = new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = tempFile,
                WindowStyle = ProcessWindowStyle.Minimized
            };

            using (var process = Process.Start(info))
            {
                Assert.True(process != null, $"Could not start {info.FileName} UseShellExecute={info.UseShellExecute}\r\n{GetAssociationDetails()}");

                try
                {
                    process.WaitForInputIdle(); // Give the file a chance to load
                    Assert.Equal("notepad", process.ProcessName);

                    if (PlatformDetection.IsUap)
                    {
                        Assert.Throws<PlatformNotSupportedException>(() => process.MainWindowTitle);
                    }
                    else
                    {
                        // On some Windows versions, the file extension is not included in the title
                        Assert.StartsWith(Path.GetFileNameWithoutExtension(tempFile), process.MainWindowTitle);
                    }
                }
                finally
                {
                    if (process != null && !process.HasExited)
                        process.Kill();
                }
            }
        }

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private unsafe static extern int AssocQueryStringW(
            int flags,
            int str,
            string pszAssoc,
            string pszExtra,
            char* pszOut,
            ref uint pcchOut);

        private unsafe static string GetAssociationString(int flags, int str, string pszAssoc, string pszExtra)
        {
            uint count = 0;
            int result = AssocQueryStringW(flags, str, pszAssoc, pszExtra, null, ref count);
            if (result != 1)
                return $"Didn't get expected HRESULT (1) when getting char count. HRESULT was 0x{result:x8}";

            string value = new string((char)0, (int)count - 1);
            fixed(char* s = value)
            {
                result = AssocQueryStringW(flags, str, pszAssoc, pszExtra, s, ref count);
            }

            if (result != 0)
                return $"Didn't get expected HRESULT (0), when getting char count. HRESULT was 0x{result:x8}";

            return value;
        }

        private static string GetAssociationDetails()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Association details for '.txt'");
            sb.AppendLine("------------------------------");

            string open = GetAssociationString(0, 1 /* ASSOCSTR_COMMAND */, ".txt", "open");
            sb.AppendFormat("Open command: {0}", open);
            sb.AppendLine();

            string progId = GetAssociationString(0, 20 /* ASSOCSTR_PROGID */, ".txt", null);
            sb.AppendFormat("ProgID: {0}", progId);
            sb.AppendLine();
            return sb.ToString();
        }


        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsWindowsNanoServer))] 
        public void ShellExecute_Nano_Fails_Start()
        {
            string tempFile = GetTestFilePath() + ".txt";
            File.Create(tempFile).Dispose();

            ProcessStartInfo info = new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = tempFile
            };

            Assert.Throws<PlatformNotSupportedException>(() => Process.Start(info));
        }

        public static TheoryData<bool> UseShellExecute
        {
            get
            {
                TheoryData<bool> data = new TheoryData<bool> { false };

                if (   !PlatformDetection.IsUap // https://github.com/dotnet/corefx/issues/20204
                    && !PlatformDetection.IsWindowsNanoServer) // By design
                    data.Add(true);
                return data;
            }
        }

        private const int ERROR_SUCCESS = 0x0;
        private const int ERROR_FILE_NOT_FOUND = 0x2;
        private const int ERROR_BAD_EXE_FORMAT = 0xC1;

        [MemberData(nameof(UseShellExecute))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void StartInfo_BadVerb(bool useShellExecute)
        {
            ProcessStartInfo info = new ProcessStartInfo
            {
                UseShellExecute = useShellExecute,
                FileName = @"foo.txt",
                Verb = "Zlorp"
            };

            Assert.Equal(ERROR_FILE_NOT_FOUND, Assert.Throws<Win32Exception>(() => Process.Start(info)).NativeErrorCode);
        }

        [MemberData(nameof(UseShellExecute))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void StartInfo_BadExe(bool useShellExecute)
        {
            string tempFile = GetTestFilePath() + ".exe";
            File.Create(tempFile).Dispose();

            ProcessStartInfo info = new ProcessStartInfo
            {
                UseShellExecute = useShellExecute,
                FileName = tempFile
            };

            int expected = ERROR_BAD_EXE_FORMAT;

            // Windows Nano bug see #10290
            if (PlatformDetection.IsWindowsNanoServer)
                expected = ERROR_SUCCESS;

            Assert.Equal(expected, Assert.Throws<Win32Exception>(() => Process.Start(info)).NativeErrorCode);
        }
    }
}
