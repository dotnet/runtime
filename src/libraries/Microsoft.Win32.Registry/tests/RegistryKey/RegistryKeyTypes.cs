// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using Microsoft.Win32;
using System;
using System.Threading;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKeyTypes : IDisposable
    {
        private RegistryKey _rk;
        private string _testKeyName = "TEST_REGISTRYOPTIONS";
        private string _testVolatileKeyName = "TEST_VolatileKey";
        private string _testNonVolatileKeyName = "TEST_NonVolatileKey";
        private string _testRegularKey = "TEST_RegularKey";

        public void TestInitialize()
        {
            _rk = Registry.CurrentUser.OpenSubKey("Software", true);
            if (_rk.OpenSubKey(_testKeyName) != null)
                _rk.DeleteSubKeyTree(_testKeyName);
            if (_rk.OpenSubKey(_testVolatileKeyName) != null)
                _rk.DeleteSubKeyTree(_testVolatileKeyName);
            if (_rk.OpenSubKey(_testNonVolatileKeyName) != null)
                _rk.DeleteSubKeyTree(_testNonVolatileKeyName);
            if (_rk.OpenSubKey(_testRegularKey) != null)
                _rk.DeleteSubKeyTree(_testRegularKey);
        }

        public RegistryKeyTypes()
        {
            TestInitialize();
        }

        [Fact]
        public void RegistryOptionsTestsInvalid()
        {
            //Options is -1
            Action a = () =>
            {
                RegistryOptions options = (RegistryOptions)(-1);
                RegistryKey rk2 = _rk.CreateSubKey(_testKeyName, true, options);
            };

            Assert.Throws<ArgumentException>(() => { a(); });

            //Options is 3
            Action a1 = () =>
            {
                RegistryOptions options = (RegistryOptions)(3);
                RegistryKey rk2 = _rk.CreateSubKey(_testKeyName, true, options);
            };

            Assert.Throws<ArgumentException>(() => { a1(); });
        }

        [Fact]
        public void RegistryOptionsTestsValid()
        {
            //Create a Volatile Key
            try
            {
                RegistryKey rk2 = _rk.CreateSubKey(_testVolatileKeyName, true, RegistryOptions.Volatile);
                if (rk2 == null)
                {
                    Assert.False(true, "Create a Volatile Key - did not create the key");
                }
                else
                {
                    _rk.DeleteSubKey(_testVolatileKeyName);
                }
            }
            catch (Exception e)
            {
                Assert.False(true, string.Format("Unexpected Exception: {0}", e));
            }

            //Create a Volatile Key that already exists non-Volatile
            try
            {
                _rk.CreateSubKey(_testNonVolatileKeyName);
                RegistryKey rk2 = _rk.CreateSubKey(_testNonVolatileKeyName, true, RegistryOptions.Volatile);
                if (rk2 == null)
                {
                    Assert.False(true, "Create a Volatile Key that already exists non-Volatile - did not create the key");
                }
                else
                {
                    _rk.DeleteSubKey(_testNonVolatileKeyName);
                }
            }
            catch (Exception e)
            {
                Assert.False(true, string.Format("Unexpected Exception: {0}", e));
            }

            //Create a Regular Key
            try
            {
                RegistryKey rk2 = _rk.CreateSubKey(_testRegularKey, true, RegistryOptions.None);
                if (rk2 == null)
                {
                    Assert.False(true, "Create a Regular Key - did not create the key");
                }
                else
                {
                    _rk.DeleteSubKey(_testRegularKey);
                }
            }
            catch (Exception e)
            {
                Assert.False(true, string.Format("Unexpected Exception: {0}", e));
            }

            //Create a Regular Key that already exists Volatile
            try
            {
                _rk.CreateSubKey(_testVolatileKeyName, true, RegistryOptions.Volatile);
                RegistryKey rk2 = _rk.CreateSubKey(_testVolatileKeyName, true, RegistryOptions.None);
                if (rk2 == null)
                {
                    Assert.False(true, "Create a Regular Key that already exists Volatile - did not create the key");
                }
                else
                {
                    _rk.DeleteSubKey(_testVolatileKeyName);
                }
            }
            catch (Exception e)
            {
                Assert.False(true, string.Format("Unexpected Exception: {0}", e));
            }
        }

        [Fact]
        public void ValueKindNoneTests()
        {
            //Set a None ValueKind
            try
            {
                RegistryKey rk2 = _rk.CreateSubKey(_testRegularKey, true, RegistryOptions.None);
                if (rk2 == null)
                {
                    Assert.False(true, "Set a None ValueKind - did not create the key");
                }
                else
                {
                    rk2.SetValue("NoneKind", new byte[] { (byte)23, (byte)32 }, RegistryValueKind.None);
                }
            }
            catch (Exception e)
            {
                Assert.False(true, string.Format("Unexpected Exception: {0}", e));
            }

            //Get a None ValueKind
            try
            {
                RegistryKey rk2 = _rk.CreateSubKey(_testRegularKey, true, RegistryOptions.None);
                if (rk2 == null)
                {
                    Assert.False(true, "Get a None ValueKind - did not create the key");
                }
                else
                {
                    if (rk2.GetValueKind("NoneKind") != RegistryValueKind.None)
                    {
                        Assert.False(true, "Get a None ValueKind - Wrong ValueKind returned");
                    }
                    else
                    {
                        byte[] value = (byte[])rk2.GetValue("NoneKind");
                        if ((value.Length != 2) || (value[0] != 23) || (value[1] != 32))
                        {
                            Assert.False(true, "Get a None ValueKind - Wrong value returned");
                        }
                        else
                        {
                            _rk.DeleteSubKey(_testRegularKey);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Assert.False(true, string.Format("Unexpected Exception: {0}", e));
            }
        }

        public void Dispose()
        {
            // cleanup
            _rk = Registry.CurrentUser.OpenSubKey("Software", true);
            if (_rk.OpenSubKey(_testKeyName) != null)
                _rk.DeleteSubKeyTree(_testKeyName);
            if (_rk.OpenSubKey(_testVolatileKeyName) != null)
                _rk.DeleteSubKeyTree(_testVolatileKeyName);
            if (_rk.OpenSubKey(_testNonVolatileKeyName) != null)
                _rk.DeleteSubKeyTree(_testNonVolatileKeyName);
            if (_rk.OpenSubKey(_testRegularKey) != null)
                _rk.DeleteSubKeyTree(_testRegularKey);
        }
    }
}