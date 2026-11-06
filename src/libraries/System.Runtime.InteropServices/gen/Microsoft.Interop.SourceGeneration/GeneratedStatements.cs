// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;

namespace Microsoft.Interop
{
    public readonly record struct GeneratedStatements
    {
        public GeneratedStatements() { }

        public string Setup { get; init; } = "";
        public string Marshal { get; init; } = "";
        public string Pin { get; init; } = "";
        public string PinnedMarshal { get; init; } = "";
        public string InvokeStatement { get; init; } = "";
        public string ErrorUnmarshalCapture { get; init; } = "";
        public string ErrorUnmarshal { get; init; } = "";
        public string Unmarshal { get; init; } = "";
        public string NotifyForSuccessfulInvoke { get; init; } = "";
        public string GuaranteedUnmarshal { get; init; } = "";
        public string ErrorCleanupCalleeAllocated { get; init; } = "";
        public string CleanupCallerAllocated { get; init; } = "";
        public string CleanupCalleeAllocated { get; init; } = "";
        public string ManagedExceptionCatchClauses { get; init; } = "";

        public static GeneratedStatements Create(BoundGenerators marshallers, StubIdentifierContext context)
        {
            var writer = new IndentedTextWriter();
            return new GeneratedStatements
            {
                Setup = GenerateStage(StubIdentifierContext.Stage.Setup),
                Marshal = GenerateStage(StubIdentifierContext.Stage.Marshal),
                Pin = GenerateStage(StubIdentifierContext.Stage.Pin),
                PinnedMarshal = GenerateStage(StubIdentifierContext.Stage.PinnedMarshal),
                InvokeStatement = ";\r\n",
                ErrorUnmarshalCapture = GenerateStage(StubIdentifierContext.Stage.UnmarshalCapture, errorHandlingOnly: true),
                ErrorUnmarshal = GenerateStage(StubIdentifierContext.Stage.Unmarshal, errorHandlingOnly: true),
                Unmarshal = GenerateStage(StubIdentifierContext.Stage.UnmarshalCapture) + GenerateStage(StubIdentifierContext.Stage.Unmarshal),
                NotifyForSuccessfulInvoke = GenerateStage(StubIdentifierContext.Stage.NotifyForSuccessfulInvoke),
                GuaranteedUnmarshal = GenerateStage(StubIdentifierContext.Stage.GuaranteedUnmarshal),
                ErrorCleanupCalleeAllocated = GenerateStage(StubIdentifierContext.Stage.CleanupCalleeAllocated, errorHandlingOnly: true),
                CleanupCallerAllocated = GenerateStage(StubIdentifierContext.Stage.CleanupCallerAllocated),
                CleanupCalleeAllocated = GenerateStage(StubIdentifierContext.Stage.CleanupCalleeAllocated),
                ManagedExceptionCatchClauses = GenerateCatchClauseForManagedException(marshallers, context, writer)
            };

            string GenerateStage(StubIdentifierContext.Stage stage, bool errorHandlingOnly = false)
            {
                writer.Clear();
                StubIdentifierContext stageContext = context with { CurrentStage = stage };
                foreach (IBoundMarshallingGenerator marshaller in marshallers.SignatureMarshallers)
                {
                    if (stage is StubIdentifierContext.Stage.UnmarshalCapture
                            or StubIdentifierContext.Stage.Unmarshal
                            or StubIdentifierContext.Stage.CleanupCalleeAllocated
                        && marshaller.TypeInfo.IsErrorHandlingPosition != errorHandlingOnly)
                    {
                        continue;
                    }

                    marshaller.Generate(writer, stageContext);
                }

                return writer.Length == 0 ? "" : $"// {stage} - {GetStageDescription(stage)}\r\n{writer}";
            }
        }

        public static GeneratedStatements Create(BoundGenerators marshallers, StubCodeContext codeContext, StubIdentifierContext context, string expressionToInvoke)
        {
            GeneratedStatements statements = Create(marshallers, context);
            StubIdentifierContext invokeContext = context with { CurrentStage = StubIdentifierContext.Stage.Invoke };
            return statements with
            {
                InvokeStatement = codeContext.Direction switch
                {
                    MarshalDirection.ManagedToUnmanaged => GenerateStatementForNativeInvoke(marshallers, invokeContext, expressionToInvoke),
                    MarshalDirection.UnmanagedToManaged => GenerateStatementForManagedInvoke(marshallers, invokeContext, expressionToInvoke),
                    _ => throw new ArgumentException("Direction must be ManagedToUnmanaged or UnmanagedToManaged", nameof(codeContext))
                }
            };
        }

        /// <summary>
        /// Creates statements for a property or indexer accessor. The caller supplies the access
        /// expression, including any marshalled index arguments.
        /// </summary>
        public static GeneratedStatements CreateForProperty(BoundGenerators marshallers, StubIdentifierContext context, string propertyAccess, bool isSetter)
        {
            GeneratedStatements statements = Create(marshallers, context);
            StubIdentifierContext invokeContext = context with { CurrentStage = StubIdentifierContext.Stage.Invoke };
            if (isSetter)
            {
                // The value parameter follows all index parameters for an indexer setter.
                IBoundMarshallingGenerator valueMarshaller = marshallers.ManagedParameterMarshallers.Last();
                return statements with
                {
                    InvokeStatement = $"{propertyAccess} = {invokeContext.GetIdentifiers(valueMarshaller.TypeInfo).managed};\r\n"
                };
            }

            return statements with
            {
                InvokeStatement = $"{invokeContext.GetIdentifiers(marshallers.ManagedReturnMarshaller.TypeInfo).managed} = {propertyAccess};\r\n"
            };
        }

        private static string GenerateStatementForNativeInvoke(BoundGenerators marshallers, StubIdentifierContext context, string expressionToInvoke)
        {
            string arguments = string.Join(", ", marshallers.NativeParameterMarshallers.Select(marshaller => marshaller.AsArgument(context)));
            string invoke = $"{expressionToInvoke}({arguments});\r\n";
            if (marshallers.NativeReturnMarshaller.TypeInfo.ManagedType == SpecialTypeInfo.Void)
            {
                return invoke;
            }

            (string managed, string native) = context.GetIdentifiers(marshallers.NativeReturnMarshaller.TypeInfo);
            string targetIdentifier = marshallers.NativeReturnMarshaller.UsesNativeIdentifier ? native : managed;
            return $"{targetIdentifier} = {invoke}";
        }

        private static string GenerateStatementForManagedInvoke(BoundGenerators marshallers, StubIdentifierContext context, string expressionToInvoke)
        {
            string arguments = string.Join(", ", marshallers.ManagedParameterMarshallers.Select(marshaller => marshaller.AsManagedArgument(context)));
            string invoke = $"{expressionToInvoke}({arguments});\r\n";
            if (marshallers.ManagedReturnMarshaller.TypeInfo.ManagedType == SpecialTypeInfo.Void)
            {
                return invoke;
            }

            return $"{context.GetIdentifiers(marshallers.ManagedReturnMarshaller.TypeInfo).managed} = {invoke}";
        }

        private static string GenerateCatchClauseForManagedException(BoundGenerators marshallers, StubIdentifierContext context, IndentedTextWriter writer)
        {
            if (!marshallers.HasManagedExceptionMarshaller)
            {
                return "";
            }

            writer.Clear();
            IBoundMarshallingGenerator marshaller = marshallers.ManagedExceptionMarshaller;
            string managed = context.GetIdentifiers(marshaller.TypeInfo).managed;
            writer.WriteLine($"catch ({TypeNames.GlobalAlias}{TypeNames.System_Exception} {managed})");
            using (writer.WriteBlock())
            {
                marshaller.Generate(writer, context with { CurrentStage = StubIdentifierContext.Stage.Marshal });
                marshaller.Generate(writer, context with { CurrentStage = StubIdentifierContext.Stage.PinnedMarshal });
            }
            return writer.ToString();
        }

        private static string GetStageDescription(StubIdentifierContext.Stage stage)
        {
            return stage switch
            {
                StubIdentifierContext.Stage.Setup => "Perform required setup.",
                StubIdentifierContext.Stage.Marshal => "Convert managed data to native data.",
                StubIdentifierContext.Stage.Pin => "Pin data in preparation for calling the P/Invoke.",
                StubIdentifierContext.Stage.PinnedMarshal => "Convert managed data to native data that requires the managed data to be pinned.",
                StubIdentifierContext.Stage.Invoke => "Call the P/Invoke.",
                StubIdentifierContext.Stage.UnmarshalCapture => "Capture the native data into marshaller instances in case conversion to managed data throws an exception.",
                StubIdentifierContext.Stage.Unmarshal => "Convert native data to managed data.",
                StubIdentifierContext.Stage.CleanupCallerAllocated => "Perform cleanup of caller allocated resources.",
                StubIdentifierContext.Stage.CleanupCalleeAllocated => "Perform cleanup of callee allocated resources.",
                StubIdentifierContext.Stage.NotifyForSuccessfulInvoke => "Keep alive any managed objects that need to stay alive across the call.",
                StubIdentifierContext.Stage.GuaranteedUnmarshal => "Convert native data to managed data even in the case of an exception during the non-cleanup phases.",
                _ => throw new ArgumentOutOfRangeException(nameof(stage))
            };
        }
    }
}
