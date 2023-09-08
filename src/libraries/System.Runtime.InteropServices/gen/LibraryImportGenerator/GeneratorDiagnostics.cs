// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;

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
            public const string NotRecommendedGeneratedComInterfaceUsage = Prefix + "1092";
        }

        private const string Category = "LibraryImportGenerator";

        /// <inheritdoc cref="SR.InvalidAttributedMethodSignatureMessageLibraryImport"/>
        public static readonly DiagnosticDescriptor InvalidAttributedMethodSignature =
            DiagnosticDescriptorHelper.Create(
            Ids.InvalidLibraryImportAttributeUsage,
            GetResourceString(nameof(SR.InvalidLibraryImportAttributeUsageTitle)),
            GetResourceString(nameof(SR.InvalidAttributedMethodSignatureMessageLibraryImport)),
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: GetResourceString(nameof(SR.InvalidAttributedMethodDescriptionLibraryImport)));

        /// <inheritdoc cref="SR.InvalidAttributedMethodContainingTypeMissingModifiersMessageLibraryImport"/>
        public static readonly DiagnosticDescriptor InvalidAttributedMethodContainingTypeMissingModifiers =
            DiagnosticDescriptorHelper.Create(
            Ids.InvalidLibraryImportAttributeUsage,
            GetResourceString(nameof(SR.InvalidLibraryImportAttributeUsageTitle)),
            GetResourceString(nameof(SR.InvalidAttributedMethodContainingTypeMissingModifiersMessageLibraryImport)),
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: GetResourceString(nameof(SR.InvalidAttributedMethodDescriptionLibraryImport)));

        /// <inheritdoc cref="SR.InvalidStringMarshallingConfigurationMessageLibraryImport"/>
        public static readonly DiagnosticDescriptor InvalidStringMarshallingConfiguration =
            DiagnosticDescriptorHelper.Create(
            Ids.InvalidLibraryImportAttributeUsage,
            GetResourceString(nameof(SR.InvalidLibraryImportAttributeUsageTitle)),
            GetResourceString(nameof(SR.InvalidStringMarshallingConfigurationMessageLibraryImport)),
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: GetResourceString(nameof(SR.InvalidStringMarshallingConfigurationDescription)));

        /// <inheritdoc cref="SR.TypeNotSupportedMessageParameterLibraryImport"/>
        public static readonly DiagnosticDescriptor ParameterTypeNotSupported =
            DiagnosticDescriptorHelper.Create(
                Ids.TypeNotSupported,
                GetResourceString(nameof(SR.TypeNotSupportedTitle)),
                GetResourceString(nameof(SR.TypeNotSupportedMessageParameterLibraryImport)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.TypeNotSupportedDescriptionLibraryImport)));

        /// <inheritdoc cref="SR.TypeNotSupportedMessageReturnLibraryImport"/>
        public static readonly DiagnosticDescriptor ReturnTypeNotSupported =
            DiagnosticDescriptorHelper.Create(
                Ids.TypeNotSupported,
                GetResourceString(nameof(SR.TypeNotSupportedTitle)),
                GetResourceString(nameof(SR.TypeNotSupportedMessageReturnLibraryImport)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.TypeNotSupportedDescriptionLibraryImport)));

        /// <inheritdoc cref="SR.TypeNotSupportedMessageParameterWithDetails"/>
        public static readonly DiagnosticDescriptor ParameterTypeNotSupportedWithDetails =
            DiagnosticDescriptorHelper.Create(
                Ids.TypeNotSupported,
                GetResourceString(nameof(SR.TypeNotSupportedTitle)),
                GetResourceString(nameof(SR.TypeNotSupportedMessageParameterWithDetails)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.TypeNotSupportedDescriptionLibraryImport)));

        /// <inheritdoc cref="SR.TypeNotSupportedMessageReturnWithDetails"/>
        public static readonly DiagnosticDescriptor ReturnTypeNotSupportedWithDetails =
            DiagnosticDescriptorHelper.Create(
                Ids.TypeNotSupported,
                GetResourceString(nameof(SR.TypeNotSupportedTitle)),
                GetResourceString(nameof(SR.TypeNotSupportedMessageReturnWithDetails)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.TypeNotSupportedDescriptionLibraryImport)));

        /// <inheritdoc cref="SR.ConfigurationNotSupportedMessageParameterLibraryImport"/>
        public static readonly DiagnosticDescriptor ParameterConfigurationNotSupported =
            DiagnosticDescriptorHelper.Create(
                Ids.ConfigurationNotSupported,
                GetResourceString(nameof(SR.ConfigurationNotSupportedTitleLibraryImport)),
                GetResourceString(nameof(SR.ConfigurationNotSupportedMessageParameterLibraryImport)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ConfigurationNotSupportedDescriptionLibraryImport)));

        /// <inheritdoc cref="SR.ConfigurationNotSupportedMessageReturnLibraryImport"/>
        public static readonly DiagnosticDescriptor ReturnConfigurationNotSupported =
            DiagnosticDescriptorHelper.Create(
                Ids.ConfigurationNotSupported,
                GetResourceString(nameof(SR.ConfigurationNotSupportedTitleLibraryImport)),
                GetResourceString(nameof(SR.ConfigurationNotSupportedMessageReturnLibraryImport)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ConfigurationNotSupportedDescriptionLibraryImport)));

        /// <inheritdoc cref="SR.MarshalAsConfigurationNotSupportedMessageParameterLibraryImport"/>
        public static readonly DiagnosticDescriptor MarshalAsParameterConfigurationNotSupported =
            DiagnosticDescriptorHelper.Create(
                GeneratorDiagnostics.Ids.ConfigurationNotSupported,
                GetResourceString(nameof(SR.ConfigurationNotSupportedTitleLibraryImport)),
                GetResourceString(nameof(SR.MarshalAsConfigurationNotSupportedMessageParameterLibraryImport)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ConfigurationNotSupportedDescriptionLibraryImport)));

        /// <inheritdoc cref="SR.MarshalAsConfigurationNotSupportedMessageReturnLibraryImport"/>
        public static readonly DiagnosticDescriptor MarshalAsReturnConfigurationNotSupported =
            DiagnosticDescriptorHelper.Create(
                GeneratorDiagnostics.Ids.ConfigurationNotSupported,
                GetResourceString(nameof(SR.ConfigurationNotSupportedTitleLibraryImport)),
                GetResourceString(nameof(SR.MarshalAsConfigurationNotSupportedMessageReturnLibraryImport)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ConfigurationNotSupportedDescriptionLibraryImport)));

        /// <inheritdoc cref="SR.ConfigurationNotSupportedMessageLibraryImport"/>
        public static readonly DiagnosticDescriptor ConfigurationNotSupported =
            DiagnosticDescriptorHelper.Create(
                Ids.ConfigurationNotSupported,
                GetResourceString(nameof(SR.ConfigurationNotSupportedTitleLibraryImport)),
                GetResourceString(nameof(SR.ConfigurationNotSupportedMessageLibraryImport)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ConfigurationNotSupportedDescriptionLibraryImport)));

        /// <inheritdoc cref="SR.ConfigurationNotSupportedMessageValueLibraryImport"/>
        public static readonly DiagnosticDescriptor ConfigurationValueNotSupported =
            DiagnosticDescriptorHelper.Create(
                Ids.ConfigurationNotSupported,
                GetResourceString(nameof(SR.ConfigurationNotSupportedTitleLibraryImport)),
                GetResourceString(nameof(SR.ConfigurationNotSupportedMessageValueLibraryImport)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ConfigurationNotSupportedDescriptionLibraryImport)));

        /// <inheritdoc cref="SR.ConfigurationNotSupportedMessageMarshallingInfoLibraryImport"/>
        public static readonly DiagnosticDescriptor MarshallingAttributeConfigurationNotSupported =
            DiagnosticDescriptorHelper.Create(
                Ids.ConfigurationNotSupported,
                GetResourceString(nameof(SR.ConfigurationNotSupportedTitleLibraryImport)),
                GetResourceString(nameof(SR.ConfigurationNotSupportedMessageMarshallingInfoLibraryImport)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ConfigurationNotSupportedDescriptionLibraryImport)));

        /// <inheritdoc cref="SR.CannotForwardToDllImportMessage"/>
        public static readonly DiagnosticDescriptor CannotForwardToDllImport =
            DiagnosticDescriptorHelper.Create(
                Ids.CannotForwardToDllImport,
                GetResourceString(nameof(SR.CannotForwardToDllImportTitle)),
                GetResourceString(nameof(SR.CannotForwardToDllImportMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.CannotForwardToDllImportDescription)));

        /// <inheritdoc cref="SR.RequiresAllowUnsafeBlocksMessageLibraryImport"/>
        public static readonly DiagnosticDescriptor RequiresAllowUnsafeBlocks =
            DiagnosticDescriptorHelper.Create(
                Ids.RequiresAllowUnsafeBlocks,
                GetResourceString(nameof(SR.RequiresAllowUnsafeBlocksTitleLibraryImport)),
                GetResourceString(nameof(SR.RequiresAllowUnsafeBlocksMessageLibraryImport)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.RequiresAllowUnsafeBlocksDescriptionLibraryImport)));

        /// <inheritdoc cref="SR.UnnecessaryParameterMarshallingInfoMessage"/>
        public static readonly DiagnosticDescriptor UnnecessaryParameterMarshallingInfo =
            DiagnosticDescriptorHelper.Create(
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
            DiagnosticDescriptorHelper.Create(
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

        /// <inheritdoc cref="SR.SizeOfCollectionMustBeKnownAtMarshalTimeMessageOutParam"/>
        public static readonly DiagnosticDescriptor SizeOfInCollectionMustBeDefinedAtCallOutParam =
            DiagnosticDescriptorHelper.Create(
                Ids.InvalidLibraryImportAttributeUsage,
                GetResourceString(nameof(SR.SizeOfCollectionMustBeKnownAtMarshalTimeTitle)),
                GetResourceString(nameof(SR.SizeOfCollectionMustBeKnownAtMarshalTimeMessageOutParam)),
                Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        /// <inheritdoc cref="SR.SizeOfCollectionMustBeKnownAtMarshalTimeMessageReturnValue"/>
        public static readonly DiagnosticDescriptor SizeOfInCollectionMustBeDefinedAtCallReturnValue =
            DiagnosticDescriptorHelper.Create(
                Ids.InvalidLibraryImportAttributeUsage,
                GetResourceString(nameof(SR.SizeOfCollectionMustBeKnownAtMarshalTimeTitle)),
                GetResourceString(nameof(SR.SizeOfCollectionMustBeKnownAtMarshalTimeMessageReturnValue)),
                Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        /// <inheritdoc cref="SR.LibraryImportUsageDoesNotFollowBestPracticesMessageWithDetails"/>
        public static readonly DiagnosticDescriptor LibraryImportUsageDoesNotFollowBestPractices =
            new DiagnosticDescriptor(
                Ids.NotRecommendedGeneratedComInterfaceUsage,
                GetResourceString(nameof(SR.LibraryImportUsageDoesNotFollowBestPracticesTitle)),
                GetResourceString(nameof(SR.LibraryImportUsageDoesNotFollowBestPracticesMessageWithDetails)),
                Category,
                DiagnosticSeverity.Info,
                isEnabledByDefault: true);

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
