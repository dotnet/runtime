// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Authentication.ExtendedProtection
{
    // NOTE: this does not inherit from the definition of SafeHandleZeroOrMinusOneIsInvalid
    // from $(CommonPath)/Microsoft/Win32/SafeHandles/SafeHandleZeroOrMinusOneIsInvalid because
    // that type is internal.
    public abstract class ChannelBinding : SafeHandle
    {
        protected ChannelBinding()
            : base(IntPtr.Zero, true)
        {
        }

        protected ChannelBinding(bool ownsHandle)
            : base(IntPtr.Zero, ownsHandle)
        {
        }

        public abstract int Size
        {
            get;
        }

        // Copied from SafeHandleZeroOrMinusOneIsInvalid
        public override bool IsInvalid
        {
            get { return handle == new IntPtr(0) || handle == new IntPtr(-1); }
        }
    }
}
