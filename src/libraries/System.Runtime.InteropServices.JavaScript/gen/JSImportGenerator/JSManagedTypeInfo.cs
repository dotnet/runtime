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
            return CreateJSTypeInfoForTypeSymbol(fullTypeName);
        }

        private static JSTypeInfo CreateJSTypeInfoForTypeSymbol(string fullTypeName)
        {
            switch (fullTypeName.Trim())
            {
                case "global::System.Void":
                case "void":
                    return new JSSimpleTypeInfo(KnownManagedType.Void)
                    {
                        Syntax = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword))
                    };
                case "global::System.Boolean":
                case "bool":
                    return new JSSimpleTypeInfo(KnownManagedType.Boolean)
                    {
                        Syntax = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword))
                    };
                case "global::System.Byte":
                case "byte":
                    return new JSSimpleTypeInfo(KnownManagedType.Byte)
                    {
                        Syntax = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ByteKeyword))
                    };
                case "global::System.Char":
                case "char":
                    return new JSSimpleTypeInfo(KnownManagedType.Char)
                    {
                        Syntax = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.CharKeyword))
                    };
                case "global::System.Int16":
                case "short":
                    return new JSSimpleTypeInfo(KnownManagedType.Int16)
                    {
                        Syntax = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ShortKeyword))
                    };
                case "global::System.Int32":
                case "int":
                    return new JSSimpleTypeInfo(KnownManagedType.Int32)
                    {
                        Syntax = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword))
                    };
                case "global::System.Int64":
                case "long":
                    return new JSSimpleTypeInfo(KnownManagedType.Int64)
                    {
                        Syntax = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.LongKeyword))
                    };
                case "global::System.Single":
                case "float":
                    return new JSSimpleTypeInfo(KnownManagedType.Single)
                    {
                        Syntax = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.FloatKeyword))
                    };
                case "global::System.Double":
                case "double":
                    return new JSSimpleTypeInfo(KnownManagedType.Double)
                    {
                        Syntax = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.DoubleKeyword))
                    };
                case "global::System.IntPtr":
                case "nint":
                case "void*":
                    return new JSSimpleTypeInfo(KnownManagedType.IntPtr)
                    {
                        Syntax = SyntaxFactory.IdentifierName("nint")
                    };
                case "global::System.DateTime":
                    return new JSSimpleTypeInfo(KnownManagedType.DateTime)
                    {
                        Syntax = SyntaxFactory.ParseTypeName(fullTypeName.Trim())
                    };
                case "global::System.DateTimeOffset":
                    return new JSSimpleTypeInfo(KnownManagedType.DateTimeOffset)
                    {
                        Syntax = SyntaxFactory.ParseTypeName(fullTypeName.Trim())
                    };
                case "global::System.Exception":
                    return new JSSimpleTypeInfo(KnownManagedType.Exception)
                    {
                        Syntax = SyntaxFactory.ParseTypeName(fullTypeName.Trim())
                    };
                case "global::System.Object":
                case "object":
                    return new JSSimpleTypeInfo(KnownManagedType.Object)
                    {
                        Syntax = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword))
                    };
                case "global::System.String":
                case "string":
                    return new JSSimpleTypeInfo(KnownManagedType.String)
                    {
                        Syntax = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword))
                    };
                case "global::System.Runtime.InteropServices.JavaScript.JSObject":
                    return new JSSimpleTypeInfo(KnownManagedType.JSObject)
                    {
                        Syntax = SyntaxFactory.ParseTypeName(fullTypeName.Trim())
                    };

                //nullable
                case string ftn when ftn.EndsWith("?"):
                    var ut = fullTypeName.Remove(fullTypeName.Length - 1);
                    if (CreateJSTypeInfoForTypeSymbol(ut) is JSSimpleTypeInfo uti)
                    {
                        return new JSNullableTypeInfo(uti);
                    }
                    return new JSInvalidTypeInfo();

                // array
                case string ftn when ftn.EndsWith("[]"):
                    var et = fullTypeName.Remove(fullTypeName.Length - 2);
                    if (CreateJSTypeInfoForTypeSymbol(et) is JSSimpleTypeInfo eti)
                    {
                        return new JSArrayTypeInfo(eti);
                    }
                    return new JSInvalidTypeInfo();

                // task
                case Constants.TaskGlobal:
                    return new JSTaskTypeInfo((JSSimpleTypeInfo)CreateJSTypeInfoForTypeSymbol("void"));
                case string ft when ft.StartsWith(Constants.TaskGlobal):
                    var rt = fullTypeName.Substring(Constants.TaskGlobal.Length + 1, fullTypeName.Length - Constants.TaskGlobal.Length - 2);
                    if (CreateJSTypeInfoForTypeSymbol(rt) is JSSimpleTypeInfo rti)
                    {
                        return new JSTaskTypeInfo(rti);
                    }
                    return new JSInvalidTypeInfo();

                // span
                case string ft when ft.StartsWith(Constants.SpanGlobal):
                    var st = fullTypeName.Substring(Constants.SpanGlobal.Length + 1, fullTypeName.Length - Constants.SpanGlobal.Length - 2);
                    if (CreateJSTypeInfoForTypeSymbol(st) is JSSimpleTypeInfo sti)
                    {
                        return new JSSpanTypeInfo(sti);
                    }
                    return new JSInvalidTypeInfo();

                // array segment
                case string ft when ft.StartsWith(Constants.ArraySegmentGlobal):
                    var gt = fullTypeName.Substring(Constants.ArraySegmentGlobal.Length + 1, fullTypeName.Length - Constants.ArraySegmentGlobal.Length - 2);
                    if (CreateJSTypeInfoForTypeSymbol(gt) is JSSimpleTypeInfo gti)
                    {
                        return new JSArraySegmentTypeInfo(gti);
                    }
                    return new JSInvalidTypeInfo();

                // action
                case Constants.ActionGlobal:
                    return new JSFunctionTypeInfo(true, Array.Empty<JSSimpleTypeInfo>());
                case string ft when ft.StartsWith(Constants.ActionGlobal):
                    var argNames = fullTypeName.Substring(Constants.ActionGlobal.Length + 1, fullTypeName.Length - Constants.ActionGlobal.Length - 2);
                    if (!argNames.Contains("<"))
                    {
                        var ga = argNames.Split(',')
                            .Select(argName => CreateJSTypeInfoForTypeSymbol(argName) as JSSimpleTypeInfo)
                            .ToArray();
                        if (ga.Any(x => x == null))
                        {
                            return new JSInvalidTypeInfo();
                        }
                        return new JSFunctionTypeInfo(true, ga);
                    }
                    return new JSInvalidTypeInfo();

                // function
                case string ft when ft.StartsWith(Constants.FuncGlobal):
                    var fargNames = fullTypeName.Substring(Constants.FuncGlobal.Length + 1, fullTypeName.Length - Constants.FuncGlobal.Length - 2);
                    if (!fargNames.Contains("<"))
                    {
                        var ga = fargNames.Split(',')
                            .Select(argName => CreateJSTypeInfoForTypeSymbol(argName) as JSSimpleTypeInfo)
                            .ToArray();
                        if (ga.Any(x => x == null))
                        {
                            return new JSInvalidTypeInfo();
                        }
                        return new JSFunctionTypeInfo(false, ga);
                    }
                    return new JSInvalidTypeInfo();
                default:
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
