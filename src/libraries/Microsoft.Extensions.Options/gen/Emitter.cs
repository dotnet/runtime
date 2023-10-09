// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;

namespace Microsoft.Extensions.Options.Generators
{
    /// <summary>
    /// Emits option validation.
    /// </summary>
    internal sealed class Emitter : EmitterBase
    {
        private const string StaticFieldHolderClassesNamespace = "__OptionValidationStaticInstances";
        private const string StaticListType = "global::System.Collections.Generic.List";
        private const string StaticValidationResultType = "global::System.ComponentModel.DataAnnotations.ValidationResult";
        private const string StaticValidationAttributeType = "global::System.ComponentModel.DataAnnotations.ValidationAttribute";

        private string _staticValidationAttributeHolderClassName = "__Attributes";
        private string _staticValidatorHolderClassName = "__Validators";
        private string _staticValidationAttributeHolderClassFQN;
        private string _staticValidatorHolderClassFQN;
        private string _modifier;
        private string _TryGetValueNullableAnnotation;

        private sealed record StaticFieldInfo(string FieldTypeFQN, int FieldOrder, string FieldName, IList<string> InstantiationLines);

        public Emitter(Compilation compilation, bool emitPreamble = true) : base(emitPreamble)
        {
            if (((CSharpCompilation)compilation).LanguageVersion >= Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp11)
            {
                _modifier = "file";
            }
            else
            {
                _modifier = "internal";
                string suffix = $"_{GetNonRandomizedHashCode(compilation.SourceModule.Name):X8}";
                _staticValidationAttributeHolderClassName += suffix;
                _staticValidatorHolderClassName += suffix;
            }

            _staticValidationAttributeHolderClassFQN = $"global::{StaticFieldHolderClassesNamespace}.{_staticValidationAttributeHolderClassName}";
            _staticValidatorHolderClassFQN = $"global::{StaticFieldHolderClassesNamespace}.{_staticValidatorHolderClassName}";
            _TryGetValueNullableAnnotation = GetNullableAnnotationStringForTryValidateValueToUseInGeneratedCode(compilation);
        }

        public string Emit(
            IEnumerable<ValidatorType> validatorTypes,
            CancellationToken cancellationToken)
        {
            var staticValidationAttributesDict = new Dictionary<string, StaticFieldInfo>();
            var staticValidatorsDict = new Dictionary<string, StaticFieldInfo>();

            foreach (var vt in validatorTypes.OrderBy(static lt => lt.Namespace + "." + lt.Name))
            {
                cancellationToken.ThrowIfCancellationRequested();
                GenValidatorType(vt, ref staticValidationAttributesDict, ref staticValidatorsDict);
            }

            GenStaticClassWithStaticReadonlyFields(staticValidationAttributesDict.Values, StaticFieldHolderClassesNamespace, _staticValidationAttributeHolderClassName);
            GenStaticClassWithStaticReadonlyFields(staticValidatorsDict.Values, StaticFieldHolderClassesNamespace, _staticValidatorHolderClassName);

            return Capture();
        }

        /// <summary>
        /// Returns the nullable annotation string to use in the code generation according to the first parameter of
        /// <see cref="System.ComponentModel.DataAnnotations.Validator.TryValidateValue(object, ValidationContext, ICollection{ValidationResult}, IEnumerable{ValidationAttribute})"/> is nullable annotated.
        /// </summary>
        /// <param name="compilation">The <see cref="Compilation"/> to consider for analysis.</param>
        /// <returns>"!" if the first parameter is not nullable annotated, otherwise an empty string.</returns>
        /// <remarks>
        /// In .NET 8.0 we have changed the nullable annotation on first parameter of the method cref="System.ComponentModel.DataAnnotations.Validator.TryValidateValue(object, ValidationContext, ICollection{ValidationResult}, IEnumerable{ValidationAttribute})"/>
        /// The source generator need to detect if we need to append "!" to the first parameter of the method call when running on down-level versions.
        /// </remarks>
        private static string GetNullableAnnotationStringForTryValidateValueToUseInGeneratedCode(Compilation compilation)
        {
            INamedTypeSymbol? validatorTypeSymbol = compilation.GetBestTypeByMetadataName("System.ComponentModel.DataAnnotations.Validator");
            if (validatorTypeSymbol is not null)
            {
                ImmutableArray<ISymbol> members = validatorTypeSymbol.GetMembers("TryValidateValue");
                if (members.Length == 1 && members[0] is IMethodSymbol tryValidateValueMethod)
                {
                    return tryValidateValueMethod.Parameters[0].NullableAnnotation == NullableAnnotation.NotAnnotated ? "!" : string.Empty;
                }
            }

            return "!";
        }

