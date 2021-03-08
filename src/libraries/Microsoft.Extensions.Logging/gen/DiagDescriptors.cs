// © Microsoft Corporation. All rights reserved.

using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Logging.Generators
{
    internal static class DiagDescriptors
    {
        public static DiagnosticDescriptor ErrorInvalidMethodName { get; } = new (
            id: "LG0000",
            title: Resources.ErrorInvalidMethodNameTitle,
            messageFormat: Resources.ErrorInvalidMethodNameMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ErrorInvalidMessage { get; } = new (
            id: "LG0001",
            title: Resources.ErrorInvalidMessageTitle,
            messageFormat: Resources.ErrorInvalidMessageMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ErrorInvalidParameterName { get; } = new (
            id: "LG0002",
            title: Resources.ErrorInvalidParameterNameTitle,
            messageFormat: Resources.ErrorInvalidParameterNameMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ErrorNestedType { get; } = new (
            id: "LG0003",
            title: Resources.ErrorNestedTypeTitle,
            messageFormat: Resources.ErrorNestedTypeMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ErrorMissingRequiredType { get; } = new (
            id: "LG0004",
            title: Resources.ErrorMissingRequiredTypeTitle,
            messageFormat: Resources.ErrorMissingRequiredTypeMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ErrorEventIdReuse { get; } = new (
            id: "LG0005",
            title: Resources.ErrorEventIdReuseTitle,
            messageFormat: Resources.ErrorEventIdReuseMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ErrorInvalidMethodReturnType { get; } = new (
            id: "LG0006",
            title: Resources.ErrorInvalidMethodReturnTypeTitle,
            messageFormat: Resources.ErrorInvalidMethodReturnTypeMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ErrorFirstArgMustBeILogger { get; } = new (
            id: "LG0007",
            title: Resources.ErrorFirstArgMustBeILoggerTitle,
            messageFormat: Resources.ErrorFirstArgMustBeILoggerMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ErrorNotStaticMethod { get; } = new (
            id: "LG0008",
            title: Resources.ErrorNotStaticMethodTitle,
            messageFormat: Resources.ErrorNotStaticMethodMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ErrorNotPartialMethod { get; } = new (
            id: "LG0009",
            title: Resources.ErrorNotPartialMethodTitle,
            messageFormat: Resources.ErrorNotPartialMethodMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ErrorMethodIsGeneric { get; } = new (
            id: "LG0010",
            title: Resources.ErrorMethodIsGenericTitle,
            messageFormat: Resources.ErrorMethodIsGenericMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor RedundantQualifierInMessage { get; } = new (
            id: "LG0011",
            title: Resources.RedundantQualifierInMessageTitle,
            messageFormat: Resources.RedundantQualifierInMessageMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor PassingDateTime { get; } = new (
            id: "LG0012",
            title: Resources.PassingDateTimeTitle,
            messageFormat: Resources.PassingDateTimeMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor DontMentionExceptionInMessage { get; } = new (
            id: "LG0013",
            title: Resources.DontMentionExceptionInMessageTitle,
            messageFormat: Resources.DontMentionExceptionInMessageMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor TemplateHasNoCorrespondingArgument { get; } = new (
            id: "LG0014",
            title: Resources.TemplateHasNoCorrespondingArgumentTitle,
            messageFormat: Resources.TemplateHasNoCorrespondingArgumentMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ArgumentHasNoCorrespondingTemplate { get; } = new (
            id: "LG0015",
            title: Resources.ArgumentHasNoCorrespondingTemplateTitle,
            messageFormat: Resources.ArgumentHasNoCorrespondingTemplateMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ErrorMethodHasBody { get; } = new (
            id: "LG0016",
            title: Resources.ErrorMethodHasBodyTitle,
            messageFormat: Resources.ErrorMethodHasBodyMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }
}
