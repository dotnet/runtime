// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System
{
    // ByReference<T> is meant to be used to represent "ref T" fields. It is working
    // around lack of first class support for byref fields in C# and IL. The JIT and 
    // type loader has special handling for it that turns it into a thin wrapper around ref T.
    internal struct ByReference<T>
    {
        private IntPtr _value;

        public ByReference(ref T value)
        {
            // Implemented as a JIT intrinsic - This default implementation is for 
            // completeness and to provide a concrete error if called via reflection
            // or if intrinsic is missed.
            throw new System.PlatformNotSupportedException();
        }

        public ref T Value
        {
            get
            {
                // Implemented as a JIT intrinsic - This default implementation is for 
                // completeness and to provide a concrete error if called via reflection
                // or if the intrinsic is missed.
                throw new System.PlatformNotSupportedException();
            }
        }
    }
}
