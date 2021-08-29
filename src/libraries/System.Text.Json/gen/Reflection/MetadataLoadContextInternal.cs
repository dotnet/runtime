// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace System.Text.Json.Reflection
{
    internal class MetadataLoadContextInternal
    {
        private readonly Compilation _compilation;

        public MetadataLoadContextInternal(Compilation compilation)
        {
            _compilation = compilation;
        }

        public Type? Resolve(Type type) => Resolve(type.FullName!);

        public Type? Resolve(string fullyQualifiedMetadataName)
        {
            INamedTypeSymbol? typeSymbol = _compilation.GetTypeByMetadataName(fullyQualifiedMetadataName);
            return typeSymbol.AsType(this);
        }

        public Type Resolve(SpecialType specialType)
        {
            INamedTypeSymbol? typeSymbol = _compilation.GetSpecialType(specialType);
            return typeSymbol.AsType(this);
        }
    }
}
