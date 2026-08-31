// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed partial class ConfigurationBindingGenerator
    {
        private sealed partial class Emitter
        {
            private void EmitBindingExtensions_IServiceCollection()
            {
                if (!ShouldEmitMethods(MethodsToGen.ServiceCollectionExt_Any))
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
                string configParam = $"{Identifier.IConfiguration} {Identifier.config}";

                if (ShouldEmitMethods(MethodsToGen.ServiceCollectionExt_Configure_T))
                {
                    EmitStartMethod(MethodsToGen.ServiceCollectionExt_Configure_T, configParam);
                    _writer.WriteLine($"return {Identifier.Configure}<{Identifier.TOptions}>({Identifier.services}, {defaultNameExpr}, {Identifier.config}, {Identifier.configureOptions}: null);");
                    EmitEndBlock();
                }

                if (ShouldEmitMethods(MethodsToGen.ServiceCollectionExt_Configure_T_name))
                {
                    EmitStartMethod(
                        MethodsToGen.ServiceCollectionExt_Configure_T_name,
                        paramList: $"string? {Identifier.name}, " + configParam);
                    _writer.WriteLine($"return {Identifier.Configure}<{Identifier.TOptions}>({Identifier.services}, {Identifier.name}, {Identifier.config}, {Identifier.configureOptions}: null);");
                    EmitEndBlock();
                }

                if (ShouldEmitMethods(MethodsToGen.ServiceCollectionExt_Configure_T_BinderOptions))
                {
                    EmitStartMethod(
                        MethodsToGen.ServiceCollectionExt_Configure_T_BinderOptions,
                        paramList: configParam + $", {TypeDisplayString.NullableActionOfBinderOptions} {Identifier.configureOptions}");
                    _writer.WriteLine($"return {Identifier.Configure}<{Identifier.TOptions}>({Identifier.services}, {defaultNameExpr}, {Identifier.config}, {Identifier.configureOptions});");
                    EmitEndBlock();
                }

                // Core Configure method that the other overloads call.
                // Like the others, it is public API that could be called directly by users.
                // So, it is always generated whenever a Configure overload is called.
                EmitStartMethod(MethodsToGen.ServiceCollectionExt_Configure_T_name_BinderOptions, paramList: $"string? {Identifier.name}, " + configParam + $", {TypeDisplayString.NullableActionOfBinderOptions} {Identifier.configureOptions}");
                EmitCheckForNullArgument_WithBlankLine(Identifier.services, _emitThrowIfNullMethod);
                EmitCheckForNullArgument_WithBlankLine(Identifier.config, _emitThrowIfNullMethod);
                _writer.WriteLine($$"""
                    OptionsServiceCollectionExtensions.AddOptions({{Identifier.services}});
                    {{Identifier.services}}.{{Identifier.AddSingleton}}<{{Identifier.IOptionsChangeTokenSource}}<{{Identifier.TOptions}}>>(new {{Identifier.ConfigurationChangeTokenSource}}<{{Identifier.TOptions}}>({{Identifier.name}}, {{Identifier.config}}));
                    return {{Identifier.services}}.{{Identifier.AddSingleton}}<IConfigureOptions<{{Identifier.TOptions}}>>(new ConfigureNamedOptions<{{Identifier.TOptions}}>({{Identifier.name}}, {{Identifier.instance}} => {{nameof(MethodsToGen_CoreBindingHelper.BindCoreMain)}}({{Identifier.config}}, {{Identifier.instance}}, typeof({{Identifier.TOptions}}), {{Identifier.configureOptions}})));
                    """);
                EmitEndBlock();
            }

            private void EmitStartMethod(MethodsToGen overload, string paramList)
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
