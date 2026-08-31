// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Interop.JavaScript
{
    internal enum KnownManagedType : int
    {
        None = 0,
        Void = 1,
        Boolean,
        Byte,
        Char,
        Int16,
        Int32,
        Int64,
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

        Unknown,
    }
}
