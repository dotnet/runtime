// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using Microsoft.Win32;
using System;
using System.Threading;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_DeleteValue_Str_Bln : IDisposable
    {
        // Variables needed
        private RegistryKey rk1, rk2;
        private String _testKeyName = "REG_TEST_6";
        private String _testKeyName2 = "BCL_TEST_1";
        private static int s_keyCount = 0;
        
        public void TestInitialize()
        {
            var counter = Interlocked.Increment(ref s_keyCount);
            _testKeyName += counter.ToString();
            _testKeyName2 += counter.ToString();
            rk1 = Microsoft.Win32.Registry.CurrentUser;
            if (rk1.OpenSubKey(_testKeyName2) != null)
                rk1.DeleteSubKeyTree(_testKeyName2);
            if (rk1.GetValue(_testKeyName2) != null)
                rk1.DeleteValue(_testKeyName2, false);

            if (rk1.GetValue(_testKeyName) != null)
                rk1.DeleteValue(_testKeyName);
            if (rk1.OpenSubKey(_testKeyName) != null)
                rk1.DeleteSubKeyTree(_testKeyName);
        }

        public RegistryKey_DeleteValue_Str_Bln()
        {
            TestInitialize();
        }

        [Fact]
        public void Test01()
        {
            // Check for ArgumentNullException

            rk2 = rk1.CreateSubKey(_testKeyName2);
            try
            {
                rk2.DeleteValue(null, false);
            }
            catch (Exception exc)
            {
                Assert.False(true, "Error NO exception expected , got exc==" + exc.ToString());
            }
        }

        [Fact]
        public void Test02()
        {
            rk2 = rk1.CreateSubKey(_testKeyName2);
            Action a = () => {rk2.DeleteValue(null, true);  };
            Assert.Throws<ArgumentException>(() => { a(); });
        }

        [Fact]
        public void Test03()
        {
            // [] Vanilla case, deleting a value

            rk2 = rk1.CreateSubKey(_testKeyName2);
            if (rk2.GetValueNames().Length != 0)
            {
                Assert.False(true, "Error Incorrect count in subkey");
            }
            rk2.SetValue(_testKeyName2, 5);
            if (rk2.GetValueNames().Length != 1)
            {
                Assert.False(true, "Error Incorrect count in subkey");
            }
            if (!rk2.GetValueNames()[0].Equals(_testKeyName2))
            {
                Assert.False(true, "Error Incorrect name of value , name==" + rk2.GetValueNames()[0]);
            }
            rk2.DeleteValue(_testKeyName2, true);
            if (rk2.GetValueNames().Length != 0)
            {
                Assert.False(true, "Error Incorrect count in subkey");
            }
        }

        [Fact]
        public void Test04()
        {
            // [] Vanilla case , add a  bunch different objects and then Delete them

            rk2 = rk1.CreateSubKey(_testKeyName2);

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
                rk2.SetValue("Test_" + i.ToString(), objArr[i]);

            if (rk2.ValueCount != objArr.Length)
            {
                Assert.False(true, "Error expected==" + objArr.Length + " , got value==" + rk2.ValueCount);
            }

            for (int i = 0; i < objArr.Length; i++)
                rk2.DeleteValue("Test_" + i.ToString(), false);

            if (rk2.ValueCount != 0)
            {
                Assert.False(true, "Error Values not deleted correctly");
            }
            // Try to grab some random value that should be deleted
            if (rk2.GetValue("Test_8") != null)
            {
                Assert.False(true, "Error Got a value that should have been deleted");
            }
        }

        [Fact]
        public void Test05()
        {
            //[] check for throwing
            rk2 = rk1.CreateSubKey(_testKeyName);

            //we will try to delete a non-existing sub key in rk2
            try
            {
                rk2.DeleteValue("ThisDoesNotExist", false);
            }
            catch (Exception ex)
            {
                Assert.False(true, "Error Unexpected expected , got exc==" + ex.GetType().Name);
            }
        }

        [Fact]
        public void Test06()
        {
            rk2 = rk1.CreateSubKey(_testKeyName);
            //we will try to delete a non-existing sub key in rk2
            Action a = () => { rk2.DeleteValue("ThisDoesNotExist", true); };
            Assert.Throws<ArgumentException>(() => { a(); });
        }

        public void Dispose()
        {
            if (rk1.OpenSubKey(_testKeyName2) != null)
                rk1.DeleteSubKeyTree(_testKeyName2);
            if (rk1.GetValue(_testKeyName2) != null)
                rk1.DeleteValue(_testKeyName2, false);

            if (rk1.GetValue(_testKeyName) != null)
                rk1.DeleteValue(_testKeyName);
            if (rk1.OpenSubKey(_testKeyName) != null)
                rk1.DeleteSubKeyTree(_testKeyName);
        }
    }
}