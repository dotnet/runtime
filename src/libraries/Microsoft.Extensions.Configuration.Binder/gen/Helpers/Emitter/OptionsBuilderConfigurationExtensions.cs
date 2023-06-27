// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed partial class ConfigurationBindingGenerator
    {
        private sealed partial class Emitter
        {
            private bool ShouldEmitMethods(MethodsToGen_Extensions_OptionsBuilder methods) => (_sourceGenSpec.MethodsToGen_OptionsBuilderExt & methods) != 0;

            private void EmitBinder_Extensions_OptionsBuilder()
            {
                if (!ShouldEmitMethods(MethodsToGen_Extensions_OptionsBuilder.Any))
                {
                    return;
                }

                EmitBlankLineIfRequired();
                _writer.WriteLine("/// <summary>Generated helper providing an AOT and linking compatible implementation for configuration binding.</summary>");
                _writer.WriteBlockStart($"internal static class {Identifier.GeneratedOptionsBuilderBinder}");
                _precedingBlockExists = false;

                EmitBindMethods_Extensions_OptionsBuilder();
                EmitBindConfigurationMethod();

                _writer.WriteBlockEnd();
            }

            private void EmitBindMethods_Extensions_OptionsBuilder()
            {
                if (!ShouldEmitMethods(MethodsToGen_Extensions_OptionsBuilder.Bind))
                {
                    return;
                }

                const string documentation = @"/// <summary>Registers a configuration instance which <typeparamref name=""TOptions""/> will bind against.</summary>";
                const string paramList = $"{FullyQualifiedDisplayString.IConfiguration} {Identifier.configuration}";

                if (ShouldEmitMethods(MethodsToGen_Extensions_OptionsBuilder.Bind_T))
                {
                    EmitMethodBlockStart("Bind", paramList, documentation);
                    _writer.WriteLine($"return global::{Identifier.GeneratedOptionsBuilderBinder}.Bind({Identifier.optionsBuilder}, {Identifier.configuration}, {Identifier.configureOptions}: null);");
                    _writer.WriteBlockEnd();
                    _writer.WriteBlankLine();
                }

                EmitMethodBlockStart(
                    "Bind",
                    paramList + $", {FullyQualifiedDisplayString.ActionOfBinderOptions}? {Identifier.configureOptions}",
                    documentation);

                EmitCheckForNullArgument_WithBlankLine(Identifier.optionsBuilder);

                _writer.WriteBlock($$"""
                    global::{{Identifier.GeneratedServiceCollectionBinder}}.{{Identifier.Configure}}<{{Identifier.TOptions}}>({{Identifier.optionsBuilder}}.{{Identifier.Services}}, {{Identifier.optionsBuilder}}.Name, {{Identifier.configuration}}, {{Identifier.configureOptions}});
                    return {{Identifier.optionsBuilder}};
                    """);

                _writer.WriteBlockEnd();
            }

            private void EmitBindConfigurationMethod()
            {
                if (!ShouldEmitMethods(MethodsToGen_Extensions_OptionsBuilder.BindConfiguration_T_path_BinderOptions))
                {
                    return;
                }

                const string documentation = $@"/// <summary>Registers the dependency injection container to bind <typeparamref name=""TOptions""/> against the <see cref=""{FullyQualifiedDisplayString.IConfiguration}""/> obtained from the DI service provider.</summary>";
                string paramList = $"string {Identifier.configSectionPath}, {FullyQualifiedDisplayString.ActionOfBinderOptions}? {Identifier.configureOptions} = null";

                EmitMethodBlockStart("BindConfiguration", paramList, documentation);

                EmitCheckForNullArgument_WithBlankLine(Identifier.optionsBuilder);
                EmitCheckForNullArgument_WithBlankLine(Identifier.configSectionPath);

                _writer.WriteBlockStart($"{Identifier.optionsBuilder}.{Identifier.Configure}<{FullyQualifiedDisplayString.IConfiguration}>(({Identifier.obj}, {Identifier.configuration}) =>");

                _writer.WriteBlock($$"""
                {{FullyQualifiedDisplayString.IConfiguration}} {{Identifier.section}} = string.Equals(string.Empty, {{Identifier.configSectionPath}}, global::System.StringComparison.OrdinalIgnoreCase) ? {{Identifier.configuration}} : {{Identifier.configuration}}.{{Identifier.GetSection}}({{Identifier.configSectionPath}});
                {{FullyQualifiedDisplayString.CoreBindingHelper}}.{{Identifier.BindCoreUntyped}}({{Identifier.section}}, {{Identifier.obj}}, typeof({{Identifier.TOptions}}), {{Identifier.configureOptions}});
            """);

                _writer.WriteBlockEnd(");");

                _writer.WriteBlankLine();

                _writer.WriteBlock($$"""
                    {{FullyQualifiedDisplayString.AddSingleton}}<{{FullyQualifiedDisplayString.IOptionsChangeTokenSource}}<{{Identifier.TOptions}}>, {{FullyQualifiedDisplayString.ConfigurationChangeTokenSource}}<{{Identifier.TOptions}}>>({{Identifier.optionsBuilder}}.{{Identifier.Services}});
                    return {{Identifier.optionsBuilder}};
                    """);

                _writer.WriteBlockEnd();
            }

            private void EmitMethodBlockStart(string methodName, string paramList, string documentation)
            {
                paramList = $"this {FullyQualifiedDisplayString.OptionsBuilderOfTOptions} {Identifier.optionsBuilder}, {paramList}";

                EmitBlankLineIfRequired();
                _writer.WriteLine(documentation);
                _writer.WriteBlockStart($"public static {FullyQualifiedDisplayString.OptionsBuilderOfTOptions} {methodName}<{Identifier.TOptions}>({paramList}) where {Identifier.TOptions} : class");
            }
        }
    }
}
