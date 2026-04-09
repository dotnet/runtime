// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed partial class ConfigurationBindingGenerator
    {
        private sealed partial class Emitter
        {
            private void EmitBindingExtensions_OptionsBuilder()
            {
                if (!ShouldEmitMethods(MethodsToGen.OptionsBuilderExt_Any))
                {
                    return;
                }

                EmitBindingExtStartRegion(TypeDisplayString.OptionsBuilderOfTOptions);
                EmitBindMethods_Extensions_OptionsBuilder();
                EmitBindConfigurationMethod();
                EmitBindingExtEndRegion();
            }

            private void EmitBindMethods_Extensions_OptionsBuilder()
            {
                if (!ShouldEmitMethods(MethodsToGen.OptionsBuilderExt_Bind))
                {
                    return;
                }

                const string documentation = @"/// <summary>Registers a configuration instance which <typeparamref name=""TOptions""/> will bind against.</summary>";
                const string paramList = $"{Identifier.IConfiguration} {Identifier.config}";

                if (ShouldEmitMethods(MethodsToGen.OptionsBuilderExt_Bind_T))
                {
                    EmitMethodStartBlock(MethodsToGen.OptionsBuilderExt_Bind_T, "Bind", paramList, documentation);
                    _writer.WriteLine($"return Bind({Identifier.optionsBuilder}, {Identifier.config}, {Identifier.configureBinder}: null);");
                    EmitEndBlock();
                }

                EmitMethodStartBlock(
                    MethodsToGen.OptionsBuilderExt_Bind_T_BinderOptions,
                    "Bind",
                    paramList + $", {TypeDisplayString.NullableActionOfBinderOptions} {Identifier.configureBinder}",
                    documentation);

                EmitCheckForNullArgument_WithBlankLine(Identifier.optionsBuilder, _emitThrowIfNullMethod);

                _writer.WriteLine($$"""
                    {{Identifier.Configure}}<{{Identifier.TOptions}}>({{Identifier.optionsBuilder}}.{{Identifier.Services}}, {{Identifier.optionsBuilder}}.{{Identifier.Name}}, {{Identifier.config}}, {{Identifier.configureBinder}});
                    return {{Identifier.optionsBuilder}};
                    """);

                EmitEndBlock();
            }

            private void EmitBindConfigurationMethod()
            {
                if (!ShouldEmitMethods(MethodsToGen.OptionsBuilderExt_BindConfiguration_T_path_BinderOptions))
                {
                    return;
                }

                const string documentation = $@"/// <summary>Registers the dependency injection container to bind <typeparamref name=""TOptions""/> against the <see cref=""{Identifier.IConfiguration}""/> obtained from the DI service provider.</summary>";
                string paramList = $"string {Identifier.configSectionPath}, {TypeDisplayString.NullableActionOfBinderOptions} {Identifier.configureBinder} = null";

                EmitMethodStartBlock(MethodsToGen.OptionsBuilderExt_BindConfiguration, "BindConfiguration", paramList, documentation);

                EmitCheckForNullArgument_WithBlankLine(Identifier.optionsBuilder, _emitThrowIfNullMethod);
                EmitCheckForNullArgument_WithBlankLine(Identifier.configSectionPath, _emitThrowIfNullMethod);

                EmitStartBlock($"{Identifier.optionsBuilder}.{Identifier.Configure}<{Identifier.IConfiguration}>(({Identifier.instance}, {Identifier.config}) =>");
                EmitCheckForNullArgument_WithBlankLine(Identifier.config, _emitThrowIfNullMethod);
                _writer.WriteLine($$"""
                    {{Identifier.IConfiguration}} {{Identifier.section}} = string.Equals(string.Empty, {{Identifier.configSectionPath}}, StringComparison.OrdinalIgnoreCase) ? {{Identifier.config}} : {{Identifier.config}}.{{Identifier.GetSection}}({{Identifier.configSectionPath}});
                    {{nameof(MethodsToGen_CoreBindingHelper.BindCoreMain)}}({{Identifier.section}}, {{Identifier.instance}}, typeof({{Identifier.TOptions}}), {{Identifier.configureBinder}});
                    """);

                EmitEndBlock(endBraceTrailingSource: ");");

                _writer.WriteLine();

                EmitStartBlock($"{Identifier.optionsBuilder}.{Identifier.Services}.{Identifier.AddSingleton}<{Identifier.IOptionsChangeTokenSource}<{Identifier.TOptions}>, {Identifier.ConfigurationChangeTokenSource}<{Identifier.TOptions}>>({Identifier.sp} =>");

                _writer.WriteLine($"return new {Identifier.ConfigurationChangeTokenSource}<{Identifier.TOptions}>({Identifier.optionsBuilder}.{Identifier.Name}, {Identifier.sp}.GetRequiredService<{Identifier.IConfiguration}>());");

                EmitEndBlock(endBraceTrailingSource: ");");

                _writer.WriteLine();

                _writer.WriteLine($"return {Identifier.optionsBuilder};");

                EmitEndBlock();
            }

            private void EmitMethodStartBlock(MethodsToGen method, string methodName, string paramList, string documentation)
            {
                paramList = $"this {TypeDisplayString.OptionsBuilderOfTOptions} {Identifier.optionsBuilder}, {paramList}";
                EmitBlankLineIfRequired();
                _writer.WriteLine(documentation);
                EmitInterceptsLocationAnnotations(method);
                EmitStartBlock($"public static {TypeDisplayString.OptionsBuilderOfTOptions} {methodName}<{Identifier.TOptions}>({paramList}) where {Identifier.TOptions} : class");
            }
        }
    }
}