        private void GenValidatorType(ValidatorType vt, ref Dictionary<string, StaticFieldInfo> staticValidationAttributesDict, ref Dictionary<string, StaticFieldInfo> staticValidatorsDict)
        {
            if (vt.Namespace.Length > 0)
            {
                OutLn($"namespace {vt.Namespace}");
                OutOpenBrace();
            }

            foreach (var p in vt.ParentTypes)
            {
                OutLn(p);
                OutOpenBrace();
            }

            if (vt.IsSynthetic)
            {
                OutGeneratedCodeAttribute();
                OutLn($"internal sealed partial {vt.DeclarationKeyword} {vt.Name}");
            }
            else
            {
                OutLn($"partial {vt.DeclarationKeyword} {vt.Name}");
            }

            OutOpenBrace();

            for (var i = 0; i < vt.ModelsToValidate.Count; i++)
            {
                var modelToValidate = vt.ModelsToValidate[i];

                GenModelValidationMethod(modelToValidate, vt.IsSynthetic, ref staticValidationAttributesDict, ref staticValidatorsDict);
            }

            OutCloseBrace();

            foreach (var _ in vt.ParentTypes)
            {
                OutCloseBrace();
            }

            if (vt.Namespace.Length > 0)
            {
                OutCloseBrace();
            }
        }

        private void GenStaticClassWithStaticReadonlyFields(IEnumerable<StaticFieldInfo> staticFields, string classNamespace, string className)
        {
            OutLn($"namespace {classNamespace}");
            OutOpenBrace();

            OutGeneratedCodeAttribute();
            OutLn($"{_modifier} static class {className}");
            OutOpenBrace();

            var staticValidationAttributes = staticFields
                .OrderBy(x => x.FieldOrder)
                .ToArray();

            for (var i = 0; i < staticValidationAttributes.Length; i++)
            {
                var attributeInstance = staticValidationAttributes[i];
                OutIndent();
                Out($"internal static readonly {attributeInstance.FieldTypeFQN} {attributeInstance.FieldName} = ");
                for (var j = 0; j < attributeInstance.InstantiationLines.Count; j++)
                {
                    var line = attributeInstance.InstantiationLines[j];
                    Out(line);
                    if (j != attributeInstance.InstantiationLines.Count - 1)
                    {
                        OutLn();
                        OutIndent();
                    }
                    else
                    {
                        Out(';');
                    }
                }

                OutLn();

                if (i != staticValidationAttributes.Length - 1)
                {
                    OutLn();
                }
            }

            OutCloseBrace();

            OutCloseBrace();
        }

        private void GenModelSelfValidationIfNecessary(ValidatedModel modelToValidate)
        {
            if (modelToValidate.SelfValidates)
            {
                OutLn($"(builder ??= new()).AddResults(((global::System.ComponentModel.DataAnnotations.IValidatableObject)options).Validate(context));");
                OutLn();
            }
        }

        private void GenModelValidationMethod(
            ValidatedModel modelToValidate,
            bool makeStatic,
            ref Dictionary<string, StaticFieldInfo> staticValidationAttributesDict,
            ref Dictionary<string, StaticFieldInfo> staticValidatorsDict)
        {
            OutLn($"/// <summary>");
            OutLn($"/// Validates a specific named options instance (or all when <paramref name=\"name\"/> is <see langword=\"null\" />).");
            OutLn($"/// </summary>");
            OutLn($"/// <param name=\"name\">The name of the options instance being validated.</param>");
            OutLn($"/// <param name=\"options\">The options instance.</param>");
            OutLn($"/// <returns>Validation result.</returns>");
            OutGeneratedCodeAttribute();

            OutLn($"public {(makeStatic ? "static " : string.Empty)}global::Microsoft.Extensions.Options.ValidateOptionsResult Validate(string? name, {modelToValidate.Name} options)");
            OutOpenBrace();
            OutLn($"global::Microsoft.Extensions.Options.ValidateOptionsResultBuilder? builder = null;");
            OutLn($"var context = new global::System.ComponentModel.DataAnnotations.ValidationContext(options);");

            int capacity = modelToValidate.MembersToValidate.Max(static vm => vm.ValidationAttributes.Count);
            if (capacity > 0)
            {
                OutLn($"var validationResults = new {StaticListType}<{StaticValidationResultType}>();");
                OutLn($"var validationAttributes = new {StaticListType}<{StaticValidationAttributeType}>({capacity});");
            }
            OutLn();

            bool cleanListsBeforeUse = false;
            foreach (var vm in modelToValidate.MembersToValidate)
            {
                if (vm.ValidationAttributes.Count > 0)
                {
                    GenMemberValidation(vm, modelToValidate.SimpleName, ref staticValidationAttributesDict, cleanListsBeforeUse);
                    cleanListsBeforeUse = true;
                    OutLn();
                }

                if (vm.TransValidatorType is not null)
                {
                    GenTransitiveValidation(vm, modelToValidate.SimpleName, ref staticValidatorsDict);
                    OutLn();
                }

                if (vm.EnumerationValidatorType is not null)
                {
                    GenEnumerationValidation(vm, modelToValidate.SimpleName, ref staticValidatorsDict);
                    OutLn();
                }
            }

            GenModelSelfValidationIfNecessary(modelToValidate);
            OutLn($"return builder is null ? global::Microsoft.Extensions.Options.ValidateOptionsResult.Success : builder.Build();");
            OutCloseBrace();
        }

