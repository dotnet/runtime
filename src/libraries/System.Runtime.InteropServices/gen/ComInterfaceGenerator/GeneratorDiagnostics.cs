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
    public class GeneratorDiagnostics : IGeneratorDiagnostics
    {
        public class Ids
        {
            // SYSLIB1050-SYSLIB1059 are reserved for source-generated Interop
            public const string Prefix = "SYSLIB";
            public const string InvalidLibraryImportAttributeUsage = Prefix + "1050";
            public const string TypeNotSupported = Prefix + "1051";
            public const string ConfigurationNotSupported = Prefix + "1052";
            public const string MethodNotDeclaredInAttributedInterface = Prefix + "1091";
            public const string InvalidGeneratedComInterfaceAttributeUsage = Prefix + "1092";
            public const string MultipleComInterfaceBaseTypes = Prefix + "1093";
        }

        private const string Category = "ComInterfaceGenerator";

        public static readonly DiagnosticDescriptor InvalidAttributedMethodSignature =
            new DiagnosticDescriptor(
            Ids.InvalidLibraryImportAttributeUsage,
            GetResourceString(nameof(SR.InvalidVirtualMethodIndexAttributeUsage)),
            GetResourceString(nameof(SR.InvalidAttributedMethodSignatureMessage)),
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: GetResourceString(nameof(SR.InvalidAttributedMethodDescription)));

        public static readonly DiagnosticDescriptor InvalidAttributedMethodContainingTypeMissingModifiers =
            new DiagnosticDescriptor(
            Ids.InvalidLibraryImportAttributeUsage,
            GetResourceString(nameof(SR.InvalidVirtualMethodIndexAttributeUsage)),
            GetResourceString(nameof(SR.InvalidAttributedMethodContainingTypeMissingModifiersMessage)),
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: GetResourceString(nameof(SR.InvalidAttributedMethodDescription)));

        public static readonly DiagnosticDescriptor InvalidAttributedMethodContainingTypeMissingUnmanagedObjectUnwrapperAttribute =
            new DiagnosticDescriptor(
            Ids.InvalidLibraryImportAttributeUsage,
            GetResourceString(nameof(SR.InvalidVirtualMethodIndexAttributeUsage)),
            GetResourceString(nameof(SR.InvalidAttributedMethodContainingTypeMissingUnmanagedObjectUnwrapperAttributeMessage)),
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: GetResourceString(nameof(SR.InvalidAttributedMethodDescription)));

        public static readonly DiagnosticDescriptor InvalidStringMarshallingConfiguration =
            new DiagnosticDescriptor(
            Ids.InvalidLibraryImportAttributeUsage,
            GetResourceString(nameof(SR.InvalidVirtualMethodIndexAttributeUsage)),
            GetResourceString(nameof(SR.InvalidStringMarshallingConfigurationMessage)),
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: GetResourceString(nameof(SR.InvalidStringMarshallingConfigurationDescription)));

        public static readonly DiagnosticDescriptor InvalidExceptionMarshallingConfiguration =
            new DiagnosticDescriptor(
            Ids.InvalidLibraryImportAttributeUsage,
            GetResourceString(nameof(SR.InvalidVirtualMethodIndexAttributeUsage)),
            GetResourceString(nameof(SR.InvalidExceptionMarshallingConfigurationMessage)),
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: GetResourceString(nameof(SR.InvalidExceptionMarshallingConfigurationDescription)));

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

        public static readonly DiagnosticDescriptor MethodNotDeclaredInAttributedInterface =
            new DiagnosticDescriptor(
                Ids.MethodNotDeclaredInAttributedInterface,
                GetResourceString(nameof(SR.MethodNotDeclaredInAttributedInterfaceTitle)),
                GetResourceString(nameof(SR.MethodNotDeclaredInAttributedInterfaceMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.MethodNotDeclaredInAttributedInterfaceDescription)));

        public static readonly DiagnosticDescriptor InvalidAttributedInterfaceMissingGuidAttribute =
            new DiagnosticDescriptor(
                Ids.InvalidGeneratedComInterfaceAttributeUsage,
                GetResourceString(nameof(SR.InvalidGeneratedComInterfaceAttributeUsageTitle)),
                GetResourceString(nameof(SR.InvalidGeneratedComInterfaceAttributeUsageMissingGuidAttribute)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.InvalidGeneratedComInterfaceAttributeUsageDescription)));

        public static readonly DiagnosticDescriptor MultipleComInterfaceBaseTypesAttribute =
            new DiagnosticDescriptor(
                Ids.MultipleComInterfaceBaseTypes,
                GetResourceString(nameof(SR.MultipleComInterfaceBaseTypesTitle)),
                GetResourceString(nameof(SR.MultipleComInterfaceBaseTypesMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(SR.MultipleComInterfaceBaseTypesDescription)));

        private readonly List<Diagnostic> _diagnostics = new List<Diagnostic>();

        public IEnumerable<Diagnostic> Diagnostics => _diagnostics;


        /// <summary>
        /// Report diagnostic for invalid configuration for string marshalling.
        /// </summary>
        /// <param name="attributeData">Attribute specifying the invalid configuration</param>
        /// <param name="methodName">Name of the method</param>
        /// <param name="detailsMessage">Specific reason the configuration is invalid</param>
        public void ReportInvalidStringMarshallingConfiguration(
            AttributeData attributeData,
            string methodName,
            string detailsMessage)
        {
            _diagnostics.Add(
                attributeData.CreateDiagnostic(
                    GeneratorDiagnostics.InvalidStringMarshallingConfiguration,
                    methodName,
                    detailsMessage));
        }

        /// <summary>
        /// Report diagnostic for invalid configuration for string marshalling.
        /// </summary>
        /// <param name="attributeData">Attribute specifying the invalid configuration</param>
        /// <param name="methodName">Name of the method</param>
        /// <param name="detailsMessage">Specific reason the configuration is invalid</param>
        public void ReportInvalidExceptionMarshallingConfiguration(
            AttributeData attributeData,
            string methodName,
            string detailsMessage)
        {
            _diagnostics.Add(
                attributeData.CreateDiagnostic(
                    GeneratorDiagnostics.InvalidExceptionMarshallingConfiguration,
                    methodName,
                    detailsMessage));
        }

        /// <summary>
        /// Report diagnostic for configuration that is not supported by the DLL import source generator
        /// </summary>
        /// <param name="attributeData">Attribute specifying the unsupported configuration</param>
        /// <param name="configurationName">Name of the configuration</param>
        /// <param name="unsupportedValue">[Optiona] Unsupported configuration value</param>
        public void ReportConfigurationNotSupported(
            AttributeData attributeData,
            string configurationName,
            string? unsupportedValue = null)
        {
            if (unsupportedValue == null)
            {
                _diagnostics.Add(
                    attributeData.CreateDiagnostic(
                        GeneratorDiagnostics.ConfigurationNotSupported,
                        configurationName));
            }
            else
            {
                _diagnostics.Add(
                    attributeData.CreateDiagnostic(
                        GeneratorDiagnostics.ConfigurationValueNotSupported,
                        unsupportedValue,
                        configurationName));
            }
        }

        /// <summary>
        /// Report diagnostic for marshalling of a parameter/return that is not supported
        /// </summary>
        /// <param name="method">Method with the parameter/return</param>
        /// <param name="info">Type info for the parameter/return</param>
        /// <param name="notSupportedDetails">[Optional] Specific reason for lack of support</param>
        public void ReportMarshallingNotSupported(
            MethodSignatureDiagnosticLocations method,
            TypePositionInfo info,
            string? notSupportedDetails)
        {
            Location diagnosticLocation = Location.None;
            string elementName = string.Empty;

            if (info.IsManagedReturnPosition)
            {
                diagnosticLocation = method.FallbackLocation;
                elementName = method.MethodIdentifier;
            }
            else
            {
                Debug.Assert(info.ManagedIndex <= method.ManagedParameterLocations.Length);
                diagnosticLocation = method.ManagedParameterLocations[info.ManagedIndex];
                elementName = info.InstanceIdentifier;
            }

            if (!string.IsNullOrEmpty(notSupportedDetails))
            {
                // Report the specific not-supported reason.
                if (info.IsManagedReturnPosition)
                {
                    _diagnostics.Add(
                        diagnosticLocation.CreateDiagnostic(
                            GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails,
                            notSupportedDetails!,
                            elementName));
                }
                else
                {
                    _diagnostics.Add(
                        diagnosticLocation.CreateDiagnostic(
                            GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails,
                            notSupportedDetails!,
                            elementName));
                }
            }
            else if (info.MarshallingAttributeInfo is MarshalAsInfo)
            {
                // Report that the specified marshalling configuration is not supported.
                // We don't forward marshalling attributes, so this is reported differently
                // than when there is no attribute and the type itself is not supported.
                if (info.IsManagedReturnPosition)
                {
                    _diagnostics.Add(
                        diagnosticLocation.CreateDiagnostic(
                            GeneratorDiagnostics.ReturnConfigurationNotSupported,
                            nameof(System.Runtime.InteropServices.MarshalAsAttribute),
                            elementName));
                }
                else
                {
                    _diagnostics.Add(
                        diagnosticLocation.CreateDiagnostic(
                            GeneratorDiagnostics.ParameterConfigurationNotSupported,
                            nameof(System.Runtime.InteropServices.MarshalAsAttribute),
                            elementName));
                }
            }
            else
            {
                // Report that the type is not supported
                if (info.IsManagedReturnPosition)
                {
                    _diagnostics.Add(
                        diagnosticLocation.CreateDiagnostic(
                            GeneratorDiagnostics.ReturnTypeNotSupported,
                            info.ManagedType.DiagnosticFormattedName,
                            elementName));
                }
                else
                {
                    _diagnostics.Add(
                        diagnosticLocation.CreateDiagnostic(
                            GeneratorDiagnostics.ParameterTypeNotSupported,
                            info.ManagedType.DiagnosticFormattedName,
                            elementName));
                }
            }
        }

        public void ReportInvalidMarshallingAttributeInfo(
            AttributeData attributeData,
            string reasonResourceName,
            params string[] reasonArgs)
        {
            _diagnostics.Add(
                attributeData.CreateDiagnostic(
                    GeneratorDiagnostics.MarshallingAttributeConfigurationNotSupported,
                    new LocalizableResourceString(reasonResourceName, SR.ResourceManager, typeof(FxResources.Microsoft.Interop.ComInterfaceGenerator.SR), reasonArgs)));
        }
        private static LocalizableResourceString GetResourceString(string resourceName)
        {
            return new LocalizableResourceString(resourceName, SR.ResourceManager, typeof(FxResources.Microsoft.Interop.ComInterfaceGenerator.SR));
        }
    }
}
