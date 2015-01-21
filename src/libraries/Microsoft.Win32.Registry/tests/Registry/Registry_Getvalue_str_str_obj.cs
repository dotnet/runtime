// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32;
using System;
using System.IO;
using System.Threading;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class Registry_GetValue_str_str_obj : IDisposable
    {
        private RegistryKey _rk1, _rk2;
        private static int s_keyCount = 0;
        private string _testKey = "CM1001_TEST";
        private string _keyString = "";

        // Variables needed
        private Object[] _objArr;
        private Random _rand = new Random(-55);

        public void TestInitialize()
        {
            var counter = Interlocked.Increment(ref s_keyCount);
            _testKey += counter.ToString();
            _rk1 = Microsoft.Win32.Registry.CurrentUser;
            if (_rk1.OpenSubKey(_testKey) != null)
                _rk1.DeleteSubKeyTree(_testKey);
            if (_rk1.GetValue(_testKey) != null)
                _rk1.DeleteValue(_testKey);

            //created the test key. if that failed it will cause many of the test scenarios below fail.
            try
            {
                _rk1 = Microsoft.Win32.Registry.CurrentUser;
                _rk2 = _rk1.CreateSubKey(_testKey);
                if (_rk2 == null)
                {
                    Assert.False(true, "ERROR: Could not create test subkey, this will fail a couple of scenarios below.");
                }
                else
                    _keyString = _rk2.ToString();
            }
            catch (Exception e)
            {
                Assert.False(true, "ERROR: unexpected exception, " + e.ToString());
            }
        }

        public Registry_GetValue_str_str_obj()
        {
            TestInitialize();
        }

        [Fact]
        public void TestGetValue01()
        {
            // [] Null arguments should throw
            Action a = () => { Microsoft.Win32.Registry.GetValue(null, null, null); };
            Assert.Throws<ArgumentNullException>(() => { a(); });
        }

        [Fact]
        public void Test02()
        {
            //Passing in null object not throw. You should be able to specify null as default
            //object to return
            try
            {
                if (Microsoft.Win32.Registry.GetValue(_keyString, "xzy", null) != null)
                {
                    Assert.False(true, "ERROR: null return expected");
                }
            }
            catch (Exception exc)
            {
                Assert.False(true, "ERROR: unexpected exception, " + exc.ToString());
            }
        }

        [Fact]
        public void Test03()
        {
            //passing a string which does NOT start with one of the valid base key names, that should throw ArgumentException.
            Action a = () => { Microsoft.Win32.Registry.GetValue("HHHH_MMMM", null, null); };
            Assert.Throws<ArgumentException>(() => { a(); });
        }

        [Fact]
        public void Test04()
        {
            // [] Add a  bunch different objects (sampling all value types)
            try
            {
                _objArr = new Object[16];
                _objArr[0] = ((Byte)(Byte)_rand.Next(Byte.MinValue, Byte.MaxValue));
                _objArr[1] = ((SByte)(SByte)_rand.Next(SByte.MinValue, SByte.MaxValue));
                _objArr[2] = ((Int16)(Int16)_rand.Next(Int16.MinValue, Int16.MaxValue));
                _objArr[3] = ((UInt16)(UInt16)_rand.Next(UInt16.MinValue, UInt16.MaxValue));
                _objArr[4] = ((Int32)_rand.Next(Int32.MinValue, Int32.MaxValue));
                _objArr[5] = ((UInt32)(UInt32)_rand.Next((int)UInt32.MinValue, Int32.MaxValue));
                _objArr[6] = ((Int64)(Int64)_rand.Next(Int32.MinValue, Int32.MaxValue));
                _objArr[7] = new Decimal(((Double)Decimal.MaxValue) * _rand.NextDouble());
                _objArr[8] = new Decimal(((Double)Decimal.MinValue) * _rand.NextDouble());
                _objArr[9] = new Decimal(((Double)Decimal.MinValue) * _rand.NextDouble());
                _objArr[10] = new Decimal(((Double)Decimal.MaxValue) * _rand.NextDouble());
                _objArr[11] = (Double)Int32.MaxValue * _rand.NextDouble();
                _objArr[12] = (Double)Int32.MinValue * _rand.NextDouble();
                _objArr[13] = (float)Int32.MaxValue * (float)_rand.NextDouble();
                _objArr[14] = (float)Int32.MinValue * (float)_rand.NextDouble();
                _objArr[15] = ((UInt64)(UInt64)_rand.Next((int)UInt64.MinValue, Int32.MaxValue));
                for (int i = 0; i < _objArr.Length; i++)
                    _rk2.SetValue("Test_" + i.ToString(), _objArr[i]);
                // [] Get the objects back and check them
                for (int i = 0; i <= _objArr.Length; i++)
                {
                    Object v = Microsoft.Win32.Registry.GetValue(_keyString, "Test_" + i.ToString(), 13);
                    if (i == _objArr.Length)
                    {
                        if (!v.ToString().Equals("13"))
                        {
                            Assert.False(true, "Error: Expected==13, got value==" + v.ToString());
                        }
                    }
                    else
                    {
                        if (!v.ToString().Equals(_objArr[i].ToString()))
                        {
                            Assert.False(true, "Error: Expected==" + _objArr[i].ToString() + " , got value==" + v.ToString());
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Assert.False(true, "ERROR: unexpected exception, " + e.ToString());
            }
        }

        [Fact]
        public void Test05()
        {
            //if the key does not exist, then the method should return null all time
            try
            {
                _keyString = Microsoft.Win32.Registry.CurrentUser.ToString() + "\\XYZ";
                if (Microsoft.Win32.Registry.GetValue(_keyString, null, -1) != null)
                {
                    Assert.False(true, string.Format("ERROR: unexpected value returned for key '{0}', expecting null, got '{1}'", _keyString, Microsoft.Win32.Registry.GetValue(_keyString, null, -1)));
                }
            }
            catch (Exception exc)
            {
                Assert.False(true, "ERROR: unexpected exception, " + exc.ToString());
            }
        }

        [Fact]
        public void Test06()
        {
            //For Code Coverage call GetValue on each of the hkeys
            try
            {
                int newValue = 11;
                int defaultValue = 0;
                Microsoft.Win32.Registry.CurrentUser.SetValue("Test1", newValue);
                if ((int)(Microsoft.Win32.Registry.GetValue("HKEY_CURRENT_USER", "Test1", defaultValue)) != newValue)
                {
                    Assert.False(true, "ERROR: Incorrect value read. Expected: " + newValue + " Actual: " + Microsoft.Win32.Registry.GetValue("HKEY_CURRENT_USER", "Test1", defaultValue));
                }
                Microsoft.Win32.Registry.CurrentUser.DeleteValue("Test1");
            }
            catch (Exception exc)
            {
                Assert.False(true, "ERROR: unexpected exception, " + exc.ToString());
            }
        }

        [Fact]
        public void Test07()
        {
            try
            {
                int newValue = 11;
                int defaultValue = 0;
                Registry.LocalMachine.SetValue("Test2", newValue);
                if ((int)(Microsoft.Win32.Registry.GetValue("HKEY_LOCAL_MACHINE", "Test2", defaultValue)) != newValue)
                {
                    Assert.False(true, "ERROR: Incorrect value read. Expected: " + newValue + " Actual: " + Microsoft.Win32.Registry.GetValue("HKEY_LOCAL_MACHINE", "Test2", defaultValue));
                }
                Registry.LocalMachine.DeleteValue("Test2");
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (Exception exc)
            {
                Assert.False(true, "ERROR: unexpected exception, " + exc.ToString());
            }
        }

        [Fact]
        public void Test08()
        {
            try
            {
                int newValue = 11;
                int defaultValue = 0;
                Registry.ClassesRoot.SetValue("Test3", newValue);
                if ((int)(Microsoft.Win32.Registry.GetValue("HKEY_CLASSES_ROOT", "Test3", defaultValue)) != newValue)
                {
                    Assert.False(true, "ERROR: Incorrect value read. Expected: " + newValue + " Actual: " + Microsoft.Win32.Registry.GetValue("HKEY_CLASSES_ROOT", "Test3", defaultValue));
                }
                Registry.ClassesRoot.DeleteValue("Test3");
            }
            catch (Exception exc)
            {
                Assert.False(true, "ERROR: unexpected exception, " + exc.ToString());
            }
        }

        [Fact]
        public void Test09()
        {
            try
            {
                int newValue = 11;
                int defaultValue = 0;
                Registry.Users.SetValue("Test4", newValue);
                if ((int)(Microsoft.Win32.Registry.GetValue("HKEY_USERS", "Test4", defaultValue)) != newValue)
                {
                    Assert.False(true, "ERROR: Incorrect value read. Expected: " + newValue + " Actual: " + Microsoft.Win32.Registry.GetValue("HKEY_USERS", "Test4", defaultValue));
                }
                Registry.Users.DeleteValue("Test4");
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (Exception exc)
            {
                Assert.False(true, "ERROR: unexpected exception, " + exc.ToString());
            }
        }

        [Fact]
        public void Test10()
        {
            try
            {
                int newValue = 11;
                int defaultValue = 0;
                Registry.PerformanceData.SetValue("Test5", newValue);
                if ((int)(Microsoft.Win32.Registry.GetValue("HKEY_PERFORMANCE_DATA", "Test5", defaultValue)) != newValue)
                {
                    Assert.False(true, "ERROR: Incorrect value read. Expected: " + newValue + " Actual: " + Microsoft.Win32.Registry.GetValue("HKEY_PERFORMANCE_DATA", "Test5", defaultValue));
                }
                Registry.PerformanceData.DeleteValue("Test5");
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (IOException)
            {
            }
            catch (Exception exc)
            {
                Assert.False(true, "ERROR: unexpected exception, " + exc.ToString());
            }
        }

        [Fact]
        public void Test11()
        {
            try
            {
                int newValue = 11;
                int defaultValue = 0;
                Registry.CurrentConfig.SetValue("Test6", newValue);
                if ((int)(Microsoft.Win32.Registry.GetValue("HKEY_CURRENT_CONFIG", "Test6", defaultValue)) != newValue)
                {
                    Assert.False(true, "ERROR: Incorrect value read. Expected: " + newValue + " Actual: " + Microsoft.Win32.Registry.GetValue("HKEY_CURRENT_CONFIG", "Test6", defaultValue));
                }
                Registry.CurrentConfig.DeleteValue("Test6");
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (Exception exc)
            {
                Assert.False(true, "ERROR: unexpected exception, " + exc.ToString());
            }
        }

        public void Dispose()
        {
            _rk1 = Microsoft.Win32.Registry.CurrentUser;
            if (_rk1.OpenSubKey(_testKey) != null)
                _rk1.DeleteSubKeyTree(_testKey);
            if (_rk1.GetValue(_testKey) != null)
                _rk1.DeleteValue(_testKey);
        }
    }
}