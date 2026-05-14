// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    internal sealed class IidParameterIndexMarshallerResolver : IMarshallingGeneratorResolver
    {
        public ResolvedGenerator Create(TypePositionInfo info, StubCodeContext context)
        {
            if (info.MarshallingAttributeInfo is not IidParameterIndexNativeMarshallingInfo iidInfo
                || context.Direction != MarshalDirection.ManagedToUnmanaged)
            {
                return ResolvedGenerator.UnresolvedGenerator;
            }

            return ResolvedGenerator.Resolved(new Marshaller(iidInfo.IidParameterIndexInfo).Bind(info, context));
        }

        private sealed class Marshaller(TypePositionInfo iidParameterIndexInfo) : IUnboundMarshallingGenerator
        {
            public ManagedTypeInfo AsNativeType(TypePositionInfo info) => new PointerTypeInfo("void*", "void*", false);

            public SignatureBehavior GetNativeSignatureBehavior(TypePositionInfo info) => info.IsByRef ? SignatureBehavior.PointerToNativeType : SignatureBehavior.NativeType;

            public ValueBoundaryBehavior GetValueBoundaryBehavior(TypePositionInfo info, StubCodeContext context)
                => info.IsByRef ? ValueBoundaryBehavior.AddressOfNativeIdentifier : ValueBoundaryBehavior.NativeIdentifier;

            public ByValueMarshalKindSupport SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, TypePositionInfo info, out GeneratorDiagnostic? diagnostic)
                => ByValueMarshalKindSupportDescriptor.Default.GetSupport(marshalKind, info, out diagnostic);

            public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => true;

            public IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext codeContext, StubIdentifierContext context)
            {
                if (context.CurrentStage != StubIdentifierContext.Stage.Marshal)
                {
                    yield break;
                }

                (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);
                string unknownIdentifier = context.GetAdditionalIdentifier(info, "unknown");
                string queryInterfaceHResultIdentifier = context.GetAdditionalIdentifier(info, "queryInterfaceHResult");
                string queriedInterfaceIdentifier = context.GetAdditionalIdentifier(info, "queriedInterface");

                ExpressionSyntax iidExpression = MarshallerHelpers.GetIndexedManagedElementExpression(iidParameterIndexInfo, codeContext, context);
                string iidExpressionText = iidExpression.NormalizeWhitespace().ToFullString();

                yield return ParseStatement($"void* {unknownIdentifier} = (void*)global::System.Runtime.InteropServices.Marshalling.ComInterfaceMarshaller<object>.ConvertToUnmanaged({managedIdentifier});");
                yield return ParseStatement($$"""
                    if ({{unknownIdentifier}} != null)
                    {
                        int {{queryInterfaceHResultIdentifier}} = global::System.Runtime.InteropServices.Marshal.QueryInterface((nint){{unknownIdentifier}}, in {{iidExpressionText}}, out nint {{queriedInterfaceIdentifier}});
                        global::System.Runtime.InteropServices.Marshal.Release((nint){{unknownIdentifier}});
                        if ({{queryInterfaceHResultIdentifier}} != 0)
                        {
                            if ({{queriedInterfaceIdentifier}} != 0)
                                global::System.Runtime.InteropServices.Marshal.Release({{queriedInterfaceIdentifier}});
                            throw new global::System.Runtime.InteropServices.COMException("QueryInterface failed for requested IID.", {{queryInterfaceHResultIdentifier}});
                        }
                        {{nativeIdentifier}} = (void*){{queriedInterfaceIdentifier}};
                    }
                    else
                    {
                        {{nativeIdentifier}} = null;
                    }
                    """);
            }
        }
    }
}
