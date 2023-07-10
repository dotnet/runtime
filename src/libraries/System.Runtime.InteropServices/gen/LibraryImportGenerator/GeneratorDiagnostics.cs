// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    /// <summary>
    /// Class for reporting diagnostics in the library import generator
    /// </summary>
    public static class GeneratorDiagnostics
    {
        public class Ids
        {
            // SYSLIB1050-SYSLIB1069 are reserved for LibraryImportGenerator
            public const string Prefix = "SYSLIB";
            public const string InvalidLibraryImportAttributeUsage = Prefix + "1050";
            public const string TypeNotSupported = Prefix + "1051";
            public const string ConfigurationNotSupported = Prefix + "1052";
            public const string CannotForwardToDllImport = Prefix + "1053";

            public const string RequiresAllowUnsafeBlocks = Prefix + "1062";
            public const string UnnecessaryMarshallingInfo = Prefix + "1063";
        }

        private const string Category = "LibraryImportGenerator";

        public static readonly DiagnosticDescriptor InvalidAttributedMethodSignature =
            new DiagnosticDescriptor(
            Ids.InvalidLibraryImportAttributeUsage,
            GetResourceString(nameof(SR.InvalidLibraryImportAttributeUsageTitle)),
            GetResourceString(nameof(SR.InvalidAttributedMethodSignatureMessage)),
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: GetResourceString(nameof(SR.InvalidAttributedMethodDescription)));

        public static readonly DiagnosticDescriptor InvalidAttributedMethodContainingTypeMissingModifiers =
            new DiagnosticDescriptor(
            Ids.InvalidLibraryImportAttributeUsage,
            GetResourceString(nameof(SR.InvalidLibraryImportAttributeUsageTitle)),
            GetResourceString(nameof(SR.InvalidAttributedMethodContainingTypeMissingModifiersMessage)),
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: GetResourceString(nameof(SR.InvalidAttributedMethodDescription)));

        public static readonly DiagnosticDescriptor InvalidStringMarshallingConfiguration =
            new DiagnosticDescriptor(
            Ids.InvalidLibraryImportAttributeUsage,
            GetResourceString(nameof(SR.InvalidLibraryImportAttributeUsageTitle)),
            GetResourceString(nameof(SR.InvalidStringMarshallingConfigurationMessage)),
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: GetResourceString(nameof(SR.InvalidStringMarshallingConfigurationDescription)));

        public static readonly DiagnosticDescriptor ParameterTypeNotSupported =
            new DiagnosticDescriptor(
                Ids.TypeNotSupported,
                GetResourceString(nameof(SR.TypeNotSupportedTitle)),
                GetResourceString(nameof(SR.TypeNotSupportedMessageParameter)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.TypeNotSupportedDescription)));

        public static readonly DiagnosticDescriptor ReturnTypeNotSupported =
            new DiagnosticDescriptor(
                Ids.TypeNotSupported,
                GetResourceString(nameof(SR.TypeNotSupportedTitle)),
                GetResourceString(nameof(SR.TypeNotSupportedMessageReturn)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.TypeNotSupportedDescription)));

        public static readonly DiagnosticDescriptor ParameterTypeNotSupportedWithDetails =
            new DiagnosticDescriptor(
                Ids.TypeNotSupported,
                GetResourceString(nameof(SR.TypeNotSupportedTitle)),
                GetResourceString(nameof(SR.TypeNotSupportedMessageParameterWithDetails)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.TypeNotSupportedDescription)));

        public static readonly DiagnosticDescriptor ReturnTypeNotSupportedWithDetails =
            new DiagnosticDescriptor(
                Ids.TypeNotSupported,
                GetResourceString(nameof(SR.TypeNotSupportedTitle)),
                GetResourceString(nameof(SR.TypeNotSupportedMessageReturnWithDetails)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.TypeNotSupportedDescription)));

        public static readonly DiagnosticDescriptor ParameterConfigurationNotSupported =
            new DiagnosticDescriptor(
                Ids.ConfigurationNotSupported,
                GetResourceString(nameof(SR.ConfigurationNotSupportedTitle)),
                GetResourceString(nameof(SR.ConfigurationNotSupportedMessageParameter)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ConfigurationNotSupportedDescription)));

        public static readonly DiagnosticDescriptor ReturnConfigurationNotSupported =
            new DiagnosticDescriptor(
                Ids.ConfigurationNotSupported,
                GetResourceString(nameof(SR.ConfigurationNotSupportedTitle)),
                GetResourceString(nameof(SR.ConfigurationNotSupportedMessageReturn)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ConfigurationNotSupportedDescription)));

        public static readonly DiagnosticDescriptor MarshalAsParameterConfigurationNotSupported =
            new DiagnosticDescriptor(
                GeneratorDiagnostics.Ids.ConfigurationNotSupported,
                GetResourceString(nameof(SR.ConfigurationNotSupportedTitle)),
                GetResourceString(nameof(SR.MarshalAsConfigurationNotSupportedMessageParameter)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ConfigurationNotSupportedDescription)));

        public static readonly DiagnosticDescriptor MarshalAsReturnConfigurationNotSupported =
            new DiagnosticDescriptor(
                GeneratorDiagnostics.Ids.ConfigurationNotSupported,
                GetResourceString(nameof(SR.ConfigurationNotSupportedTitle)),
                GetResourceString(nameof(SR.MarshalAsConfigurationNotSupportedMessageReturn)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ConfigurationNotSupportedDescription)));

        public static readonly DiagnosticDescriptor ConfigurationNotSupported =
            new DiagnosticDescriptor(
                Ids.ConfigurationNotSupported,
                GetResourceString(nameof(SR.ConfigurationNotSupportedTitle)),
                GetResourceString(nameof(SR.ConfigurationNotSupportedMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ConfigurationNotSupportedDescription)));

        public static readonly DiagnosticDescriptor ConfigurationValueNotSupported =
            new DiagnosticDescriptor(
                Ids.ConfigurationNotSupported,
                GetResourceString(nameof(SR.ConfigurationNotSupportedTitle)),
                GetResourceString(nameof(SR.ConfigurationNotSupportedMessageValue)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ConfigurationNotSupportedDescription)));

        public static readonly DiagnosticDescriptor MarshallingAttributeConfigurationNotSupported =
            new DiagnosticDescriptor(
                Ids.ConfigurationNotSupported,
                GetResourceString(nameof(SR.ConfigurationNotSupportedTitle)),
                GetResourceString(nameof(SR.ConfigurationNotSupportedMessageMarshallingInfo)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ConfigurationNotSupportedDescription)));

        public static readonly DiagnosticDescriptor CannotForwardToDllImport =
            new DiagnosticDescriptor(
                Ids.CannotForwardToDllImport,
                GetResourceString(nameof(SR.CannotForwardToDllImportTitle)),
                GetResourceString(nameof(SR.CannotForwardToDllImportMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.CannotForwardToDllImportDescription)));

        public static readonly DiagnosticDescriptor RequiresAllowUnsafeBlocks =
            new DiagnosticDescriptor(
                Ids.RequiresAllowUnsafeBlocks,
                GetResourceString(nameof(SR.RequiresAllowUnsafeBlocksTitle)),
                GetResourceString(nameof(SR.RequiresAllowUnsafeBlocksMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.RequiresAllowUnsafeBlocksDescription)));

        public static readonly DiagnosticDescriptor UnnecessaryParameterMarshallingInfo =
            new DiagnosticDescriptor(
                Ids.UnnecessaryMarshallingInfo,
                GetResourceString(nameof(SR.UnnecessaryMarshallingInfoTitle)),
                GetResourceString(nameof(SR.UnnecessaryParameterMarshallingInfoMessage)),
                Category,
                DiagnosticSeverity.Info,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.UnnecessaryMarshallingInfoDescription)),
                customTags: new[]
                {
                    WellKnownDiagnosticTags.Unnecessary
                });
        public static readonly DiagnosticDescriptor UnnecessaryReturnMarshallingInfo =
            new DiagnosticDescriptor(
                Ids.UnnecessaryMarshallingInfo,
                GetResourceString(nameof(SR.UnnecessaryMarshallingInfoTitle)),
                GetResourceString(nameof(SR.UnnecessaryReturnMarshallingInfoMessage)),
                Category,
                DiagnosticSeverity.Info,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.UnnecessaryMarshallingInfoDescription)),
                customTags: new[]
                {
                    WellKnownDiagnosticTags.Unnecessary
                });

        /// <summary>
        /// Report diagnostic for invalid configuration for string marshalling.
        /// </summary>
        /// <param name="attributeData">Attribute specifying the invalid configuration</param>
        /// <param name="methodName">Name of the method</param>
        /// <param name="detailsMessage">Specific reason the configuration is invalid</param>
        public static void ReportInvalidStringMarshallingConfiguration(
            this GeneratorDiagnosticsBag diagnostics,
            AttributeData attributeData,
            string methodName,
            string detailsMessage)
        {
            diagnostics.ReportDiagnostic(
                attributeData.CreateDiagnosticInfo(
                    GeneratorDiagnostics.InvalidStringMarshallingConfiguration,
                    methodName,
                    detailsMessage));
        }

        /// <summary>
        /// Report diagnostic for configuration that cannot be forwarded to <see cref="DllImportAttribute" />
        /// </summary>
        /// <param name="method">Method with the configuration that cannot be forwarded</param>
        /// <param name="name">Configuration name</param>
        /// <param name="value">Configuration value</param>
        public static void ReportCannotForwardToDllImport(this GeneratorDiagnosticsBag diagnostics, MethodSignatureDiagnosticLocations method, string name, string? value = null)
        {
            diagnostics.ReportDiagnostic(
                DiagnosticInfo.Create(
                    CannotForwardToDllImport,
                    method.FallbackLocation,
                    value is null ? name : $"{name}={value}"));
        }

        private static LocalizableResourceString GetResourceString(string resourceName)
        {
            return new LocalizableResourceString(resourceName, SR.ResourceManager, typeof(FxResources.Microsoft.Interop.LibraryImportGenerator.SR));
        }
    }
}
