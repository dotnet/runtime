using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    internal static class DiagnosticExtensions
    {
        public static Diagnostic CreateDiagnostic(
            this ISymbol symbol,
            DiagnosticDescriptor descriptor,
            params object[] args)
        {
            IEnumerable<Location> locationsInSource = symbol.Locations.Where(l => l.IsInSource);
            if (!locationsInSource.Any())
                return Diagnostic.Create(descriptor, Location.None, args);

            return Diagnostic.Create(
                descriptor,
                location: locationsInSource.First(),
                additionalLocations: locationsInSource.Skip(1),
                messageArgs: args);
        }

        public static Diagnostic CreateDiagnostic(
            this ImmutableArray<Location> locations,
            DiagnosticDescriptor descriptor,
            params object[] args)
        {
            IEnumerable<Location> locationsInSource = locations.Where(l => l.IsInSource);
            if (!locationsInSource.Any())
                return Diagnostic.Create(descriptor, Location.None, args);

            return Diagnostic.Create(
                descriptor,
                location: locationsInSource.First(),
                additionalLocations: locationsInSource.Skip(1),
                messageArgs: args);
        }

        public static Diagnostic CreateDiagnostic(
            this AttributeData attributeData,
            DiagnosticDescriptor descriptor,
            params object[] args)
        {
            SyntaxReference? syntaxReference = attributeData.ApplicationSyntaxReference;
            Location location = syntaxReference is not null
                ? syntaxReference.GetSyntax().GetLocation()
                : Location.None;

            return Diagnostic.Create(
                descriptor,
                location: location.IsInSource ? location : Location.None,
                messageArgs: args);
        }
    }

    /// <summary>
    /// Class for reporting diagnostics in the DLL import generator
    /// </summary>
    public class GeneratorDiagnostics
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

        public readonly static DiagnosticDescriptor TargetFrameworkNotSupported =
            new DiagnosticDescriptor(
                Ids.TargetFrameworkNotSupported,
                GetResourceString(nameof(Resources.TargetFrameworkNotSupportedTitle)),
                GetResourceString(nameof(Resources.TargetFrameworkNotSupportedMessage)),
                Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: GetResourceString(nameof(Resources.TargetFrameworkNotSupportedDescription)));

        private readonly GeneratorExecutionContext context;

        public GeneratorDiagnostics(GeneratorExecutionContext context)
        {
            this.context = context;
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
                this.context.ReportDiagnostic(
                    attributeData.CreateDiagnostic(
                        GeneratorDiagnostics.ConfigurationNotSupported,
                        configurationName));
            }
            else
            {
                this.context.ReportDiagnostic(
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
        internal void ReportMarshallingNotSupported(
            IMethodSymbol method,
            TypePositionInfo info,
            string? notSupportedDetails)
        {
            if (!string.IsNullOrEmpty(notSupportedDetails))
            {
                // Report the specific not-supported reason.
                if (info.IsManagedReturnPosition)
                {
                    this.context.ReportDiagnostic(
                        method.CreateDiagnostic(
                            GeneratorDiagnostics.ReturnTypeNotSupportedWithDetails,
                            notSupportedDetails!,
                            method.Name));
                }
                else
                {
                    Debug.Assert(info.ManagedIndex <= method.Parameters.Length);
                    IParameterSymbol paramSymbol = method.Parameters[info.ManagedIndex];
                    this.context.ReportDiagnostic(
                        paramSymbol.CreateDiagnostic(
                            GeneratorDiagnostics.ParameterTypeNotSupportedWithDetails,
                            notSupportedDetails!,
                            paramSymbol.Name));
                }
            }
            else if (info.MarshallingAttributeInfo is MarshalAsInfo)
            {
                // Report that the specified marshalling configuration is not supported.
                // We don't forward marshalling attributes, so this is reported differently
                // than when there is no attribute and the type itself is not supported.
                if (info.IsManagedReturnPosition)
                {
                    this.context.ReportDiagnostic(
                        method.CreateDiagnostic(
                            GeneratorDiagnostics.ReturnConfigurationNotSupported,
                            nameof(System.Runtime.InteropServices.MarshalAsAttribute),
                            method.Name));
                }
                else
                {
                    Debug.Assert(info.ManagedIndex <= method.Parameters.Length);
                    IParameterSymbol paramSymbol = method.Parameters[info.ManagedIndex];
                    this.context.ReportDiagnostic(
                        paramSymbol.CreateDiagnostic(
                            GeneratorDiagnostics.ParameterConfigurationNotSupported,
                            nameof(System.Runtime.InteropServices.MarshalAsAttribute),
                            paramSymbol.Name));
                }
            }
            else
            {
                // Report that the type is not supported
                if (info.IsManagedReturnPosition)
                {
                    this.context.ReportDiagnostic(
                        method.CreateDiagnostic(
                            GeneratorDiagnostics.ReturnTypeNotSupported,
                            method.ReturnType.ToDisplayString(),
                            method.Name));
                }
                else
                {
                    Debug.Assert(info.ManagedIndex <= method.Parameters.Length);
                    IParameterSymbol paramSymbol = method.Parameters[info.ManagedIndex];
                    this.context.ReportDiagnostic(
                        paramSymbol.CreateDiagnostic(
                            GeneratorDiagnostics.ParameterTypeNotSupported,
                            paramSymbol.Type.ToDisplayString(),
                            paramSymbol.Name));
                }
            }
        }

        /// <summary>
        /// Report diagnostic for targeting a framework that is not supported
        /// </summary>
        /// <param name="minimumSupportedVersion">Minimum supported version of .NET</param>
        public void ReportTargetFrameworkNotSupported(Version minimumSupportedVersion)
        {
            this.context.ReportDiagnostic(
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
