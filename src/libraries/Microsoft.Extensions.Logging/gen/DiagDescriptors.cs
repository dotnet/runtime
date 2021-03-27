// © Microsoft Corporation. All rights reserved.

using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Logging.Generators
{
    internal static class DiagDescriptors
    {
        public static DiagnosticDescriptor InvalidLoggingMethodName { get; } = new (
            id: "LG0000",
            title: Resources.InvalidLoggingMethodNameTitle,
            messageFormat: Resources.InvalidLoggingMethodNameMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ShouldntMentionLogLevelInMessage { get; } = new (
            id: "LG0001",
            title: Resources.ShouldntMentionLogLevelInMessageTitle,
            messageFormat: Resources.ShouldntMentionLogLevelInMessageMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor InvalidLoggingMethodParameterName { get; } = new (
            id: "LG0002",
            title: Resources.InvalidLoggingMethodParameterNameTitle,
            messageFormat: Resources.InvalidLoggingMethodParameterNameMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor LoggingMethodInNestedType { get; } = new (
            id: "LG0003",
            title: Resources.LoggingMethodInNestedTypeTitle,
            messageFormat: Resources.LoggingMethodInNestedTypeMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor MissingRequiredType { get; } = new (
            id: "LG0004",
            title: Resources.MissingRequiredTypeTitle,
            messageFormat: Resources.MissingRequiredTypeMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ShouldntReuseEventIds { get; } = new (
            id: "LG0005",
            title: Resources.ShouldntReuseEventIdsTitle,
            messageFormat: Resources.ShouldntReuseEventIdsMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor LoggingMethodMustReturnVoid { get; } = new (
            id: "LG0006",
            title: Resources.LoggingMethodMustReturnVoidTitle,
            messageFormat: Resources.LoggingMethodMustReturnVoidMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor MissingLoggerArgument { get; } = new (
            id: "LG0007",
            title: Resources.MissingLoggerArgumentTitle,
            messageFormat: Resources.MissingLoggerArgumentMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor LoggingMethodShouldBeStatic { get; } = new (
            id: "LG0008",
            title: Resources.LoggingMethodShouldBeStaticTitle,
            messageFormat: Resources.LoggingMethodShouldBeStaticMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor LoggingMethodMustBePartial { get; } = new (
            id: "LG0009",
            title: Resources.LoggingMethodMustBePartialTitle,
            messageFormat: Resources.LoggingMethodMustBePartialMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor LoggingMethodIsGeneric { get; } = new (
            id: "LG0010",
            title: Resources.LoggingMethodIsGenericTitle,
            messageFormat: Resources.LoggingMethodIsGenericMessage,
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

        public static DiagnosticDescriptor ShouldntMentionExceptionInMessage { get; } = new (
            id: "LG0013",
            title: Resources.ShouldntMentionExceptionInMessageTitle,
            messageFormat: Resources.ShouldntMentionExceptionInMessageMessage,
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
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor LoggingMethodHasBody { get; } = new (
            id: "LG0016",
            title: Resources.LoggingMethodHasBodyTitle,
            messageFormat: Resources.LoggingMethodHasBodyMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor MissingLogLevel { get; } = new (
            id: "LG0017",
            title: Resources.MissingLogLevelTitle,
            messageFormat: Resources.MissingLogLevelMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor DontMentionLoggerInMessage { get; } = new (
            id: "LG0018",
            title: Resources.DontMentionLoggerInMessageTitle,
            messageFormat: Resources.DontMentionLoggerInMessageMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor MissingLoggerField { get; } = new (
            id: "LG0019",
            title: Resources.MissingLoggerFieldTitle,
            messageFormat: Resources.MissingLoggerFieldMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor MultipleLoggerFields { get; } = new(
            id: "LG0020",
            title: Resources.MultipleLoggerFieldsTitle,
            messageFormat: Resources.MultipleLoggerFieldsMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }
}
