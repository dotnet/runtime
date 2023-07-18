// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Microsoft.Extensions.Options.Generators
{
    /// <summary>
    /// Emits option validation.
    /// </summary>
    internal sealed class Emitter : EmitterBase
    {
        private const string StaticValidationAttributeHolderClassName = "__Attributes";
        private const string StaticValidatorHolderClassName = "__Validators";
        private const string StaticFieldHolderClassesNamespace = "__OptionValidationStaticInstances";
        private const string StaticValidationAttributeHolderClassFQN = $"global::{StaticFieldHolderClassesNamespace}.{StaticValidationAttributeHolderClassName}";
        private const string StaticValidatorHolderClassFQN = $"global::{StaticFieldHolderClassesNamespace}.{StaticValidatorHolderClassName}";
        private const string StaticListType = "global::System.Collections.Generic.List";
        private const string StaticValidationResultType = "global::System.ComponentModel.DataAnnotations.ValidationResult";
        private const string StaticValidationAttributeType = "global::System.ComponentModel.DataAnnotations.ValidationAttribute";

        private sealed record StaticFieldInfo(string FieldTypeFQN, int FieldOrder, string FieldName, IList<string> InstantiationLines);

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

            GenStaticClassWithStaticReadonlyFields(staticValidationAttributesDict.Values, StaticFieldHolderClassesNamespace, StaticValidationAttributeHolderClassName);
            GenStaticClassWithStaticReadonlyFields(staticValidatorsDict.Values, StaticFieldHolderClassesNamespace, StaticValidatorHolderClassName);

            return Capture();
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
            OutLn($"internal static class {className}");
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
                OutLn($"builder.AddResults(((global::System.ComponentModel.DataAnnotations.IValidatableObject)options).Validate(context));");
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
            OutLn($"var baseName = (string.IsNullOrEmpty(name) ? \"{modelToValidate.SimpleName}\" : name) + \".\";");
            OutLn($"var builder = new global::Microsoft.Extensions.Options.ValidateOptionsResultBuilder();");
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
                    GenMemberValidation(vm, ref staticValidationAttributesDict, cleanListsBeforeUse);
                    cleanListsBeforeUse = true;
                    OutLn();
                }

                if (vm.TransValidatorType is not null)
                {
                    GenTransitiveValidation(vm, ref staticValidatorsDict);
                    OutLn();
                }

                if (vm.EnumerationValidatorType is not null)
                {
                    GenEnumerationValidation(vm, ref staticValidatorsDict);
                    OutLn();
                }
            }

            GenModelSelfValidationIfNecessary(modelToValidate);
            OutLn($"return builder.Build();");
            OutCloseBrace();
        }

        private void GenMemberValidation(ValidatedMember vm, ref Dictionary<string, StaticFieldInfo> staticValidationAttributesDict, bool cleanListsBeforeUse)
        {
            OutLn($"context.MemberName = \"{vm.Name}\";");
            OutLn($"context.DisplayName = baseName + \"{vm.Name}\";");

            if (cleanListsBeforeUse)
            {
                OutLn($"validationResults.Clear();");
                OutLn($"validationAttributes.Clear();");
            }

            foreach (var attr in vm.ValidationAttributes)
            {
                var staticValidationAttributeInstance = GetOrAddStaticValidationAttribute(ref staticValidationAttributesDict, attr);
                OutLn($"validationAttributes.Add({StaticValidationAttributeHolderClassFQN}.{staticValidationAttributeInstance.FieldName});");
            }

            OutLn($"if (!global::System.ComponentModel.DataAnnotations.Validator.TryValidateValue(options.{vm.Name}!, context, validationResults, validationAttributes))");
            OutOpenBrace();
            OutLn($"builder.AddResults(validationResults);");
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

            var instantiationStatement = string.Join(Environment.NewLine, attrInstantiationStatementLines);

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

        private void GenTransitiveValidation(ValidatedMember vm, ref Dictionary<string, StaticFieldInfo> staticValidatorsDict)
        {
            string callSequence;
            if (vm.TransValidateTypeIsSynthetic)
            {
                callSequence = vm.TransValidatorType!;
            }
            else
            {
                var staticValidatorInstance = GetOrAddStaticValidator(ref staticValidatorsDict, vm.TransValidatorType!);

                callSequence = $"{StaticValidatorHolderClassFQN}.{staticValidatorInstance.FieldName}";
            }

            var valueAccess = (vm.IsNullable && vm.IsValueType) ? ".Value" : string.Empty;

            if (vm.IsNullable)
            {
                OutLn($"if (options.{vm.Name} is not null)");
                OutOpenBrace();
                OutLn($"builder.AddResult({callSequence}.Validate(baseName + \"{vm.Name}\", options.{vm.Name}{valueAccess}));");
                OutCloseBrace();
            }
            else
            {
                OutLn($"builder.AddResult({callSequence}.Validate(baseName + \"{vm.Name}\", options.{vm.Name}{valueAccess}));");
            }
        }

        private void GenEnumerationValidation(ValidatedMember vm, ref Dictionary<string, StaticFieldInfo> staticValidatorsDict)
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

                callSequence = $"{StaticValidatorHolderClassFQN}.{staticValidatorInstance.FieldName}";
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
                OutLn($"builder.AddResult({callSequence}.Validate(baseName + $\"{vm.Name}[{{count}}]\", o{enumeratedValueAccess}));");
                OutCloseBrace();

                if (!vm.EnumeratedMayBeNull)
                {
                    OutLn($"else");
                    OutOpenBrace();
                    OutLn($"builder.AddError(baseName + $\"{vm.Name}[{{count}}] is null\");");
                    OutCloseBrace();
                }

                OutLn($"count++;");
            }
            else
            {
                OutLn($"builder.AddResult({callSequence}.Validate(baseName + $\"{vm.Name}[{{count++}}]\", o{enumeratedValueAccess}));");
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
    }
}
