// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime.InteropServices;

namespace SafeHandleTests
{
    public class TestSafeHandle : SafeHandle
    {
        public TestSafeHandle()
            : base(IntPtr.Zero, true)
        {}

        public TestSafeHandle(IntPtr handleValue)
            : this()
        {
            handle = handleValue;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        protected override bool ReleaseHandle()
        {
            handle = IntPtr.Zero;
            return true;
        }
    }

    public abstract class AbstractDerivedSafeHandle : SafeHandle
    {
        public AbstractDerivedSafeHandle()
            : base(IntPtr.Zero, true)
        {}
    }

    public class AbstractDerivedSafeHandleImplementation : AbstractDerivedSafeHandle
    {
        public AbstractDerivedSafeHandleImplementation()
            : base()
        {}

        public AbstractDerivedSafeHandleImplementation(IntPtr handleValue)
            : this()
        {
            handle = handleValue;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        protected override bool ReleaseHandle()
        {
            handle = IntPtr.Zero;
            return true;
        }

    }

    public class NoDefaultConstructorSafeHandle : SafeHandle
    {
        public NoDefaultConstructorSafeHandle(IntPtr handleValue)
            : base(IntPtr.Zero, true)
        {
            handle = handleValue;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        protected override bool ReleaseHandle()
        {
            handle = IntPtr.Zero;
            return true;
        }
    }
}
