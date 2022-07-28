// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Reflection;
using Microsoft.CodeAnalysis;

namespace System.Text.Json.SourceGeneration
{
    internal sealed class SourceGenerationSpec
    {
        public List<ContextGenerationSpec> ContextGenerationSpecList { get; init; }

#if DEBUG
        public MetadataLoadContextInternal MetadataLoadContext { get; init; }
#endif
        public Type BooleanType { get; init; }
        public Type ByteArrayType { get; init; }
        public Type CharType { get; init; }
        public Type DateTimeType { private get; init; }
        public Type DateTimeOffsetType { private get; init; }
        public Type GuidType { private get; init; }
        public Type StringType { private get; init; }

        public HashSet<Type> NumberTypes { private get; init; }

        public bool IsStringBasedType(Type type)
            => type == StringType || type == DateTimeType || type == DateTimeOffsetType || type == GuidType;

        public bool IsNumberType(Type type) => NumberTypes.Contains(type);
    }
}
