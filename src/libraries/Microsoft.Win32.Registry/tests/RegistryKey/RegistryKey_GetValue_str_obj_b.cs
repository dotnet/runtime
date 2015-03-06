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
    public class RegistryKey_GetValue_str_obj_b : IDisposable
    {
        // Variables needed
        private RegistryKey _rk1, _rk2;
        private String _strTest = "This is a test string";
        private String _testKeyName2 = "BCL_TEST_5";
        private static int s_keyCount = 0;

        public void TestInitialize()
        {
            var counter = Interlocked.Increment(ref s_keyCount);
            _testKeyName2 += counter.ToString();
            _rk1 = Microsoft.Win32.Registry.CurrentUser;
            //Make sure we don't have registry key that we create and test here
            CleanRegistryKeys();
        }

        public RegistryKey_GetValue_str_obj_b()
        {
            TestInitialize();
        }

        [Fact]
        public void Test01()
        {
            // [] Null arguments should be ignored
            try
            {
#pragma warning disable  0618
                Object obj = _rk1.GetValue(null, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
#pragma warning restore  0618
                if (obj != null)
                {
                    Assert.False(true, "Error Key value is incorrect...");
                }
            }
            catch (Exception e)
            {
                Assert.False(true, "Error Unexpected exception occured , got exc==" + e.ToString());
            }
        }

        [Fact]
        public void Test02()
        {
            // [] Passing in null object not throw. You should be able to specify null as default object to return
            try
            {
#pragma warning disable  0618
                if (_rk1.GetValue("tt", null, RegistryValueOptions.DoNotExpandEnvironmentNames) != null)
#pragma warning restore  0618
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
        public void Test03()
        {
            // [] Pass name=string.Empty
            try
            {
#pragma warning disable  0618
                Object obj1 = _rk1.GetValue(String.Empty, _strTest, RegistryValueOptions.DoNotExpandEnvironmentNames);
#pragma warning restore  0618
                if (obj1.ToString() != _strTest)
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
        public void Test04()
        {
            // [] Pass name=Existing key, default value = null 
            _rk2 = _rk1.CreateSubKey(_testKeyName2);
            try
            {
                string strKey = "MyTestKey";
                _rk2.SetValue(strKey, _strTest, RegistryValueKind.ExpandString);
#pragma warning disable  0618
                Object obj1 = _rk2.GetValue(strKey, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
#pragma warning restore  0618
                if (obj1.ToString() != _strTest)
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
        public void Test05()
        {
            // [] Pass name=null, default value = some value 
            _rk2 = _rk1.CreateSubKey(_testKeyName2);
            string strKey = "MyTestKey";

            try
            {
                _rk2.SetValue(strKey, _strTest, RegistryValueKind.ExpandString);
#pragma warning disable  0618
                Object obj1 = _rk2.GetValue(null, _strTest, RegistryValueOptions.DoNotExpandEnvironmentNames);
#pragma warning restore  0618
                if (obj1.ToString() != _strTest)
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
            // [] Make sure NoExpand = false works with some valid values.
            string[] strTestValues = new string[] { @"%Systemroot%\mydrive\mydirectory\myfile.xxx", @"%tmp%\gfdhghdfgk\fsdfds\dsd.yyy", @"%path%\rwerew.zzz", @"%Systemroot%\mydrive\%path%\myfile.xxx" };
            string[] strExpectedTestValues = new string[] { Environment.ExpandEnvironmentVariables("%Systemroot%") + @"\mydrive\mydirectory\myfile.xxx", Environment.ExpandEnvironmentVariables("%tmp%") + @"\gfdhghdfgk\fsdfds\dsd.yyy", Environment.ExpandEnvironmentVariables("%path%") + @"\rwerew.zzz", Environment.ExpandEnvironmentVariables("%Systemroot%") + @"\mydrive\" + Environment.ExpandEnvironmentVariables("%path%") + @"\myfile.xxx" };

            _rk2 = _rk1.CreateSubKey(_testKeyName2);
            try
            {
                for (int iLoop = 0; iLoop < strTestValues.Length; iLoop++)
                {
                    string strKey = "MyTestKey" + iLoop.ToString();
                    _rk2.SetValue(strKey, strTestValues[iLoop], RegistryValueKind.ExpandString);
#pragma warning disable  0618
                    Object obj1 = _rk2.GetValue(strKey, strTestValues[iLoop], RegistryValueOptions.None);
#pragma warning restore  0618
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
        public void Test07()
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
#pragma warning disable  0618
                    Object obj1 = _rk2.GetValue(strKey, string.Empty, RegistryValueOptions.DoNotExpandEnvironmentNames);
#pragma warning restore  0618
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
        public void Test08()
        {
            // [] Vanilla case , add a bunch of different objects (sampling all value types)
            Random rand = new Random(-55);
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
                    _rk2.SetValue("Test_" + obj.ToString(), obj.ToString(), RegistryValueKind.ExpandString);

                // [] Get the objects back and check them
                foreach (Object obj in al)
                {
#pragma warning disable  0618
                    Object v = _rk2.GetValue("Test_" + obj.ToString(), 13, RegistryValueOptions.DoNotExpandEnvironmentNames);
#pragma warning restore  0618
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
                _rk1 = Microsoft.Win32.Registry.CurrentUser;
                if (_rk1.OpenSubKey(_testKeyName2) != null)
                    _rk1.DeleteSubKeyTree(_testKeyName2);
                if (_rk1.GetValue(_testKeyName2) != null)
                    _rk1.DeleteValue(_testKeyName2);
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