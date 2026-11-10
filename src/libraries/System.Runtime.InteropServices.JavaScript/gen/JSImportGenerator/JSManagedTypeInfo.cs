// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;

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
                    return new JSSimpleTypeInfo(KnownManagedType.Void, "void");
                case { SpecialType: SpecialType.System_Boolean }:
                    return new JSSimpleTypeInfo(KnownManagedType.Boolean, "bool");
                case { SpecialType: SpecialType.System_Byte }:
                    return new JSSimpleTypeInfo(KnownManagedType.Byte, "byte");
                case { SpecialType: SpecialType.System_Char }:
                    return new JSSimpleTypeInfo(KnownManagedType.Char, "char");
                case { SpecialType: SpecialType.System_Int16 }:
                    return new JSSimpleTypeInfo(KnownManagedType.Int16, "short");
                case { SpecialType: SpecialType.System_Int32 }:
                    return new JSSimpleTypeInfo(KnownManagedType.Int32, "int");
                case { SpecialType: SpecialType.System_Int64 }:
                    return new JSSimpleTypeInfo(KnownManagedType.Int64, "long");
                case { SpecialType: SpecialType.System_Single }:
                    return new JSSimpleTypeInfo(KnownManagedType.Single, "float");
                case { SpecialType: SpecialType.System_Double }:
                    return new JSSimpleTypeInfo(KnownManagedType.Double, "double");
                case { SpecialType: SpecialType.System_IntPtr }:
                case IPointerTypeSymbol { PointedAtType.SpecialType: SpecialType.System_Void }:
                    return new JSSimpleTypeInfo(KnownManagedType.IntPtr, "nint");
                case { SpecialType: SpecialType.System_DateTime }:
                    return new JSSimpleTypeInfo(KnownManagedType.DateTime, fullTypeName);
                case ITypeSymbol when fullTypeName == "global::System.DateTimeOffset":
                    return new JSSimpleTypeInfo(KnownManagedType.DateTimeOffset, fullTypeName);
                case ITypeSymbol when fullTypeName == "global::System.Exception":
                    return new JSSimpleTypeInfo(KnownManagedType.Exception, fullTypeName);
                case { SpecialType: SpecialType.System_Object }:
                    return new JSSimpleTypeInfo(KnownManagedType.Object, "object");
                case { SpecialType: SpecialType.System_String }:
                    return new JSSimpleTypeInfo(KnownManagedType.String, "string");
                case ITypeSymbol when fullTypeName == "global::System.Runtime.InteropServices.JavaScript.JSObject":
                    return new JSSimpleTypeInfo(KnownManagedType.JSObject, fullTypeName);

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
                    return new JSTaskTypeInfo(new JSSimpleTypeInfo(KnownManagedType.Void, "void"));
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

    internal sealed record JSInvalidTypeInfo() : JSSimpleTypeInfo(KnownManagedType.None, "");

    internal record JSSimpleTypeInfo(KnownManagedType KnownType, string FullTypeName) : JSTypeInfo(KnownType);

    internal sealed record JSArrayTypeInfo(JSSimpleTypeInfo ElementTypeInfo) : JSTypeInfo(KnownManagedType.Array);

    internal sealed record JSSpanTypeInfo(JSSimpleTypeInfo ElementTypeInfo) : JSTypeInfo(KnownManagedType.Span);

    internal sealed record JSArraySegmentTypeInfo(JSSimpleTypeInfo ElementTypeInfo) : JSTypeInfo(KnownManagedType.ArraySegment);

    internal sealed record JSTaskTypeInfo(JSSimpleTypeInfo ResultTypeInfo) : JSTypeInfo(KnownManagedType.Task);

    internal sealed record JSNullableTypeInfo(JSSimpleTypeInfo ResultTypeInfo) : JSTypeInfo(KnownManagedType.Nullable);

    internal sealed record JSFunctionTypeInfo(bool IsAction, JSSimpleTypeInfo[] ArgsTypeInfo) : JSTypeInfo(IsAction ? KnownManagedType.Action : KnownManagedType.Function)
    {
        public bool Equals(JSFunctionTypeInfo? other)
        {
            return other is not null
                && IsAction == other.IsAction
                && ArgsTypeInfo.SequenceEqual(other.ArgsTypeInfo);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = IsAction.GetHashCode();
                foreach (JSSimpleTypeInfo argument in ArgsTypeInfo)
                {
                    hash = hash * 31 + argument.GetHashCode();
                }
                return hash;
            }
        }
    }
}
