// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.Interop
{
    /// <summary>
    /// Class for reporting diagnostics in the library import generator
    /// </summary>
    public static class GeneratorDiagnostics
    {
        public class Ids
        {
            // SYSLIB1070-SYSLIB1089 are reserved for JSImportGenerator
            public const string Prefix = "SYSLIB";
            public const string InvalidJSImportAttributeUsage = Prefix + "1070";
            public const string InvalidJSExportAttributeUsage = Prefix + "1071";
            public const string TypeNotSupported = Prefix + "1072";
            public const string ConfigurationNotSupported = Prefix + "1073";
            public const string JSImportRequiresAllowUnsafeBlocks = Prefix + "1074";
            public const string JSExportRequiresAllowUnsafeBlocks = Prefix + "1075";
        }

        private const string Category = "JSImportGenerator";

        public static readonly DiagnosticDescriptor ConfigurationNotSupported =
            DiagnosticDescriptorHelper.Create(
                Ids.ConfigurationNotSupported,
                GetResourceString(nameof(SR.ConfigurationNotSupportedTitle)),
                GetResourceString(nameof(SR.ConfigurationNotSupportedMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ConfigurationNotSupportedDescription)));

        public static readonly DiagnosticDescriptor ConfigurationValueNotSupported =
            DiagnosticDescriptorHelper.Create(
                Ids.ConfigurationNotSupported,
                GetResourceString(nameof(SR.ConfigurationNotSupportedTitle)),
                GetResourceString(nameof(SR.ConfigurationNotSupportedMessageValue)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ConfigurationNotSupportedDescription)));

        public static readonly DiagnosticDescriptor MarshallingAttributeConfigurationNotSupported =
            DiagnosticDescriptorHelper.Create(
                Ids.ConfigurationNotSupported,
                GetResourceString(nameof(SR.ConfigurationNotSupportedTitle)),
                GetResourceString(nameof(SR.ConfigurationNotSupportedMessageMarshallingInfo)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ConfigurationNotSupportedDescription)));

        public static readonly DiagnosticDescriptor ReturnTypeNotSupportedWithDetails =
            DiagnosticDescriptorHelper.Create(
                Ids.TypeNotSupported,
                GetResourceString(nameof(SR.TypeNotSupportedTitle)),
                GetResourceString(nameof(SR.TypeNotSupportedMessageReturnWithDetails)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.TypeNotSupportedDescription)));

        public static readonly DiagnosticDescriptor ParameterTypeNotSupported =
            DiagnosticDescriptorHelper.Create(
                Ids.TypeNotSupported,
                GetResourceString(nameof(SR.TypeNotSupportedTitle)),
                GetResourceString(nameof(SR.TypeNotSupportedMessageParameter)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.TypeNotSupportedDescription)));

        public static readonly DiagnosticDescriptor ReturnTypeNotSupported =
            DiagnosticDescriptorHelper.Create(
                Ids.TypeNotSupported,
                GetResourceString(nameof(SR.TypeNotSupportedTitle)),
                GetResourceString(nameof(SR.TypeNotSupportedMessageReturn)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.TypeNotSupportedDescription)));

        public static readonly DiagnosticDescriptor ParameterTypeNotSupportedWithDetails =
            DiagnosticDescriptorHelper.Create(
                Ids.TypeNotSupported,
                GetResourceString(nameof(SR.TypeNotSupportedTitle)),
                GetResourceString(nameof(SR.TypeNotSupportedMessageParameterWithDetails)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.TypeNotSupportedDescription)));

        public static readonly DiagnosticDescriptor ParameterConfigurationNotSupported =
            DiagnosticDescriptorHelper.Create(
                Ids.ConfigurationNotSupported,
                GetResourceString(nameof(SR.ConfigurationNotSupportedTitle)),
                GetResourceString(nameof(SR.ConfigurationNotSupportedMessageParameter)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ConfigurationNotSupportedDescription)));

        public static readonly DiagnosticDescriptor ReturnConfigurationNotSupported =
            DiagnosticDescriptorHelper.Create(
                Ids.ConfigurationNotSupported,
                GetResourceString(nameof(SR.ConfigurationNotSupportedTitle)),
                GetResourceString(nameof(SR.ConfigurationNotSupportedMessageReturn)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ConfigurationNotSupportedDescription)));

        public static readonly DiagnosticDescriptor InvalidImportAttributedMethodSignature =
            DiagnosticDescriptorHelper.Create(
            Ids.InvalidJSImportAttributeUsage,
            GetResourceString(nameof(SR.InvalidJSImportAttributeUsageTitle)),
            GetResourceString(nameof(SR.InvalidJSImportAttributedMethodSignatureMessage)),
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: GetResourceString(nameof(SR.InvalidJSImportAttributedMethodDescription)));

        public static readonly DiagnosticDescriptor InvalidExportAttributedMethodSignature =
            DiagnosticDescriptorHelper.Create(
            Ids.InvalidJSExportAttributeUsage,
            GetResourceString(nameof(SR.InvalidJSExportAttributeUsageTitle)),
            GetResourceString(nameof(SR.InvalidJSExportAttributedMethodSignatureMessage)),
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: GetResourceString(nameof(SR.InvalidJSExportAttributedMethodDescription)));

        public static readonly DiagnosticDescriptor InvalidImportAttributedMethodContainingTypeMissingModifiers =
            DiagnosticDescriptorHelper.Create(
            Ids.InvalidJSImportAttributeUsage,
            GetResourceString(nameof(SR.InvalidJSImportAttributeUsageTitle)),
            GetResourceString(nameof(SR.InvalidAttributedMethodContainingTypeMissingModifiersMessage)),
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: GetResourceString(nameof(SR.InvalidJSImportAttributedMethodDescription)));

        public static readonly DiagnosticDescriptor InvalidExportAttributedMethodContainingTypeMissingModifiers =
            DiagnosticDescriptorHelper.Create(
            Ids.InvalidJSExportAttributeUsage,
            GetResourceString(nameof(SR.InvalidJSExportAttributeUsageTitle)),
            GetResourceString(nameof(SR.InvalidAttributedMethodContainingTypeMissingModifiersMessage)),
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: GetResourceString(nameof(SR.InvalidJSExportAttributedMethodDescription)));

        public static readonly DiagnosticDescriptor JSImportRequiresAllowUnsafeBlocks =
                   DiagnosticDescriptorHelper.Create(
                       Ids.JSImportRequiresAllowUnsafeBlocks,
                       GetResourceString(nameof(SR.JSImportRequiresAllowUnsafeBlocksTitle)),
                       GetResourceString(nameof(SR.JSImportRequiresAllowUnsafeBlocksMessage)),
                       Category,
                       DiagnosticSeverity.Error,
                       isEnabledByDefault: true,
                       description: GetResourceString(nameof(SR.JSImportRequiresAllowUnsafeBlocksDescription)));

        public static readonly DiagnosticDescriptor JSExportRequiresAllowUnsafeBlocks =
                   DiagnosticDescriptorHelper.Create(
                       Ids.JSExportRequiresAllowUnsafeBlocks,
                       GetResourceString(nameof(SR.JSExportRequiresAllowUnsafeBlocksTitle)),
                       GetResourceString(nameof(SR.JSExportRequiresAllowUnsafeBlocksMessage)),
                       Category,
                       DiagnosticSeverity.Error,
                       isEnabledByDefault: true,
                       description: GetResourceString(nameof(SR.JSExportRequiresAllowUnsafeBlocksDescription)));

        private static LocalizableResourceString GetResourceString(string resourceName)
        {
            return new LocalizableResourceString(resourceName, SR.ResourceManager, typeof(FxResources.Microsoft.Interop.JavaScript.JSImportGenerator.SR));
        }
    }
}
