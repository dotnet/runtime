// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Interop
{
    public sealed class StaticPinnableManagedValueMarshaller(IBoundMarshallingGenerator innerMarshallingGenerator, string getPinnableReferenceType) : IBoundMarshallingGenerator
    {
        public TypePositionInfo TypeInfo => innerMarshallingGenerator.TypeInfo;

        public StubCodeContext CodeContext => innerMarshallingGenerator.CodeContext;

        public ManagedTypeInfo NativeType => innerMarshallingGenerator.NativeType;

        public SignatureBehavior NativeSignatureBehavior => innerMarshallingGenerator.NativeSignatureBehavior;

        public ValueBoundaryBehavior ValueBoundaryBehavior
        {
            get
            {
                if (IsPinningPathSupported(CodeContext))
                {
                    if (NativeType is PointerTypeInfo { IsFunctionPointer: false, FullTypeName: "void*" })
                    {
                        return Interop.ValueBoundaryBehavior.NativeIdentifier;
                    }

                    // Cast to native type if it is not void*
                    return Interop.ValueBoundaryBehavior.CastNativeIdentifier;
                }

                return innerMarshallingGenerator.ValueBoundaryBehavior;
            }
        }

        public void Generate(IndentedTextWriter writer, StubIdentifierContext context)
        {
            if (IsPinningPathSupported(CodeContext))
            {
                GeneratePinningPath(writer, context);
                return;
            }

            innerMarshallingGenerator.Generate(writer, context);
        }

        public bool UsesNativeIdentifier
        {
            get
            {
                if (IsPinningPathSupported(CodeContext))
                {
                    return false;
                }

                return innerMarshallingGenerator.UsesNativeIdentifier;
            }
        }

        private bool IsPinningPathSupported(StubCodeContext context)
        {
            return context.SingleFrameSpansNativeContext && !TypeInfo.IsByRef && !context.IsInStubReturnPosition(TypeInfo);
        }

        private void GeneratePinningPath(IndentedTextWriter writer, StubIdentifierContext context)
        {
            if (context.CurrentStage == StubIdentifierContext.Stage.Pin)
            {
                (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(innerMarshallingGenerator.TypeInfo);

                writer.WriteLine($"fixed (void* {nativeIdentifier} = &{getPinnableReferenceType}.{ShapeMemberNames.GetPinnableReference}({managedIdentifier}))");
            }
        }

        public ByValueMarshalKindSupport SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, out GeneratorDiagnostic? diagnostic)
        {
            return innerMarshallingGenerator.SupportsByValueMarshalKind(marshalKind, out diagnostic);
        }
    }
}
