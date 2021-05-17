// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace System.Text.Json.SourceGeneration
{
    internal class SourceGenerationSpec
    {
        public List<ContextGenerationSpec> ContextGenerationSpecList { get; init; }

        #region Known types
        public Type BooleanType { get; init; }
        public Type ByteArrayType { get; init; }
        public Type CharType { get; init; }
        public Type DateTimeType { get; init; }
        public Type DateTimeOffsetType { get; init; }
        public Type GuidType { get; init; }
        public Type StringType { get; init; }

        public HashSet<Type> NumberTypes { private get; init; }

        public bool IsStringBasedType(Type type)
            => type == StringType || type == DateTimeType || type == DateTimeOffsetType || type == GuidType;

        public bool IsNumberType(Type type) => NumberTypes.Contains(type);
        #endregion
    }
}
