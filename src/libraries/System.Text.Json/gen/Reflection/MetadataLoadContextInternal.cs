// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;

namespace System.Text.Json.Reflection
{
    internal sealed class MetadataLoadContextInternal
    {
        private readonly Compilation _compilation;

        public MetadataLoadContextInternal(Compilation compilation)
        {
            _compilation = compilation;
        }

        public Compilation Compilation => _compilation;

        public Type? Resolve(Type type)
        {
            Debug.Assert(!type.IsArray, "Resolution logic only capable of handling named types.");
            return Resolve(type.FullName!);
        }

        public Type? Resolve(string fullyQualifiedMetadataName)
        {
            INamedTypeSymbol? typeSymbol = _compilation.GetBestTypeByMetadataName(fullyQualifiedMetadataName);
            return typeSymbol.AsType(this);
        }

        public Type Resolve(SpecialType specialType)
        {
            INamedTypeSymbol? typeSymbol = _compilation.GetSpecialType(specialType);
            return typeSymbol.AsType(this);
        }
    }
}
