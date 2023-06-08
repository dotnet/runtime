// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Interop.JavaScript
{
    internal abstract record JSTypeInfo(KnownManagedType KnownType)
    {
        public static JSTypeInfo CreateJSTypeInfoForTypeSymbol(ITypeSymbol type)
        {
            string fullTypeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            switch (type)
            {
                case { SpecialType: SpecialType.System_Void }:
                    return new JSSimpleTypeInfo(KnownManagedType.Void)
                    {
                        Syntax = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword))
                    };
                case { SpecialType: SpecialType.System_Boolean }:
                    return new JSSimpleTypeInfo(KnownManagedType.Boolean)
                    {
                        Syntax = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword))
                    };
                case { SpecialType: SpecialType.System_Byte }:
                    return new JSSimpleTypeInfo(KnownManagedType.Byte)
                    {
                        Syntax = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ByteKeyword))
                    };
                case { SpecialType: SpecialType.System_Char }:
                    return new JSSimpleTypeInfo(KnownManagedType.Char)
                    {
                        Syntax = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.CharKeyword))
                    };
                case { SpecialType: SpecialType.System_Int16 }:
                    return new JSSimpleTypeInfo(KnownManagedType.Int16)
                    {
                        Syntax = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ShortKeyword))
                    };
                case { SpecialType: SpecialType.System_Int32 }:
                    return new JSSimpleTypeInfo(KnownManagedType.Int32)
                    {
                        Syntax = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword))
                    };
                case { SpecialType: SpecialType.System_Int64 }:
                    return new JSSimpleTypeInfo(KnownManagedType.Int64)
                    {
                        Syntax = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.LongKeyword))
                    };
                case { SpecialType: SpecialType.System_Single }:
                    return new JSSimpleTypeInfo(KnownManagedType.Single)
                    {
                        Syntax = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.FloatKeyword))
                    };
                case { SpecialType: SpecialType.System_Double }:
                    return new JSSimpleTypeInfo(KnownManagedType.Double)
                    {
                        Syntax = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.DoubleKeyword))
                    };
                case { SpecialType: SpecialType.System_IntPtr }:
                case IPointerTypeSymbol { PointedAtType.SpecialType: SpecialType.System_Void }:
                    return new JSSimpleTypeInfo(KnownManagedType.IntPtr)
                    {
                        Syntax = SyntaxFactory.IdentifierName("nint")
                    };
                case { SpecialType: SpecialType.System_DateTime }:
                    return new JSSimpleTypeInfo(KnownManagedType.DateTime)
                    {
                        Syntax = SyntaxFactory.ParseTypeName(fullTypeName.Trim())
                    };
                case ITypeSymbol when fullTypeName == "global::System.DateTimeOffset":
                    return new JSSimpleTypeInfo(KnownManagedType.DateTimeOffset)
                    {
                        Syntax = SyntaxFactory.ParseTypeName(fullTypeName.Trim())
                    };
                case ITypeSymbol when fullTypeName == "global::System.Exception":
                    return new JSSimpleTypeInfo(KnownManagedType.Exception)
                    {
                        Syntax = SyntaxFactory.ParseTypeName(fullTypeName.Trim())
                    };
                case { SpecialType: SpecialType.System_Object }:
                    return new JSSimpleTypeInfo(KnownManagedType.Object)
                    {
                        Syntax = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword))
                    };
                case { SpecialType: SpecialType.System_String }:
                    return new JSSimpleTypeInfo(KnownManagedType.String)
                    {
                        Syntax = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword))
                    };
                case ITypeSymbol when fullTypeName == "global::System.Runtime.InteropServices.JavaScript.JSObject":
                    return new JSSimpleTypeInfo(KnownManagedType.JSObject)
                    {
                        Syntax = SyntaxFactory.ParseTypeName(fullTypeName.Trim())
                    };

                //nullable
                case INamedTypeSymbol { ConstructedFrom.SpecialType: SpecialType.System_Nullable_T } nullable:
                    if (CreateJSTypeInfoForTypeSymbol(nullable.TypeArguments[0]) is JSSimpleTypeInfo uti)
                    {
                        return new JSNullableTypeInfo(uti);
                    }
                    return new JSInvalidTypeInfo();

                // array
                case IArrayTypeSymbol { IsSZArray: true, ElementType: ITypeSymbol elementType }:
                    if (CreateJSTypeInfoForTypeSymbol(elementType) is JSSimpleTypeInfo eti)
                    {
                        return new JSArrayTypeInfo(eti);
                    }
                    return new JSInvalidTypeInfo();

                // task
                case ITypeSymbol when fullTypeName == Constants.TaskGlobal:
                    return new JSTaskTypeInfo(new JSSimpleTypeInfo(KnownManagedType.Void, SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword))));
                case INamedTypeSymbol { TypeArguments.Length: 1 } taskType when fullTypeName.StartsWith(Constants.TaskGlobal, StringComparison.Ordinal):
                    if (CreateJSTypeInfoForTypeSymbol(taskType.TypeArguments[0]) is JSSimpleTypeInfo rti)
                    {
                        return new JSTaskTypeInfo(rti);
                    }
                    return new JSInvalidTypeInfo();

                // span
                case INamedTypeSymbol { TypeArguments.Length: 1 } spanType when fullTypeName.StartsWith(Constants.SpanGlobal, StringComparison.Ordinal):
                    if (CreateJSTypeInfoForTypeSymbol(spanType.TypeArguments[0]) is JSSimpleTypeInfo sti)
                    {
                        return new JSSpanTypeInfo(sti);
                    }
                    return new JSInvalidTypeInfo();

                // array segment
                case INamedTypeSymbol { TypeArguments.Length: 1 } arraySegmentType when fullTypeName.StartsWith(Constants.ArraySegmentGlobal, StringComparison.Ordinal):
                    if (CreateJSTypeInfoForTypeSymbol(arraySegmentType.TypeArguments[0]) is JSSimpleTypeInfo gti)
                    {
                        return new JSArraySegmentTypeInfo(gti);
                    }
                    return new JSInvalidTypeInfo();

                // action
                case ITypeSymbol when fullTypeName == Constants.ActionGlobal:
                    return new JSFunctionTypeInfo(true, Array.Empty<JSSimpleTypeInfo>());
                case INamedTypeSymbol actionType when fullTypeName.StartsWith(Constants.ActionGlobal, StringComparison.Ordinal):
                    var argumentTypes = actionType.TypeArguments
                        .Select(arg => CreateJSTypeInfoForTypeSymbol(arg) as JSSimpleTypeInfo)
                        .ToArray();
                    if (argumentTypes.Any(x => x is null))
                    {
                        return new JSInvalidTypeInfo();
                    }
                    return new JSFunctionTypeInfo(true, argumentTypes);

                // function
                case INamedTypeSymbol funcType when fullTypeName.StartsWith(Constants.FuncGlobal, StringComparison.Ordinal):
                    var signatureTypes = funcType.TypeArguments
                        .Select(argName => CreateJSTypeInfoForTypeSymbol(argName) as JSSimpleTypeInfo)
                        .ToArray();
                    if (signatureTypes.Any(x => x is null))
                    {
                        return new JSInvalidTypeInfo();
                    }
                    return new JSFunctionTypeInfo(false, signatureTypes);
                default:
                    // JS Interop generator does not support the marshalling of structs
                    // In case structs were to be allowed for marshalling in the future,
                    // disallow marshalling of structs with the InlineArrayAttribute
                    return new JSInvalidTypeInfo();
            }
        }
    }

    internal sealed record JSInvalidTypeInfo() : JSSimpleTypeInfo(KnownManagedType.None);

    internal record JSSimpleTypeInfo(KnownManagedType KnownType) : JSTypeInfo(KnownType)
    {
        public JSSimpleTypeInfo(KnownManagedType knownType, TypeSyntax syntax)
            : this(knownType)
        {
            Syntax = syntax;
        }
        public TypeSyntax Syntax { get; init; }
    }

    internal sealed record JSArrayTypeInfo(JSSimpleTypeInfo ElementTypeInfo) : JSTypeInfo(KnownManagedType.Array);

    internal sealed record JSSpanTypeInfo(JSSimpleTypeInfo ElementTypeInfo) : JSTypeInfo(KnownManagedType.Span);

    internal sealed record JSArraySegmentTypeInfo(JSSimpleTypeInfo ElementTypeInfo) : JSTypeInfo(KnownManagedType.ArraySegment);

    internal sealed record JSTaskTypeInfo(JSSimpleTypeInfo ResultTypeInfo) : JSTypeInfo(KnownManagedType.Task);

    internal sealed record JSNullableTypeInfo(JSSimpleTypeInfo ResultTypeInfo) : JSTypeInfo(KnownManagedType.Nullable);

    internal sealed record JSFunctionTypeInfo(bool IsAction, JSSimpleTypeInfo[] ArgsTypeInfo) : JSTypeInfo(IsAction ? KnownManagedType.Action : KnownManagedType.Function);
}
