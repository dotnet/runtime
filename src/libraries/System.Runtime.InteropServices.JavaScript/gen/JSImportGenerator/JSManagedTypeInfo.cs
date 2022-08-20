// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop.JavaScript
{
    internal abstract record JSTypeInfo(string FullTypeName, string DiagnosticFormattedName, KnownManagedType KnownType) : ManagedTypeInfo(FullTypeName, DiagnosticFormattedName)
    {
        public static ManagedTypeInfo CreateJSTypeInfoForTypeSymbol(ITypeSymbol type)
        {
            string fullTypeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (fullTypeName == "void")
            {
                return SpecialTypeInfo.Void;
            }
            string diagnosticFormattedName = type.ToDisplayString();
            return CreateJSTypeInfoForTypeSymbol(fullTypeName, diagnosticFormattedName);
        }

        public static ManagedTypeInfo CreateJSTypeInfoForTypeSymbol(string fullTypeName, string diagnosticFormattedName)
        {
            switch (fullTypeName.Trim())
            {
                case "global::System.Void":
                case "void":
                    return new JSSimpleTypeInfo(fullTypeName, diagnosticFormattedName, KnownManagedType.Void);
                case "global::System.Boolean":
                case "bool":
                    return new JSSimpleTypeInfo(fullTypeName, diagnosticFormattedName, KnownManagedType.Boolean);
                case "global::System.Byte":
                case "byte":
                    return new JSSimpleTypeInfo(fullTypeName, diagnosticFormattedName, KnownManagedType.Byte);
                case "global::System.Char":
                case "char":
                    return new JSSimpleTypeInfo(fullTypeName, diagnosticFormattedName, KnownManagedType.Char);
                case "global::System.Int16":
                case "short":
                    return new JSSimpleTypeInfo(fullTypeName, diagnosticFormattedName, KnownManagedType.Int16);
                case "global::System.Int32":
                case "int":
                    return new JSSimpleTypeInfo(fullTypeName, diagnosticFormattedName, KnownManagedType.Int32);
                case "global::System.Int64":
                case "long":
                    return new JSSimpleTypeInfo(fullTypeName, diagnosticFormattedName, KnownManagedType.Int64);
                case "global::System.Single":
                case "float":
                    return new JSSimpleTypeInfo(fullTypeName, diagnosticFormattedName, KnownManagedType.Single);
                case "global::System.Double":
                case "double":
                    return new JSSimpleTypeInfo(fullTypeName, diagnosticFormattedName, KnownManagedType.Double);
                case "global::System.IntPtr":
                case "nint":
                case "void*":
                    return new JSSimpleTypeInfo(fullTypeName, diagnosticFormattedName, KnownManagedType.IntPtr);
                case "global::System.DateTime":
                    return new JSSimpleTypeInfo(fullTypeName, diagnosticFormattedName, KnownManagedType.DateTime);
                case "global::System.DateTimeOffset":
                    return new JSSimpleTypeInfo(fullTypeName, diagnosticFormattedName, KnownManagedType.DateTimeOffset);
                case "global::System.Exception":
                    return new JSSimpleTypeInfo(fullTypeName, diagnosticFormattedName, KnownManagedType.Exception);
                case "global::System.Object":
                case "object":
                    return new JSSimpleTypeInfo(fullTypeName, diagnosticFormattedName, KnownManagedType.Object);
                case "global::System.String":
                case "string":
                    return new JSSimpleTypeInfo(fullTypeName, diagnosticFormattedName, KnownManagedType.String);
                case "global::System.Runtime.InteropServices.JavaScript.JSObject":
                    return new JSSimpleTypeInfo(fullTypeName, diagnosticFormattedName, KnownManagedType.JSObject);

                //nullable
                case string ftn when ftn.EndsWith("?"):
                    var ut = fullTypeName.Remove(fullTypeName.Length - 1);
                    if (CreateJSTypeInfoForTypeSymbol(ut, diagnosticFormattedName) is JSSimpleTypeInfo uti)
                    {
                        return new JSNullableTypeInfo(fullTypeName, diagnosticFormattedName, uti);
                    }
                    return new JSInvalidTypeInfo(fullTypeName, diagnosticFormattedName);

                // array
                case string ftn when ftn.EndsWith("[]"):
                    var et = fullTypeName.Remove(fullTypeName.Length - 2);
                    if (CreateJSTypeInfoForTypeSymbol(et, diagnosticFormattedName) is JSSimpleTypeInfo eti)
                    {
                        return new JSArrayTypeInfo(fullTypeName, diagnosticFormattedName, eti);
                    }
                    return new JSInvalidTypeInfo(fullTypeName, diagnosticFormattedName);

                // task
                case Constants.TaskGlobal:
                    return new JSTaskTypeInfo(fullTypeName, diagnosticFormattedName, (JSSimpleTypeInfo)CreateJSTypeInfoForTypeSymbol("void", diagnosticFormattedName));
                case string ft when ft.StartsWith(Constants.TaskGlobal):
                    var rt = fullTypeName.Substring(Constants.TaskGlobal.Length + 1, fullTypeName.Length - Constants.TaskGlobal.Length - 2);
                    if (CreateJSTypeInfoForTypeSymbol(rt, diagnosticFormattedName) is JSSimpleTypeInfo rti)
                    {
                        return new JSTaskTypeInfo(fullTypeName, diagnosticFormattedName, rti);
                    }
                    return new JSInvalidTypeInfo(fullTypeName, diagnosticFormattedName);

                // span
                case string ft when ft.StartsWith(Constants.SpanGlobal):
                    var st = fullTypeName.Substring(Constants.SpanGlobal.Length + 1, fullTypeName.Length - Constants.SpanGlobal.Length - 2);
                    if (CreateJSTypeInfoForTypeSymbol(st, diagnosticFormattedName) is JSSimpleTypeInfo sti)
                    {
                        return new JSSpanTypeInfo(fullTypeName, diagnosticFormattedName, sti);
                    }
                    return new JSInvalidTypeInfo(fullTypeName, diagnosticFormattedName);

                // array segment
                case string ft when ft.StartsWith(Constants.ArraySegmentGlobal):
                    var gt = fullTypeName.Substring(Constants.ArraySegmentGlobal.Length + 1, fullTypeName.Length - Constants.ArraySegmentGlobal.Length - 2);
                    if (CreateJSTypeInfoForTypeSymbol(gt, diagnosticFormattedName) is JSSimpleTypeInfo gti)
                    {
                        return new JSArraySegmentTypeInfo(fullTypeName, diagnosticFormattedName, gti);
                    }
                    return new JSInvalidTypeInfo(fullTypeName, diagnosticFormattedName);

                // action
                case Constants.ActionGlobal:
                    return new JSFunctionTypeInfo(fullTypeName, diagnosticFormattedName, true, Array.Empty<JSSimpleTypeInfo>());
                case string ft when ft.StartsWith(Constants.ActionGlobal):
                    var argNames = fullTypeName.Substring(Constants.ActionGlobal.Length + 1, fullTypeName.Length - Constants.ActionGlobal.Length - 2);
                    if (!argNames.Contains("<"))
                    {
                        var ga = argNames.Split(',')
                            .Select(argName => CreateJSTypeInfoForTypeSymbol(argName, diagnosticFormattedName) as JSSimpleTypeInfo)
                            .ToArray();
                        if (ga.Any(x => x == null))
                        {
                            return new JSInvalidTypeInfo(fullTypeName, diagnosticFormattedName);
                        }
                        return new JSFunctionTypeInfo(fullTypeName, diagnosticFormattedName, true, ga);
                    }
                    return new JSInvalidTypeInfo(fullTypeName, diagnosticFormattedName);

                // function
                case string ft when ft.StartsWith(Constants.FuncGlobal):
                    var fargNames = fullTypeName.Substring(Constants.FuncGlobal.Length + 1, fullTypeName.Length - Constants.FuncGlobal.Length - 2);
                    if (!fargNames.Contains("<"))
                    {
                        var ga = fargNames.Split(',')
                            .Select(argName => CreateJSTypeInfoForTypeSymbol(argName, diagnosticFormattedName) as JSSimpleTypeInfo)
                            .ToArray();
                        if (ga.Any(x => x == null))
                        {
                            return new JSInvalidTypeInfo(fullTypeName, diagnosticFormattedName);
                        }
                        return new JSFunctionTypeInfo(fullTypeName, diagnosticFormattedName, false, ga);
                    }
                    return new JSInvalidTypeInfo(fullTypeName, diagnosticFormattedName);
                default:
                    return new JSInvalidTypeInfo(fullTypeName, diagnosticFormattedName);
            }
        }

        public static TypePositionInfo CreateForType(TypePositionInfo inner, ITypeSymbol type, MarshallingInfo jsMarshallingInfo, Compilation compilation)
        {
            ManagedTypeInfo jsTypeInfo = CreateJSTypeInfoForTypeSymbol(type);
            var typeInfo = new TypePositionInfo(jsTypeInfo, jsMarshallingInfo)
            {
                InstanceIdentifier = inner.InstanceIdentifier,
                RefKind = inner.RefKind,
                RefKindSyntax = inner.RefKindSyntax,
                ByValueContentsMarshalKind = inner.ByValueContentsMarshalKind
            };

            return typeInfo;
        }
    }

    internal sealed record JSInvalidTypeInfo(string FullTypeName, string DiagnosticFormattedName) : JSSimpleTypeInfo(FullTypeName, DiagnosticFormattedName, KnownManagedType.None);

    internal record JSSimpleTypeInfo(string FullTypeName, string DiagnosticFormattedName, KnownManagedType KnownType) : JSTypeInfo(FullTypeName, DiagnosticFormattedName, KnownType);

    internal sealed record JSArrayTypeInfo(string FullTypeName, string DiagnosticFormattedName, JSSimpleTypeInfo ElementTypeInfo) : JSTypeInfo(FullTypeName, DiagnosticFormattedName, KnownManagedType.Array);

    internal sealed record JSSpanTypeInfo(string FullTypeName, string DiagnosticFormattedName, JSSimpleTypeInfo ElementTypeInfo) : JSTypeInfo(FullTypeName, DiagnosticFormattedName, KnownManagedType.Span);

    internal sealed record JSArraySegmentTypeInfo(string FullTypeName, string DiagnosticFormattedName, JSSimpleTypeInfo ElementTypeInfo) : JSTypeInfo(FullTypeName, DiagnosticFormattedName, KnownManagedType.ArraySegment);

    internal sealed record JSTaskTypeInfo(string FullTypeName, string DiagnosticFormattedName, JSSimpleTypeInfo ResultTypeInfo) : JSTypeInfo(FullTypeName, DiagnosticFormattedName, KnownManagedType.Task);

    internal sealed record JSNullableTypeInfo(string FullTypeName, string DiagnosticFormattedName, JSSimpleTypeInfo ResultTypeInfo) : JSTypeInfo(FullTypeName, DiagnosticFormattedName, KnownManagedType.Nullable);

    internal sealed record JSFunctionTypeInfo(string FullTypeName, string DiagnosticFormattedName, bool IsAction, JSSimpleTypeInfo[] ArgsTypeInfo) : JSTypeInfo(FullTypeName, DiagnosticFormattedName, (IsAction ? KnownManagedType.Action : KnownManagedType.Function));
}
