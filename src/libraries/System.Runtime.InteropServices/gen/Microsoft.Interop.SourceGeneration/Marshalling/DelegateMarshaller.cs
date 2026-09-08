// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Interop
{
    public sealed class DelegateMarshaller : IUnboundMarshallingGenerator
    {
        public ManagedTypeInfo AsNativeType(TypePositionInfo info)
        {
            return SpecialTypeInfo.IntPtr;
        }

        public SignatureBehavior GetNativeSignatureBehavior(TypePositionInfo info)
        {
            return info.IsByRef ? SignatureBehavior.PointerToNativeType : SignatureBehavior.NativeType;
        }

        public ValueBoundaryBehavior GetValueBoundaryBehavior(TypePositionInfo info, StubCodeContext context)
        {
            return info.IsByRef ? ValueBoundaryBehavior.AddressOfNativeIdentifier : ValueBoundaryBehavior.NativeIdentifier;
        }

        public void Generate(IndentedTextWriter writer, TypePositionInfo info, StubCodeContext codeContext, StubIdentifierContext context)
        {
            MarshalDirection elementMarshalDirection = MarshallerHelpers.GetMarshalDirection(info, codeContext);
            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);
            switch (context.CurrentStage)
            {
                case StubIdentifierContext.Stage.Setup:
                    break;
                case StubIdentifierContext.Stage.Marshal:
                    if (elementMarshalDirection is MarshalDirection.ManagedToUnmanaged or MarshalDirection.Bidirectional)
                    {
                        // <nativeIdentifier> = <managedIdentifier> != null ? Marshal.GetFunctionPointerForDelegate(<managedIdentifier>) : default;
                        writer.WriteLine($"{nativeIdentifier} = {managedIdentifier} != null ? {TypeNames.GlobalAlias}{TypeNames.System_Runtime_InteropServices_Marshal}.GetFunctionPointerForDelegate({managedIdentifier}) : default;");
                    }
                    break;
                case StubIdentifierContext.Stage.Unmarshal:
                    if (elementMarshalDirection is MarshalDirection.UnmanagedToManaged or MarshalDirection.Bidirectional)
                    {
                        // <managedIdentifier> = <nativeIdentifier> != default : Marshal.GetDelegateForFunctionPointer<<managedType>>(<nativeIdentifier>) : null;
                        writer.WriteLine($"{managedIdentifier} = {nativeIdentifier} != default ? {TypeNames.GlobalAlias}{TypeNames.System_Runtime_InteropServices_Marshal}.GetDelegateForFunctionPointer<{info.ManagedType.FullTypeName}>({nativeIdentifier}) : null;");
                    }
                    break;
                case StubIdentifierContext.Stage.NotifyForSuccessfulInvoke:
                    if (elementMarshalDirection is MarshalDirection.ManagedToUnmanaged or MarshalDirection.Bidirectional)
                    {
                        writer.WriteLine($"{TypeNames.GlobalAlias}{TypeNames.System_GC}.KeepAlive({managedIdentifier});");
                    }
                    break;
                default:
                    break;
            }
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => true;

        public ByValueMarshalKindSupport SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, TypePositionInfo info, out GeneratorDiagnostic? diagnostic)
            => ByValueMarshalKindSupportDescriptor.Default.GetSupport(marshalKind, info, out diagnostic);
    }
}
