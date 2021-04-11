// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Logging.Generators
{
    internal static class DiagDescriptors
    {
        public static DiagnosticDescriptor InvalidLoggingMethodName { get; } = new (
            id: "LG0000",
            title: SR.InvalidLoggingMethodNameTitle,
            messageFormat: SR.InvalidLoggingMethodNameMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ShouldntMentionLogLevelInMessage { get; } = new (
            id: "LG0001",
            title: SR.ShouldntMentionLogLevelInMessageTitle,
            messageFormat: SR.ShouldntMentionLogLevelInMessageMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor InvalidLoggingMethodParameterName { get; } = new (
            id: "LG0002",
            title: SR.InvalidLoggingMethodParameterNameTitle,
            messageFormat: SR.InvalidLoggingMethodParameterNameMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor LoggingMethodInNestedType { get; } = new (
            id: "LG0003",
            title: SR.LoggingMethodInNestedTypeTitle,
            messageFormat: SR.LoggingMethodInNestedTypeMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor MissingRequiredType { get; } = new (
            id: "LG0004",
            title: SR.MissingRequiredTypeTitle,
            messageFormat: SR.MissingRequiredTypeMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ShouldntReuseEventIds { get; } = new (
            id: "LG0005",
            title: SR.ShouldntReuseEventIdsTitle,
            messageFormat: SR.ShouldntReuseEventIdsMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor LoggingMethodMustReturnVoid { get; } = new (
            id: "LG0006",
            title: SR.LoggingMethodMustReturnVoidTitle,
            messageFormat: SR.LoggingMethodMustReturnVoidMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor MissingLoggerArgument { get; } = new (
            id: "LG0007",
            title: SR.MissingLoggerArgumentTitle,
            messageFormat: SR.MissingLoggerArgumentMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor LoggingMethodShouldBeStatic { get; } = new (
            id: "LG0008",
            title: SR.LoggingMethodShouldBeStaticTitle,
            messageFormat: SR.LoggingMethodShouldBeStaticMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor LoggingMethodMustBePartial { get; } = new (
            id: "LG0009",
            title: SR.LoggingMethodMustBePartialTitle,
            messageFormat: SR.LoggingMethodMustBePartialMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor LoggingMethodIsGeneric { get; } = new (
            id: "LG0010",
            title: SR.LoggingMethodIsGenericTitle,
            messageFormat: SR.LoggingMethodIsGenericMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor RedundantQualifierInMessage { get; } = new (
            id: "LG0011",
            title: SR.RedundantQualifierInMessageTitle,
            messageFormat: SR.RedundantQualifierInMessageMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        // TODO: LG0012 is currently unused

        public static DiagnosticDescriptor ShouldntMentionExceptionInMessage { get; } = new (
            id: "LG0013",
            title: SR.ShouldntMentionExceptionInMessageTitle,
            messageFormat: SR.ShouldntMentionExceptionInMessageMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor TemplateHasNoCorrespondingArgument { get; } = new (
            id: "LG0014",
            title: SR.TemplateHasNoCorrespondingArgumentTitle,
            messageFormat: SR.TemplateHasNoCorrespondingArgumentMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ArgumentHasNoCorrespondingTemplate { get; } = new (
            id: "LG0015",
            title: SR.ArgumentHasNoCorrespondingTemplateTitle,
            messageFormat: SR.ArgumentHasNoCorrespondingTemplateMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor LoggingMethodHasBody { get; } = new (
            id: "LG0016",
            title: SR.LoggingMethodHasBodyTitle,
            messageFormat: SR.LoggingMethodHasBodyMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor MissingLogLevel { get; } = new (
            id: "LG0017",
            title: SR.MissingLogLevelTitle,
            messageFormat: SR.MissingLogLevelMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ShouldntMentionLoggerInMessage { get; } = new (
            id: "LG0018",
            title: SR.ShouldntMentionLoggerInMessageTitle,
            messageFormat: SR.ShouldntMentionLoggerInMessageMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor MissingLoggerField { get; } = new (
            id: "LG0019",
            title: SR.MissingLoggerFieldTitle,
            messageFormat: SR.MissingLoggerFieldMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor MultipleLoggerFields { get; } = new(
            id: "LG0020",
            title: SR.MultipleLoggerFieldsTitle,
            messageFormat: SR.MultipleLoggerFieldsMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }
}
