using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.Interop
{
    /// <summary>
    /// Implements generating code for an <see cref="ICustomNativeTypeMarshallingStrategy"/> instance.
    /// </summary>
    internal sealed class CustomNativeTypeMarshallingGenerator : IMarshallingGenerator
    {
        private readonly ICustomNativeTypeMarshallingStrategy nativeTypeMarshaller;
        private readonly bool enableByValueContentsMarshalling;

        public CustomNativeTypeMarshallingGenerator(ICustomNativeTypeMarshallingStrategy nativeTypeMarshaller, bool enableByValueContentsMarshalling)
        {
            this.nativeTypeMarshaller = nativeTypeMarshaller;
            this.enableByValueContentsMarshalling = enableByValueContentsMarshalling;
        }

        public ArgumentSyntax AsArgument(TypePositionInfo info, StubCodeContext context)
        {
            return nativeTypeMarshaller.AsArgument(info, context);
        }

        public TypeSyntax AsNativeType(TypePositionInfo info)
        {
            return nativeTypeMarshaller.AsNativeType(info);
        }

        public ParameterSyntax AsParameter(TypePositionInfo info)
        {
            var type = info.IsByRef
                ? PointerType(AsNativeType(info))
                : AsNativeType(info);
            return Parameter(Identifier(info.InstanceIdentifier))
                .WithType(type);
        }

        public IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context)
        {
            // Although custom native type marshalling doesn't support [In] or [Out] by value marshalling,
            // other marshallers that wrap this one might, so we handle the correct cases here.
            switch (context.CurrentStage)
            {
                case StubCodeContext.Stage.Setup:
                    return nativeTypeMarshaller.GenerateSetupStatements(info, context);
                case StubCodeContext.Stage.Marshal:
                    if (!info.IsManagedReturnPosition && info.RefKind != RefKind.Out)
                    {
                        return nativeTypeMarshaller.GenerateMarshalStatements(info, context, nativeTypeMarshaller.GetNativeTypeConstructorArguments(info, context));
                    }
                    break;
                case StubCodeContext.Stage.Pin:
                    if (!info.IsByRef || info.RefKind == RefKind.In)
                    {
                        return nativeTypeMarshaller.GeneratePinStatements(info, context);
                    }
                    break;
                case StubCodeContext.Stage.Unmarshal:
                    if (info.IsManagedReturnPosition || (info.IsByRef && info.RefKind != RefKind.In)
                        || (enableByValueContentsMarshalling && !info.IsByRef && info.ByValueContentsMarshalKind.HasFlag(ByValueContentsMarshalKind.Out)))
                    {
                        return nativeTypeMarshaller.GenerateUnmarshalStatements(info, context);
                    }
                    break;
                case StubCodeContext.Stage.Cleanup:
                    return nativeTypeMarshaller.GenerateCleanupStatements(info, context);
                default:
                    break;
            }

            return Array.Empty<StatementSyntax>();
        }

        public bool SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, StubCodeContext context)
        {
            return enableByValueContentsMarshalling;
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context)
        {
            return nativeTypeMarshaller.UsesNativeIdentifier(info, context);
        }
    }
}
