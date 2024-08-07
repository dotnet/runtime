// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Formats.Nrbf;

[DebuggerDisplay("{Parent.RecordType}, {Allowed}, {PrimitiveType}")]
internal readonly struct NextInfo
{
    internal NextInfo(AllowedRecordTypes allowed, SerializationRecord parent,
        Stack<NextInfo> stack, PrimitiveType primitiveType = default)
    {
        Allowed = allowed;
        Parent = parent;
        Stack = stack;
        PrimitiveType = primitiveType;
    }

    internal AllowedRecordTypes Allowed { get; }

    internal SerializationRecord Parent { get; }

    internal Stack<NextInfo> Stack { get; }

    internal PrimitiveType PrimitiveType { get; }

    internal NextInfo With(AllowedRecordTypes allowed, PrimitiveType primitiveType)
        => allowed == Allowed && primitiveType == PrimitiveType
            ? this // previous record was of the same type
            : new(allowed, Parent, Stack, primitiveType);
}
