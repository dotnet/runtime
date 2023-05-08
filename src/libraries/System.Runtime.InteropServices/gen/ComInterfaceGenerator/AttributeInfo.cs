// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.Interop
{
    /// <summary>
    /// Provides the info necessary for copying an attribute from user code to generated code.
    /// </summary>
    internal sealed record AttributeInfo(ManagedTypeInfo Type, SequenceEqualImmutableArray<string> Arguments)
    {
        internal static AttributeInfo From(AttributeData attribute)
        {
            var type = ManagedTypeInfo.CreateTypeInfoForTypeSymbol(attribute.AttributeClass);
            var args = attribute.ConstructorArguments.Select(ca => ca.ToCSharpString());
            return new(type, args.ToSequenceEqualImmutableArray());
        }
    }
}
