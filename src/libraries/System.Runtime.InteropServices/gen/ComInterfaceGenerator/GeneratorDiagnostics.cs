// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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
            // SYSLIB1050-SYSLIB1059 are reserved for source-generated Interop
            public const string Prefix = "SYSLIB";
            public const string InvalidLibraryImportAttributeUsage = Prefix + "1050";
            public const string TypeNotSupported = Prefix + "1051";
            public const string ConfigurationNotSupported = Prefix + "1052";
            public const string RequiresAllowUnsafeBlocks = Prefix + "1062";
            public const string UnnecessaryMarshallingInfo = Prefix + "1063";
            public const string InvalidGeneratedComInterfaceAttributeUsage = Prefix + "1090";
            public const string MemberWillNotBeSourceGenerated = Prefix + "1091";
            public const string MultipleComInterfaceBaseTypes = Prefix + "1092";
            public const string AnalysisFailed = Prefix + "1093";
            public const string BaseInterfaceFailedGeneration = Prefix + "1094";
            public const string InvalidGeneratedComClassAttributeUsage = Prefix + "1095";
        }

        private const string Category = "ComInterfaceGenerator";

        /// <inheritdoc cref="SR.RequiresAllowUnsafeBlocksMessage"/>
        public static readonly DiagnosticDescriptor RequiresAllowUnsafeBlocks =
            new DiagnosticDescriptor(
                Ids.RequiresAllowUnsafeBlocks,
                GetResourceString(nameof(SR.RequiresAllowUnsafeBlocksTitle)),
                GetResourceString(nameof(SR.RequiresAllowUnsafeBlocksMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.RequiresAllowUnsafeBlocksDescription)));

        /// <inheritdoc cref="SR.InvalidAttributedMethodSignatureMessage"/>
        public static readonly DiagnosticDescriptor InvalidAttributedMethodSignature =
            new DiagnosticDescriptor(
            Ids.InvalidLibraryImportAttributeUsage,
            GetResourceString(nameof(SR.InvalidVirtualMethodIndexAttributeUsage)),
            GetResourceString(nameof(SR.InvalidAttributedMethodSignatureMessage)),
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: GetResourceString(nameof(SR.InvalidAttributedMethodDescription)));

        /// <inheritdoc cref="SR.InvalidAttributedMethodContainingTypeMissingModifiersMessage"/>
        public static readonly DiagnosticDescriptor InvalidAttributedMethodContainingTypeMissingModifiers =
            new DiagnosticDescriptor(
            Ids.InvalidLibraryImportAttributeUsage,
            GetResourceString(nameof(SR.InvalidVirtualMethodIndexAttributeUsage)),
            GetResourceString(nameof(SR.InvalidAttributedMethodContainingTypeMissingModifiersMessage)),
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: GetResourceString(nameof(SR.InvalidAttributedMethodDescription)));

        /// <inheritdoc cref="SR.InvalidGeneratedComInterfaceUsageMissingPartialModifier"/>
        public static readonly DiagnosticDescriptor InvalidAttributedInterfaceMissingPartialModifiers =
            new DiagnosticDescriptor(
            Ids.InvalidGeneratedComInterfaceAttributeUsage,
            GetResourceString(nameof(SR.InvalidGeneratedComInterfaceAttributeUsageTitle)),
            GetResourceString(nameof(SR.InvalidGeneratedComInterfaceUsageMissingPartialModifier)),
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: GetResourceString(nameof(SR.InvalidGeneratedComInterfaceAttributeUsageDescription)));

        /// <inheritdoc cref="SR.InvalidAttributedMethodContainingTypeMissingUnmanagedObjectUnwrapperAttributeMessage"/>
        public static readonly DiagnosticDescriptor InvalidAttributedMethodContainingTypeMissingUnmanagedObjectUnwrapperAttribute =
            new DiagnosticDescriptor(
            Ids.InvalidLibraryImportAttributeUsage,
            GetResourceString(nameof(SR.InvalidVirtualMethodIndexAttributeUsage)),
            GetResourceString(nameof(SR.InvalidAttributedMethodContainingTypeMissingUnmanagedObjectUnwrapperAttributeMessage)),
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: GetResourceString(nameof(SR.InvalidAttributedMethodDescription)));

        /// <inheritdoc cref="SR.InvalidStringMarshallingConfigurationOnInterfaceMessage"/>
        public static readonly DiagnosticDescriptor InvalidStringMarshallingMismatchBetweenBaseAndDerived =
            new DiagnosticDescriptor(
                Ids.InvalidGeneratedComInterfaceAttributeUsage,
            GetResourceString(nameof(SR.InvalidGeneratedComInterfaceAttributeUsageTitle)),
            GetResourceString(nameof(SR.InvalidStringMarshallingConfigurationOnInterfaceMessage)),
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: GetResourceString(nameof(SR.GeneratedComInterfaceStringMarshallingMustMatchBase)));

        /// <inheritdoc cref="SR.InvalidOptionsOnInterfaceMessage"/>
        public static readonly DiagnosticDescriptor InvalidOptionsOnInterface =
            new DiagnosticDescriptor(
                Ids.InvalidGeneratedComInterfaceAttributeUsage,
            GetResourceString(nameof(SR.InvalidGeneratedComInterfaceAttributeUsageTitle)),
            GetResourceString(nameof(SR.InvalidOptionsOnInterfaceMessage)),
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: GetResourceString(nameof(SR.InvalidOptionsOnInterfaceDescription)));

        /// <inheritdoc cref="SR.InvalidStringMarshallingConfigurationOnMethodMessage"/>
        public static readonly DiagnosticDescriptor InvalidStringMarshallingConfigurationOnMethod =
            new DiagnosticDescriptor(
            Ids.InvalidLibraryImportAttributeUsage,
            GetResourceString(nameof(SR.InvalidVirtualMethodIndexAttributeUsage)),
            GetResourceString(nameof(SR.InvalidStringMarshallingConfigurationOnMethodMessage)),
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: GetResourceString(nameof(SR.InvalidStringMarshallingConfigurationDescription)));

        /// <inheritdoc cref="SR.InvalidStringMarshallingConfigurationOnInterfaceMessage"/>
        public static readonly DiagnosticDescriptor InvalidStringMarshallingConfigurationOnInterface =
            new DiagnosticDescriptor(
            Ids.InvalidGeneratedComInterfaceAttributeUsage,
            GetResourceString(nameof(SR.InvalidGeneratedComInterfaceAttributeUsageTitle)),
            GetResourceString(nameof(SR.InvalidStringMarshallingConfigurationOnInterfaceMessage)),
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: GetResourceString(nameof(SR.InvalidStringMarshallingConfigurationDescription)));

        /// <inheritdoc cref="SR.StringMarshallingCustomTypeNotAccessibleByGeneratedCode"/>
        public static readonly DiagnosticDescriptor StringMarshallingCustomTypeNotAccessibleByGeneratedCode =
            new DiagnosticDescriptor(
            Ids.InvalidGeneratedComInterfaceAttributeUsage,
            GetResourceString(nameof(SR.InvalidGeneratedComInterfaceAttributeUsageTitle)),
            GetResourceString(nameof(SR.StringMarshallingCustomTypeNotAccessibleByGeneratedCode)),
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <inheritdoc cref="SR.InvalidExceptionMarshallingConfigurationMessage"/>
        public static readonly DiagnosticDescriptor InvalidExceptionMarshallingConfiguration =
            new DiagnosticDescriptor(
            Ids.InvalidLibraryImportAttributeUsage,
            GetResourceString(nameof(SR.InvalidVirtualMethodIndexAttributeUsage)),
            GetResourceString(nameof(SR.InvalidExceptionMarshallingConfigurationMessage)),
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: GetResourceString(nameof(SR.InvalidExceptionMarshallingConfigurationDescription)));

        /// <inheritdoc cref="SR.InvalidExceptionMarshallingConfigurationMessage"/>
        public static readonly DiagnosticDescriptor ParameterTypeNotSupported =
            new DiagnosticDescriptor(
                Ids.TypeNotSupported,
                GetResourceString(nameof(SR.TypeNotSupportedTitle)),
                GetResourceString(nameof(SR.InvalidExceptionMarshallingConfigurationMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.TypeNotSupportedDescription)));

        /// <inheritdoc cref="SR.TypeNotSupportedMessageReturn"/>
        public static readonly DiagnosticDescriptor ReturnTypeNotSupported =
            new DiagnosticDescriptor(
                Ids.TypeNotSupported,
                GetResourceString(nameof(SR.TypeNotSupportedTitle)),
                GetResourceString(nameof(SR.TypeNotSupportedMessageReturn)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.TypeNotSupportedDescription)));

        /// <inheritdoc cref="SR.TypeNotSupportedMessageParameterWithDetails"/>
        public static readonly DiagnosticDescriptor ParameterTypeNotSupportedWithDetails =
            new DiagnosticDescriptor(
                Ids.TypeNotSupported,
                GetResourceString(nameof(SR.TypeNotSupportedTitle)),
                GetResourceString(nameof(SR.TypeNotSupportedMessageParameterWithDetails)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.TypeNotSupportedDescription)));

        /// <inheritdoc cref="SR.TypeNotSupportedMessageReturnWithDetails"/>
        public static readonly DiagnosticDescriptor ReturnTypeNotSupportedWithDetails =
            new DiagnosticDescriptor(
                Ids.TypeNotSupported,
                GetResourceString(nameof(SR.TypeNotSupportedTitle)),
                GetResourceString(nameof(SR.TypeNotSupportedMessageReturnWithDetails)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.TypeNotSupportedDescription)));

        /// <inheritdoc cref="SR.ConfigurationNotSupportedMessageParameter"/>
        public static readonly DiagnosticDescriptor ParameterConfigurationNotSupported =
            new DiagnosticDescriptor(
                Ids.ConfigurationNotSupported,
                GetResourceString(nameof(SR.ConfigurationNotSupportedTitle)),
                GetResourceString(nameof(SR.ConfigurationNotSupportedMessageParameter)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ConfigurationNotSupportedDescription)));

        /// <inheritdoc cref="SR.ConfigurationNotSupportedMessageReturn"/>
        public static readonly DiagnosticDescriptor ReturnConfigurationNotSupported =
            new DiagnosticDescriptor(
                Ids.ConfigurationNotSupported,
                GetResourceString(nameof(SR.ConfigurationNotSupportedTitle)),
                GetResourceString(nameof(SR.ConfigurationNotSupportedMessageReturn)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ConfigurationNotSupportedDescription)));

        /// <inheritdoc cref="SR.MarshalAsConfigurationNotSupportedMessageParameter"/>
        public static readonly DiagnosticDescriptor MarshalAsParameterConfigurationNotSupported =
            new DiagnosticDescriptor(
                GeneratorDiagnostics.Ids.ConfigurationNotSupported,
                GetResourceString(nameof(SR.ConfigurationNotSupportedTitle)),
                GetResourceString(nameof(SR.MarshalAsConfigurationNotSupportedMessageParameter)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ConfigurationNotSupportedDescription)));

        /// <inheritdoc cref="SR.MarshalAsConfigurationNotSupportedMessageReturn"/>
        public static readonly DiagnosticDescriptor MarshalAsReturnConfigurationNotSupported =
            new DiagnosticDescriptor(
                GeneratorDiagnostics.Ids.ConfigurationNotSupported,
                GetResourceString(nameof(SR.ConfigurationNotSupportedTitle)),
                GetResourceString(nameof(SR.MarshalAsConfigurationNotSupportedMessageReturn)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ConfigurationNotSupportedDescription)));

        /// <inheritdoc cref="SR.ConfigurationNotSupportedMessage"/>
        public static readonly DiagnosticDescriptor ConfigurationNotSupported =
            new DiagnosticDescriptor(
                Ids.ConfigurationNotSupported,
                GetResourceString(nameof(SR.ConfigurationNotSupportedTitle)),
                GetResourceString(nameof(SR.ConfigurationNotSupportedMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ConfigurationNotSupportedDescription)));

        /// <inheritdoc cref="SR.ConfigurationNotSupportedMessageValue"/>
        public static readonly DiagnosticDescriptor ConfigurationValueNotSupported =
            new DiagnosticDescriptor(
                Ids.ConfigurationNotSupported,
                GetResourceString(nameof(SR.ConfigurationNotSupportedTitle)),
                GetResourceString(nameof(SR.ConfigurationNotSupportedMessageValue)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ConfigurationNotSupportedDescription)));

        /// <inheritdoc cref="SR.ConfigurationNotSupportedMessageMarshallingInfo"/>
        public static readonly DiagnosticDescriptor MarshallingAttributeConfigurationNotSupported =
            new DiagnosticDescriptor(
                Ids.ConfigurationNotSupported,
                GetResourceString(nameof(SR.ConfigurationNotSupportedTitle)),
                GetResourceString(nameof(SR.ConfigurationNotSupportedMessageMarshallingInfo)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ConfigurationNotSupportedDescription)));

        /// <inheritdoc cref="SR.MethodNotDeclaredInAttributedInterfaceMessage"/>
        public static readonly DiagnosticDescriptor MethodNotDeclaredInAttributedInterface =
            new DiagnosticDescriptor(
                Ids.MemberWillNotBeSourceGenerated,
                GetResourceString(nameof(SR.MethodNotDeclaredInAttributedInterfaceTitle)),
                GetResourceString(nameof(SR.MethodNotDeclaredInAttributedInterfaceMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.MethodNotDeclaredInAttributedInterfaceDescription)));

        /// <inheritdoc cref="SR.InstancePropertyDeclaredInInterfaceMessage"/>
        public static readonly DiagnosticDescriptor InstancePropertyDeclaredInInterface =
            new DiagnosticDescriptor(
                Ids.MemberWillNotBeSourceGenerated,
                GetResourceString(nameof(SR.InstancePropertyDeclaredInInterfaceTitle)),
                GetResourceString(nameof(SR.InstancePropertyDeclaredInInterfaceMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.InstancePropertyDeclaredInInterfaceDescription)));

        /// <inheritdoc cref="SR.InstanceEventDeclaredInInterfaceMessage"/>
        public static readonly DiagnosticDescriptor InstanceEventDeclaredInInterface =
            new DiagnosticDescriptor(
                Ids.MemberWillNotBeSourceGenerated,
                GetResourceString(nameof(SR.InstanceEventDeclaredInInterfaceTitle)),
                GetResourceString(nameof(SR.InstanceEventDeclaredInInterfaceMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.InstanceEventDeclaredInInterfaceDescription)));

        /// <inheritdoc cref="SR.InvalidGeneratedComInterfaceAttributeUsageInterfaceNotAccessible"/>
        public static readonly DiagnosticDescriptor InvalidAttributedInterfaceNotAccessible =
            new DiagnosticDescriptor(
                Ids.InvalidGeneratedComInterfaceAttributeUsage,
                GetResourceString(nameof(SR.InvalidGeneratedComInterfaceAttributeUsageTitle)),
                GetResourceString(nameof(SR.InvalidGeneratedComInterfaceAttributeUsageInterfaceNotAccessible)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.InvalidGeneratedComInterfaceAttributeUsageDescription)));

        /// <inheritdoc cref="SR.InvalidGeneratedComInterfaceAttributeUsageMissingGuidAttribute"/>
        public static readonly DiagnosticDescriptor InvalidAttributedInterfaceMissingGuidAttribute =
            new DiagnosticDescriptor(
                Ids.InvalidGeneratedComInterfaceAttributeUsage,
                GetResourceString(nameof(SR.InvalidGeneratedComInterfaceAttributeUsageTitle)),
                GetResourceString(nameof(SR.InvalidGeneratedComInterfaceAttributeUsageMissingGuidAttribute)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.InvalidGeneratedComInterfaceAttributeUsageDescription)));

        /// <inheritdoc cref="SR.InvalidGeneratedComInterfaceAttributeUsageInterfaceIsGeneric"/>
        public static readonly DiagnosticDescriptor InvalidAttributedInterfaceGenericNotSupported =
            new DiagnosticDescriptor(
                Ids.InvalidGeneratedComInterfaceAttributeUsage,
                GetResourceString(nameof(SR.InvalidGeneratedComInterfaceAttributeUsageTitle)),
                GetResourceString(nameof(SR.InvalidGeneratedComInterfaceAttributeUsageInterfaceIsGeneric)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.InvalidGeneratedComInterfaceAttributeUsageDescription)));

        /// <inheritdoc cref="SR.MultipleComInterfaceBaseTypesMessage"/>
        public static readonly DiagnosticDescriptor MultipleComInterfaceBaseTypes =
            new DiagnosticDescriptor(
                Ids.MultipleComInterfaceBaseTypes,
                GetResourceString(nameof(SR.MultipleComInterfaceBaseTypesTitle)),
                GetResourceString(nameof(SR.MultipleComInterfaceBaseTypesMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.MultipleComInterfaceBaseTypesDescription)));

        /// <inheritdoc cref="SR.AnalysisFailedMethodMessage"/>
        public static readonly DiagnosticDescriptor CannotAnalyzeMethodPattern =
            new DiagnosticDescriptor(
                Ids.AnalysisFailed,
                GetResourceString(nameof(SR.AnalysisFailedTitle)),
                GetResourceString(nameof(SR.AnalysisFailedMethodMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.AnalysisFailedDescription)));

        /// <inheritdoc cref="SR.AnalysisFailedInterfaceMessage"/>
        public static readonly DiagnosticDescriptor CannotAnalyzeInterfacePattern =
            new DiagnosticDescriptor(
                Ids.AnalysisFailed,
                GetResourceString(nameof(SR.AnalysisFailedTitle)),
                GetResourceString(nameof(SR.AnalysisFailedInterfaceMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.AnalysisFailedDescription)));

        /// <inheritdoc cref="SR.BaseInterfaceCannotBeGeneratedMessage"/>
        public static readonly DiagnosticDescriptor BaseInterfaceIsNotGenerated =
            new DiagnosticDescriptor(
                Ids.BaseInterfaceFailedGeneration,
                GetResourceString(nameof(SR.BaseInterfaceCannotBeGeneratedTitle)),
                GetResourceString(nameof(SR.BaseInterfaceCannotBeGeneratedMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.BaseInterfaceCannotBeGeneratedDescription)));

        /// <inheritdoc cref="SR.InvalidGeneratedComClassAttributeUsageMissingPartialModifier"/>
        public static readonly DiagnosticDescriptor InvalidAttributedClassMissingPartialModifier =
            new DiagnosticDescriptor(
                Ids.InvalidGeneratedComClassAttributeUsage,
                GetResourceString(nameof(SR.InvalidGeneratedComClassAttributeUsageTitle)),
                GetResourceString(nameof(SR.InvalidGeneratedComClassAttributeUsageMissingPartialModifier)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.InvalidGeneratedComClassAttributeUsageDescription)));

        /// <inheritdoc cref="SR.InterfaceTypeNotSupportedMessage"/>
        public static readonly DiagnosticDescriptor InterfaceTypeNotSupported =
            new DiagnosticDescriptor(
                Ids.InvalidGeneratedComInterfaceAttributeUsage,
                GetResourceString(nameof(SR.InterfaceTypeNotSupportedTitle)),
                GetResourceString(nameof(SR.InterfaceTypeNotSupportedMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.InterfaceTypeNotSupportedMessage)));

        /// <inheritdoc cref="SR.ClassDoesNotImplementAnyGeneratedComInterfacesMessage"/>
        public static readonly DiagnosticDescriptor ClassDoesNotImplementAnyGeneratedComInterface =
            new DiagnosticDescriptor(
                Ids.InvalidGeneratedComClassAttributeUsage,
                GetResourceString(nameof(SR.InvalidGeneratedComClassAttributeUsageTitle)),
                GetResourceString(nameof(SR.ClassDoesNotImplementAnyGeneratedComInterfacesMessage)),
                Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ClassDoesNotImplementAnyGeneratedComInterfacesDescription)));

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
                    GeneratorDiagnostics.InvalidStringMarshallingConfigurationOnMethod,
                    methodName,
                    detailsMessage));
        }
        /// <summary>
        /// Report diagnostic for invalid configuration for string marshalling.
        /// </summary>
        /// <param name="attributeData">Attribute specifying the invalid configuration</param>
        /// <param name="methodName">Name of the method</param>
        /// <param name="detailsMessage">Specific reason the configuration is invalid</param>
        public static void ReportInvalidExceptionMarshallingConfiguration(
            this GeneratorDiagnosticsBag diagnostics,
            AttributeData attributeData,
            string methodName,
            string detailsMessage)
        {
            diagnostics.ReportDiagnostic(
                attributeData.CreateDiagnosticInfo(
                    GeneratorDiagnostics.InvalidExceptionMarshallingConfiguration,
                    methodName,
                    detailsMessage));
        }
        private static LocalizableResourceString GetResourceString(string resourceName)
        {
            return new LocalizableResourceString(resourceName, SR.ResourceManager, typeof(FxResources.Microsoft.Interop.ComInterfaceGenerator.SR));
        }
    }
}
