// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Internal.TypeSystem
{
    /// <summary>
    /// This type represents a type that has one field in metadata,
    /// but has that field repeated at runtime to represent an array of elements inline.
    /// </summary>
    public sealed partial class TypeWithRepeatedFields : MetadataType
    {
        protected internal override int CompareToImpl(TypeDesc other, TypeSystemComparer comparer) => comparer.Compare(MetadataType, ((TypeWithRepeatedFields)other).MetadataType);
        protected internal override int ClassCode => 779393465;
    }
}
