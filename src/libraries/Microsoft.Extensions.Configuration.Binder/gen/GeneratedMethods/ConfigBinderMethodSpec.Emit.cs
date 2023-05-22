// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using static Microsoft.Extensions.Configuration.Binder.SourceGeneration.Emitter;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal sealed partial record ConfigBinderMethodSpec : MethodSpec
    {
        public bool Any() =>
            ShouldEmitMethods(MethodSpecifier.Get | MethodSpecifier.Bind | MethodSpecifier.GetValue);

        public bool ShouldEmitMethods(MethodSpecifier methods) => (_methodsToGen & methods) != 0;

        public override void Emit(Emitter emitter)
        {
            Debug.Assert(_typesForBindMethodGen.Count <= 3 &&
                !_typesForBindMethodGen.Keys.Any(overload => (overload & MethodSpecifier.Bind) is 0));

            if (!Any())
            {
                return;
            }

            emitter.Writer.WriteLine("/// <summary>Generated helper providing an AOT and linking compatible implementation for configuration binding.</summary>");
            emitter.Writer.WriteBlockStart($"internal static class {Identifier.GeneratedConfigurationBinder}");

            EmitGetMethods(emitter);
            EmitGetValueMethods(emitter);
            EmitBindMethods(emitter);

            emitter.Writer.WriteBlockEnd();

            emitter.PrecedingBlockExists = true;
        }

        private void EmitGetMethods(Emitter emitter)
        {
            const string expressionForGetCore = $"{FullyQualifiedDisplayString.CoreBindingHelper}.{Identifier.GetCore}";
            const string documentation = "Attempts to bind the configuration instance to a new instance of type T.";

            if (ShouldEmitMethods(MethodSpecifier.Get_T))
            {
                StartMethodDefinition(emitter, documentation);
                emitter.Writer.WriteLine($"public static T? {Identifier.Get}<T>(this {FullyQualifiedDisplayString.IConfiguration} {Identifier.configuration}) => " +
                    $"(T?)({expressionForGetCore}({Identifier.configuration}, typeof(T), {Identifier.configureOptions}: null) ?? default(T));");
            }

            if (ShouldEmitMethods(MethodSpecifier.Get_T_BinderOptions))
            {
                StartMethodDefinition(emitter, documentation);
                emitter.Writer.WriteLine($"public static T? {Identifier.Get}<T>(this {FullyQualifiedDisplayString.IConfiguration} {Identifier.configuration}, {FullyQualifiedDisplayString.ActionOfBinderOptions}? {Identifier.configureOptions}) => " +
                    $"(T?)({expressionForGetCore}({Identifier.configuration}, typeof(T), {Identifier.configureOptions}) ?? default(T));");
            }

            if (ShouldEmitMethods(MethodSpecifier.Get_TypeOf))
            {
                StartMethodDefinition(emitter, documentation);
                emitter.Writer.WriteLine($"public static object? {Identifier.Get}(this {FullyQualifiedDisplayString.IConfiguration} {Identifier.configuration}, {FullyQualifiedDisplayString.Type} {Identifier.type}) => " +
                    $"{expressionForGetCore}({Identifier.configuration}, {Identifier.type}, {Identifier.configureOptions}: null);");
            }

            if (ShouldEmitMethods(MethodSpecifier.Get_TypeOf_BinderOptions))
            {
                StartMethodDefinition(emitter, documentation);
                emitter.Writer.WriteLine($"public static object? {Identifier.Get}(this {FullyQualifiedDisplayString.IConfiguration} {Identifier.configuration}, {FullyQualifiedDisplayString.Type} {Identifier.type}, {FullyQualifiedDisplayString.ActionOfBinderOptions}? {Identifier.configureOptions}) => " +
                    $"{expressionForGetCore}({Identifier.configuration}, {Identifier.type}, {Identifier.configureOptions});");
            }
        }

        private void EmitGetValueMethods(Emitter emitter)
        {
            const string expressionForGetValueCore = $"{FullyQualifiedDisplayString.CoreBindingHelper}.{Identifier.GetValueCore}";
            const string documentation = "Extracts the value with the specified key and converts it to the specified type.";

            if (ShouldEmitMethods(MethodSpecifier.GetValue_T_key))
            {
                StartMethodDefinition(emitter, documentation);
                emitter.Writer.WriteLine($"public static T? {Identifier.GetValue}<T>(this {FullyQualifiedDisplayString.IConfiguration} {Identifier.configuration}, string {Identifier.key}) => " +
                    $"(T?)({expressionForGetValueCore}({Identifier.configuration}, typeof(T), {Identifier.key}) ?? default(T));");
            }

            if (ShouldEmitMethods(MethodSpecifier.GetValue_T_key_defaultValue))
            {
                StartMethodDefinition(emitter, documentation);
                emitter.Writer.WriteLine($"public static T? {Identifier.GetValue}<T>(this {FullyQualifiedDisplayString.IConfiguration} {Identifier.configuration}, string {Identifier.key}, T {Identifier.defaultValue}) => " +
                    $"(T?)({expressionForGetValueCore}({Identifier.configuration}, typeof(T), {Identifier.key}) ?? {Identifier.defaultValue});");
            }

            if (ShouldEmitMethods(MethodSpecifier.GetValue_TypeOf_key))
            {
                StartMethodDefinition(emitter, documentation);
                emitter.Writer.WriteLine($"public static object? {Identifier.GetValue}(this {FullyQualifiedDisplayString.IConfiguration} {Identifier.configuration}, {FullyQualifiedDisplayString.Type} {Identifier.type}, string {Identifier.key}) => " +
                    $"{expressionForGetValueCore}({Identifier.configuration}, {Identifier.type}, {Identifier.key});");
            }

            if (ShouldEmitMethods(MethodSpecifier.GetValue_TypeOf_key_defaultValue))
            {
                StartMethodDefinition(emitter, documentation);
                emitter.Writer.WriteLine($"public static object? {Identifier.GetValue}(this {FullyQualifiedDisplayString.IConfiguration} {Identifier.configuration}, {FullyQualifiedDisplayString.Type} {Identifier.type}, string {Identifier.key}, object? {Identifier.defaultValue}) => " +
                    $"{expressionForGetValueCore}({Identifier.configuration}, {Identifier.type}, {Identifier.key}) ?? {Identifier.defaultValue};");
            }
        }

        private void EmitBindMethods(Emitter emitter)
        {
            if (!ShouldEmitMethods(MethodSpecifier.Bind))
            {
                return;
            }

            if (_typesForBindMethodGen.TryGetValue(MethodSpecifier.Bind_instance, out HashSet<TypeSpec>? typeSpecs))
            {
                foreach (TypeSpec type in typeSpecs)
                {
                    EmitMethodImplementation(
                        type,
                        additionalParams: GetObjParameter(type),
                        configExpression: Identifier.configuration,
                        configureOptions: false);
                }
            }

            if (_typesForBindMethodGen.TryGetValue(MethodSpecifier.Bind_instance_BinderOptions, out typeSpecs))
            {
                foreach (TypeSpec type in typeSpecs)
                {
                    EmitMethodImplementation(
                        type,
                        additionalParams: $"{GetObjParameter(type)}, {FullyQualifiedDisplayString.ActionOfBinderOptions}? {Identifier.configureOptions}",
                        configExpression: Identifier.configuration,
                        configureOptions: true);
                }
            }

            if (_typesForBindMethodGen.TryGetValue(MethodSpecifier.Bind_key_instance, out typeSpecs))
            {
                foreach (TypeSpec type in typeSpecs)
                {
                    EmitMethodImplementation(
                        type,
                        additionalParams: $"string {Identifier.key}, {GetObjParameter(type)}",
                        configExpression: $"{Identifier.configuration}.{Identifier.GetSection}({Identifier.key})",
                        configureOptions: false);
                }
            }

            void EmitMethodImplementation(TypeSpec type, string additionalParams, string configExpression, bool configureOptions)
            {
                string binderOptionsArg = configureOptions ? $"{Expression.GetBinderOptions}({Identifier.configureOptions})" : $"{Identifier.binderOptions}: null";
                string returnExpression = type.CanInitialize
                    ? $"{FullyQualifiedDisplayString.CoreBindingHelper}.{Identifier.BindCore}({configExpression}, ref {Identifier.obj}, {binderOptionsArg})"
                    : emitter.GetInitException(type.InitExceptionMessage);

                StartMethodDefinition(emitter, "Attempts to bind the given object instance to configuration values by matching property names against configuration keys recursively.");
                emitter.Writer.WriteLine($"public static void {Identifier.Bind}(this {FullyQualifiedDisplayString.IConfiguration} {Identifier.configuration}, {additionalParams}) => "
                    + $"{returnExpression};");
            }

            string GetObjParameter(TypeSpec type) => $"{type.FullyQualifiedDisplayString} {Identifier.obj}";
        }

        private static void StartMethodDefinition(Emitter emitter, string documentation)
        {
            emitter.EmitBlankLineIfRequired();
            emitter.Writer.WriteLine($"/// <summary>{documentation}</summary>");
        }
    }
}
