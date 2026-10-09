// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    public sealed class Utf16CharMarshaller : IUnboundMarshallingGenerator
    {
        private static readonly ManagedTypeInfo s_nativeType = SpecialTypeInfo.UInt16;

        public ValueBoundaryBehavior GetValueBoundaryBehavior(TypePositionInfo info, StubCodeContext context)
        {
            if (IsPinningPathSupported(info, context))
            {
                return ValueBoundaryBehavior.NativeIdentifier;
            }
            else if (!UsesNativeIdentifier(info, context))
            {
                return ValueBoundaryBehavior.ManagedIdentifier;
            }
            else if (info.IsByRef)
            {
                return ValueBoundaryBehavior.AddressOfNativeIdentifier;
            }

            return ValueBoundaryBehavior.NativeIdentifier;
        }

        public ManagedTypeInfo AsNativeType(TypePositionInfo info)
        {
            Debug.Assert(info.ManagedType is SpecialTypeInfo {SpecialType: SpecialType.System_Char });
            return s_nativeType;
        }

        public SignatureBehavior GetNativeSignatureBehavior(TypePositionInfo info)
        {
            return info.IsByRef ? SignatureBehavior.PointerToNativeType : SignatureBehavior.NativeType;
        }

        public void Generate(IndentedTextWriter writer, TypePositionInfo info, StubCodeContext codeContext, StubIdentifierContext context)
        {
            (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);

            if (IsPinningPathSupported(info, codeContext))
            {
                if (context.CurrentStage == StubIdentifierContext.Stage.Pin)
                {
                    writer.WriteLine($"fixed (char* {PinnedIdentifier(info.InstanceIdentifier)} = &{managedIdentifier})");
                }
                else if (context.CurrentStage == StubIdentifierContext.Stage.PinnedMarshal)
                {
                    string nativeType = AsNativeType(info).FullTypeName;
                    // The alias must live inside the body shared by all fixed headers.
                    writer.WriteLine($"{nativeType}* {nativeIdentifier} = ({nativeType}*){PinnedIdentifier(info.InstanceIdentifier)};");
                }
                return;
            }

            MarshalDirection elementMarshalDirection = MarshallerHelpers.GetMarshalDirection(info, codeContext);

            switch (context.CurrentStage)
            {
                case StubIdentifierContext.Stage.Setup:
                    break;
                case StubIdentifierContext.Stage.Marshal:
                    if (elementMarshalDirection is MarshalDirection.ManagedToUnmanaged or MarshalDirection.Bidirectional)
                    {
                        // There's an implicit conversion from char to ushort,
                        // so we simplify the generated code to just pass the char value directly
                        if (info.IsByRef)
                        {
                            writer.WriteLine($"{nativeIdentifier} = {managedIdentifier};");
                        }
                    }

                    break;
                case StubIdentifierContext.Stage.Unmarshal:
                    if (elementMarshalDirection is MarshalDirection.UnmanagedToManaged or MarshalDirection.Bidirectional)
                    {
                        writer.WriteLine($"{managedIdentifier} = (char){nativeIdentifier};");
                    }

                    break;
                default:
                    break;
            }
        }

        public bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context)
        {
            MarshalDirection elementMarshalDirection = MarshallerHelpers.GetMarshalDirection(info, context);
            return !IsPinningPathSupported(info, context) && (elementMarshalDirection != MarshalDirection.ManagedToUnmanaged || info.IsByRef);
        }

        private static bool IsPinningPathSupported(TypePositionInfo info, StubCodeContext context)
        {
            return context.SingleFrameSpansNativeContext
                && !context.IsInStubReturnPosition(info)
                && info.IsByRef;
        }

        private static string PinnedIdentifier(string identifier) => $"{identifier}__pinned";
        public ByValueMarshalKindSupport SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, TypePositionInfo info, out GeneratorDiagnostic? diagnostic)
        {
            return ByValueMarshalKindSupportDescriptor.Default.GetSupport(marshalKind, info, out diagnostic);
        }

    }
}
