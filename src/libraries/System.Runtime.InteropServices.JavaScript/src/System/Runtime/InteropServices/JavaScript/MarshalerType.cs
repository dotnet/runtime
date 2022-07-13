﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.JavaScript
{
    // maps to names of System.Runtime.InteropServices.JavaScript.JSMarshalerType
    // please sync with src\mono\wasm\runtime\marshal.ts
    internal enum MarshalerType : int
    {
        None = 0,
        Void = 1,
        Discard,
        Boolean,
        Byte,
        Char,
        Int16,
        Int32,
        Int52,
        BigInt64,
        Double,
        Single,
        IntPtr,
        JSObject,
        Object,
        String,
        Exception,
        DateTime,
        DateTimeOffset,

        Nullable,
        Task,
        Array,
        ArraySegment,
        Span,
        Action,
        Function,

#if !JSIMPORTGENERATOR
        // only on runtime
        JSException,
#endif
    }
}