        private void GenMemberValidation(ValidatedMember vm, string modelName, ref Dictionary<string, StaticFieldInfo> staticValidationAttributesDict, bool cleanListsBeforeUse)
        {
            OutLn($"context.MemberName = \"{vm.Name}\";");
            OutLn($"context.DisplayName = string.IsNullOrEmpty(name) ? \"{modelName}.{vm.Name}\" : $\"{{name}}.{vm.Name}\";");

            if (cleanListsBeforeUse)
            {
                OutLn($"validationResults.Clear();");
                OutLn($"validationAttributes.Clear();");
            }

            foreach (var attr in vm.ValidationAttributes)
            {
                var staticValidationAttributeInstance = GetOrAddStaticValidationAttribute(ref staticValidationAttributesDict, attr);
                OutLn($"validationAttributes.Add({_staticValidationAttributeHolderClassFQN}.{staticValidationAttributeInstance.FieldName});");
            }

            OutLn($"if (!global::System.ComponentModel.DataAnnotations.Validator.TryValidateValue(options.{vm.Name}{_TryGetValueNullableAnnotation}, context, validationResults, validationAttributes))");
            OutOpenBrace();
            OutLn($"(builder ??= new()).AddResults(validationResults);");
            OutCloseBrace();
        }

        private StaticFieldInfo GetOrAddStaticValidationAttribute(ref Dictionary<string, StaticFieldInfo> staticValidationAttributesDict, ValidationAttributeInfo attr)
        {
            var attrInstantiationStatementLines = new List<string>();

            if (attr.ConstructorArguments.Count > 0)
            {
                attrInstantiationStatementLines.Add($"new {attr.AttributeName}(");

                for (var i = 0; i < attr.ConstructorArguments.Count; i++)
                {
                    if (i != attr.ConstructorArguments.Count - 1)
                    {
                        attrInstantiationStatementLines.Add($"{GetPaddingString(1)}{attr.ConstructorArguments[i]},");
                    }
                    else
                    {
                        attrInstantiationStatementLines.Add($"{GetPaddingString(1)}{attr.ConstructorArguments[i]})");
                    }
                }
            }
            else
            {
                attrInstantiationStatementLines.Add($"new {attr.AttributeName}()");
            }

            if (attr.Properties.Count > 0)
            {
                attrInstantiationStatementLines.Add("{");

                var propertiesOrderedByKey = attr.Properties
                    .OrderBy(p => p.Key)
                    .ToArray();

                for (var i = 0; i < propertiesOrderedByKey.Length; i++)
                {
                    var prop = propertiesOrderedByKey[i];
                    var notLast = i != propertiesOrderedByKey.Length - 1;
                    attrInstantiationStatementLines.Add($"{GetPaddingString(1)}{prop.Key} = {prop.Value}{(notLast ? "," : string.Empty)}");
                }

                attrInstantiationStatementLines.Add("}");
            }

            var instantiationStatement = string.Join("\n", attrInstantiationStatementLines);

            if (!staticValidationAttributesDict.TryGetValue(instantiationStatement, out var staticValidationAttributeInstance))
            {
                var fieldNumber = staticValidationAttributesDict.Count + 1;
                staticValidationAttributeInstance = new StaticFieldInfo(
                    FieldTypeFQN: attr.AttributeName,
                    FieldOrder: fieldNumber,
                    FieldName: $"A{fieldNumber}",
                    InstantiationLines: attrInstantiationStatementLines);

                staticValidationAttributesDict.Add(instantiationStatement, staticValidationAttributeInstance);
            }

            return staticValidationAttributeInstance;
        }

