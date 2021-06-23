// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Logging.Generators
{
    public static class DiagnosticDescriptors
    {
        public static DiagnosticDescriptor InvalidLoggingMethodName { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1001",
            title: new LocalizableResourceString(nameof(SR.InvalidLoggingMethodNameMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.InvalidLoggingMethodNameMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ShouldntMentionLogLevelInMessage { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1002",
            title: new LocalizableResourceString(nameof(SR.ShouldntMentionLogLevelInMessageTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.ShouldntMentionInTemplateMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor InvalidLoggingMethodParameterName { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1003",
            title: new LocalizableResourceString(nameof(SR.InvalidLoggingMethodParameterNameMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.InvalidLoggingMethodParameterNameMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor MissingRequiredType { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1005",
            title: new LocalizableResourceString(nameof(SR.MissingRequiredTypeTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.MissingRequiredTypeMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ShouldntReuseEventIds { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1006",
            title: new LocalizableResourceString(nameof(SR.ShouldntReuseEventIdsTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.ShouldntReuseEventIdsMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor LoggingMethodMustReturnVoid { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1007",
            title: new LocalizableResourceString(nameof(SR.LoggingMethodMustReturnVoidMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.LoggingMethodMustReturnVoidMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor MissingLoggerArgument { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1008",
            title: new LocalizableResourceString(nameof(SR.MissingLoggerArgumentTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.MissingLoggerArgumentMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor LoggingMethodShouldBeStatic { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1009",
            title: new LocalizableResourceString(nameof(SR.LoggingMethodShouldBeStaticMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.LoggingMethodShouldBeStaticMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor LoggingMethodMustBePartial { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1010",
            title: new LocalizableResourceString(nameof(SR.LoggingMethodMustBePartialMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.LoggingMethodMustBePartialMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor LoggingMethodIsGeneric { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1011",
            title: new LocalizableResourceString(nameof(SR.LoggingMethodIsGenericMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.LoggingMethodIsGenericMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor RedundantQualifierInMessage { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1012",
            title: new LocalizableResourceString(nameof(SR.RedundantQualifierInMessageTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.RedundantQualifierInMessageMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ShouldntMentionExceptionInMessage { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1013",
            title: new LocalizableResourceString(nameof(SR.ShouldntMentionExceptionInMessageTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.ShouldntMentionInTemplateMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor TemplateHasNoCorrespondingArgument { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1014",
            title: new LocalizableResourceString(nameof(SR.TemplateHasNoCorrespondingArgumentTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.TemplateHasNoCorrespondingArgumentMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ArgumentHasNoCorrespondingTemplate { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1015",
            title: new LocalizableResourceString(nameof(SR.ArgumentHasNoCorrespondingTemplateTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.ArgumentHasNoCorrespondingTemplateMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor LoggingMethodHasBody { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1016",
            title: new LocalizableResourceString(nameof(SR.LoggingMethodHasBodyMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.LoggingMethodHasBodyMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor MissingLogLevel { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1017",
            title: new LocalizableResourceString(nameof(SR.MissingLogLevelMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.MissingLogLevelMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ShouldntMentionLoggerInMessage { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1018",
            title: new LocalizableResourceString(nameof(SR.ShouldntMentionLoggerInMessageTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.ShouldntMentionInTemplateMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor MissingLoggerField { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1019",
            title: new LocalizableResourceString(nameof(SR.MissingLoggerFieldTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.MissingLoggerFieldMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor MultipleLoggerFields { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1020",
            title: new LocalizableResourceString(nameof(SR.MultipleLoggerFieldsTitle), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.MultipleLoggerFieldsMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor InconsistentTemplateCasing { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1021",
            title: new LocalizableResourceString(nameof(SR.InconsistentTemplateCasingMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.InconsistentTemplateCasingMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor MalformedFormatStrings { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1022",
            title: new LocalizableResourceString(nameof(SR.MalformedFormatStringsMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.MalformedFormatStringsMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor GeneratingForMax6Arguments { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1023",
            title: new LocalizableResourceString(nameof(SR.GeneratingForMax6ArgumentsMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            messageFormat: new LocalizableResourceString(nameof(SR.GeneratingForMax6ArgumentsMessage), SR.ResourceManager, typeof(FxResources.Microsoft.Extensions.Logging.Generators.SR)),
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }
}
