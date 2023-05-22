// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using static Microsoft.Extensions.Configuration.Binder.SourceGeneration.ConfigurationBindingGenerator;
using static Microsoft.Extensions.Configuration.Binder.SourceGeneration.Emitter;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal sealed partial record ServiceCollectionMethodSpec : MethodSpec
    {
        public bool Any() => ShouldEmitMethods(MethodSpecifier.Configure);

        private bool ShouldEmitMethods(MethodSpecifier methods) => (_methodsToGen & methods) != 0;

        public override void Emit(Emitter emitter)
        {
            if (!Any())
            {
                return;
            }

            emitter.EmitBlankLineIfRequired();

            emitter.Writer.WriteLine("/// <summary>Generated helper providing an AOT and linking compatible implementation for configuration binding.</summary>");
            emitter.Writer.WriteBlockStart($"internal static class {Identifier.GeneratedServiceCollectionBinder}");
            emitter.PrecedingBlockExists = false;

            const string defaultNameExpr = "string.Empty";
            const string configureMethodString = $"global::{Identifier.GeneratedServiceCollectionBinder}.{Identifier.Configure}";
            string configParam = $"{FullyQualifiedDisplayString.IConfiguration} {Identifier.configuration}";

            if (ShouldEmitMethods(MethodSpecifier.Configure_T))
            {
                EmitBlockStart(emitter, configParam);
                emitter.Writer.WriteLine($"return {configureMethodString}<{Identifier.TOptions}>({Identifier.services}, {defaultNameExpr}, {Identifier.configuration}, {Identifier.configureOptions}: null);");
                emitter.Writer.WriteBlockEnd();
            }

            if (ShouldEmitMethods(MethodSpecifier.Configure_T_name))
            {
                EmitBlockStart(
                    emitter,
                    paramList: $"string? {Identifier.name}, " + configParam);
                emitter.Writer.WriteLine($"return {configureMethodString}<{Identifier.TOptions}>({Identifier.services}, {Identifier.name}, {Identifier.configuration}, {Identifier.configureOptions}: null);");
                emitter.Writer.WriteBlockEnd();
            }

            if (ShouldEmitMethods(MethodSpecifier.Configure_T_BinderOptions))
            {
                EmitBlockStart(
                    emitter,
                    paramList: configParam + $", {FullyQualifiedDisplayString.ActionOfBinderOptions}? {Identifier.configureOptions}");
                emitter.Writer.WriteLine($"return {configureMethodString}<{Identifier.TOptions}>({Identifier.services}, {defaultNameExpr}, {Identifier.configuration}, {Identifier.configureOptions});");
                emitter.Writer.WriteBlockEnd();
            }

            string optionsNamespaceName = "global::Microsoft.Extensions.Options";
            string bindCoreUntypedDisplayString = emitter.GetHelperMethodDisplayString(Identifier.BindCoreUntyped);

            EmitBlockStart(
                emitter,
                paramList: $"string? {Identifier.name}, " + configParam + $", {FullyQualifiedDisplayString.ActionOfBinderOptions}? {Identifier.configureOptions}");

            emitter.EmitCheckForNullArgument_WithBlankLine(Identifier.services);
            emitter.EmitCheckForNullArgument_WithBlankLine(Identifier.configuration);

            emitter.Writer.WriteBlock($$"""
                    global::Microsoft.Extensions.DependencyInjection.OptionsServiceCollectionExtensions.AddOptions({{Identifier.services}});
                    {{FullyQualifiedDisplayString.AddSingleton}}<{{FullyQualifiedDisplayString.IOptionsChangeTokenSource}}<{{Identifier.TOptions}}>>({{Identifier.services}}, new {{FullyQualifiedDisplayString.ConfigurationChangeTokenSource}}<{{Identifier.TOptions}}>({{Identifier.name}}, {{Identifier.configuration}}));
                    return {{FullyQualifiedDisplayString.AddSingleton}}<{{optionsNamespaceName}}.IConfigureOptions<{{Identifier.TOptions}}>>({{Identifier.services}}, new {{optionsNamespaceName}}.ConfigureNamedOptions<{{Identifier.TOptions}}>({{Identifier.name}}, {{Identifier.obj}} => {{bindCoreUntypedDisplayString}}({{Identifier.configuration}}, {{Identifier.obj}}, typeof({{Identifier.TOptions}}), {{Identifier.configureOptions}})));
                }
                """);

            emitter.Writer.WriteBlockEnd();
            emitter.PrecedingBlockExists = true;
        }

        private static void EmitBlockStart(Emitter emitter, string paramList)
        {
            paramList = $"this {FullyQualifiedDisplayString.IServiceCollection} {Identifier.services}, {paramList}";

            emitter.EmitBlankLineIfRequired();
            emitter.Writer.WriteBlock($$"""
                /// <summary>Registers a configuration instance which TOptions will bind against.</summary>
                public static {{FullyQualifiedDisplayString.IServiceCollection}} {{Identifier.Configure}}<{{Identifier.TOptions}}>({{paramList}}) where {{Identifier.TOptions}} : class
                {
                """);
        }
    }
}
