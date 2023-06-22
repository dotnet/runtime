// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed partial class ConfigurationBindingGenerator
    {
        private sealed partial class Emitter
        {
            private bool ShouldEmitMethods(MethodsToGen_Extensions_ServiceCollection methods) => (_sourceGenSpec.MethodsToGen_ServiceCollectionExt & methods) != 0;

            private void EmitBinder_Extensions_ServiceCollection()
            {
                if (!ShouldEmitMethods(MethodsToGen_Extensions_ServiceCollection.Any))
                {
                    return;
                }

                EmitBlankLineIfRequired();

                _writer.WriteLine("/// <summary>Generated helper providing an AOT and linking compatible implementation for configuration binding.</summary>");
                _writer.WriteBlockStart($"internal static class {Identifier.GeneratedServiceCollectionBinder}");
                _precedingBlockExists = false;

                const string defaultNameExpr = "string.Empty";
                const string configureMethodString = $"global::{Identifier.GeneratedServiceCollectionBinder}.{Identifier.Configure}";
                string configParam = $"{FullyQualifiedDisplayString.IConfiguration} {Identifier.configuration}";

                if (ShouldEmitMethods(MethodsToGen_Extensions_ServiceCollection.Configure_T))
                {
                    EmitBlockStart(configParam);
                    _writer.WriteLine($"return {configureMethodString}<{Identifier.TOptions}>({Identifier.services}, {defaultNameExpr}, {Identifier.configuration}, {Identifier.configureOptions}: null);");
                    _writer.WriteBlockEnd();
                }

                if (ShouldEmitMethods(MethodsToGen_Extensions_ServiceCollection.Configure_T_name))
                {
                    EmitBlockStart(
                        paramList: $"string? {Identifier.name}, " + configParam);
                    _writer.WriteLine($"return {configureMethodString}<{Identifier.TOptions}>({Identifier.services}, {Identifier.name}, {Identifier.configuration}, {Identifier.configureOptions}: null);");
                    _writer.WriteBlockEnd();
                }

                if (ShouldEmitMethods(MethodsToGen_Extensions_ServiceCollection.Configure_T_BinderOptions))
                {
                    EmitBlockStart(
                        paramList: configParam + $", {FullyQualifiedDisplayString.ActionOfBinderOptions}? {Identifier.configureOptions}");
                    _writer.WriteLine($"return {configureMethodString}<{Identifier.TOptions}>({Identifier.services}, {defaultNameExpr}, {Identifier.configuration}, {Identifier.configureOptions});");
                    _writer.WriteBlockEnd();
                }

                string optionsNamespaceName = "global::Microsoft.Extensions.Options";
                string bindCoreUntypedDisplayString = GetHelperMethodDisplayString(Identifier.BindCoreUntyped);

                EmitBlockStart(paramList: $"string? {Identifier.name}, " + configParam + $", {FullyQualifiedDisplayString.ActionOfBinderOptions}? {Identifier.configureOptions}");

                EmitCheckForNullArgument_WithBlankLine(Identifier.services);
                EmitCheckForNullArgument_WithBlankLine(Identifier.configuration);

                _writer.WriteBlock($$"""
                    global::Microsoft.Extensions.DependencyInjection.OptionsServiceCollectionExtensions.AddOptions({{Identifier.services}});
                    {{FullyQualifiedDisplayString.AddSingleton}}<{{FullyQualifiedDisplayString.IOptionsChangeTokenSource}}<{{Identifier.TOptions}}>>({{Identifier.services}}, new {{FullyQualifiedDisplayString.ConfigurationChangeTokenSource}}<{{Identifier.TOptions}}>({{Identifier.name}}, {{Identifier.configuration}}));
                    return {{FullyQualifiedDisplayString.AddSingleton}}<{{optionsNamespaceName}}.IConfigureOptions<{{Identifier.TOptions}}>>({{Identifier.services}}, new {{optionsNamespaceName}}.ConfigureNamedOptions<{{Identifier.TOptions}}>({{Identifier.name}}, {{Identifier.obj}} => {{bindCoreUntypedDisplayString}}({{Identifier.configuration}}, {{Identifier.obj}}, typeof({{Identifier.TOptions}}), {{Identifier.configureOptions}})));
                }
                """);

                _writer.WriteBlockEnd();
                _precedingBlockExists = true;
            }

            private void EmitBlockStart(string paramList)
            {
                paramList = $"this {FullyQualifiedDisplayString.IServiceCollection} {Identifier.services}, {paramList}";

                EmitBlankLineIfRequired();
                _writer.WriteBlock($$"""
                /// <summary>Registers a configuration instance which TOptions will bind against.</summary>
                public static {{FullyQualifiedDisplayString.IServiceCollection}} {{Identifier.Configure}}<{{Identifier.TOptions}}>({{paramList}}) where {{Identifier.TOptions}} : class
                {
                """);
            }
        }
    }
}
