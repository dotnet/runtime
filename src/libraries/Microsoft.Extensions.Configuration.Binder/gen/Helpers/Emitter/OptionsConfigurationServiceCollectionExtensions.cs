// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed partial class ConfigurationBindingGenerator
    {
        private sealed partial class Emitter
        {
            private bool ShouldEmitMethods(MethodsToGen_Extensions_ServiceCollection methods) => (_sourceGenSpec.MethodsToGen_ServiceCollectionExt & methods) != 0;

            private void EmitBindingExtensions_IServiceCollection()
            {
                if (!ShouldEmitMethods(MethodsToGen_Extensions_ServiceCollection.Any))
                {
                    return;
                }

                EmitBindingExtStartRegion(Identifier.IServiceCollection);
                EmitConfigureMethods();
                EmitBindingExtEndRegion();
            }

            private void EmitConfigureMethods()
            {
                const string defaultNameExpr = "string.Empty";
                string configParam = $"{Identifier.IConfiguration} {Identifier.configuration}";

                if (ShouldEmitMethods(MethodsToGen_Extensions_ServiceCollection.Configure_T))
                {
                    EmitStartMethod(MethodsToGen_Extensions_ServiceCollection.Configure_T, configParam);
                    _writer.WriteLine($"return {Identifier.Configure}<{Identifier.TOptions}>({Identifier.services}, {defaultNameExpr}, {Identifier.configuration}, {Identifier.configureOptions}: null);");
                    EmitEndBlock();
                }

                if (ShouldEmitMethods(MethodsToGen_Extensions_ServiceCollection.Configure_T_name))
                {
                    EmitStartMethod(
                        MethodsToGen_Extensions_ServiceCollection.Configure_T_name,
                        paramList: $"string? {Identifier.name}, " + configParam);
                    _writer.WriteLine($"return {Identifier.Configure}<{Identifier.TOptions}>({Identifier.services}, {Identifier.name}, {Identifier.configuration}, {Identifier.configureOptions}: null);");
                    EmitEndBlock();
                }

                if (ShouldEmitMethods(MethodsToGen_Extensions_ServiceCollection.Configure_T_BinderOptions))
                {
                    EmitStartMethod(
                        MethodsToGen_Extensions_ServiceCollection.Configure_T_BinderOptions,
                        paramList: configParam + $", {TypeDisplayString.NullableActionOfBinderOptions} {Identifier.configureOptions}");
                    _writer.WriteLine($"return {Identifier.Configure}<{Identifier.TOptions}>({Identifier.services}, {defaultNameExpr}, {Identifier.configuration}, {Identifier.configureOptions});");
                    EmitEndBlock();
                }

                // Core Configure method that the other overloads call.
                // Like the others, it is public API that could be called directly by users.
                // So, it is always generated whenever a Configure overload is called.
                string optionsNamespaceName = "Microsoft.Extensions.Options";

                EmitStartMethod(MethodsToGen_Extensions_ServiceCollection.Configure_T_name_BinderOptions, paramList: $"string? {Identifier.name}, " + configParam + $", {TypeDisplayString.NullableActionOfBinderOptions} {Identifier.configureOptions}");
                EmitCheckForNullArgument_WithBlankLine(Identifier.services);
                EmitCheckForNullArgument_WithBlankLine(Identifier.configuration);
                _writer.WriteLine($$"""
                    OptionsServiceCollectionExtensions.AddOptions({{Identifier.services}});
                    {{Identifier.services}}.{{Identifier.AddSingleton}}<{{Identifier.IOptionsChangeTokenSource}}<{{Identifier.TOptions}}>>(new {{Identifier.ConfigurationChangeTokenSource}}<{{Identifier.TOptions}}>({{Identifier.name}}, {{Identifier.configuration}}));
                    return {{Identifier.services}}.{{Identifier.AddSingleton}}<{{optionsNamespaceName}}.IConfigureOptions<{{Identifier.TOptions}}>>(new {{optionsNamespaceName}}.ConfigureNamedOptions<{{Identifier.TOptions}}>({{Identifier.name}}, {{Identifier.obj}} => {{nameof(MethodsToGen_CoreBindingHelper.BindCoreMain)}}({{Identifier.configuration}}, {{Identifier.obj}}, typeof({{Identifier.TOptions}}){{Identifier.configureOptions}})));
                    """);
                EmitEndBlock();
            }

            private void EmitStartMethod(MethodsToGen_Extensions_ServiceCollection overload, string paramList)
            {
                paramList = $"this {Identifier.IServiceCollection} {Identifier.services}, {paramList}";

                EmitBlankLineIfRequired();
                _writer.WriteLine("/// <summary>Registers a configuration instance which TOptions will bind against.</summary>");
                EmitInterceptsLocationAnnotations(overload);
                EmitStartBlock($"public static {Identifier.IServiceCollection} {Identifier.Configure}<{Identifier.TOptions}>({paramList}) where {Identifier.TOptions} : class");
            }
        }
    }
}
