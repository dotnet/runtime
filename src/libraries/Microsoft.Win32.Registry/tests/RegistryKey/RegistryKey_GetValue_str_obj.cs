// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using Microsoft.Win32;
using System;
using System.Threading;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_GetValue_str_obj : IDisposable
    {
        // Variables needed
        private RegistryKey _rk1, _rk2;
        private String _testKeyName = "BCL_TEST_4";
        private static int s_keyCount = 0;

        public void TestInitialize()
        {
            var counter = Interlocked.Increment(ref s_keyCount);
            _testKeyName += counter.ToString();
            _rk1 = Microsoft.Win32.Registry.CurrentUser;
            if (_rk1.OpenSubKey(_testKeyName) != null)
                _rk1.DeleteSubKeyTree(_testKeyName);
            if (_rk1.GetValue(_testKeyName) != null)
                _rk1.DeleteValue(_testKeyName);
            _rk2 = _rk1.CreateSubKey(_testKeyName);
        }

        public RegistryKey_GetValue_str_obj()
        {
            TestInitialize();
        }

        [Fact]
        public void Test01()
        {
            if (!_rk2.IsDefaultValueSet())
            {
                Assert.Equal(Helpers._DefaultValue, _rk2.GetValue(null, Helpers._DefaultValue));
                Assert.Equal(Helpers._DefaultValue, _rk2.GetValue(string.Empty, Helpers._DefaultValue));
            }

            Assert.True(_rk2.SetDefaultValue());
            Assert.Equal(Helpers._DefaultValue, _rk2.GetValue(null, null));
            Assert.Equal(Helpers._DefaultValue, _rk2.GetValue(string.Empty, null));
        }

        [Fact]
        public void Test02()
        {
            // [] Passing in null object not throw. You should be able to specify null as default
            //    object to return

            _rk1 = Microsoft.Win32.Registry.CurrentUser;
            if (_rk1.GetValue("tt", null) != null)
            {
                Assert.False(true, "Error null return expected");
            }
        }

        [Fact]
        public void Test03()
        {
            // [] Vanilla case , add a  bunch different objects (sampling all value types)

            Random rand = new Random(-55);

            var objArr = new Object[16];
            objArr[0] = ((Byte)(Byte)rand.Next(Byte.MinValue, Byte.MaxValue));
            objArr[1] = ((SByte)(SByte)rand.Next(SByte.MinValue, SByte.MaxValue));
            objArr[2] = ((Int16)(Int16)rand.Next(Int16.MinValue, Int16.MaxValue));
            objArr[3] = ((UInt16)(UInt16)rand.Next(UInt16.MinValue, UInt16.MaxValue));
            objArr[4] = ((Int32)rand.Next(Int32.MinValue, Int32.MaxValue));
            objArr[5] = ((UInt32)(UInt32)rand.Next((int)UInt32.MinValue, Int32.MaxValue));
            objArr[6] = ((Int64)(Int64)rand.Next(Int32.MinValue, Int32.MaxValue));
            objArr[7] = new Decimal(((Double)Decimal.MaxValue) * rand.NextDouble());
            objArr[8] = new Decimal(((Double)Decimal.MinValue) * rand.NextDouble());
            objArr[9] = new Decimal(((Double)Decimal.MinValue) * rand.NextDouble());
            objArr[10] = new Decimal(((Double)Decimal.MaxValue) * rand.NextDouble());
            objArr[11] = (Double)Int32.MaxValue * rand.NextDouble();
            objArr[12] = (Double)Int32.MinValue * rand.NextDouble();
            objArr[13] = (float)Int32.MaxValue * (float)rand.NextDouble();
            objArr[14] = (float)Int32.MinValue * (float)rand.NextDouble();
            objArr[15] = ((UInt64)(UInt64)rand.Next((int)UInt64.MinValue, Int32.MaxValue));


            for (int i = 0; i < objArr.Length; i++)
                _rk2.SetValue("Test_" + i.ToString(), objArr[i]);

            // [] Get the objects back and check them

            for (int i = 0; i <= objArr.Length; i++)
            {
                Object v = _rk2.GetValue("Test_" + i.ToString(), 13);
                if (i == objArr.Length)
                {
                    if (!v.ToString().Equals("13"))
                    {
                        Assert.False(true, "Error Expected==13, got value==" + v.ToString());
                    }
                }
                else
                {
                    if (!v.ToString().Equals(objArr[i].ToString()))
                    {
                        Assert.False(true, "Error Expected==" + objArr[i].ToString() + " , got value==" + v.ToString());
                    }
                }
            }
        }

        public void Dispose()
        {
            _rk1 = Microsoft.Win32.Registry.CurrentUser;
            if (_rk1.OpenSubKey(_testKeyName) != null)
                _rk1.DeleteSubKeyTree(_testKeyName);
            if (_rk1.GetValue(_testKeyName) != null)
                _rk1.DeleteValue(_testKeyName);
        }
    }
}