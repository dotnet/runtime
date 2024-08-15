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
            // SYSLIB1050-SYSLIB1059 are reserved for source-generated Interop
            public const string Prefix = "SYSLIB";
            public const string InvalidLibraryImportAttributeUsage = Prefix + "1050";
            public const string TypeNotSupported = Prefix + "1051";
            public const string ConfigurationNotSupported = Prefix + "1052";
            public const string RequiresAllowUnsafeBlocks = Prefix + "1062";
            public const string UnnecessaryMarshallingInfo = Prefix + "1063";
            public const string InvalidGeneratedComInterfaceAttributeUsage = Prefix + "1090";
            public const string MemberWillNotBeSourceGenerated = Prefix + "1091";
            public const string NotRecommendedGeneratedComInterfaceUsage = Prefix + "1092";
            public const string AnalysisFailed = Prefix + "1093";
            public const string BaseInterfaceFailedGeneration = Prefix + "1094";
            public const string InvalidGeneratedComClassAttributeUsage = Prefix + "1095";
            public const string BaseInterfaceDefinedInOtherAssembly = Prefix + "1230";
        }

        private const string Category = "ComInterfaceGenerator";

        /// <inheritdoc cref="SR.RequiresAllowUnsafeBlocksMessageCom"/>
        public static readonly DiagnosticDescriptor RequiresAllowUnsafeBlocks =
            DiagnosticDescriptorHelper.Create(
                Ids.RequiresAllowUnsafeBlocks,
                GetResourceString(nameof(SR.RequiresAllowUnsafeBlocksTitleCom)),
                GetResourceString(nameof(SR.RequiresAllowUnsafeBlocksMessageCom)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.RequiresAllowUnsafeBlocksDescriptionCom)));

        /// <inheritdoc cref="SR.InvalidAttributedMethodSignatureMessageCom"/>
        public static readonly DiagnosticDescriptor InvalidAttributedMethodSignature =
            DiagnosticDescriptorHelper.Create(
            Ids.InvalidLibraryImportAttributeUsage,
            GetResourceString(nameof(SR.InvalidVirtualMethodIndexAttributeUsage)),
            GetResourceString(nameof(SR.InvalidAttributedMethodSignatureMessageCom)),
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: GetResourceString(nameof(SR.InvalidAttributedMethodDescriptionCom)));

        /// <inheritdoc cref="SR.InvalidAttributedMethodContainingTypeMissingModifiersMessageCom"/>
        public static readonly DiagnosticDescriptor InvalidAttributedMethodContainingTypeMissingModifiers =
            DiagnosticDescriptorHelper.Create(
            Ids.InvalidLibraryImportAttributeUsage,
            GetResourceString(nameof(SR.InvalidVirtualMethodIndexAttributeUsage)),
            GetResourceString(nameof(SR.InvalidAttributedMethodContainingTypeMissingModifiersMessageCom)),
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: GetResourceString(nameof(SR.InvalidAttributedMethodDescriptionCom)));

        /// <inheritdoc cref="SR.InvalidGeneratedComInterfaceUsageMissingPartialModifier"/>
        public static readonly DiagnosticDescriptor InvalidAttributedInterfaceMissingPartialModifiers =
            DiagnosticDescriptorHelper.Create(
            Ids.InvalidGeneratedComInterfaceAttributeUsage,
            GetResourceString(nameof(SR.InvalidGeneratedComInterfaceAttributeUsageTitle)),
            GetResourceString(nameof(SR.InvalidGeneratedComInterfaceUsageMissingPartialModifier)),
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: GetResourceString(nameof(SR.InvalidGeneratedComInterfaceAttributeUsageDescription)));

        /// <inheritdoc cref="SR.InvalidAttributedMethodContainingTypeMissingUnmanagedObjectUnwrapperAttributeMessage"/>
        public static readonly DiagnosticDescriptor InvalidAttributedMethodContainingTypeMissingUnmanagedObjectUnwrapperAttribute =
            DiagnosticDescriptorHelper.Create(
            Ids.InvalidLibraryImportAttributeUsage,
            GetResourceString(nameof(SR.InvalidVirtualMethodIndexAttributeUsage)),
            GetResourceString(nameof(SR.InvalidAttributedMethodContainingTypeMissingUnmanagedObjectUnwrapperAttributeMessage)),
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: GetResourceString(nameof(SR.InvalidAttributedMethodDescriptionCom)));

        /// <inheritdoc cref="SR.InvalidStringMarshallingConfigurationOnInterfaceMessage"/>
        public static readonly DiagnosticDescriptor InvalidStringMarshallingMismatchBetweenBaseAndDerived =
            DiagnosticDescriptorHelper.Create(
                Ids.InvalidGeneratedComInterfaceAttributeUsage,
            GetResourceString(nameof(SR.InvalidGeneratedComInterfaceAttributeUsageTitle)),
            GetResourceString(nameof(SR.InvalidStringMarshallingConfigurationOnInterfaceMessage)),
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: GetResourceString(nameof(SR.GeneratedComInterfaceStringMarshallingMustMatchBase)));

        /// <inheritdoc cref="SR.InvalidOptionsOnInterfaceMessage"/>
        public static readonly DiagnosticDescriptor InvalidOptionsOnInterface =
            DiagnosticDescriptorHelper.Create(
                Ids.InvalidGeneratedComInterfaceAttributeUsage,
            GetResourceString(nameof(SR.InvalidGeneratedComInterfaceAttributeUsageTitle)),
            GetResourceString(nameof(SR.InvalidOptionsOnInterfaceMessage)),
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: GetResourceString(nameof(SR.InvalidOptionsOnInterfaceDescription)));

        /// <inheritdoc cref="SR.InvalidStringMarshallingConfigurationOnMethodMessage"/>
        public static readonly DiagnosticDescriptor InvalidStringMarshallingConfigurationOnMethod =
            DiagnosticDescriptorHelper.Create(
            Ids.InvalidLibraryImportAttributeUsage,
            GetResourceString(nameof(SR.InvalidVirtualMethodIndexAttributeUsage)),
            GetResourceString(nameof(SR.InvalidStringMarshallingConfigurationOnMethodMessage)),
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: GetResourceString(nameof(SR.InvalidStringMarshallingConfigurationDescription)));

        /// <inheritdoc cref="SR.InvalidStringMarshallingConfigurationOnInterfaceMessage"/>
        public static readonly DiagnosticDescriptor InvalidStringMarshallingConfigurationOnInterface =
            DiagnosticDescriptorHelper.Create(
            Ids.InvalidGeneratedComInterfaceAttributeUsage,
            GetResourceString(nameof(SR.InvalidGeneratedComInterfaceAttributeUsageTitle)),
            GetResourceString(nameof(SR.InvalidStringMarshallingConfigurationOnInterfaceMessage)),
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: GetResourceString(nameof(SR.InvalidStringMarshallingConfigurationDescription)));

        /// <inheritdoc cref="SR.StringMarshallingCustomTypeNotAccessibleByGeneratedCode"/>
        public static readonly DiagnosticDescriptor StringMarshallingCustomTypeNotAccessibleByGeneratedCode =
            DiagnosticDescriptorHelper.Create(
            Ids.InvalidGeneratedComInterfaceAttributeUsage,
            GetResourceString(nameof(SR.InvalidGeneratedComInterfaceAttributeUsageTitle)),
            GetResourceString(nameof(SR.StringMarshallingCustomTypeNotAccessibleByGeneratedCode)),
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <inheritdoc cref="SR.InvalidExceptionMarshallingConfigurationMessage"/>
        public static readonly DiagnosticDescriptor InvalidExceptionMarshallingConfiguration =
            DiagnosticDescriptorHelper.Create(
            Ids.InvalidLibraryImportAttributeUsage,
            GetResourceString(nameof(SR.InvalidVirtualMethodIndexAttributeUsage)),
            GetResourceString(nameof(SR.InvalidExceptionMarshallingConfigurationMessage)),
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: GetResourceString(nameof(SR.InvalidExceptionMarshallingConfigurationDescription)));

        /// <inheritdoc cref="SR.TypeNotSupportedMessageParameterCom"/>
        public static readonly DiagnosticDescriptor ParameterTypeNotSupported =
            DiagnosticDescriptorHelper.Create(
                Ids.TypeNotSupported,
                GetResourceString(nameof(SR.TypeNotSupportedTitleCom)),
                GetResourceString(nameof(SR.TypeNotSupportedMessageParameterCom)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.TypeNotSupportedDescriptionCom)));

        /// <inheritdoc cref="SR.TypeNotSupportedMessageReturnCom"/>
        public static readonly DiagnosticDescriptor ReturnTypeNotSupported =
            DiagnosticDescriptorHelper.Create(
                Ids.TypeNotSupported,
                GetResourceString(nameof(SR.TypeNotSupportedTitleCom)),
                GetResourceString(nameof(SR.TypeNotSupportedMessageReturnCom)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.TypeNotSupportedDescriptionCom)));

        /// <inheritdoc cref="SR.TypeNotSupportedMessageParameterWithDetails"/>
        public static readonly DiagnosticDescriptor ParameterTypeNotSupportedWithDetails =
            DiagnosticDescriptorHelper.Create(
                Ids.TypeNotSupported,
                GetResourceString(nameof(SR.TypeNotSupportedTitleCom)),
                GetResourceString(nameof(SR.TypeNotSupportedMessageParameterWithDetails)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.TypeNotSupportedDescriptionCom)));

        /// <inheritdoc cref="SR.TypeNotSupportedMessageReturnWithDetails"/>
        public static readonly DiagnosticDescriptor ReturnTypeNotSupportedWithDetails =
            DiagnosticDescriptorHelper.Create(
                Ids.TypeNotSupported,
                GetResourceString(nameof(SR.TypeNotSupportedTitleCom)),
                GetResourceString(nameof(SR.TypeNotSupportedMessageReturnWithDetails)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.TypeNotSupportedDescriptionCom)));

        /// <inheritdoc cref="SR.ConfigurationNotSupportedMessageParameterCom"/>
        public static readonly DiagnosticDescriptor ParameterConfigurationNotSupported =
            DiagnosticDescriptorHelper.Create(
                Ids.ConfigurationNotSupported,
                GetResourceString(nameof(SR.ConfigurationNotSupportedTitleCom)),
                GetResourceString(nameof(SR.ConfigurationNotSupportedMessageParameterCom)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ConfigurationNotSupportedDescriptionCom)));

        /// <inheritdoc cref="SR.ConfigurationNotSupportedMessageReturnCom"/>
        public static readonly DiagnosticDescriptor ReturnConfigurationNotSupported =
            DiagnosticDescriptorHelper.Create(
                Ids.ConfigurationNotSupported,
                GetResourceString(nameof(SR.ConfigurationNotSupportedTitleCom)),
                GetResourceString(nameof(SR.ConfigurationNotSupportedMessageReturnCom)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ConfigurationNotSupportedDescriptionCom)));

        /// <inheritdoc cref="SR.MarshalAsConfigurationNotSupportedMessageParameterCom"/>
        public static readonly DiagnosticDescriptor MarshalAsParameterConfigurationNotSupported =
            DiagnosticDescriptorHelper.Create(
                GeneratorDiagnostics.Ids.ConfigurationNotSupported,
                GetResourceString(nameof(SR.ConfigurationNotSupportedTitleCom)),
                GetResourceString(nameof(SR.MarshalAsConfigurationNotSupportedMessageParameterCom)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ConfigurationNotSupportedDescriptionCom)));

        /// <inheritdoc cref="SR.MarshalAsConfigurationNotSupportedMessageReturnCom"/>
        public static readonly DiagnosticDescriptor MarshalAsReturnConfigurationNotSupported =
            DiagnosticDescriptorHelper.Create(
                GeneratorDiagnostics.Ids.ConfigurationNotSupported,
                GetResourceString(nameof(SR.ConfigurationNotSupportedTitleCom)),
                GetResourceString(nameof(SR.MarshalAsConfigurationNotSupportedMessageReturnCom)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ConfigurationNotSupportedDescriptionCom)));

        /// <inheritdoc cref="SR.ConfigurationNotSupportedMessageCom"/>
        public static readonly DiagnosticDescriptor ConfigurationNotSupported =
            DiagnosticDescriptorHelper.Create(
                Ids.ConfigurationNotSupported,
                GetResourceString(nameof(SR.ConfigurationNotSupportedTitleCom)),
                GetResourceString(nameof(SR.ConfigurationNotSupportedMessageCom)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ConfigurationNotSupportedDescriptionCom)));

        /// <inheritdoc cref="SR.ConfigurationNotSupportedMessageValueCom"/>
        public static readonly DiagnosticDescriptor ConfigurationValueNotSupported =
            DiagnosticDescriptorHelper.Create(
                Ids.ConfigurationNotSupported,
                GetResourceString(nameof(SR.ConfigurationNotSupportedTitleCom)),
                GetResourceString(nameof(SR.ConfigurationNotSupportedMessageValueCom)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ConfigurationNotSupportedDescriptionCom)));

        /// <inheritdoc cref="SR.ConfigurationNotSupportedMessageMarshallingInfoCom"/>
        public static readonly DiagnosticDescriptor MarshallingAttributeConfigurationNotSupported =
            DiagnosticDescriptorHelper.Create(
                Ids.ConfigurationNotSupported,
                GetResourceString(nameof(SR.ConfigurationNotSupportedTitleCom)),
                GetResourceString(nameof(SR.ConfigurationNotSupportedMessageMarshallingInfoCom)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ConfigurationNotSupportedDescriptionCom)));

        /// <inheritdoc cref="SR.MethodNotDeclaredInAttributedInterfaceMessage"/>
        public static readonly DiagnosticDescriptor MethodNotDeclaredInAttributedInterface =
            DiagnosticDescriptorHelper.Create(
                Ids.MemberWillNotBeSourceGenerated,
                GetResourceString(nameof(SR.MethodNotDeclaredInAttributedInterfaceTitle)),
                GetResourceString(nameof(SR.MethodNotDeclaredInAttributedInterfaceMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.MethodNotDeclaredInAttributedInterfaceDescription)));

        /// <inheritdoc cref="SR.InstancePropertyDeclaredInInterfaceMessage"/>
        public static readonly DiagnosticDescriptor InstancePropertyDeclaredInInterface =
            DiagnosticDescriptorHelper.Create(
                Ids.MemberWillNotBeSourceGenerated,
                GetResourceString(nameof(SR.InstancePropertyDeclaredInInterfaceTitle)),
                GetResourceString(nameof(SR.InstancePropertyDeclaredInInterfaceMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.InstancePropertyDeclaredInInterfaceDescription)));

        /// <inheritdoc cref="SR.InstanceEventDeclaredInInterfaceMessage"/>
        public static readonly DiagnosticDescriptor InstanceEventDeclaredInInterface =
            DiagnosticDescriptorHelper.Create(
                Ids.MemberWillNotBeSourceGenerated,
                GetResourceString(nameof(SR.InstanceEventDeclaredInInterfaceTitle)),
                GetResourceString(nameof(SR.InstanceEventDeclaredInInterfaceMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.InstanceEventDeclaredInInterfaceDescription)));

        /// <inheritdoc cref="SR.InvalidGeneratedComInterfaceAttributeUsageInterfaceNotAccessible"/>
        public static readonly DiagnosticDescriptor InvalidAttributedInterfaceNotAccessible =
            DiagnosticDescriptorHelper.Create(
                Ids.InvalidGeneratedComInterfaceAttributeUsage,
                GetResourceString(nameof(SR.InvalidGeneratedComInterfaceAttributeUsageTitle)),
                GetResourceString(nameof(SR.InvalidGeneratedComInterfaceAttributeUsageInterfaceNotAccessible)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.InvalidGeneratedComInterfaceAttributeUsageDescription)));

        /// <inheritdoc cref="SR.InvalidGeneratedComInterfaceAttributeUsageMissingGuidAttribute"/>
        public static readonly DiagnosticDescriptor InvalidAttributedInterfaceMissingGuidAttribute =
            DiagnosticDescriptorHelper.Create(
                Ids.InvalidGeneratedComInterfaceAttributeUsage,
                GetResourceString(nameof(SR.InvalidGeneratedComInterfaceAttributeUsageTitle)),
                GetResourceString(nameof(SR.InvalidGeneratedComInterfaceAttributeUsageMissingGuidAttribute)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.InvalidGeneratedComInterfaceAttributeUsageDescription)));

        /// <inheritdoc cref="SR.InvalidGeneratedComInterfaceAttributeUsageInterfaceIsGeneric"/>
        public static readonly DiagnosticDescriptor InvalidAttributedInterfaceGenericNotSupported =
            DiagnosticDescriptorHelper.Create(
                Ids.InvalidGeneratedComInterfaceAttributeUsage,
                GetResourceString(nameof(SR.InvalidGeneratedComInterfaceAttributeUsageTitle)),
                GetResourceString(nameof(SR.InvalidGeneratedComInterfaceAttributeUsageInterfaceIsGeneric)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.InvalidGeneratedComInterfaceAttributeUsageDescription)));

        /// <inheritdoc cref="SR.MultipleComInterfaceBaseTypesMessage"/>
        public static readonly DiagnosticDescriptor MultipleComInterfaceBaseTypes =
            DiagnosticDescriptorHelper.Create(
                Ids.InvalidGeneratedComInterfaceAttributeUsage,
                GetResourceString(nameof(SR.MultipleComInterfaceBaseTypesTitle)),
                GetResourceString(nameof(SR.MultipleComInterfaceBaseTypesMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.MultipleComInterfaceBaseTypesDescription)));

        /// <inheritdoc cref="SR.AnalysisFailedMethodMessage"/>
        public static readonly DiagnosticDescriptor CannotAnalyzeMethodPattern =
            DiagnosticDescriptorHelper.Create(
                Ids.AnalysisFailed,
                GetResourceString(nameof(SR.AnalysisFailedTitle)),
                GetResourceString(nameof(SR.AnalysisFailedMethodMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.AnalysisFailedDescription)));

        /// <inheritdoc cref="SR.AnalysisFailedInterfaceMessage"/>
        public static readonly DiagnosticDescriptor CannotAnalyzeInterfacePattern =
            DiagnosticDescriptorHelper.Create(
                Ids.AnalysisFailed,
                GetResourceString(nameof(SR.AnalysisFailedTitle)),
                GetResourceString(nameof(SR.AnalysisFailedInterfaceMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.AnalysisFailedDescription)));

        /// <inheritdoc cref="SR.BaseInterfaceCannotBeGeneratedMessage"/>
        public static readonly DiagnosticDescriptor BaseInterfaceIsNotGenerated =
            DiagnosticDescriptorHelper.Create(
                Ids.BaseInterfaceFailedGeneration,
                GetResourceString(nameof(SR.BaseInterfaceCannotBeGeneratedTitle)),
                GetResourceString(nameof(SR.BaseInterfaceCannotBeGeneratedMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.BaseInterfaceCannotBeGeneratedDescription)));

        /// <inheritdoc cref="SR.InvalidGeneratedComClassAttributeUsageMissingPartialModifier"/>
        public static readonly DiagnosticDescriptor InvalidAttributedClassMissingPartialModifier =
            DiagnosticDescriptorHelper.Create(
                Ids.InvalidGeneratedComClassAttributeUsage,
                GetResourceString(nameof(SR.InvalidGeneratedComClassAttributeUsageTitle)),
                GetResourceString(nameof(SR.InvalidGeneratedComClassAttributeUsageMissingPartialModifier)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.InvalidGeneratedComClassAttributeUsageDescription)));

        /// <inheritdoc cref="SR.InterfaceTypeNotSupportedMessage"/>
        public static readonly DiagnosticDescriptor InterfaceTypeNotSupported =
            DiagnosticDescriptorHelper.Create(
                Ids.InvalidGeneratedComInterfaceAttributeUsage,
                GetResourceString(nameof(SR.InterfaceTypeNotSupportedTitle)),
                GetResourceString(nameof(SR.InterfaceTypeNotSupportedMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.InterfaceTypeNotSupportedMessage)));

        /// <inheritdoc cref="SR.ClassDoesNotImplementAnyGeneratedComInterfacesMessage"/>
        public static readonly DiagnosticDescriptor ClassDoesNotImplementAnyGeneratedComInterface =
            DiagnosticDescriptorHelper.Create(
                Ids.InvalidGeneratedComClassAttributeUsage,
                GetResourceString(nameof(SR.InvalidGeneratedComClassAttributeUsageTitle)),
                GetResourceString(nameof(SR.ClassDoesNotImplementAnyGeneratedComInterfacesMessage)),
                Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.ClassDoesNotImplementAnyGeneratedComInterfacesDescription)));

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
                customTags:
                [
                    WellKnownDiagnosticTags.Unnecessary
                ]);

        /// <inheritdoc cref="SR.UnnecessaryReturnMarshallingInfoMessage"/>
        public static readonly DiagnosticDescriptor UnnecessaryReturnMarshallingInfo =
            DiagnosticDescriptorHelper.Create(
                Ids.UnnecessaryMarshallingInfo,
                GetResourceString(nameof(SR.UnnecessaryMarshallingInfoTitle)),
                GetResourceString(nameof(SR.UnnecessaryReturnMarshallingInfoMessage)),
                Category,
                DiagnosticSeverity.Info,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.UnnecessaryMarshallingInfoDescription)),
                customTags:
                [
                    WellKnownDiagnosticTags.Unnecessary
                ]);

        /// <inheritdoc cref="SR.SizeOfCollectionMustBeKnownAtMarshalTimeMessageOutParam"/>
        public static readonly DiagnosticDescriptor SizeOfInCollectionMustBeDefinedAtCallOutParam =
            DiagnosticDescriptorHelper.Create(
                Ids.InvalidGeneratedComInterfaceAttributeUsage,
                GetResourceString(nameof(SR.SizeOfCollectionMustBeKnownAtMarshalTimeTitle)),
                GetResourceString(nameof(SR.SizeOfCollectionMustBeKnownAtMarshalTimeMessageOutParam)),
                Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        /// <inheritdoc cref="SR.SizeOfCollectionMustBeKnownAtMarshalTimeMessageReturnValue"/>
        public static readonly DiagnosticDescriptor SizeOfInCollectionMustBeDefinedAtCallReturnValue =
            DiagnosticDescriptorHelper.Create(
                Ids.InvalidGeneratedComInterfaceAttributeUsage,
                GetResourceString(nameof(SR.SizeOfCollectionMustBeKnownAtMarshalTimeTitle)),
                GetResourceString(nameof(SR.SizeOfCollectionMustBeKnownAtMarshalTimeMessageReturnValue)),
                Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        /// <inheritdoc cref="SR.ComMethodReturningIntWillBeOutParameterMessage"/>
        public static readonly DiagnosticDescriptor ComMethodManagedReturnWillBeOutVariable =
            DiagnosticDescriptorHelper.Create(
                Ids.NotRecommendedGeneratedComInterfaceUsage,
                GetResourceString(nameof(SR.ComMethodReturningIntWillBeOutParameterTitle)),
                GetResourceString(nameof(SR.ComMethodReturningIntWillBeOutParameterMessage)),
                Category,
                DiagnosticSeverity.Info,
                isEnabledByDefault: true);

        /// <inheritdoc cref="SR.HResultTypeWillBeTreatedAsStructMessage"/>
        public static readonly DiagnosticDescriptor HResultTypeWillBeTreatedAsStruct =
            DiagnosticDescriptorHelper.Create(
                Ids.NotRecommendedGeneratedComInterfaceUsage,
                GetResourceString(nameof(SR.HResultTypeWillBeTreatedAsStructTitle)),
                GetResourceString(nameof(SR.HResultTypeWillBeTreatedAsStructMessage)),
                Category,
                DiagnosticSeverity.Info,
                isEnabledByDefault: true);

        /// <inheritdoc cref="SR.ComInterfaceUsageDoesNotFollowBestPracticesMessageWithDetails"/>
        public static readonly DiagnosticDescriptor GeneratedComInterfaceUsageDoesNotFollowBestPractices =
            new DiagnosticDescriptor(
                Ids.NotRecommendedGeneratedComInterfaceUsage,
                GetResourceString(nameof(SR.ComInterfaceUsageDoesNotFollowBestPracticesTitle)),
                GetResourceString(nameof(SR.ComInterfaceUsageDoesNotFollowBestPracticesMessageWithDetails)),
                Category,
                DiagnosticSeverity.Info,
                isEnabledByDefault: true,
                helpLinkUri: "aka.ms/GeneratedComInterfaceUsage");

        /// <inheritdoc cref="SR.BaseInterfaceDefinedInOtherAssemblyMessage" />
        public static readonly DiagnosticDescriptor BaseInterfaceDefinedInOtherAssembly =
            new DiagnosticDescriptor(
                Ids.BaseInterfaceDefinedInOtherAssembly,
                GetResourceString(nameof(SR.BaseInterfaceDefinedInOtherAssemblyTitle)),
                GetResourceString(nameof(SR.BaseInterfaceDefinedInOtherAssemblyMessage)),
                Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                helpLinkUri: "aka.ms/GeneratedComInterfaceUsage");

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