        private void GenTransitiveValidation(ValidatedMember vm, string modelName, ref Dictionary<string, StaticFieldInfo> staticValidatorsDict)
        {
            string callSequence;
            if (vm.TransValidateTypeIsSynthetic)
            {
                callSequence = vm.TransValidatorType!;
            }
            else
            {
                var staticValidatorInstance = GetOrAddStaticValidator(ref staticValidatorsDict, vm.TransValidatorType!);

                callSequence = $"{_staticValidatorHolderClassFQN}.{staticValidatorInstance.FieldName}";
            }

            var valueAccess = (vm.IsNullable && vm.IsValueType) ? ".Value" : string.Empty;

            var baseName = $"string.IsNullOrEmpty(name) ? \"{modelName}.{vm.Name}\" : $\"{{name}}.{vm.Name}\"";

            if (vm.IsNullable)
            {
                OutLn($"if (options.{vm.Name} is not null)");
                OutOpenBrace();
                OutLn($"(builder ??= new()).AddResult({callSequence}.Validate({baseName}, options.{vm.Name}{valueAccess}));");
                OutCloseBrace();
            }
            else
            {
                OutLn($"(builder ??= new()).AddResult({callSequence}.Validate({baseName}, options.{vm.Name}{valueAccess}));");
            }
        }

        private void GenEnumerationValidation(ValidatedMember vm, string modelName, ref Dictionary<string, StaticFieldInfo> staticValidatorsDict)
        {
            var valueAccess = (vm.IsValueType && vm.IsNullable) ? ".Value" : string.Empty;
            var enumeratedValueAccess = (vm.EnumeratedIsNullable && vm.EnumeratedIsValueType) ? ".Value" : string.Empty;
            string callSequence;
            if (vm.EnumerationValidatorTypeIsSynthetic)
            {
                callSequence = vm.EnumerationValidatorType!;
            }
            else
            {
                var staticValidatorInstance = GetOrAddStaticValidator(ref staticValidatorsDict, vm.EnumerationValidatorType!);

                callSequence = $"{_staticValidatorHolderClassFQN}.{staticValidatorInstance.FieldName}";
            }

            if (vm.IsNullable)
            {
                OutLn($"if (options.{vm.Name} is not null)");
            }

            OutOpenBrace();

            OutLn($"var count = 0;");
            OutLn($"foreach (var o in options.{vm.Name}{valueAccess})");
            OutOpenBrace();

            if (vm.EnumeratedIsNullable)
            {
                OutLn($"if (o is not null)");
                OutOpenBrace();
                var propertyName = $"string.IsNullOrEmpty(name) ? $\"{modelName}.{vm.Name}[{{count}}]\" : $\"{{name}}.{vm.Name}[{{count}}]\"";
                OutLn($"(builder ??= new()).AddResult({callSequence}.Validate({propertyName}, o{enumeratedValueAccess}));");
                OutCloseBrace();

                if (!vm.EnumeratedMayBeNull)
                {
                    OutLn($"else");
                    OutOpenBrace();
                    var error = $"string.IsNullOrEmpty(name) ? $\"{modelName}.{vm.Name}[{{count}}] is null\" : $\"{{name}}.{vm.Name}[{{count}}] is null\"";
                    OutLn($"(builder ??= new()).AddError({error});");
                    OutCloseBrace();
                }

                OutLn($"count++;");
            }
            else
            {
                var propertyName = $"string.IsNullOrEmpty(name) ? $\"{modelName}.{vm.Name}[{{count++}}] is null\" : $\"{{name}}.{vm.Name}[{{count++}}] is null\"";
                OutLn($"(builder ??= new()).AddResult({callSequence}.Validate({propertyName}, o{enumeratedValueAccess}));");
            }

            OutCloseBrace();
            OutCloseBrace();
        }

    #pragma warning disable CA1822 // Mark members as static: static should come before non-static, but we want the method to be here
        private StaticFieldInfo GetOrAddStaticValidator(ref Dictionary<string, StaticFieldInfo> staticValidatorsDict, string validatorTypeFQN)
    #pragma warning restore CA1822
        {
            if (!staticValidatorsDict.TryGetValue(validatorTypeFQN, out var staticValidatorInstance))
            {
                var fieldNumber = staticValidatorsDict.Count + 1;
                staticValidatorInstance = new StaticFieldInfo(
                    FieldTypeFQN: validatorTypeFQN,
                    FieldOrder: fieldNumber,
                    FieldName: $"V{fieldNumber}",
                    InstantiationLines: new[] { $"new {validatorTypeFQN}()" });

                staticValidatorsDict.Add(validatorTypeFQN, staticValidatorInstance);
            }

            return staticValidatorInstance;
        }

        /// <summary>
        /// Returns a non-randomized hash code for the given string.
        /// We always return a positive value.
        /// </summary>
        internal static int GetNonRandomizedHashCode(string s)
        {
            uint result = 2166136261u;
            foreach (char c in s)
            {
                result = (c ^ result) * 16777619;
            }
            return Math.Abs((int)result);
        }
    }
}
