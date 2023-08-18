// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed partial class ConfigurationBindingGenerator
    {
        private sealed partial class Emitter
        {
            private bool ShouldEmitMethods(MethodsToGen_Extensions_ServiceCollection methods) => (_sourceGenSpec.MethodsToGen_ServiceCollectionExt & methods) != 0;

            private void EmitBinder_Extensions_IServiceCollection()
            {
                if (!ShouldEmitMethods(MethodsToGen_Extensions_ServiceCollection.Any))
                {
                    return;
                }

                EmitRootBindingClassStartBlock(Identifier.GeneratedServiceCollectionBinder);

                const string defaultNameExpr = "string.Empty";
                const string configureMethodString = $"global::{Identifier.GeneratedServiceCollectionBinder}.{Identifier.Configure}";
                string configParam = $"{FullyQualifiedDisplayString.IConfiguration} {Identifier.configuration}";

                if (ShouldEmitMethods(MethodsToGen_Extensions_ServiceCollection.Configure_T))
                {
                    EmitStartMethod(configParam);
                    _writer.WriteLine($"return {configureMethodString}<{Identifier.TOptions}>({Identifier.services}, {defaultNameExpr}, {Identifier.configuration}, {Identifier.configureOptions}: null);");
                    EmitEndBlock();
                }

                if (ShouldEmitMethods(MethodsToGen_Extensions_ServiceCollection.Configure_T_name))
                {
                    EmitStartMethod(
                        paramList: $"string? {Identifier.name}, " + configParam);
                    _writer.WriteLine($"return {configureMethodString}<{Identifier.TOptions}>({Identifier.services}, {Identifier.name}, {Identifier.configuration}, {Identifier.configureOptions}: null);");
                    EmitEndBlock();
                }

                if (ShouldEmitMethods(MethodsToGen_Extensions_ServiceCollection.Configure_T_BinderOptions))
                {
                    EmitStartMethod(
                        paramList: configParam + $", {FullyQualifiedDisplayString.ActionOfBinderOptions}? {Identifier.configureOptions}");
                    _writer.WriteLine($"return {configureMethodString}<{Identifier.TOptions}>({Identifier.services}, {defaultNameExpr}, {Identifier.configuration}, {Identifier.configureOptions});");
                    EmitEndBlock();
                }

                // Core Configure method that the other overloads call.
                // Like the others, it is public API that could be called directly by users.
                // So, it is always generated whenever a Configure overload is called.
                string optionsNamespaceName = "global::Microsoft.Extensions.Options";
                string bindCoreUntypedDisplayString = GetHelperMethodDisplayString(nameof(MethodsToGen_CoreBindingHelper.BindCoreUntyped));

                EmitStartMethod(paramList: $"string? {Identifier.name}, " + configParam + $", {FullyQualifiedDisplayString.ActionOfBinderOptions}? {Identifier.configureOptions}");
                EmitCheckForNullArgument_WithBlankLine(Identifier.services);
                EmitCheckForNullArgument_WithBlankLine(Identifier.configuration);
                _writer.WriteLine($$"""
                    global::Microsoft.Extensions.DependencyInjection.OptionsServiceCollectionExtensions.AddOptions({{Identifier.services}});
                    {{FullyQualifiedDisplayString.AddSingleton}}<{{FullyQualifiedDisplayString.IOptionsChangeTokenSource}}<{{Identifier.TOptions}}>>({{Identifier.services}}, new {{FullyQualifiedDisplayString.ConfigurationChangeTokenSource}}<{{Identifier.TOptions}}>({{Identifier.name}}, {{Identifier.configuration}}));
                    return {{FullyQualifiedDisplayString.AddSingleton}}<{{optionsNamespaceName}}.IConfigureOptions<{{Identifier.TOptions}}>>({{Identifier.services}}, new {{optionsNamespaceName}}.ConfigureNamedOptions<{{Identifier.TOptions}}>({{Identifier.name}}, {{Identifier.obj}} => {{bindCoreUntypedDisplayString}}({{Identifier.configuration}}, {{Identifier.obj}}, typeof({{Identifier.TOptions}}), {{Identifier.configureOptions}})));
                    """);
                EmitEndBlock();

                EmitEndBlock();
                _emitBlankLineBeforeNextStatement = true;
            }

            private void EmitStartMethod(string paramList)
            {
                paramList = $"this {FullyQualifiedDisplayString.IServiceCollection} {Identifier.services}, {paramList}";

                EmitBlankLineIfRequired();
                EmitStartBlock($$"""
                    /// <summary>Registers a configuration instance which TOptions will bind against.</summary>
                    public static {{FullyQualifiedDisplayString.IServiceCollection}} {{Identifier.Configure}}<{{Identifier.TOptions}}>({{paramList}}) where {{Identifier.TOptions}} : class
                    """);
            }
        }
    }
}
