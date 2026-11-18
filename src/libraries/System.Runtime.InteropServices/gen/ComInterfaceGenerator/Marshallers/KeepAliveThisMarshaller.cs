// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Interop
{
    internal sealed class KeepAliveThisMarshaller : IUnboundMarshallingGenerator
    {
        public static readonly KeepAliveThisMarshaller Instance = new();

        public ManagedTypeInfo AsNativeType(TypePositionInfo info) => info.ManagedType;
        public void Generate(IndentedTextWriter writer, TypePositionInfo info, StubCodeContext codeContext, StubIdentifierContext context)
        {
            if (context.CurrentStage != StubIdentifierContext.Stage.NotifyForSuccessfulInvoke)
            {
                return;
            }

            writer.WriteLine($"{TypeNames.GlobalAlias}{TypeNames.System_GC}.KeepAlive(this);");
        }

        public SignatureBehavior GetNativeSignatureBehavior(TypePositionInfo info) => SignatureBehavior.NativeType;
        public ValueBoundaryBehavior GetValueBoundaryBehavior(TypePositionInfo info, StubCodeContext context) => ValueBoundaryBehavior.ManagedIdentifier;
        public ByValueMarshalKindSupport SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, TypePositionInfo info, out GeneratorDiagnostic? diagnostic)
            => ByValueMarshalKindSupportDescriptor.Default.GetSupport(marshalKind, info, out diagnostic);

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context) => false;
    }
}
