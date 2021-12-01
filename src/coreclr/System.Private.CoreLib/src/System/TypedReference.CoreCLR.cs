// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// TypedReference is basically only ever seen on the call stack, and in param arrays.
//  These are blob that must be dealt with by the compiler.

using System.Reflection;
using System.Runtime.CompilerServices;
using Internal.Runtime.CompilerServices;

namespace System
{
    [System.Runtime.Versioning.NonVersionable] // This only applies to field layout
    public ref partial struct TypedReference
    {
        private readonly ByReference<byte> _value;
        private readonly IntPtr _type;
    }
}
