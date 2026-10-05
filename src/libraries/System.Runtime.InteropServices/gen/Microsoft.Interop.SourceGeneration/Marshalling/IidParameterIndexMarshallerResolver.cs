// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Interop
{
    internal sealed class IidParameterIndexMarshallerResolver : IMarshallingGeneratorResolver
    {
        public ResolvedGenerator Create(TypePositionInfo info, StubCodeContext context)
        {
            if (info.MarshallingAttributeInfo is not IidParameterIndexNativeMarshallingInfo iidInfo
                || context.Direction != MarshalDirection.UnmanagedToManaged)
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

            public void Generate(IndentedTextWriter writer, TypePositionInfo info, StubCodeContext codeContext, StubIdentifierContext context)
            {
                if (context.CurrentStage != StubIdentifierContext.Stage.Marshal)
                {
                    return;
                }

                (string managedIdentifier, string nativeIdentifier) = context.GetIdentifiers(info);
                string unknownIdentifier = context.GetAdditionalIdentifier(info, "unknown");
                string queryInterfaceHResultIdentifier = context.GetAdditionalIdentifier(info, "queryInterfaceHResult");
                string queriedInterfaceIdentifier = context.GetAdditionalIdentifier(info, "queriedInterface");
                string iidExpression = MarshallerHelpers.GetIndexedManagedElementExpression(iidParameterIndexInfo, codeContext, context);

                writer.WriteLine($"void* {unknownIdentifier} = (void*)global::System.Runtime.InteropServices.Marshalling.ComInterfaceMarshaller<object>.ConvertToUnmanaged({managedIdentifier});");
                writer.WriteLine($"if ({unknownIdentifier} != null)");
                using (writer.WriteBlock())
                {
                    writer.WriteLine($"{TypeNames.GlobalAlias}{TypeNames.System_IntPtr} {queriedInterfaceIdentifier} = 0;");
                    writer.WriteLine($"int {queryInterfaceHResultIdentifier} = {TypeNames.GlobalAlias}{TypeNames.System_Runtime_InteropServices_Marshal}.QueryInterface(({TypeNames.GlobalAlias}{TypeNames.System_IntPtr}){unknownIdentifier}, in {iidExpression}, out {queriedInterfaceIdentifier});");
                    writer.WriteLine($"global::System.Runtime.InteropServices.Marshalling.ComInterfaceMarshaller<object>.Free({unknownIdentifier});");
                    writer.WriteLine($"if ({queryInterfaceHResultIdentifier} < 0)");
                    using (writer.WriteBlock())
                    {
                        writer.WriteLine($"if ({queriedInterfaceIdentifier} != 0)");
                        using (writer.WriteBlock())
                        {
                            writer.WriteLine($"{TypeNames.GlobalAlias}{TypeNames.System_Runtime_InteropServices_Marshal}.Release({queriedInterfaceIdentifier});");
                        }
                        writer.WriteLine($"{nativeIdentifier} = null;");
                        // The stub's exception marshaller converts the failure back to an HRESULT and runs cleanup.
                        writer.WriteLine($"{TypeNames.GlobalAlias}{TypeNames.System_Runtime_InteropServices_Marshal}.ThrowExceptionForHR({queryInterfaceHResultIdentifier});");
                    }
                    writer.WriteLine($"{nativeIdentifier} = (void*){queriedInterfaceIdentifier};");
                }
                writer.WriteLine("else");
                using (writer.WriteBlock())
                {
                    writer.WriteLine($"{nativeIdentifier} = null;");
                }
            }
        }
    }
}
