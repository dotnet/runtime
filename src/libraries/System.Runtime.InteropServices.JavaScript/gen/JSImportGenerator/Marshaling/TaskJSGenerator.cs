// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.JavaScript;

namespace Microsoft.Interop.JavaScript
{
    internal sealed class TaskJSGenerator(TypePositionInfo info, StubCodeContext context, MarshalerType resultMarshalerType) : BaseJSGenerator(info, context)
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
            var taskType = (JSTaskTypeInfo)((JSMarshallingInfo)TypeInfo.MarshallingAttributeInfo).TypeInfo;
            var (managed, js) = context.GetIdentifiers(TypeInfo);
            string method = toManaged ? GetToManagedMethod(MarshalerType.Task) : GetToJSMethod(MarshalerType.Task);
            writer.Write($"{js}.{method}({(toManaged ? "out " : "")}{managed}");
            if (taskType.ResultTypeInfo.KnownType != KnownManagedType.Void)
            {
                writer.Write(", ");
                WriteMarshallingLambda(writer, taskType.ResultTypeInfo.FullTypeName, "__task_result_arg", "__task_result", resultMarshalerType, toManaged);
            }
            writer.WriteLine(");");
        }
    }
}
