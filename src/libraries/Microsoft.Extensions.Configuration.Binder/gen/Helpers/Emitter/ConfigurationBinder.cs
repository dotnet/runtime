// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed partial class ConfigurationBindingGenerator
    {
        private sealed partial class Emitter
        {
            private bool ShouldEmitMethods(MethodsToGen_ConfigurationBinder methods) => (_sourceGenSpec.MethodsToGen_ConfigurationBinder & methods) != 0;

            private void EmitBinder_Extensions_IConfiguration()
            {
                Debug.Assert(_sourceGenSpec.TypesForGen_ConfigurationBinder_BindMethods.Count <= 3 &&
                    !_sourceGenSpec.TypesForGen_ConfigurationBinder_BindMethods.Keys.Any(overload => (overload & MethodsToGen_ConfigurationBinder.Bind) is 0));

                if (!ShouldEmitMethods(MethodsToGen_ConfigurationBinder.Any))
                {
                    return;
                }

                _emitBlankLineBeforeNextStatement = false;
                EmitRootBindingClassStartBlock(Identifier.GeneratedConfigurationBinder);

                EmitGetMethods();
                EmitGetValueMethods();
                EmitBindMethods_ConfigurationBinder();

                EmitEndBlock();
                _emitBlankLineBeforeNextStatement = true;
            }

            private void EmitGetMethods()
            {
                const string expressionForGetCore = $"{FullyQualifiedDisplayString.CoreBindingHelper}.{nameof(MethodsToGen_CoreBindingHelper.GetCore)}";
                const string documentation = "Attempts to bind the configuration instance to a new instance of type T.";

                if (ShouldEmitMethods(MethodsToGen_ConfigurationBinder.Get_T))
                {
                    StartMethodDefinition(documentation);
                    _writer.WriteLine($"public static T? {Identifier.Get}<T>(this {FullyQualifiedDisplayString.IConfiguration} {Identifier.configuration}) => " +
                        $"(T?)({expressionForGetCore}({Identifier.configuration}, typeof(T), {Identifier.configureOptions}: null) ?? default(T));");
                }

                if (ShouldEmitMethods(MethodsToGen_ConfigurationBinder.Get_T_BinderOptions))
                {
                    StartMethodDefinition(documentation);
                    _writer.WriteLine($"public static T? {Identifier.Get}<T>(this {FullyQualifiedDisplayString.IConfiguration} {Identifier.configuration}, {FullyQualifiedDisplayString.ActionOfBinderOptions}? {Identifier.configureOptions}) => " +
                        $"(T?)({expressionForGetCore}({Identifier.configuration}, typeof(T), {Identifier.configureOptions}) ?? default(T));");
                }

                if (ShouldEmitMethods(MethodsToGen_ConfigurationBinder.Get_TypeOf))
                {
                    StartMethodDefinition(documentation);
                    _writer.WriteLine($"public static object? {Identifier.Get}(this {FullyQualifiedDisplayString.IConfiguration} {Identifier.configuration}, {FullyQualifiedDisplayString.Type} {Identifier.type}) => " +
                        $"{expressionForGetCore}({Identifier.configuration}, {Identifier.type}, {Identifier.configureOptions}: null);");
                }

                if (ShouldEmitMethods(MethodsToGen_ConfigurationBinder.Get_TypeOf_BinderOptions))
                {
                    StartMethodDefinition(documentation);
                    _writer.WriteLine($"public static object? {Identifier.Get}(this {FullyQualifiedDisplayString.IConfiguration} {Identifier.configuration}, {FullyQualifiedDisplayString.Type} {Identifier.type}, {FullyQualifiedDisplayString.ActionOfBinderOptions}? {Identifier.configureOptions}) => " +
                        $"{expressionForGetCore}({Identifier.configuration}, {Identifier.type}, {Identifier.configureOptions});");
                }
            }

            private void EmitGetValueMethods()
            {
                const string expressionForGetValueCore = $"{FullyQualifiedDisplayString.CoreBindingHelper}.{nameof(MethodsToGen_CoreBindingHelper.GetValueCore)}";
                const string documentation = "Extracts the value with the specified key and converts it to the specified type.";

                if (ShouldEmitMethods(MethodsToGen_ConfigurationBinder.GetValue_T_key))
                {
                    StartMethodDefinition(documentation);
                    _writer.WriteLine($"public static T? {Identifier.GetValue}<T>(this {FullyQualifiedDisplayString.IConfiguration} {Identifier.configuration}, string {Identifier.key}) => " +
                        $"(T?)({expressionForGetValueCore}({Identifier.configuration}, typeof(T), {Identifier.key}) ?? default(T));");
                }

                if (ShouldEmitMethods(MethodsToGen_ConfigurationBinder.GetValue_T_key_defaultValue))
                {
                    StartMethodDefinition(documentation);
                    _writer.WriteLine($"public static T? {Identifier.GetValue}<T>(this {FullyQualifiedDisplayString.IConfiguration} {Identifier.configuration}, string {Identifier.key}, T {Identifier.defaultValue}) => " +
                        $"(T?)({expressionForGetValueCore}({Identifier.configuration}, typeof(T), {Identifier.key}) ?? {Identifier.defaultValue});");
                }

                if (ShouldEmitMethods(MethodsToGen_ConfigurationBinder.GetValue_TypeOf_key))
                {
                    StartMethodDefinition(documentation);
                    _writer.WriteLine($"public static object? {Identifier.GetValue}(this {FullyQualifiedDisplayString.IConfiguration} {Identifier.configuration}, {FullyQualifiedDisplayString.Type} {Identifier.type}, string {Identifier.key}) => " +
                        $"{expressionForGetValueCore}({Identifier.configuration}, {Identifier.type}, {Identifier.key});");
                }

                if (ShouldEmitMethods(MethodsToGen_ConfigurationBinder.GetValue_TypeOf_key_defaultValue))
                {
                    StartMethodDefinition(documentation);
                    _writer.WriteLine($"public static object? {Identifier.GetValue}(this {FullyQualifiedDisplayString.IConfiguration} {Identifier.configuration}, {FullyQualifiedDisplayString.Type} {Identifier.type}, string {Identifier.key}, object? {Identifier.defaultValue}) => " +
                        $"{expressionForGetValueCore}({Identifier.configuration}, {Identifier.type}, {Identifier.key}) ?? {Identifier.defaultValue};");
                }
            }

            private void EmitBindMethods_ConfigurationBinder()
            {
                if (!ShouldEmitMethods(MethodsToGen_ConfigurationBinder.Bind))
                {
                    return;
                }

                Dictionary<MethodsToGen_ConfigurationBinder, HashSet<TypeSpec>> types = _sourceGenSpec.TypesForGen_ConfigurationBinder_BindMethods;

                if (types.TryGetValue(MethodsToGen_ConfigurationBinder.Bind_instance, out HashSet<TypeSpec>? typeSpecs))
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

                if (types.TryGetValue(MethodsToGen_ConfigurationBinder.Bind_instance_BinderOptions, out typeSpecs))
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

                if (types.TryGetValue(MethodsToGen_ConfigurationBinder.Bind_key_instance, out typeSpecs))
                {
                    foreach (TypeSpec type in typeSpecs)
                    {
                        EmitMethodImplementation(
                            type,
                            additionalParams: $"string {Identifier.key}, {GetObjParameter(type)}",
                            configExpression: $"{Expression.configurationGetSection}({Identifier.key})",
                            configureOptions: false);
                    }
                }

                void EmitMethodImplementation(TypeSpec type, string additionalParams, string configExpression, bool configureOptions)
                {
                    string binderOptionsArg = configureOptions ? $"{Expression.GetBinderOptions}({Identifier.configureOptions})" : $"{Identifier.binderOptions}: null";

                    string returnExpression;
                    if (type.CanInitialize)
                    {
                        returnExpression = type.NeedsMemberBinding
                            ? $"{FullyQualifiedDisplayString.CoreBindingHelper}.{nameof(MethodsToGen_CoreBindingHelper.BindCore)}({configExpression}, ref {Identifier.obj}, {binderOptionsArg})"
                            : "{ }";
                    }
                    else
                    {
                        returnExpression = GetInitException(type.InitExceptionMessage);
                    }

                    StartMethodDefinition("Attempts to bind the given object instance to configuration values by matching property names against configuration keys recursively.");
                    _writer.WriteLine($"public static void {Identifier.Bind}(this {FullyQualifiedDisplayString.IConfiguration} {Identifier.configuration}, {additionalParams}) => "
                        + $"{returnExpression};");
                }

                string GetObjParameter(TypeSpec type) => $"{type.FullyQualifiedDisplayString} {Identifier.obj}";
            }

            private void StartMethodDefinition(string documentation)
            {
                EmitBlankLineIfRequired();
                _writer.WriteLine($"/// <summary>{documentation}</summary>");
            }
        }
    }
}
