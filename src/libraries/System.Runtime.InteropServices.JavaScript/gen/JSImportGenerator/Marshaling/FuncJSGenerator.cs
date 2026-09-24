// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.InteropServices.JavaScript;

namespace Microsoft.Interop.JavaScript
{
    internal sealed class FuncJSGenerator(TypePositionInfo info, StubCodeContext context, bool isAction, MarshalerType[] argumentMarshalerTypes) : BaseJSGenerator(info, context)
    {
        public override void Generate(IndentedTextWriter writer, StubIdentifierContext context)
        {
            base.Generate(writer, context);

            MarshalDirection marshalDirection = MarshallerHelpers.GetMarshalDirection(TypeInfo, CodeContext);

            if (marshalDirection == MarshalDirection.UnmanagedToManaged
                && ((context.CurrentStage == StubIdentifierContext.Stage.UnmarshalCapture && CodeContext.Direction == MarshalDirection.ManagedToUnmanaged)
                    || (context.CurrentStage == StubIdentifierContext.Stage.Unmarshal && CodeContext.Direction == MarshalDirection.UnmanagedToManaged)))
            {
                WriteMarshal(writer, context, toManaged: true);
            }

            if (marshalDirection == MarshalDirection.ManagedToUnmanaged
                && ((context.CurrentStage == StubIdentifierContext.Stage.Marshal && CodeContext.Direction == MarshalDirection.UnmanagedToManaged)
                    || (context.CurrentStage == StubIdentifierContext.Stage.PinnedMarshal && CodeContext.Direction == MarshalDirection.ManagedToUnmanaged)))
            {
                WriteMarshal(writer, context, toManaged: false);
            }
        }

        private void WriteMarshal(IndentedTextWriter writer, StubIdentifierContext context, bool toManaged)
        {
            var functionType = (JSFunctionTypeInfo)((JSMarshallingInfo)TypeInfo.MarshallingAttributeInfo).TypeInfo;
            var (managed, js) = context.GetIdentifiers(TypeInfo);
            MarshalerType marshalerType = isAction ? MarshalerType.Action : MarshalerType.Function;
            string method = toManaged ? GetToManagedMethod(marshalerType) : GetToJSMethod(marshalerType);
            writer.Write($"{js}.{method}({(toManaged ? "out " : "")}{managed}");
            for (int i = 0; i < functionType.ArgsTypeInfo.Length; i++)
            {
                bool isReturn = !isAction && i == functionType.ArgsTypeInfo.Length - 1;
                string index = (i + 1).ToString(CultureInfo.InvariantCulture);
                writer.Write(", ");
                WriteMarshallingLambda(
                    writer,
                    functionType.ArgsTypeInfo[i].FullTypeName,
                    "__delegate_arg_arg" + index,
                    "__delegate_arg" + index,
                    argumentMarshalerTypes[i],
                    toManaged == isReturn);
            }
            writer.WriteLine(");");
        }
    }
}
