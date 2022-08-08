// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Internal.TypeSystem
{
    // The following enum is required for interop with the VS Debugger
    // Prior to making any changes to this enum, please reach out to the VS Debugger
    // team to make sure that your changes are not going to prevent the debugger
    // from working.
    public enum WellKnownType
    {
        Unknown,

        // Primitive types are first - keep in sync with type flags
        Void,
        Boolean,
        Char,
        SByte,
        Byte,
        Int16,
        UInt16,
        Int32,
        UInt32,
        Int64,
        UInt64,
        IntPtr,
        UIntPtr,
        Single,
        Double,

        ValueType,
        Enum,
        Nullable,

        Object,
        String,
        Array,
        MulticastDelegate,

        RuntimeTypeHandle,
        RuntimeMethodHandle,
        RuntimeFieldHandle,

        Exception,

        TypedReference,
    }
}
