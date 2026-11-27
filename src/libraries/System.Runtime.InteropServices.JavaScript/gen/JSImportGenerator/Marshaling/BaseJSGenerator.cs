// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.JavaScript;

namespace Microsoft.Interop.JavaScript
{
    internal abstract class BaseJSGenerator(TypePositionInfo info, StubCodeContext codeContext) : IBoundMarshallingGenerator
    {
        private static readonly ValueTypeInfo s_jsMarshalerArgument = new(Constants.JSMarshalerArgumentGlobal, Constants.JSMarshalerArgument, IsByRefLike: false);

        public TypePositionInfo TypeInfo => info;

        public StubCodeContext CodeContext => codeContext;

        public ManagedTypeInfo NativeType => s_jsMarshalerArgument;

        public SignatureBehavior NativeSignatureBehavior => TypeInfo.IsByRef ? SignatureBehavior.PointerToNativeType : SignatureBehavior.NativeType;

        public ValueBoundaryBehavior ValueBoundaryBehavior => TypeInfo.IsByRef ? ValueBoundaryBehavior.AddressOfNativeIdentifier : ValueBoundaryBehavior.NativeIdentifier;

        public virtual bool UsesNativeIdentifier => true;

        public ByValueMarshalKindSupport SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, out GeneratorDiagnostic? diagnostic)
        {
            diagnostic = null;
            return ByValueMarshalKindSupport.NotSupported;
        }

        public virtual void Generate(IndentedTextWriter writer, StubIdentifierContext context)
        {
            MarshalDirection marshalDirection = MarshallerHelpers.GetMarshalDirection(TypeInfo, CodeContext);
            if (context.CurrentStage == StubIdentifierContext.Stage.Setup
                && marshalDirection == MarshalDirection.ManagedToUnmanaged
                && !TypeInfo.IsManagedReturnPosition)
            {
                var (_, js) = context.GetIdentifiers(TypeInfo);
                writer.WriteLine($"{TypeNames.GlobalAlias}{TypeNames.System_Runtime_CompilerServices_Unsafe}.SkipInit(out {js});");
            }
        }

        protected static string GetToManagedMethod(MarshalerType marshalerType)
        {
            return marshalerType == MarshalerType.BigInt64 ? Constants.ToManagedBigMethod : Constants.ToManagedMethod;
        }

        protected static string GetToJSMethod(MarshalerType marshalerType)
        {
            return marshalerType == MarshalerType.BigInt64 ? Constants.ToJSBigMethod : Constants.ToJSMethod;
        }

        protected static void WriteMarshallingLambda(
            IndentedTextWriter writer,
            string sourceType,
            string argumentIdentifier,
            string managedIdentifier,
            MarshalerType marshalerType,
            bool toManaged)
        {
            string modifier = toManaged ? "out " : "";
            string method = toManaged ? GetToManagedMethod(marshalerType) : GetToJSMethod(marshalerType);
            writer.WriteLine($"static (ref {Constants.JSMarshalerArgumentGlobal} {argumentIdentifier}, {modifier}{sourceType} {managedIdentifier}) =>");
            writer.WriteLine('{');
            writer.Indent++;
            writer.WriteLine($"{argumentIdentifier}.{method}({modifier}{managedIdentifier});");
            writer.Indent--;
            writer.Write('}');
        }
    }
}
