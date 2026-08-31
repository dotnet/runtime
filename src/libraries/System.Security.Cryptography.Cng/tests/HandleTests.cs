// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.Security.Cryptography.Cng.Tests
{
    public static class HandleTests
    {
        [Fact]
        public static void HandleDuplication()
        {
            using (CngKey key = CngKey.Import(TestData.Key_ECDiffieHellmanP256, CngKeyBlobFormat.GenericPublicBlob))
            {
                SafeNCryptKeyHandle keyHandle1 = key.Handle;
                SafeNCryptKeyHandle keyHandle2 = key.Handle;
                Assert.NotSame(keyHandle1, keyHandle2);

                keyHandle1.Dispose();
                keyHandle2.Dispose();

                // Make sure that disposing the spawned off handles didn't dispose the original. Set and get a custom property to ensure
                // the original is still in good condition.
                string propertyName = "Are you alive";
                bool hasProperty = key.HasProperty(propertyName, CngPropertyOptions.CustomProperty);
                Assert.False(hasProperty);

                byte[] propertyValue = { 1, 2, 3 };
                CngProperty property = new CngProperty(propertyName, propertyValue, CngPropertyOptions.CustomProperty);
                key.SetProperty(property);

                byte[] actualValue = key.GetProperty(propertyName, CngPropertyOptions.CustomProperty).GetValue();
                Assert.Equal<byte>(propertyValue, actualValue);
            }
        }

        [Fact]
        public static void SafeNCryptKeyHandle_ParentHandle_Invalid()
        {
            Assert.Throws<ArgumentNullException>(() => new SafeNCryptKeyHandle(IntPtr.Zero, null));

            using (SafeHandle openButInvalid = new StateInformingSafeHandle(false))
            using (SafeHandle closedAndInvalid = new StateInformingSafeHandle())
            using (SafeHandle closedButValid = new StateInformingSafeHandle(true, false))
            {
                closedAndInvalid.Dispose();
                closedButValid.Dispose();

                // Preconditions.
                Assert.False(openButInvalid.IsClosed, "openButInvalid.IsClosed");
                Assert.True(openButInvalid.IsInvalid, "openButInvalid.IsInvalid");
                Assert.True(closedButValid.IsClosed, "closedButValid.IsClosed");
                Assert.False(closedButValid.IsInvalid, "closedButValid.IsInvalid");
                Assert.True(closedAndInvalid.IsClosed, "closedAndInvalid.IsClosed");
                Assert.True(closedAndInvalid.IsInvalid, "closedAndInvalid.IsInvalid");

                // Tests
                AssertExtensions.Throws<ArgumentException>("parentHandle", () => new SafeNCryptKeyHandle(IntPtr.Zero, openButInvalid));
                AssertExtensions.Throws<ArgumentException>("parentHandle", () => new SafeNCryptKeyHandle(IntPtr.Zero, closedButValid));
                AssertExtensions.Throws<ArgumentException>("parentHandle", () => new SafeNCryptKeyHandle(IntPtr.Zero, closedAndInvalid));
            }
        }

        [Fact]
        public static void SafeNCryptKeyHandle_InvalidKey_ParentHandle_NotKeptAlive()
        {
            using (SafeHandle parentHandle = new StateInformingSafeHandle())
            using (var keyHandle = new SafeNCryptKeyHandle(IntPtr.Zero, parentHandle))
            {
                Assert.False(parentHandle.IsInvalid, "After ctor, parentHandle.IsInvalid");
                Assert.False(parentHandle.IsClosed, "After ctor, parentHandle.IsClosed");

                parentHandle.Dispose();

                Assert.True(parentHandle.IsInvalid, "After parentHandle.Dispose, parentHandle.IsInvalid");
                Assert.True(parentHandle.IsClosed, "After parentHandle.Dispose, parentHandle.IsClosed");
            }
        }

        [Fact]
        public static void SafeNCryptKeyHandle_ValidKey_ParentHandle_KeptAlive()
        {
            // NCryptFreeObject will check dwMagic values to determine what kind of object
            // it was given.  Since we don't really want to leak a key in this test we'll
            // track a test-local buffer.
            IntPtr fakeKeyPtr = Marshal.AllocHGlobal(512);
            try
            {
                Marshal.WriteInt32(fakeKeyPtr, 0);

                using (SafeHandle parentHandle = new StateInformingSafeHandle())
                {
                    using (var keyHandle = new SafeNCryptKeyHandle(fakeKeyPtr, parentHandle))
                    {
                        Assert.False(parentHandle.IsInvalid, "After ctor, parentHandle.IsInvalid");
                        Assert.False(parentHandle.IsClosed, "After ctor, parentHandle.IsClosed");

                        parentHandle.Dispose();

                        Assert.False(parentHandle.IsInvalid, "After parentHandle.Dispose, parentHandle.IsInvalid");
                        Assert.False(parentHandle.IsClosed, "After parentHandle.Dispose, parentHandle.IsClosed");
                    }

                    Assert.True(parentHandle.IsInvalid, "After keyHandle.Dispose, parentHandle.IsInvalid");
                    Assert.True(parentHandle.IsClosed, "After keyHandle.Dispose, parentHandle.IsClosed");
                }
            }
            finally
            {
                Marshal.FreeHGlobal(fakeKeyPtr);
            }
        }

        [Fact]
        public static void SafeNCryptKeyHandle_ValidKey_ParentHandle_DoesNotForciblyClose()
        {
            // NCryptFreeObject will check dwMagic values to determine what kind of object
            // it was given.  Since we don't really want to leak a key in this test we'll
            // track a test-local buffer.
            IntPtr fakeKeyPtr = Marshal.AllocHGlobal(512);
            try
            {
                Marshal.WriteInt32(fakeKeyPtr, 0);

                using (SafeHandle parentHandle = new StateInformingSafeHandle())
                {
                    using (var keyHandle = new SafeNCryptKeyHandle(fakeKeyPtr, parentHandle))
                    {
                    }

                    Assert.False(parentHandle.IsInvalid, "After keyHandle.Dispose, parentHandle.IsInvalid");
                    Assert.False(parentHandle.IsClosed, "After keyHandle.Dispose, parentHandle.IsClosed");
                }
            }
            finally
            {
                Marshal.FreeHGlobal(fakeKeyPtr);
            }
        }

        private class StateInformingSafeHandle : SafeHandle
        {
            private bool _isOpen;
            private readonly bool _invalidateOnClose;

            public StateInformingSafeHandle()
                : this(true)
            {
            }

            public StateInformingSafeHandle(bool isOpen)
                : this(isOpen, true)
            {
            }

            public StateInformingSafeHandle(bool isOpen, bool invalidateOnClose)
                : base(IntPtr.Zero, true)
            {
                _isOpen = isOpen;
                _invalidateOnClose = invalidateOnClose;
            }

            protected override bool ReleaseHandle()
            {
                if (_invalidateOnClose)
                    _isOpen = false;

                return true;
            }

            public override bool IsInvalid => !_isOpen;
        }
    }
}
