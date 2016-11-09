// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Xunit;

namespace System.Diagnostics.Tests
{
    public partial class ProcessStartInfoTests
    {
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

            Assert.Throws<ArgumentException>(() => { psi.EnvironmentVariables.Add("NewKey2", "NewValue2"); });
            psi.EnvironmentVariables.Add("NewKey3", "NewValue3");

            Assert.Throws<ArgumentException>(() => { psi.Environment.Add("NewKey3", "NewValue3"); });

            psi.EnvironmentVariables.Clear();
            Assert.Equal(0, psi.Environment.Count);

            psi.EnvironmentVariables.Add("NewKey", "NewValue");
            psi.EnvironmentVariables.Add("NewKey2", "NewValue2");

            string environmentResultKey = "";
            string environmentResultValue = "";
            foreach(var entry in psi.Environment)
            {
                environmentResultKey += entry.Key;
                environmentResultValue += entry.Value;
            }

            Assert.Equal("NewKeyNewKey2", environmentResultKey);
            Assert.Equal("NewValueNewValue2", environmentResultValue);

            string envVarResultKey = "";
            string envVarResultValue = "";
            foreach(KeyValuePair<string, string> entry in psi.EnvironmentVariables)
            {
                envVarResultKey += entry.Key;
                envVarResultValue += entry.Value;
            }

            Assert.Equal(environmentResultKey, envVarResultKey);
            Assert.Equal(environmentResultValue, envVarResultValue);

            psi.EnvironmentVariables.Add("NewKey3", "NewValue3");
            KeyValuePair<string, string>[] kvpa = new KeyValuePair<string, string>[5];
            psi.Environment.CopyTo(kvpa, 0);
            Assert.Equal("NewKey3", kvpa[2].Key);
            Assert.Equal("NewValue3", kvpa[2].Value);

            psi.EnvironmentVariables.Remove("NewKey3");
            Assert.False(psi.Environment.Contains(new KeyValuePair<string,string>("NewKey3", "NewValue3")));            
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [Fact]
        public void TestVerbsProperty()
        {
            var psi = new ProcessStartInfo();
            psi.FileName = $"{Process.GetCurrentProcess().ProcessName}.exe";

            Assert.Contains("open", psi.Verbs, StringComparer.OrdinalIgnoreCase);
            Assert.Contains("runas", psi.Verbs, StringComparer.OrdinalIgnoreCase);
            Assert.Contains("runasuser", psi.Verbs, StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain("printto", psi.Verbs, StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain("closed", psi.Verbs, StringComparer.OrdinalIgnoreCase);
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [Fact]
        public void TestEnvironmentVariablesPropertyUnix(){
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
            Assert.Throws<ArgumentException>(() => { environmentVariables.Add("NewKey2", "NewValue2"); });
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
            foreach (KeyValuePair<string, string> e1 in environmentVariables)
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

            //Invalid Key to add
            Assert.Throws<ArgumentException>(() => environmentVariables.Add("newkey2", "NewValue2"));

            //Use KeyValuePair Enumerator
            var x = environmentVariables.GetEnumerator() as IEnumerator<KeyValuePair<string, string>>;
            x.MoveNext();
            var y1 = x.Current;
            Assert.Equal("NewKey newvalue", y1.Key + " " + y1.Value);
            x.MoveNext();
            y1 = x.Current;
            Assert.Equal("newkey2 NewValue2", y1.Key + " " + y1.Value);

            environmentVariables.Add("newkey3", "newvalue3");

            KeyValuePair<string, string>[] kvpa = new KeyValuePair<string, string>[10];
            environmentVariables.CopyTo(kvpa, 0);
            Assert.Equal("NewKey", kvpa[0].Key);
            Assert.Equal("newkey3", kvpa[2].Key);
            Assert.Equal("newvalue3", kvpa[2].Value);

            string[] kvp = new string[10];
            Assert.Throws<ArgumentException>(() => { environmentVariables.CopyTo(kvp, 6); });
            environmentVariables.CopyTo(kvpa, 6);
            Assert.Equal("NewKey", kvpa[6].Key);
            Assert.Equal("newvalue", kvpa[6].Value);

            Assert.Throws<ArgumentOutOfRangeException>(() => { environmentVariables.CopyTo(kvpa, -1); });

            Assert.Throws<ArgumentException>(() => { environmentVariables.CopyTo(kvpa, 9); });

            Assert.Throws<ArgumentNullException>(() =>
            {
                KeyValuePair<string, string>[] kvpanull = null;
                environmentVariables.CopyTo(kvpanull, 0);
            });
        }
    }
}
        