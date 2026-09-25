// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.Interop
{
    internal sealed record ObjectUnwrapperInfo(string UnwrapperType) : MarshallingInfo;

    internal sealed class ObjectUnwrapperResolver : IMarshallingGeneratorResolver
    {
        public ResolvedGenerator Create(TypePositionInfo info, StubCodeContext context)
        {
            if (info.MarshallingAttributeInfo is ObjectUnwrapperInfo)
            {
                return context.Direction == MarshalDirection.UnmanagedToManaged
                    ? ResolvedGenerator.Resolved(new Marshaller().Bind(info, context))
                    : ResolvedGenerator.Resolved(KeepAliveThisMarshaller.Instance.Bind(info, context));
            }
            else
            {
                return ResolvedGenerator.UnresolvedGenerator;
            }
        }

        private sealed class Marshaller : IUnboundMarshallingGenerator
        {
            public ManagedTypeInfo AsNativeType(TypePositionInfo info) => new PointerTypeInfo("void*", "void*", false);
            public void Generate(IndentedTextWriter writer, TypePositionInfo info, StubCodeContext codeContext, StubIdentifierContext context)
            {
                Debug.Assert(info.MarshallingAttributeInfo is ObjectUnwrapperInfo);
                string unwrapperType = ((ObjectUnwrapperInfo)info.MarshallingAttributeInfo).UnwrapperType;
                if (context.CurrentStage != StubIdentifierContext.Stage.Unmarshal)
                {
                    return;
                }

                (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);

                writer.WriteLine($"{managedIdentifier} = ({info.ManagedType.FullTypeName}){TypeNames.GlobalAlias}{TypeNames.UnmanagedObjectUnwrapper}.GetObjectForUnmanagedWrapper<{unwrapperType}>({nativeIdentifier});");
            }

            public SignatureBehavior GetNativeSignatureBehavior(TypePositionInfo info) => SignatureBehavior.NativeType;
            public ValueBoundaryBehavior GetValueBoundaryBehavior(TypePositionInfo info, StubCodeContext context) => ValueBoundaryBehavior.NativeIdentifier;
            public ByValueMarshalKindSupport SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, TypePositionInfo info, out GeneratorDiagnostic? diagnostic)
                => ByValueMarshalKindSupportDescriptor.Default.GetSupport(marshalKind, info, out diagnostic);
            public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => true;
        }
    }
}
