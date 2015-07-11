// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Xunit;

namespace System.Diagnostics.ProcessTests
{
    public class ProcessStartInfoTests : ProcessTestBase
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

            Assert.Equal(global::Interop.IsWindows, environment.ContainsKey("newkey"));
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
            Assert.Equal(global::Interop.IsWindows, environment.Contains(new KeyValuePair<string, string>("nEwKeY", "NewValue")));
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

            if (global::Interop.IsWindows)
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
            Assert.Equal(global::Interop.IsWindows, environment.Contains(new KeyValuePair<string, string>("NEWKeY2", "NewValue2")));

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
        public void TestUseShellExecuteProperty()
        {
            ProcessStartInfo psi = new ProcessStartInfo();

            // Calling the setter
            psi.UseShellExecute = false;
            Assert.Throws<PlatformNotSupportedException>(() => { psi.UseShellExecute = true; });

            // Calling the getter
            Assert.False(psi.UseShellExecute, "UseShellExecute=true is not supported on onecore.");
        }
    }
}
