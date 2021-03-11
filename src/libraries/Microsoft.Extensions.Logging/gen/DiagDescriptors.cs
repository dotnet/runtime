// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Logging.Generators
{
    internal static class DiagDescriptors
    {
        public static DiagnosticDescriptor ErrorInvalidMethodName { get; } = new (
            id: "LG0000",
            title: SR.ErrorInvalidMethodNameTitle,
            messageFormat: SR.ErrorInvalidMethodNameMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ErrorInvalidMessage { get; } = new (
            id: "LG0001",
            title: SR.ErrorInvalidMessageTitle,
            messageFormat: SR.ErrorInvalidMessageMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ErrorInvalidParameterName { get; } = new (
            id: "LG0002",
            title: SR.ErrorInvalidParameterNameTitle,
            messageFormat: SR.ErrorInvalidParameterNameMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ErrorNestedType { get; } = new (
            id: "LG0003",
            title: SR.ErrorNestedTypeTitle,
            messageFormat: SR.ErrorNestedTypeMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ErrorMissingRequiredType { get; } = new (
            id: "LG0004",
            title: SR.ErrorMissingRequiredTypeTitle,
            messageFormat: SR.ErrorMissingRequiredTypeMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ErrorEventIdReuse { get; } = new (
            id: "LG0005",
            title: SR.ErrorEventIdReuseTitle,
            messageFormat: SR.ErrorEventIdReuseMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ErrorInvalidMethodReturnType { get; } = new (
            id: "LG0006",
            title: SR.ErrorInvalidMethodReturnTypeTitle,
            messageFormat: SR.ErrorInvalidMethodReturnTypeMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ErrorFirstArgMustBeILogger { get; } = new (
            id: "LG0007",
            title: SR.ErrorFirstArgMustBeILoggerTitle,
            messageFormat: SR.ErrorFirstArgMustBeILoggerMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ErrorNotStaticMethod { get; } = new (
            id: "LG0008",
            title: SR.ErrorNotStaticMethodTitle,
            messageFormat: SR.ErrorNotStaticMethodMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ErrorNotPartialMethod { get; } = new (
            id: "LG0009",
            title: SR.ErrorNotPartialMethodTitle,
            messageFormat: SR.ErrorNotPartialMethodMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ErrorMethodIsGeneric { get; } = new (
            id: "LG0010",
            title: SR.ErrorMethodIsGenericTitle,
            messageFormat: SR.ErrorMethodIsGenericMessage,
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

        public static DiagnosticDescriptor PassingDateTime { get; } = new (
            id: "LG0012",
            title: SR.PassingDateTimeTitle,
            messageFormat: SR.PassingDateTimeMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor DontMentionExceptionInMessage { get; } = new (
            id: "LG0013",
            title: SR.DontMentionExceptionInMessageTitle,
            messageFormat: SR.DontMentionExceptionInMessageMessage,
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
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor ErrorMethodHasBody { get; } = new (
            id: "LG0016",
            title: SR.ErrorMethodHasBodyTitle,
            messageFormat: SR.ErrorMethodHasBodyMessage,
            category: "LoggingGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }
}
