// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System
{
    // ByReference<T> is meant to be used to represent "ref T" fields. It is working
    // around lack of first class support for byref fields in C# and IL. The JIT and 
    // type loader has special handling for it that turns it into a thin wrapper around ref T.
    [NonVersionable]
    internal
#if !PROJECTN // readonly breaks codegen contract and asserts UTC
    readonly
#endif
    ref struct ByReference<T>
    {
        // CS0169: The private field '{blah}' is never used
#pragma warning disable 169
         private readonly IntPtr _value;
#pragma warning restore

        [Intrinsic]
        public ByReference(ref T value)
        {
            // Implemented as a JIT intrinsic - This default implementation is for 
            // completeness and to provide a concrete error if called via reflection
            // or if intrinsic is missed.
            throw new PlatformNotSupportedException();
        }

        public ref T Value
        {
            [Intrinsic]
            get
            {
                // Implemented as a JIT intrinsic - This default implementation is for 
                // completeness and to provide a concrete error if called via reflection
                // or if the intrinsic is missed.
                throw new PlatformNotSupportedException();
            }
        }
    }
}
