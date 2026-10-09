// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.Interop
{
    internal sealed class StructAsHResultMarshallerFactory : IMarshallingGeneratorResolver
    {
        private static readonly Marshaller s_marshaller = new();

        public ResolvedGenerator Create(TypePositionInfo info, StubCodeContext context)
        {
            // Value type with MarshalAs(UnmanagedType.Error), to be marshalled as an unmanaged HRESULT.
            if (info is { ManagedType: ValueTypeInfo, MarshallingAttributeInfo: MarshalAsInfo(UnmanagedType.Error, _) })
            {
                return ResolvedGenerator.Resolved(s_marshaller.Bind(info, context));
            }

            return ResolvedGenerator.UnresolvedGenerator;
        }

        private sealed class Marshaller : IUnboundMarshallingGenerator
        {
            public ManagedTypeInfo AsNativeType(TypePositionInfo info) => SpecialTypeInfo.Int32;

            public void Generate(IndentedTextWriter writer, TypePositionInfo info, StubCodeContext codeContext, StubIdentifierContext context)
            {
                var (managed, unmanaged) = context.GetIdentifiers(info);

                switch (context.CurrentStage)
                {
                    case StubIdentifierContext.Stage.Marshal:
                        if (MarshallerHelpers.GetMarshalDirection(info, codeContext) is MarshalDirection.ManagedToUnmanaged or MarshalDirection.Bidirectional)
                        {
                            writer.WriteLine($"{unmanaged} = {TypeNames.GlobalAlias}{TypeNames.System_Runtime_CompilerServices_Unsafe}.BitCast<{info.ManagedType.FullTypeName}, {AsNativeType(info).FullTypeName}>({managed});");
                        }
                        break;
                    case StubIdentifierContext.Stage.Unmarshal:
                        if (MarshallerHelpers.GetMarshalDirection(info, codeContext) is MarshalDirection.UnmanagedToManaged or MarshalDirection.Bidirectional)
                        {
                            writer.WriteLine($"{managed} = {TypeNames.GlobalAlias}{TypeNames.System_Runtime_CompilerServices_Unsafe}.BitCast<{AsNativeType(info).FullTypeName}, {info.ManagedType.FullTypeName}>({unmanaged});");
                        }
                        break;
                    default:
                        break;
                }
            }

            public SignatureBehavior GetNativeSignatureBehavior(TypePositionInfo info)
            {
                return info.IsByRef ? SignatureBehavior.PointerToNativeType : SignatureBehavior.NativeType;
            }

            public ValueBoundaryBehavior GetValueBoundaryBehavior(TypePositionInfo info, StubCodeContext context)
            {
                if (info.IsByRef)
                {
                    return ValueBoundaryBehavior.AddressOfNativeIdentifier;
                }

                return ValueBoundaryBehavior.NativeIdentifier;
            }

            public ByValueMarshalKindSupport SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, TypePositionInfo info, out GeneratorDiagnostic? diagnostic)
                => ByValueMarshalKindSupportDescriptor.Default.GetSupport(marshalKind, info, out diagnostic);

            public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => true;
        }
    }
}
