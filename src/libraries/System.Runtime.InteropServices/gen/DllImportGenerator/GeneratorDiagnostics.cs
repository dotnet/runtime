using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace Microsoft.Interop
{

    /// <summary>
    /// Class for reporting diagnostics in the DLL import generator
    /// </summary>
    public class GeneratorDiagnostics : IGeneratorDiagnostics
    {
        public class Ids
        {
            public const string Prefix = "DLLIMPORTGEN";
            public const string TypeNotSupported = Prefix + "001";
            public const string ConfigurationNotSupported = Prefix + "002";
            public const string TargetFrameworkNotSupported = Prefix + "003";
        }

        private const string Category = "SourceGeneration";

        public readonly static DiagnosticDescriptor ParameterTypeNotSupported =
            new DiagnosticDescriptor(
                Ids.TypeNotSupported,
                GetResourceString(nameof(Resources.TypeNotSupportedTitle)),
                GetResourceString(nameof(Resources.TypeNotSupportedMessageParameter)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.TypeNotSupportedDescription)));

        public readonly static DiagnosticDescriptor ReturnTypeNotSupported =
            new DiagnosticDescriptor(
                Ids.TypeNotSupported,
                GetResourceString(nameof(Resources.TypeNotSupportedTitle)),
                GetResourceString(nameof(Resources.TypeNotSupportedMessageReturn)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.TypeNotSupportedDescription)));

        public readonly static DiagnosticDescriptor ParameterTypeNotSupportedWithDetails =
            new DiagnosticDescriptor(
                Ids.TypeNotSupported,
                GetResourceString(nameof(Resources.TypeNotSupportedTitle)),
                GetResourceString(nameof(Resources.TypeNotSupportedMessageParameterWithDetails)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.TypeNotSupportedDescription)));

        public readonly static DiagnosticDescriptor ReturnTypeNotSupportedWithDetails =
            new DiagnosticDescriptor(
                Ids.TypeNotSupported,
                GetResourceString(nameof(Resources.TypeNotSupportedTitle)),
                GetResourceString(nameof(Resources.TypeNotSupportedMessageReturnWithDetails)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.TypeNotSupportedDescription)));

        public readonly static DiagnosticDescriptor ParameterConfigurationNotSupported =
            new DiagnosticDescriptor(
                Ids.ConfigurationNotSupported,
                GetResourceString(nameof(Resources.ConfigurationNotSupportedTitle)),
                GetResourceString(nameof(Resources.ConfigurationNotSupportedMessageParameter)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.ConfigurationNotSupportedDescription)));

        public readonly static DiagnosticDescriptor ReturnConfigurationNotSupported =
            new DiagnosticDescriptor(
                Ids.ConfigurationNotSupported,
                GetResourceString(nameof(Resources.ConfigurationNotSupportedTitle)),
                GetResourceString(nameof(Resources.ConfigurationNotSupportedMessageReturn)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.ConfigurationNotSupportedDescription)));

        public readonly static DiagnosticDescriptor ConfigurationNotSupported =
            new DiagnosticDescriptor(
                Ids.ConfigurationNotSupported,
                GetResourceString(nameof(Resources.ConfigurationNotSupportedTitle)),
                GetResourceString(nameof(Resources.ConfigurationNotSupportedMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.ConfigurationNotSupportedDescription)));

        public readonly static DiagnosticDescriptor ConfigurationValueNotSupported =
            new DiagnosticDescriptor(
                Ids.ConfigurationNotSupported,
                GetResourceString(nameof(Resources.ConfigurationNotSupportedTitle)),
                GetResourceString(nameof(Resources.ConfigurationNotSupportedMessageValue)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.ConfigurationNotSupportedDescription)));

        public readonly static DiagnosticDescriptor MarshallingAttributeConfigurationNotSupported =
            new DiagnosticDescriptor(
                Ids.ConfigurationNotSupported,
                GetResourceString(nameof(Resources.ConfigurationNotSupportedTitle)),
                GetResourceString(nameof(Resources.ConfigurationNotSupportedMessageMarshallingInfo)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.ConfigurationNotSupportedDescription)));

        public readonly static DiagnosticDescriptor TargetFrameworkNotSupported =
            new DiagnosticDescriptor(
                Ids.TargetFrameworkNotSupported,
                GetResourceString(nameof(Resources.TargetFrameworkNotSupportedTitle)),
                GetResourceString(nameof(Resources.TargetFrameworkNotSupportedMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.TargetFrameworkNotSupportedDescription)));

        private readonly List<Diagnostic> diagnostics = new List<Diagnostic>();

        public IEnumerable<Diagnostic> Diagnostics => diagnostics;

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
                diagnostics.Add(
                    attributeData.CreateDiagnostic(
                        GeneratorDiagnostics.ConfigurationNotSupported,
                        configurationName));
            }
            else
            {
                diagnostics.Add(
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
            MethodDeclarationSyntax method,
            TypePositionInfo info,
            string? notSupportedDetails)
        {
            Location diagnosticLocation = Location.None;
            string elementName = string.Empty;

            if (info.IsManagedReturnPosition)
            {
                diagnosticLocation = Location.Create(method.SyntaxTree, method.Identifier.Span);
                elementName = method.Identifier.ValueText;
            }
            else
            {
                Debug.Assert(info.ManagedIndex <= method.ParameterList.Parameters.Count);
                ParameterSyntax param = method.ParameterList.Parameters[info.ManagedIndex];
                diagnosticLocation = Location.Create(param.SyntaxTree, param.Identifier.Span);
                elementName = param.Identifier.ValueText;
            }

            if (!string.IsNullOrEmpty(notSupportedDetails))
            {
                // Report the specific not-supported reason.
                if (info.IsManagedReturnPosition)
                {
                    diagnostics.Add(
                        diagnosticLocation.CreateDiagnostic(
                            GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails,
                            notSupportedDetails!,
                            elementName));
                }
                else
                {
                    diagnostics.Add(
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
                    diagnostics.Add(
                        diagnosticLocation.CreateDiagnostic(
                            GeneratorDiagnostics.ReturnConfigurationNotSupported,
                            nameof(System.Runtime.InteropServices.MarshalAsAttribute),
                            elementName));
                }
                else
                {
                    diagnostics.Add(
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
                    diagnostics.Add(
                        diagnosticLocation.CreateDiagnostic(
                            GeneratorDiagnostics.ReturnTypeNotSupported,
                            info.ManagedType.DiagnosticFormattedName,
                            elementName));
                }
                else
                {
                    diagnostics.Add(
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
            diagnostics.Add(
                attributeData.CreateDiagnostic(
                    GeneratorDiagnostics.MarshallingAttributeConfigurationNotSupported,
                    new LocalizableResourceString(reasonResourceName, Resources.ResourceManager, typeof(Resources), reasonArgs)));
        }

        /// <summary>
        /// Report diagnostic for targeting a framework that is not supported
        /// </summary>
        /// <param name="minimumSupportedVersion">Minimum supported version of .NET</param>
        public void ReportTargetFrameworkNotSupported(Version minimumSupportedVersion)
        {
            diagnostics.Add(
                Diagnostic.Create(
                    TargetFrameworkNotSupported,
                    Location.None,
                    minimumSupportedVersion.ToString(2)));
        }

        private static LocalizableResourceString GetResourceString(string resourceName)
        {
            return new LocalizableResourceString(resourceName, Resources.ResourceManager, typeof(Resources));
        }
    }
}
