// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Resources;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    /// <summary>
    /// A bag of diagnostics that generators can report diagnostics to.
    /// </summary>
    public class GeneratorDiagnosticsBag
    {
        private readonly IDiagnosticDescriptorProvider _descriptorProvider;
        private readonly ISignatureDiagnosticLocations _locations;
        private readonly ResourceManager _resourceManager;
        private readonly Type _resourceSource;

        private readonly List<DiagnosticInfo> _diagnostics = new();
        public IEnumerable<DiagnosticInfo> Diagnostics => _diagnostics;

        /// <summary>
        /// Construct a generator diagnostics bag.
        /// </summary>
        /// <param name="descriptorProvider">A provider of specific diagnostic descriptors to use.</param>
        /// <param name="locations">A provider of locations for diagnostics on <see cref="TypePositionInfo"/>s.</param>
        /// <param name="resourceManager">A resource manager to resolve resource string parameters to diagnostics.</param>
        /// <param name="resourceSource">The type that represents the resource source.</param>
        public GeneratorDiagnosticsBag(IDiagnosticDescriptorProvider descriptorProvider, ISignatureDiagnosticLocations locations, ResourceManager resourceManager, Type resourceSource)
        {
            _descriptorProvider = descriptorProvider;
            _locations = locations;
            _resourceManager = resourceManager;
            _resourceSource = resourceSource;
        }

        /// <summary>
        /// Report a diagnostic.
        /// </summary>
        /// <param name="diagnostic">The diagnostic info.</param>
        public void ReportDiagnostic(DiagnosticInfo diagnostic)
        {
            _diagnostics.Add(diagnostic);
        }

        /// <summary>
        /// Report diagnostic for configuration that is not supported by the DLL import source generator
        /// </summary>
        /// <param name="attributeData">Attribute specifying the unsupported configuration</param>
        /// <param name="configurationName">Name of the configuration</param>
        /// <param name="unsupportedValue">Unsupported configuration value</param>
        public void ReportConfigurationNotSupported(
            AttributeData attributeData,
            string configurationName,
            string unsupportedValue)
        {
            ReportDiagnostic(attributeData.CreateDiagnosticInfo(_descriptorProvider.ConfigurationValueNotSupported, unsupportedValue, configurationName));
        }

        /// <summary>
        /// Report diagnostic for configuration that is not supported by the DLL import source generator
        /// </summary>
        /// <param name="attributeData">Attribute specifying the unsupported configuration</param>
        /// <param name="configurationName">Name of the configuration</param>
        public void ReportConfigurationNotSupported(
            AttributeData attributeData,
            string configurationName)
        {
            ReportDiagnostic(attributeData.CreateDiagnosticInfo(_descriptorProvider.ConfigurationNotSupported, configurationName));
        }

        /// <summary>
        /// Report a diagnostic for an invalid marshalling attribute.
        /// </summary>
        /// <param name="attributeData">The attribute data.</param>
        /// <param name="reasonResourceName">A resource name with the string describing the reason the attribute is invalid.</param>
        /// <param name="reasonArgs">The arguments for the localizable string.</param>
        public void ReportInvalidMarshallingAttributeInfo(
            AttributeData attributeData,
            string reasonResourceName,
            params string[] reasonArgs)
        {
            ReportDiagnostic(attributeData.CreateDiagnosticInfo(_descriptorProvider.InvalidMarshallingAttributeInfo, new LocalizableResourceString(reasonResourceName, _resourceManager, _resourceSource, reasonArgs)));
        }

        /// <summary>
        /// Report a diagnostic generated when resolving a generator.
        /// </summary>
        /// <param name="diagnostic">The generated diagnostic.</param>
        /// <exception cref="InvalidOperationException">The <see cref="IDiagnosticDescriptorProvider"/> did not provide a diagnostic for a fatal diagnostic.</exception>
        public void ReportGeneratorDiagnostic(GeneratorDiagnostic diagnostic)
        {
            DiagnosticDescriptor? descriptor = _descriptorProvider.GetDescriptor(diagnostic);
            if (descriptor is null)
            {
                if (diagnostic.IsFatal)
                {
                    throw new InvalidOperationException();
                }
                return;
            }
            _diagnostics.Add(_locations.CreateDiagnosticInfo(descriptor, diagnostic));
        }

        /// <summary>
        /// Report a diagnostic generated when resolving a generator.
        /// </summary>
        /// <param name="diagnostic">The generated diagnostic.</param>
        /// <exception cref="InvalidOperationException">The <see cref="IDiagnosticDescriptorProvider"/> did not provide a diagnostic for a fatal diagnostic.</exception>
        public void ReportGeneratorDiagnostics(ImmutableArray<GeneratorDiagnostic> diagnostics)
        {
            foreach (var diagnostic in diagnostics)
            {
                ReportGeneratorDiagnostic(diagnostic);
            }
        }
    }

    /// <summary>
    /// Properties that are common to many interop generators' diagnostics. These properties are used by code-fixers to provide a good user experience.
    /// </summary>
    public class GeneratorDiagnosticProperties
    {
        /// <summary>
        /// The diagnostic can be resolved by adding the System.Runtime.CompilerServices.DisableRuntimeMarshallingAttribute to the assembly
        /// </summary>
        public const string AddDisableRuntimeMarshallingAttribute = nameof(AddDisableRuntimeMarshallingAttribute);

        /// <summary>
        /// The diagnostic can be resolved by adding the System.Runtime.InteropServices.MarshalAsAttribute to the element with one of the UnmanagedType values
        /// specified in the comma-separated list of values specified in the value of this property.
        /// </summary>
        public const string AddMarshalAsAttribute = nameof(AddMarshalAsAttribute);
    }
}
