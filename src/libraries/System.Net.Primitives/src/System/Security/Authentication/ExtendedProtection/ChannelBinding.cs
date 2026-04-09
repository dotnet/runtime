// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Authentication.ExtendedProtection
{
    public abstract class ChannelBinding : SafeHandleZeroOrMinusOneIsInvalid
    {
        protected ChannelBinding()
            : base(true)
        {
        }

        protected ChannelBinding(bool ownsHandle)
            : base(ownsHandle)
        {
        }

        public abstract int Size
        {
            get;
        }
    }
}
