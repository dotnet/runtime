// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.Interop
{
    /// <summary>
    /// Class for reporting diagnostics in the library import generator
    /// </summary>
    public class GeneratorDiagnostics : IGeneratorDiagnostics
    {
        public class Ids
        {
            // SYSLIB1050-SYSLIB1059 are reserved for LibraryImportGenerator
            public const string Prefix = "SYSLIB";
            public const string InvalidLibraryImportAttributeUsage = Prefix + "1050";
            public const string TypeNotSupported = Prefix + "1051";
            public const string ConfigurationNotSupported = Prefix + "1052";
            public const string CannotForwardToDllImport = Prefix + "1053";
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
        /// <param name="diagnosticLocations">Method with the parameter/return</param>
        /// <param name="info">Type info for the parameter/return</param>
        /// <param name="notSupportedDetails">[Optional] Specific reason for lack of support</param>
        public void ReportMarshallingNotSupported(
            MethodSignatureDiagnosticLocations diagnosticLocations,
            TypePositionInfo info,
            string? notSupportedDetails,
            ImmutableDictionary<string, string> diagnosticProperties)
        {
            Location diagnosticLocation = Location.None;
            string elementName = string.Empty;

            if (info.IsManagedReturnPosition)
            {
                diagnosticLocation = diagnosticLocations.FallbackLocation;
                elementName = diagnosticLocations.MethodIdentifier;
            }
            else
            {
                Debug.Assert(info.ManagedIndex <= diagnosticLocations.ManagedParameterLocations.Length);
                diagnosticLocation = diagnosticLocations.ManagedParameterLocations[info.ManagedIndex];
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
                            diagnosticProperties,
                            notSupportedDetails!,
                            elementName));
                }
                else
                {
                    _diagnostics.Add(
                        diagnosticLocation.CreateDiagnostic(
                            GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails,
                            diagnosticProperties,
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
                            diagnosticProperties,
                            nameof(System.Runtime.InteropServices.MarshalAsAttribute),
                            elementName));
                }
                else
                {
                    _diagnostics.Add(
                        diagnosticLocation.CreateDiagnostic(
                            GeneratorDiagnostics.ParameterConfigurationNotSupported,
                            diagnosticProperties,
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
                            diagnosticProperties,
                            info.ManagedType.DiagnosticFormattedName,
                            elementName));
                }
                else
                {
                    _diagnostics.Add(
                        diagnosticLocation.CreateDiagnostic(
                            GeneratorDiagnostics.ParameterTypeNotSupported,
                            diagnosticProperties,
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
                    new LocalizableResourceString(reasonResourceName, SR.ResourceManager, typeof(FxResources.Microsoft.Interop.LibraryImportGenerator.SR), reasonArgs)));
        }

        /// <summary>
        /// Report diagnostic for configuration that cannot be forwarded to <see cref="DllImportAttribute" />
        /// </summary>
        /// <param name="method">Method with the configuration that cannot be forwarded</param>
        /// <param name="name">Configuration name</param>
        /// <param name="value">Configuration value</param>
        public void ReportCannotForwardToDllImport(MethodSignatureDiagnosticLocations method, string name, string? value = null)
        {
            _diagnostics.Add(
                Diagnostic.Create(
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
