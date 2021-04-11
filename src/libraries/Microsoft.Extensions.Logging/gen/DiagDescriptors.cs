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
            title: new LocalizableResourceString(nameof(SR.InvalidLoggingMethodNameTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.InvalidLoggingMethodNameMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ShouldntMentionLogLevelInMessage { get; } = new (
            id: "LG0001",
            title: new LocalizableResourceString(nameof(SR.ShouldntMentionLogLevelInMessageTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.ShouldntMentionLogLevelInMessageMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor InvalidLoggingMethodParameterName { get; } = new (
            id: "LG0002",
            title: new LocalizableResourceString(nameof(SR.InvalidLoggingMethodParameterNameTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.InvalidLoggingMethodParameterNameMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor LoggingMethodInNestedType { get; } = new (
            id: "LG0003",
            title: new LocalizableResourceString(nameof(SR.LoggingMethodInNestedTypeTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.LoggingMethodInNestedTypeMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor MissingRequiredType { get; } = new (
            id: "LG0004",
            title: new LocalizableResourceString(nameof(SR.MissingRequiredTypeTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.MissingRequiredTypeMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ShouldntReuseEventIds { get; } = new (
            id: "LG0005",
            title: new LocalizableResourceString(nameof(SR.ShouldntReuseEventIdsTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.ShouldntReuseEventIdsMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor LoggingMethodMustReturnVoid { get; } = new (
            id: "LG0006",
            title: new LocalizableResourceString(nameof(SR.LoggingMethodMustReturnVoidTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.LoggingMethodMustReturnVoidMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor MissingLoggerArgument { get; } = new (
            id: "LG0007",
            title: new LocalizableResourceString(nameof(SR.MissingLoggerArgumentTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.MissingLoggerArgumentMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor LoggingMethodShouldBeStatic { get; } = new (
            id: "LG0008",
            title: new LocalizableResourceString(nameof(SR.LoggingMethodShouldBeStaticTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.LoggingMethodShouldBeStaticMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor LoggingMethodMustBePartial { get; } = new (
            id: "LG0009",
            title: new LocalizableResourceString(nameof(SR.LoggingMethodMustBePartialTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.LoggingMethodMustBePartialMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor LoggingMethodIsGeneric { get; } = new (
            id: "LG0010",
            title: new LocalizableResourceString(nameof(SR.LoggingMethodIsGenericTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.LoggingMethodIsGenericMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor RedundantQualifierInMessage { get; } = new (
            id: "LG0011",
            title: new LocalizableResourceString(nameof(SR.RedundantQualifierInMessageTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.RedundantQualifierInMessageMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        // TODO: LG0012 is currently unused

        public static DiagnosticDescriptor ShouldntMentionExceptionInMessage { get; } = new (
            id: "LG0013",
            title: new LocalizableResourceString(nameof(SR.ShouldntMentionExceptionInMessageTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.ShouldntMentionExceptionInMessageMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor TemplateHasNoCorrespondingArgument { get; } = new (
            id: "LG0014",
            title: new LocalizableResourceString(nameof(SR.TemplateHasNoCorrespondingArgumentTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.TemplateHasNoCorrespondingArgumentMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ArgumentHasNoCorrespondingTemplate { get; } = new (
            id: "LG0015",
            title: new LocalizableResourceString(nameof(SR.ArgumentHasNoCorrespondingTemplateTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.ArgumentHasNoCorrespondingTemplateMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor LoggingMethodHasBody { get; } = new (
            id: "LG0016",
            title: new LocalizableResourceString(nameof(SR.LoggingMethodHasBodyTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.LoggingMethodHasBodyMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor MissingLogLevel { get; } = new (
            id: "LG0017",
            title: new LocalizableResourceString(nameof(SR.MissingLogLevelTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.MissingLogLevelMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ShouldntMentionLoggerInMessage { get; } = new (
            id: "LG0018",
            title: new LocalizableResourceString(nameof(SR.ShouldntMentionLoggerInMessageTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.ShouldntMentionLoggerInMessageMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor MissingLoggerField { get; } = new (
            id: "LG0019",
            title: new LocalizableResourceString(nameof(SR.MissingLoggerFieldTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.MissingLoggerFieldMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor MultipleLoggerFields { get; } = new(
            id: "LG0020",
            title: new LocalizableResourceString(nameof(SR.MultipleLoggerFieldsTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.MultipleLoggerFieldsMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }
}
