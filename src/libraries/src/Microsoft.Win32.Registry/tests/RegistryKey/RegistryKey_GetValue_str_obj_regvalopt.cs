// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_GetValue_str_obj_regvalopt : IDisposable
    {
        // Variables needed
        private RegistryKey _rk1, _rk2;
        private String _testKeyName2 = "BCL_TEST_6";
        private static int s_keyCount = 0;

        public void TestInitialize()
        {
            var counter = Interlocked.Increment(ref s_keyCount);
            _testKeyName2 += counter.ToString();
            _rk1 = Microsoft.Win32.Registry.CurrentUser;
            CleanRegistryKeys();
        }

        public RegistryKey_GetValue_str_obj_regvalopt()
        {
            TestInitialize();
        }

        [Fact]
        public void Test02()
        {
            //passing negative value for the enum should throw
            Action a = () => { Object obj = _rk1.GetValue(null, null, (RegistryValueOptions)(-1)); };
            Assert.Throws<ArgumentException>(() => { a(); });
        }

        [Fact]
        public void Test03()
        {
            Action a = () => { Object obj = _rk1.GetValue(null, null, (RegistryValueOptions)2); };
            Assert.Throws<ArgumentException>(() => { a(); });
        }

        [Fact]
        public void Test04()
        {
            // [] Passing in null object not throw. You should be able to specify null as default object to return
            try
            {
                if (_rk1.GetValue("tt", null, RegistryValueOptions.DoNotExpandEnvironmentNames) != null)
                {
                    Assert.False(true, "Error null return expected");
                }
            }
            catch (Exception e)
            {
                Assert.False(true, "Error Unexpected exception occured.... exception message...:" + e.ToString());
            }
        }

        [Fact]
        public void Test06()
        {
            // [] Pass name=Existing key, default value = null 
            String strTest = "This is a test string";
            _rk2 = _rk1.CreateSubKey(_testKeyName2);
            try
            {
                string strKey = "MyTestKey";
                _rk2.SetValue(strKey, strTest, RegistryValueKind.ExpandString);
                Object obj1 = _rk2.GetValue(strKey, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                if (obj1.ToString() != strTest)
                {
                    Assert.False(true, "Error null return expected");
                }
            }
            catch (Exception e)
            {
                Assert.False(true, "Error Unexpected exception occured.... exception message...:" + e.ToString());
            }
        }

        [Fact]
        public void Test07()
        {
            _rk2 = _rk1.CreateSubKey(_testKeyName2);
            // [] Make sure NoExpand = false works with some valid values.
            string[] strTestValues = new string[] { @"%Systemroot%\mydrive\mydirectory\myfile.xxx", @"%tmp%\gfdhghdfgk\fsdfds\dsd.yyy", @"%path%\rwerew.zzz", @"%Systemroot%\mydrive\%path%\myfile.xxx" };
            string[] strExpectedTestValues = new string[] { Environment.ExpandEnvironmentVariables("%Systemroot%") + @"\mydrive\mydirectory\myfile.xxx", Environment.ExpandEnvironmentVariables("%tmp%") + @"\gfdhghdfgk\fsdfds\dsd.yyy", Environment.ExpandEnvironmentVariables("%path%") + @"\rwerew.zzz", Environment.ExpandEnvironmentVariables("%Systemroot%") + @"\mydrive\" + Environment.ExpandEnvironmentVariables("%path%") + @"\myfile.xxx" };

            try
            {
                for (int iLoop = 0; iLoop < strTestValues.Length; iLoop++)
                {
                    string strKey = "MyTestKey" + iLoop.ToString();
                    _rk2.SetValue(strKey, strTestValues[iLoop], RegistryValueKind.ExpandString);
                    Object obj1 = _rk2.GetValue(strKey, strTestValues[iLoop], RegistryValueOptions.None);
                    if (obj1.ToString() != strExpectedTestValues[iLoop])
                    {
                        Assert.False(true, string.Format("Error GetValue retried unexpected value.... Expected...:{0}, Actual...:{1}", strExpectedTestValues[iLoop], obj1.ToString()));
                    }
                }
            }
            catch (Exception e)
            {
                Assert.False(true, "Error Unexpected exception occured.... exception message...:" + e.ToString());
            }
        }

        [Fact]
        public void Test09()
        {
            _rk2 = _rk1.CreateSubKey(_testKeyName2);
            //Set some new environment variables and make sure the
            string[] strMyNewEnvVariables = new string[] { "MyEnv", "PathPath", "Name", "blah", "TestKEyyyyyyyyyyyyyy" };
            try
            {
                for (int iLoop = 0; iLoop < strMyNewEnvVariables.Length; iLoop++)
                {
                    SetEnvironmentVariable(strMyNewEnvVariables[iLoop], @"C:\UsedToBeCurrentDirectoryButAnythingWorks");
                    string strKey = "MyNewKey" + iLoop.ToString();
                    string strValue = "%" + strMyNewEnvVariables[iLoop] + "%" + @"\subdirectory\myfile.txt";
                    _rk2.SetValue(strKey, strValue, RegistryValueKind.ExpandString);
                    Object obj1 = _rk2.GetValue(strKey, string.Empty, RegistryValueOptions.DoNotExpandEnvironmentNames);
                    if (obj1.ToString() != strValue)
                    {
                        Assert.False(true, string.Format("Error GetValue retried unexpected value.... Expected...:{0}, Actual...:{1}", strValue, obj1.ToString()));
                    }
                }
            }
            catch (Exception e)
            {
                Assert.False(true, "Error Unexpected exception occured.... exception message...:" + e.ToString());
            }
        }

        [DllImport("api-ms-win-core-processenvironment-l1-1-0.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetEnvironmentVariable(string lpName, string lpValue);

        [Fact]
        public void Test10()
        {
            // [] Vanilla case , add a bunch of different objects (sampling all value types)
            Random rand = new Random(42);
            List<object> al = new List<object>();
            al.Add((Byte)rand.Next(Byte.MinValue, Byte.MaxValue));
            al.Add((SByte)rand.Next(SByte.MinValue, SByte.MaxValue));
            al.Add((Int16)rand.Next(Int16.MinValue, Int16.MaxValue));
            al.Add((UInt16)rand.Next(UInt16.MinValue, UInt16.MaxValue));
            al.Add(rand.Next(Int32.MinValue, Int32.MaxValue));
            al.Add((UInt32)rand.Next((int)UInt32.MinValue, Int32.MaxValue));
            al.Add((Int64)rand.Next(Int32.MinValue, Int32.MaxValue));
            al.Add(new Decimal(((Double)Decimal.MaxValue) * rand.NextDouble()));
            al.Add(new Decimal(((Double)Decimal.MinValue) * rand.NextDouble()));
            al.Add(new Decimal(((Double)Decimal.MinValue) * rand.NextDouble()));
            al.Add(new Decimal(((Double)Decimal.MaxValue) * rand.NextDouble()));
            al.Add((Double)Int32.MaxValue * rand.NextDouble());
            al.Add((Double)Int32.MinValue * rand.NextDouble());
            al.Add((float)Int32.MaxValue * (float)rand.NextDouble());
            al.Add((float)Int32.MinValue * (float)rand.NextDouble());
            al.Add(((UInt64)(UInt64)rand.Next((int)UInt64.MinValue, Int32.MaxValue)));
            al.Add(@"%Systemroot%\mydrive\mydirectory\myfile.xxx");
            al.Add(@"%tmp%\mydrive\mydirectory\myfile.xxx");
            al.Add(@"%path%\mydrive\mydirectory\myfile.xxx");
            al.Add(@"%Systemroot%\myblah\%username%\mydrive\mydirectory\myfile.xxx");
            al.Add(@"%path%\mydrive\%tmp%\myfile.xxx");

            _rk2 = _rk1.CreateSubKey(_testKeyName2);
            try
            {
                IDictionary strEnvVariables = Environment.GetEnvironmentVariables();
                IEnumerator e = (IEnumerator)strEnvVariables.GetEnumerator();
                while (e.MoveNext())
                {
                    al.Add(((DictionaryEntry)e.Current).Value);
                }

                foreach (Object obj in al)
                    _rk2.SetValue("Test_" + al.IndexOf(obj).ToString(), obj.ToString(), RegistryValueKind.ExpandString);

                // [] Get the objects back and check them
                foreach (Object obj in al)
                {
                    Object v = _rk2.GetValue("Test_" + al.IndexOf(obj).ToString(), 13, RegistryValueOptions.DoNotExpandEnvironmentNames);
                    if (!v.ToString().Equals(obj.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        Assert.False(true, "Error Expected==" + obj.ToString() + " , got value==" + v.ToString());
                    }
                }
            }
            catch (Exception e)
            {
                Assert.False(true, "Error Unexpected exception occured.... exception message...:" + e.ToString());
            }
        }

        private void CleanRegistryKeys()
        {
            try
            {
                RegistryKey rk1 = Microsoft.Win32.Registry.CurrentUser;
                if (rk1.OpenSubKey(_testKeyName2) != null)
                    rk1.DeleteSubKeyTree(_testKeyName2);
                if (rk1.GetValue(_testKeyName2) != null)
                    rk1.DeleteValue(_testKeyName2);
            }
            catch (Exception e)
            {
                Assert.False(true, "Unexpected exception occured in CleanRegistryKeys method... exception message...:" + e.Message);
            }
        }

        public void Dispose()
        {
            CleanRegistryKeys();
        }
    }
}