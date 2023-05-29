// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    /// <summary>
    /// Provides the info necessary for copying an attribute from user code to generated code.
    /// </summary>
    internal sealed record AttributeInfo(ManagedTypeInfo Type, SequenceEqualImmutableArray<string> Arguments)
    {
        internal AttributeSyntax GenerateSyntax()
        {
            return Attribute((NameSyntax)Type.Syntax, AttributeArgumentList(SeparatedList(Arguments.Select(arg => AttributeArgument(ParseExpression(arg))))));
        }
        internal AttributeListSyntax GenerateAttributeList()
        {
            return AttributeList(SingletonSeparatedList(GenerateSyntax()));
        }
        internal static AttributeInfo From(AttributeData attribute)
        {
            var type = ManagedTypeInfo.CreateTypeInfoForTypeSymbol(attribute.AttributeClass);
            var args = attribute.ConstructorArguments.Select(ca => ca.ToCSharpString());
            return new(type, args.ToSequenceEqualImmutableArray());
        }
    }
}
