// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using static Microsoft.Extensions.Configuration.Binder.SourceGeneration.Emitter;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal sealed partial record OptionsBuilderMethodSpec : MethodSpec
    {
        public override void Emit(Emitter emitter)
        {
            if (!Any())
            {
                return;
            }

            emitter.EmitBlankLineIfRequired();
            emitter.Writer.WriteLine("/// <summary>Generated helper providing an AOT and linking compatible implementation for configuration binding.</summary>");
            emitter.Writer.WriteBlockStart($"internal static class {Identifier.GeneratedOptionsBuilderBinder}");
            emitter.PrecedingBlockExists = false;

            EmitBindMethods(emitter);
            EmitBindConfigurationMethod(emitter);

            emitter.Writer.WriteBlockEnd();
        }

        private void EmitBindMethods(Emitter emitter)
        {
            if (!ShouldEmitMethods(MethodSpecifier.Bind))
            {
                return;
            }

            const string documentation = @"/// <summary>Registers a configuration instance which <typeparamref name=""TOptions""/> will bind against.</summary>";
            const string paramList = $"{FullyQualifiedDisplayString.IConfiguration} {Identifier.configuration}";

            if (ShouldEmitMethods(MethodSpecifier.Bind_T))
            {
                EmitMethodBlockStart(emitter, "Bind", paramList, documentation);
                emitter.Writer.WriteLine($"return global::{Identifier.GeneratedOptionsBuilderBinder}.Bind({Identifier.optionsBuilder}, {Identifier.configuration}, {Identifier.configureOptions}: null);");
                emitter.Writer.WriteBlockEnd();
                emitter.Writer.WriteBlankLine();
            }

            EmitMethodBlockStart(
                emitter,
                "Bind",
                paramList + $", {FullyQualifiedDisplayString.ActionOfBinderOptions}? {Identifier.configureOptions}",
                documentation);

            emitter.EmitCheckForNullArgument_WithBlankLine(Identifier.optionsBuilder);

            emitter.Writer.WriteBlock($$"""
                    global::{{Identifier.GeneratedServiceCollectionBinder}}.{{Identifier.Configure}}<{{Identifier.TOptions}}>({{Identifier.optionsBuilder}}.{{Identifier.Services}}, {{Identifier.optionsBuilder}}.Name, {{Identifier.configuration}}, {{Identifier.configureOptions}});
                    return {{Identifier.optionsBuilder}};
                    """);

            emitter.Writer.WriteBlockEnd();
        }

        private void EmitBindConfigurationMethod(Emitter emitter)
        {
            if (!ShouldEmitMethods(MethodSpecifier.BindConfiguration_T_path_BinderOptions))
            {
                return;
            }

            const string documentation = $@"/// <summary>Registers the dependency injection container to bind <typeparamref name=""TOptions""/> against the <see cref=""{FullyQualifiedDisplayString.IConfiguration}""/> obtained from the DI service provider.</summary>";
            string paramList = $"string {Identifier.configSectionPath}, {FullyQualifiedDisplayString.ActionOfBinderOptions}? {Identifier.configureOptions} = null";

            EmitMethodBlockStart(emitter, "BindConfiguration", paramList, documentation);

            emitter.EmitCheckForNullArgument_WithBlankLine(Identifier.optionsBuilder);
            emitter.EmitCheckForNullArgument_WithBlankLine(Identifier.configSectionPath);

            emitter.Writer.WriteBlockStart($"{Identifier.optionsBuilder}.{Identifier.Configure}<{FullyQualifiedDisplayString.IConfiguration}>(({Identifier.obj}, {Identifier.configuration}) =>");

            emitter.Writer.WriteBlock($$"""
                {{FullyQualifiedDisplayString.IConfiguration}} {{Identifier.section}} = string.Equals(string.Empty, {{Identifier.configSectionPath}}, global::System.StringComparison.OrdinalIgnoreCase) ? {{Identifier.configuration}} : {{Identifier.configuration}}.{{Identifier.GetSection}}({{Identifier.configSectionPath}});
                {{FullyQualifiedDisplayString.CoreBindingHelper}}.{{Identifier.BindCoreUntyped}}({{Identifier.section}}, {{Identifier.obj}}, typeof({{Identifier.TOptions}}), {{Identifier.configureOptions}});
            """);

            emitter.Writer.WriteBlockEnd(");");

            emitter.Writer.WriteBlankLine();

            emitter.Writer.WriteBlock($$"""
                    {{FullyQualifiedDisplayString.AddSingleton}}<{{FullyQualifiedDisplayString.IOptionsChangeTokenSource}}<{{Identifier.TOptions}}>, {{FullyQualifiedDisplayString.ConfigurationChangeTokenSource}}<{{Identifier.TOptions}}>>({{Identifier.optionsBuilder}}.{{Identifier.Services}});
                    return {{Identifier.optionsBuilder}};
                    """);

            emitter.Writer.WriteBlockEnd();
        }

        private bool ShouldEmitMethods(MethodSpecifier methods) => (_methodsToGen & methods) != 0;

        private static void EmitMethodBlockStart(Emitter emitter, string methodName, string paramList, string documentation)
        {
            paramList = $"this {FullyQualifiedDisplayString.OptionsBuilderOfTOptions} {Identifier.optionsBuilder}, {paramList}";

            emitter.EmitBlankLineIfRequired();
            emitter.Writer.WriteLine(documentation);
            emitter.Writer.WriteBlockStart($"public static {FullyQualifiedDisplayString.OptionsBuilderOfTOptions} {methodName}<{Identifier.TOptions}>({paramList}) where {Identifier.TOptions} : class");
        }
    }
}
