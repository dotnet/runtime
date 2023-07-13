// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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

        /// <inheritdoc cref="SR.InvalidAttributedMethodSignatureMessageLibraryImport"/>
        public static readonly DiagnosticDescriptor InvalidAttributedMethodSignature =
            new DiagnosticDescriptor(
            Ids.InvalidLibraryImportAttributeUsage,
            GetResourceString(nameof(SR.InvalidLibraryImportAttributeUsageTitle)),
            GetResourceString(nameof(SR.InvalidAttributedMethodSignatureMessageLibraryImport)),
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: GetResourceString(nameof(SR.InvalidAttributedMethodDescriptionLibraryImport)));

        /// <inheritdoc cref="SR.InvalidAttributedMethodContainingTypeMissingModifiersMessageLibraryImport"/>
        public static readonly DiagnosticDescriptor InvalidAttributedMethodContainingTypeMissingModifiers =
            new DiagnosticDescriptor(
            Ids.InvalidLibraryImportAttributeUsage,
            GetResourceString(nameof(SR.InvalidLibraryImportAttributeUsageTitle)),
            GetResourceString(nameof(SR.InvalidAttributedMethodContainingTypeMissingModifiersMessageLibraryImport)),
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: GetResourceString(nameof(SR.InvalidAttributedMethodDescriptionLibraryImport)));

        /// <inheritdoc cref="SR.InvalidStringMarshallingConfigurationMessageLibraryImport"/>
        public static readonly DiagnosticDescriptor InvalidStringMarshallingConfiguration =
            new DiagnosticDescriptor(
            Ids.InvalidLibraryImportAttributeUsage,
            GetResourceString(nameof(SR.InvalidLibraryImportAttributeUsageTitle)),
            GetResourceString(nameof(SR.InvalidStringMarshallingConfigurationMessageLibraryImport)),
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: GetResourceString(nameof(SR.InvalidStringMarshallingConfigurationDescription)));

        /// <inheritdoc cref="SR.TypeNotSupportedMessageParameterLibraryImport"/>
        public static readonly DiagnosticDescriptor ParameterTypeNotSupported =
            new DiagnosticDescriptor(
                Ids.TypeNotSupported,
                GetResourceString(nameof(SR.TypeNotSupportedTitle)),
                GetResourceString(nameof(SR.TypeNotSupportedMessageParameterLibraryImport)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.TypeNotSupportedDescriptionLibraryImport)));

        /// <inheritdoc cref="SR.TypeNotSupportedMessageReturnLibraryImport"/>
        public static readonly DiagnosticDescriptor ReturnTypeNotSupported =
            new DiagnosticDescriptor(
                Ids.TypeNotSupported,
                GetResourceString(nameof(SR.TypeNotSupportedTitle)),
                GetResourceString(nameof(SR.TypeNotSupportedMessageReturnLibraryImport)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.TypeNotSupportedDescriptionLibraryImport)));

        /// <inheritdoc cref="SR.TypeNotSupportedMessageParameterWithDetails"/>
        public static readonly DiagnosticDescriptor ParameterTypeNotSupportedWithDetails =
            new DiagnosticDescriptor(
                Ids.TypeNotSupported,
                GetResourceString(nameof(SR.TypeNotSupportedTitle)),
                GetResourceString(nameof(SR.TypeNotSupportedMessageParameterWithDetails)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.TypeNotSupportedDescriptionLibraryImport)));

        /// <inheritdoc cref="SR.TypeNotSupportedMessageReturnWithDetails"/>
        public static readonly DiagnosticDescriptor ReturnTypeNotSupportedWithDetails =
            new DiagnosticDescriptor(
                Ids.TypeNotSupported,
                GetResourceString(nameof(SR.TypeNotSupportedTitle)),
                GetResourceString(nameof(SR.TypeNotSupportedMessageReturnWithDetails)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.TypeNotSupportedDescriptionLibraryImport)));

        /// <inheritdoc cref="SR.ConfigurationNotSupportedMessageParameterLibraryImport"/>
        public static readonly DiagnosticDescriptor ParameterConfigurationNotSupported =
            new DiagnosticDescriptor(
                Ids.ConfigurationNotSupported,
                GetResourceString(nameof(SR.ConfigurationNotSupportedTitleLibraryImport)),
                GetResourceString(nameof(SR.ConfigurationNotSupportedMessageParameterLibraryImport)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ConfigurationNotSupportedDescriptionLibraryImport)));

        /// <inheritdoc cref="SR.ConfigurationNotSupportedMessageReturnLibraryImport"/>
        public static readonly DiagnosticDescriptor ReturnConfigurationNotSupported =
            new DiagnosticDescriptor(
                Ids.ConfigurationNotSupported,
                GetResourceString(nameof(SR.ConfigurationNotSupportedTitleLibraryImport)),
                GetResourceString(nameof(SR.ConfigurationNotSupportedMessageReturnLibraryImport)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ConfigurationNotSupportedDescriptionLibraryImport)));

        /// <inheritdoc cref="SR.MarshalAsConfigurationNotSupportedMessageParameterLibraryImport"/>
        public static readonly DiagnosticDescriptor MarshalAsParameterConfigurationNotSupported =
            new DiagnosticDescriptor(
                GeneratorDiagnostics.Ids.ConfigurationNotSupported,
                GetResourceString(nameof(SR.ConfigurationNotSupportedTitleLibraryImport)),
                GetResourceString(nameof(SR.MarshalAsConfigurationNotSupportedMessageParameterLibraryImport)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ConfigurationNotSupportedDescriptionLibraryImport)));

        /// <inheritdoc cref="SR.MarshalAsConfigurationNotSupportedMessageReturnLibraryImport"/>
        public static readonly DiagnosticDescriptor MarshalAsReturnConfigurationNotSupported =
            new DiagnosticDescriptor(
                GeneratorDiagnostics.Ids.ConfigurationNotSupported,
                GetResourceString(nameof(SR.ConfigurationNotSupportedTitleLibraryImport)),
                GetResourceString(nameof(SR.MarshalAsConfigurationNotSupportedMessageReturnLibraryImport)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ConfigurationNotSupportedDescriptionLibraryImport)));

        /// <inheritdoc cref="SR.ConfigurationNotSupportedMessageLibraryImport"/>
        public static readonly DiagnosticDescriptor ConfigurationNotSupported =
            new DiagnosticDescriptor(
                Ids.ConfigurationNotSupported,
                GetResourceString(nameof(SR.ConfigurationNotSupportedTitleLibraryImport)),
                GetResourceString(nameof(SR.ConfigurationNotSupportedMessageLibraryImport)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ConfigurationNotSupportedDescriptionLibraryImport)));

        /// <inheritdoc cref="SR.ConfigurationNotSupportedMessageValueLibraryImport"/>
        public static readonly DiagnosticDescriptor ConfigurationValueNotSupported =
            new DiagnosticDescriptor(
                Ids.ConfigurationNotSupported,
                GetResourceString(nameof(SR.ConfigurationNotSupportedTitleLibraryImport)),
                GetResourceString(nameof(SR.ConfigurationNotSupportedMessageValueLibraryImport)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ConfigurationNotSupportedDescriptionLibraryImport)));

        /// <inheritdoc cref="SR.ConfigurationNotSupportedMessageMarshallingInfoLibraryImport"/>
        public static readonly DiagnosticDescriptor MarshallingAttributeConfigurationNotSupported =
            new DiagnosticDescriptor(
                Ids.ConfigurationNotSupported,
                GetResourceString(nameof(SR.ConfigurationNotSupportedTitleLibraryImport)),
                GetResourceString(nameof(SR.ConfigurationNotSupportedMessageMarshallingInfoLibraryImport)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ConfigurationNotSupportedDescriptionLibraryImport)));

        /// <inheritdoc cref="SR.CannotForwardToDllImportMessage"/>
        public static readonly DiagnosticDescriptor CannotForwardToDllImport =
            new DiagnosticDescriptor(
                Ids.CannotForwardToDllImport,
                GetResourceString(nameof(SR.CannotForwardToDllImportTitle)),
                GetResourceString(nameof(SR.CannotForwardToDllImportMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.CannotForwardToDllImportDescription)));

        /// <inheritdoc cref="SR.RequiresAllowUnsafeBlocksMessageLibraryImport"/>
        public static readonly DiagnosticDescriptor RequiresAllowUnsafeBlocks =
            new DiagnosticDescriptor(
                Ids.RequiresAllowUnsafeBlocks,
                GetResourceString(nameof(SR.RequiresAllowUnsafeBlocksTitleLibraryImport)),
                GetResourceString(nameof(SR.RequiresAllowUnsafeBlocksMessageLibraryImport)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.RequiresAllowUnsafeBlocksDescriptionLibraryImport)));

        /// <inheritdoc cref="SR.UnnecessaryParameterMarshallingInfoMessage"/>
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

        /// <inheritdoc cref="SR.UnnecessaryMarshallingInfoDescription"/>
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
