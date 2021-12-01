// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
    internal abstract class SafeKeyHandle : SafeHandle
    {
        protected SafeKeyHandle()
            : base(IntPtr.Zero, true)
        {
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }

        internal abstract SafeKeyHandle DuplicateHandle();
    }
}
