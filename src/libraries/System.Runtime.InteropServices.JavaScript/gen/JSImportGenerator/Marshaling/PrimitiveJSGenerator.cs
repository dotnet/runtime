// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.JavaScript;

namespace Microsoft.Interop.JavaScript
{
    internal sealed class PrimitiveJSGenerator(TypePositionInfo info, StubCodeContext context, MarshalerType elementMarshallerType) : BaseJSGenerator(info, context)
    {
        // TODO order parameters in such way that affinity capturing parameters are emitted first
        public override void Generate(IndentedTextWriter writer, StubIdentifierContext context)
        {
            base.Generate(writer, context);

            var (managed, js) = context.GetIdentifiers(TypeInfo);

            MarshalDirection marshalDirection = MarshallerHelpers.GetMarshalDirection(TypeInfo, CodeContext);

            if (context.CurrentStage == StubIdentifierContext.Stage.UnmarshalCapture && marshalDirection is MarshalDirection.UnmanagedToManaged or MarshalDirection.Bidirectional)
            {
                writer.WriteLine($"{js}.{GetToManagedMethod(elementMarshallerType)}(out {managed});");
            }

            if (context.CurrentStage == StubIdentifierContext.Stage.Marshal && marshalDirection is MarshalDirection.ManagedToUnmanaged or MarshalDirection.Bidirectional)
            {
                writer.WriteLine($"{js}.{GetToJSMethod(elementMarshallerType)}({managed});");
            }
        }
    }
}
