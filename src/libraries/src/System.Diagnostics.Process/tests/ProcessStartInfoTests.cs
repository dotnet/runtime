// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Xunit;
using System.Text;

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

            Assert.NotEqual(environment.Count, 0);

            int CountItems = environment.Count;

            environment.Add("NewKey", "NewValue");
            environment.Add("NewKey2", "NewValue2");

            Assert.Equal(CountItems + 2, environment.Count);
            environment.Remove("NewKey");
            Assert.Equal(CountItems + 1, environment.Count);

            //Exception not thrown with invalid key
            Assert.Throws<ArgumentException>(() => { environment.Add("NewKey2", "NewValue2"); });

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
            foreach (string e1 in environment.Values)
            {
                index++;
                result += e1;
            }
            Assert.Equal(2, index);
            Assert.Equal("NewValueNewValue2", result);

            result = null;
            index = 0;
            foreach (string e1 in environment.Keys)
            {
                index++;
                result += e1;
            }
            Assert.Equal("NewKeyNewKey2", result);
            Assert.Equal(2, index);

            result = null;
            index = 0;
            foreach (KeyValuePair<string, string> e1 in environment)
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

            //Invalid Key to add
            Assert.Throws<ArgumentException>(() => environment.Add("NewKey2", "NewValue2"));

            //Remove Item
            environment.Remove("NewKey98");
            environment.Remove("NewKey98");   //2nd occurrence should not assert

            //Exception not thrown with null key
            Assert.Throws<ArgumentNullException>(() => { environment.Remove(null); });

            //"Exception not thrown with null key"
            Assert.Throws<KeyNotFoundException>(() => environment["1bB"]);

            Assert.True(environment.Contains(new KeyValuePair<string, string>("NewKey2", "NewValue2")));
            Assert.Equal(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), environment.Contains(new KeyValuePair<string, string>("NEWKeY2", "NewValue2")));

            Assert.False(environment.Contains(new KeyValuePair<string, string>("NewKey2", "newvalue2")));
            Assert.False(environment.Contains(new KeyValuePair<string, string>("newkey2", "newvalue2")));

            //Use KeyValuePair Enumerator
            var x = environment.GetEnumerator();
            x.MoveNext();
            var y1 = x.Current;
            Assert.Equal("NewKey NewValue", y1.Key + " " + y1.Value);
            x.MoveNext();
            y1 = x.Current;
            Assert.Equal("NewKey2 NewValue2", y1.Key + " " + y1.Value);

            //IsReadonly
            Assert.False(environment.IsReadOnly);

            environment.Add(new KeyValuePair<string, string>("NewKey3", "NewValue3"));
            environment.Add(new KeyValuePair<string, string>("NewKey4", "NewValue4"));


            //CopyTo
            KeyValuePair<string, string>[] kvpa = new KeyValuePair<string, string>[10];
            environment.CopyTo(kvpa, 0);
            Assert.Equal("NewKey", kvpa[0].Key);
            Assert.Equal("NewKey3", kvpa[2].Key);

            environment.CopyTo(kvpa, 6);
            Assert.Equal("NewKey", kvpa[6].Key);

            //Exception not thrown with null key
            Assert.Throws<ArgumentOutOfRangeException>(() => { environment.CopyTo(kvpa, -1); });

            //Exception not thrown with null key
            Assert.Throws<ArgumentException>(() => { environment.CopyTo(kvpa, 9); });

            //Exception not thrown with null key
            Assert.Throws<ArgumentNullException>(() =>
            {
                KeyValuePair<string, string>[] kvpanull = null;
                environment.CopyTo(kvpanull, 0);
            });
        }

        [Fact]
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
                    Console.Write(string.Join(ItemSeparator, Environment.GetEnvironmentVariables().Cast<DictionaryEntry>().Select(e => e.Key + "=" + e.Value)));
                    return SuccessExitCode;
                });
                p.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                p.StartInfo.RedirectStandardOutput = true;
                p.Start();
                string output = p.StandardOutput.ReadToEnd();
                Assert.True(p.WaitForExit(WaitInMS));

                // Parse the env vars from the child process
                var actualEnv = new HashSet<string>(output.Split(new[] { ItemSeparator }, StringSplitOptions.None));

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

        [PlatformSpecific(TestPlatforms.Windows)] // UseShellExecute currently not supported on Windows
        [Fact]
        public void TestUseShellExecuteProperty_SetAndGet_Windows()
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            Assert.False(psi.UseShellExecute);

            // Calling the setter
            Assert.Throws<PlatformNotSupportedException>(() => { psi.UseShellExecute = true; });
            psi.UseShellExecute = false;

            // Calling the getter
            Assert.False(psi.UseShellExecute, "UseShellExecute=true is not supported on onecore.");
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [Fact]
        public void TestUseShellExecuteProperty_SetAndGet_Unix()
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            Assert.False(psi.UseShellExecute);

            psi.UseShellExecute = true;
            Assert.True(psi.UseShellExecute);

            psi.UseShellExecute = false;
            Assert.False(psi.UseShellExecute);
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
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


        [Fact, PlatformSpecific(TestPlatforms.AnyUnix)]
        public void TestUserCredentialsPropertiesOnUnix()
        {
            Assert.Throws<PlatformNotSupportedException>(() => _process.StartInfo.Domain);
            Assert.Throws<PlatformNotSupportedException>(() => _process.StartInfo.UserName);
            Assert.Throws<PlatformNotSupportedException>(() => _process.StartInfo.PasswordInClearText);
            Assert.Throws<PlatformNotSupportedException>(() => _process.StartInfo.LoadUserProfile);
        }

        [Fact]
        public void TestWorkingDirectoryProperty()
        {
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
        [Fact, PlatformSpecific(TestPlatforms.Windows), OuterLoop] // Requires admin privileges
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
    }
}
