// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
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
        internal const string StaticGeneratedValidationAttributesClassesNamespace = "__OptionValidationGeneratedAttributes";
        internal const string StaticAttributeClassNamePrefix = "__SourceGen_";
        internal const string StaticGeneratedMaxLengthAttributeClassesName = "__SourceGen_MaxLengthAttribute";
        private const string StaticListType = "global::System.Collections.Generic.List";
        private const string StaticValidationResultType = "global::System.ComponentModel.DataAnnotations.ValidationResult";
        private const string StaticValidationAttributeType = "global::System.ComponentModel.DataAnnotations.ValidationAttribute";
        private const string StaticValidationContextType = "global::System.ComponentModel.DataAnnotations.ValidationContext";
        private string _staticValidationAttributeHolderClassName = "__Attributes";
        private string _staticValidatorHolderClassName = "__Validators";
        private string _staticValidationAttributeHolderClassFQN;
        private string _staticValidatorHolderClassFQN;
        private string _TryGetValueNullableAnnotation;
        private readonly SymbolHolder _symbolHolder;
        private readonly OptionsSourceGenContext _optionsSourceGenContext;


        private sealed record StaticFieldInfo(string FieldTypeFQN, int FieldOrder, string FieldName, IList<string> InstantiationLines);

        public Emitter(Compilation compilation, SymbolHolder symbolHolder, OptionsSourceGenContext optionsSourceGenContext, bool emitPreamble = true) : base(emitPreamble)
        {
            _optionsSourceGenContext = optionsSourceGenContext;

            if (!_optionsSourceGenContext.IsLangVersion11AndAbove)
            {
                _staticValidationAttributeHolderClassName += _optionsSourceGenContext.Suffix;
                _staticValidatorHolderClassName += _optionsSourceGenContext.Suffix;
            }

            _staticValidationAttributeHolderClassFQN = $"global::{StaticFieldHolderClassesNamespace}.{_staticValidationAttributeHolderClassName}";
            _staticValidatorHolderClassFQN = $"global::{StaticFieldHolderClassesNamespace}.{_staticValidatorHolderClassName}";
            _TryGetValueNullableAnnotation = GetNullableAnnotationStringForTryValidateValueToUseInGeneratedCode(compilation);

            _symbolHolder = symbolHolder;
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
            GenValidationAttributesClasses();

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

                if (modelToValidate.GenerateValidateMethod)
                {
                    GenModelValidationMethod(modelToValidate, vt.IsSynthetic, ref staticValidationAttributesDict, ref staticValidatorsDict);
                }

                if (modelToValidate.GenerateAsyncValidateMethod && _symbolHolder.AsyncValidateOptionsSymbol is not null)
                {
                    if (modelToValidate.GenerateValidateMethod)
                    {
                        OutLn();
                    }

                    GenAsyncModelValidationMethod(modelToValidate, vt.IsSynthetic, ref staticValidationAttributesDict, ref staticValidatorsDict);
                }
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
            OutLn($"{_optionsSourceGenContext.ClassModifier} static class {className}");
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

        public void EmitMaxLengthAttribute(string modifier, string prefix, string className, string linesToInsert, string suffix)
        {
            OutGeneratedCodeAttribute();

            string qualifiedClassName = $"{prefix}{suffix}_{className}";
            string formatMessageOverride = GenerateFormatMessageOverride("Length");

            OutLn($$"""
[global::System.AttributeUsage(global::System.AttributeTargets.Property | global::System.AttributeTargets.Field | global::System.AttributeTargets.Parameter, AllowMultiple = false)]
    {{modifier}} class {{qualifiedClassName}} : {{StaticValidationAttributeType}}
    {
        private const int MaxAllowableLength = -1;
        private static string DefaultErrorMessageString => "The field {0} must be a string or array type with a maximum length of '{1}'.";
        public {{qualifiedClassName}}(int length) : base(() => DefaultErrorMessageString) { Length = length; }
        public {{qualifiedClassName}}(): base(() => DefaultErrorMessageString) { Length = MaxAllowableLength; }
        public int Length { get; }
        public override string FormatErrorMessage(string name) => string.Format(global::System.Globalization.CultureInfo.CurrentCulture, ErrorMessageString, name, Length);
{{formatMessageOverride}}        public override bool IsValid(object? value)
        {
            if (Length == 0 || Length < -1)
            {
                throw new global::System.InvalidOperationException("MaxLengthAttribute must have a Length value that is greater than zero. Use MaxLength() without parameters to indicate that the string or array can have the maximum allowable length.");
            }
            if (value == null || MaxAllowableLength == Length)
            {
                return true;
            }

            int length;
            if (value is string stringValue)
            {
                length = stringValue.Length;
            }
            else if (value is global::System.Collections.ICollection collectionValue)
            {
                length = collectionValue.Count;
            }
            {{linesToInsert}}else
            {
                throw new global::System.InvalidCastException($"The field of type {value.GetType()} must be a string, array, or ICollection type.");
            }

            return length <= Length;
        }
    }
""");
        }

        public void EmitMinLengthAttribute(string modifier, string prefix, string className, string linesToInsert, string suffix)
        {
            OutGeneratedCodeAttribute();

            string qualifiedClassName = $"{prefix}{suffix}_{className}";
            string formatMessageOverride = GenerateFormatMessageOverride("Length");

            OutLn($$"""
[global::System.AttributeUsage(global::System.AttributeTargets.Property | global::System.AttributeTargets.Field | global::System.AttributeTargets.Parameter, AllowMultiple = false)]
    {{modifier}} class {{qualifiedClassName}} : {{StaticValidationAttributeType}}
    {
        private static string DefaultErrorMessageString => "The field {0} must be a string or array type with a minimum length of '{1}'.";

        public {{qualifiedClassName}}(int length) : base(() => DefaultErrorMessageString) { Length = length; }
        public int Length { get; }
        public override bool IsValid(object? value)
        {
            if (Length < -1)
            {
                throw new global::System.InvalidOperationException("MinLengthAttribute must have a Length value that is zero or greater.");
            }
            if (value == null)
            {
                return true;
            }

            int length;
            if (value is string stringValue)
            {
                length = stringValue.Length;
            }
            else if (value is global::System.Collections.ICollection collectionValue)
            {
                length = collectionValue.Count;
            }
            {{linesToInsert}}else
            {
                throw new global::System.InvalidCastException($"The field of type {value.GetType()} must be a string, array, or ICollection type.");
            }

            return length >= Length;
        }
        public override string FormatErrorMessage(string name) => string.Format(global::System.Globalization.CultureInfo.CurrentCulture, ErrorMessageString, name, Length);
{{formatMessageOverride}}    }
""");
        }

        public void EmitLengthAttribute(string modifier, string prefix, string className, string linesToInsert, string suffix)
        {
            OutGeneratedCodeAttribute();

            string qualifiedClassName = $"{prefix}{suffix}_{className}";
            string formatMessageOverride = GenerateFormatMessageOverride("MinimumLength, MaximumLength");

            OutLn($$"""
[global::System.AttributeUsage(global::System.AttributeTargets.Property | global::System.AttributeTargets.Field | global::System.AttributeTargets.Parameter, AllowMultiple = false)]
    {{modifier}} class {{qualifiedClassName}} : {{StaticValidationAttributeType}}
    {
        private static string DefaultErrorMessageString => "The field {0} must be a string or collection type with a minimum length of '{1}' and maximum length of '{2}'.";
        public {{qualifiedClassName}}(int minimumLength, int maximumLength) : base(() => DefaultErrorMessageString) { MinimumLength = minimumLength; MaximumLength = maximumLength; }
        public int MinimumLength { get; }
        public int MaximumLength { get; }
        public override bool IsValid(object? value)
        {
            if (MinimumLength < 0)
            {
                throw new global::System.InvalidOperationException("LengthAttribute must have a MinimumLength value that is zero or greater.");
            }
            if (MaximumLength < MinimumLength)
            {
                throw new global::System.InvalidOperationException("LengthAttribute must have a MaximumLength value that is greater than or equal to MinimumLength.");
            }
            if (value == null)
            {
                return true;
            }

            int length;
            if (value is string stringValue)
            {
                length = stringValue.Length;
            }
            else if (value is global::System.Collections.ICollection collectionValue)
            {
                length = collectionValue.Count;
            }
            {{linesToInsert}}else
            {
                throw new global::System.InvalidCastException($"The field of type {value.GetType()} must be a string, array, or ICollection type.");
            }

            return (uint)(length - MinimumLength) <= (uint)(MaximumLength - MinimumLength);
        }
        public override string FormatErrorMessage(string name) => string.Format(global::System.Globalization.CultureInfo.CurrentCulture, ErrorMessageString, name, MinimumLength, MaximumLength);
{{formatMessageOverride}}    }
""");
        }

        public void EmitCompareAttribute(string modifier, string prefix, string className, string linesToInsert, string suffix)
        {
            OutGeneratedCodeAttribute();

            string qualifiedClassName = $"{prefix}{suffix}_{className}";
            string formatMessageOverride = GenerateFormatMessageOverride("OtherProperty");

            OutLn($$"""
[global::System.AttributeUsage(global::System.AttributeTargets.Property, AllowMultiple = false)]
    {{modifier}} class {{qualifiedClassName}} : {{StaticValidationAttributeType}}
    {
        private static string DefaultErrorMessageString => "'{0}' and '{1}' do not match.";
        public {{qualifiedClassName}}(string otherProperty) : base(() => DefaultErrorMessageString)
        {
            if (otherProperty == null)
            {
                throw new global::System.ArgumentNullException(nameof(otherProperty));
            }
            OtherProperty = otherProperty;
        }
        public string OtherProperty { get; }
        public override bool RequiresValidationContext => true;

        protected override {{StaticValidationResultType}}? IsValid(object? value, {{StaticValidationContextType}} validationContext)
        {
            bool result = true;

            {{linesToInsert}}
            if (!result)
            {
                string[]? memberNames = validationContext.MemberName is null ? null : new string[] { validationContext.MemberName };
                return new {{StaticValidationResultType}}(FormatErrorMessage(validationContext.DisplayName), memberNames);
            }

            return null;
        }
        public override string FormatErrorMessage(string name) => string.Format(global::System.Globalization.CultureInfo.CurrentCulture, ErrorMessageString, name, OtherProperty);
{{formatMessageOverride}}    }
""");
        }

        public void EmitRangeAttribute(string modifier, string prefix, string className, string suffix, bool emitTimeSpanSupport)
        {
            OutGeneratedCodeAttribute();

            string qualifiedClassName = $"{prefix}{suffix}_{className}";
            string formatMessageOverride = GenerateFormatMessageOverride("Minimum, Maximum", ensureInitialized: true);

            string initializationString = emitTimeSpanSupport ?
            """
                                        if (OperandType == typeof(global::System.TimeSpan))
                                        {
                                            if (!global::System.TimeSpan.TryParse((string)Minimum, culture, out global::System.TimeSpan timeSpanMinimum) ||
                                                !global::System.TimeSpan.TryParse((string)Maximum, culture, out global::System.TimeSpan timeSpanMaximum))
                                            {
                                                throw new global::System.InvalidOperationException(MinMaxError);
                                            }
                                            Minimum = timeSpanMinimum;
                                            Maximum = timeSpanMaximum;
                                        }
                                        else
                                        {
                                            Minimum = ConvertValue(Minimum, culture) ?? throw new global::System.InvalidOperationException(MinMaxError);
                                            Maximum = ConvertValue(Maximum, culture) ?? throw new global::System.InvalidOperationException(MinMaxError);
                                        }
            """
            :
            """
                                        Minimum = ConvertValue(Minimum, culture) ?? throw new global::System.InvalidOperationException(MinMaxError);
                                        Maximum = ConvertValue(Maximum, culture) ?? throw new global::System.InvalidOperationException(MinMaxError);
            """;

            string convertValue = emitTimeSpanSupport ?
            """
                        if (OperandType == typeof(global::System.TimeSpan))
                        {
                            if (value is global::System.TimeSpan)
                            {
                                convertedValue = value;
                            }
                            else if (value is string)
                            {
                                if (!global::System.TimeSpan.TryParse((string)value, formatProvider, out global::System.TimeSpan timeSpanValue))
                                {
                                    return false;
                                }
                                convertedValue = timeSpanValue;
                            }
                            else
                            {
                                throw new global::System.InvalidOperationException($"A value type {value.GetType()} that is not a TimeSpan or a string has been given. This might indicate a problem with the source generator.");
                            }
                        }
                        else
                        {
                            try
                            {
                                convertedValue = ConvertValue(value, formatProvider);
                            }
                            catch (global::System.Exception e) when (e is global::System.FormatException or global::System.InvalidCastException or global::System.NotSupportedException or global::System.OverflowException)
                            {
                                return false;
                            }
                        }
            """
            :
            """
                        try
                        {
                            convertedValue = ConvertValue(value, formatProvider);
                        }
                        catch (global::System.Exception e) when (e is global::System.FormatException or global::System.InvalidCastException or global::System.NotSupportedException or global::System.OverflowException)
                        {
                            return false;
                        }
            """;

            string ensureInitializedBody = $$"""
            if (!_initialized)
            {
                lock (_lock)
                {
                    if (!_initialized)
                    {
                        if (Minimum is null || Maximum is null)
                        {
                            throw new global::System.InvalidOperationException(MinMaxError);
                        }
                        if (_needToConvertMinMax)
                        {
                            global::System.Globalization.CultureInfo culture = ParseLimitsInInvariantCulture ? global::System.Globalization.CultureInfo.InvariantCulture : global::System.Globalization.CultureInfo.CurrentCulture;
{{initializationString}}
                        }
                        int cmp = ((global::System.IComparable)Minimum).CompareTo((global::System.IComparable)Maximum);
                        if (cmp > 0)
                        {
                            throw new global::System.InvalidOperationException("The maximum value '{Maximum}' must be greater than or equal to the minimum value '{Minimum}'.");
                        }
                        else if (cmp == 0 && (MinimumIsExclusive || MaximumIsExclusive))
                        {
                            throw new global::System.InvalidOperationException("Cannot use exclusive bounds when the maximum value is equal to the minimum value.");
                        }
                        _initialized = true;
                    }
                }
            }
""";

            string initializeRange = ensureInitializedBody;
            string ensureInitializedMethod = string.Empty;
            if (_symbolHolder.HasValidationAttributeFormatMessageMethod)
            {
                initializeRange = "            EnsureInitialized();";

                StringBuilder sb = new();
                sb.AppendLine("        private void EnsureInitialized()");
                sb.AppendLine("        {");
                sb.AppendLine(ensureInitializedBody);
                sb.AppendLine("        }");
                ensureInitializedMethod = sb.ToString();
            }

            OutLn($$"""
[global::System.AttributeUsage(global::System.AttributeTargets.Property | global::System.AttributeTargets.Field | global::System.AttributeTargets.Parameter, AllowMultiple = false)]
    {{modifier}} class {{qualifiedClassName}} : {{StaticValidationAttributeType}}
    {
        public {{qualifiedClassName}}(int minimum, int maximum) : base()
        {
            Minimum = minimum;
            Maximum = maximum;
            OperandType = typeof(int);
        }
        public {{qualifiedClassName}}(double minimum, double maximum) : base()
        {
            Minimum = minimum;
            Maximum = maximum;
            OperandType = typeof(double);
        }
        public {{qualifiedClassName}}(global::System.Type type, string minimum, string maximum) : base()
        {
            OperandType = type;
            _needToConvertMinMax = true;
            Minimum = minimum;
            Maximum = maximum;
        }
        public object Minimum { get; private set; }
        public object Maximum { get; private set; }
        public bool MinimumIsExclusive { get; set; }
        public bool MaximumIsExclusive { get; set; }
        public global::System.Type OperandType { get; }
        public bool ParseLimitsInInvariantCulture { get; set; }
        public bool ConvertValueInInvariantCulture { get; set; }
        public override string FormatErrorMessage(string name) =>
                string.Format(global::System.Globalization.CultureInfo.CurrentCulture, GetValidationErrorMessage(), name, Minimum, Maximum);
{{formatMessageOverride}}        private readonly bool _needToConvertMinMax;
        private volatile bool _initialized;
        private readonly object _lock = new();
        private const string MinMaxError = "The minimum and maximum values must be set to valid values.";

        public override bool IsValid(object? value)
        {
{{initializeRange}}

            if (value is null or string { Length: 0 })
            {
                return true;
            }

            global::System.Globalization.CultureInfo formatProvider = ConvertValueInInvariantCulture ? global::System.Globalization.CultureInfo.InvariantCulture : global::System.Globalization.CultureInfo.CurrentCulture;
            object? convertedValue;

{{convertValue}}

            var min = (global::System.IComparable)Minimum;
            var max = (global::System.IComparable)Maximum;

            return
                (MinimumIsExclusive ? min.CompareTo(convertedValue) < 0 : min.CompareTo(convertedValue) <= 0) &&
                (MaximumIsExclusive ? max.CompareTo(convertedValue) > 0 : max.CompareTo(convertedValue) >= 0);
        }
{{ensureInitializedMethod}}        private string GetValidationErrorMessage()
        {
            return (MinimumIsExclusive, MaximumIsExclusive) switch
            {
                (false, false) => "The field {0} must be between {1} and {2}.",
                (true, false) => "The field {0} must be between {1} exclusive and {2}.",
                (false, true) => "The field {0} must be between {1} and {2} exclusive.",
                (true, true) => "The field {0} must be between {1} exclusive and {2} exclusive.",
            };
        }
        private object? ConvertValue(object? value, global::System.Globalization.CultureInfo formatProvider)
        {
            if (value is string stringValue)
            {
                value = global::System.Convert.ChangeType(stringValue, OperandType, formatProvider);
            }
            else
            {
                value = global::System.Convert.ChangeType(value, OperandType, formatProvider);
            }
            return value;
        }
    }
""");
        }

        private string GenerateFormatMessageOverride(string arguments, bool ensureInitialized = false)
        {
            if (!_symbolHolder.HasValidationAttributeFormatMessageMethod)
            {
                return string.Empty;
            }

            StringBuilder sb = new();
            sb.AppendLine("""        public override string FormatMessage([global::System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("CompositeFormat")] string format, string name)""");
            sb.AppendLine("        {");

            if (ensureInitialized)
            {
                sb.AppendLine("            EnsureInitialized();");
                sb.AppendLine();
            }

            sb.AppendLine($"            return string.Format(global::System.Globalization.CultureInfo.CurrentCulture, format, name, {arguments});");
            sb.AppendLine("        }");
            return sb.ToString();
        }

        private string GenerateStronglyTypedCodeForLengthAttributes(HashSet<object> data)
        {
            if (data.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder sb = new();
            string padding = GetPaddingString(3);

            foreach (var type in data)
            {
                string typeName = (string)type;
                sb.AppendLine($"else if (value is {typeName})");
                sb.AppendLine($"{padding}{{");
                sb.AppendLine($"{padding}    length = (({typeName})value).Count;");
                sb.AppendLine($"{padding}}}");
                sb.Append($"{padding}");
            }

            return sb.ToString();
        }

        private string GenerateStronglyTypedCodeForCompareAttribute(HashSet<object>? data)
        {
            if (data is null || data.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder sb = new();
            string padding = GetPaddingString(3);
            bool first = true;

            foreach (var obj in data)
            {
                (string type, string property) = ((string, string))obj;
                sb.Append(first ? $"if " : $"{padding}else if ");
                sb.AppendLine($"(validationContext.ObjectInstance is {type} && OtherProperty == \"{property}\")");
                sb.AppendLine($"{padding}{{");
                sb.AppendLine($"{padding}    result = Equals(value, (({type})validationContext.ObjectInstance).{EscapeIdentifier(property)});");
                sb.AppendLine($"{padding}}}");
                first = false;
            }

            return sb.ToString();
        }

        private void GenValidationAttributesClasses()
        {
            if (_optionsSourceGenContext.AttributesToGenerate.Count == 0)
            {
                return;
            }

            var attributesData = _optionsSourceGenContext.AttributesToGenerate.OrderBy(static kvp => kvp.Key, StringComparer.Ordinal).ToArray();

            OutLn($"namespace {StaticGeneratedValidationAttributesClassesNamespace}");
            OutOpenBrace();

            foreach (var attributeData in attributesData)
            {
                if (attributeData.Key == _symbolHolder.MaxLengthAttributeSymbol.Name)
                {
                    string linesToInsert = attributeData.Value is not null ? GenerateStronglyTypedCodeForLengthAttributes((HashSet<object>)attributeData.Value) : string.Empty;
                    EmitMaxLengthAttribute(_optionsSourceGenContext.ClassModifier, Emitter.StaticAttributeClassNamePrefix, attributeData.Key, linesToInsert, _optionsSourceGenContext.Suffix);
                }
                else if (attributeData.Key == _symbolHolder.MinLengthAttributeSymbol.Name)
                {
                    string linesToInsert = attributeData.Value is not null ? GenerateStronglyTypedCodeForLengthAttributes((HashSet<object>)attributeData.Value) : string.Empty;
                    EmitMinLengthAttribute(_optionsSourceGenContext.ClassModifier, Emitter.StaticAttributeClassNamePrefix, attributeData.Key, linesToInsert, _optionsSourceGenContext.Suffix);
                }
                else if (_symbolHolder.LengthAttributeSymbol is not null && attributeData.Key == _symbolHolder.LengthAttributeSymbol.Name)
                {
                    string linesToInsert = attributeData.Value is not null ? GenerateStronglyTypedCodeForLengthAttributes((HashSet<object>)attributeData.Value) : string.Empty;
                    EmitLengthAttribute(_optionsSourceGenContext.ClassModifier, Emitter.StaticAttributeClassNamePrefix, attributeData.Key, linesToInsert, _optionsSourceGenContext.Suffix);
                }
                else if (attributeData.Key == _symbolHolder.CompareAttributeSymbol.Name && attributeData.Value is not null)
                {
                    string linesToInsert = GenerateStronglyTypedCodeForCompareAttribute((HashSet<object>)attributeData.Value);
                    EmitCompareAttribute(_optionsSourceGenContext.ClassModifier, Emitter.StaticAttributeClassNamePrefix, attributeData.Key, linesToInsert: linesToInsert, _optionsSourceGenContext.Suffix);
                }
                else if (attributeData.Key == _symbolHolder.RangeAttributeSymbol.Name)
                {
                    EmitRangeAttribute(_optionsSourceGenContext.ClassModifier, Emitter.StaticAttributeClassNamePrefix, attributeData.Key, _optionsSourceGenContext.Suffix, attributeData.Value is not null);
                }
            }

            OutCloseBrace();
        }

        private void GenModelSelfValidationIfNecessary(ValidatedModel modelToValidate)
        {
            if (modelToValidate.SelfValidates)
            {
                OutLn($"context.MemberName = \"Validate\";");
                OutLn($"context.DisplayName = string.IsNullOrEmpty(name) ? \"Validate\" : $\"{{name}}.Validate\";");
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

            if (_symbolHolder.UnconditionalSuppressMessageAttributeSymbol is not null)
            {
                // We disable the warning on `new ValidationContext(object)` usage as we use it in a safe way that not require executing the reflection code.
                // This is done by initializing the DisplayName in the context which is the part trigger reflection if it is not initialized. For
                // projects targeting .NET 10 and above, we can avoid the suppression since we use the new trim-safe constructor.
                OutLn("#if !NET");
                OutLn($"[global::System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(\"Trimming\", \"IL2026:RequiresUnreferencedCode\",");
                OutLn($"     Justification = \"The created ValidationContext object is used in a way that never call reflection\")]");
                OutLn("#endif");
            }

            OutLn($"public {(makeStatic ? "static " : string.Empty)}global::Microsoft.Extensions.Options.ValidateOptionsResult Validate(string? name, {modelToValidate.Name} options)");
            OutOpenBrace();
            OutLn($"global::Microsoft.Extensions.Options.ValidateOptionsResultBuilder? builder = null;");
            OutLn("#if NET");
            OutLn($"var context = new {StaticValidationContextType}(options, \"{modelToValidate.SimpleName}\", null, null);");
            OutLn("#else");
            OutLn($"var context = new {StaticValidationContextType}(options);");
            OutLn("#endif");

            int capacity = modelToValidate.MembersToValidate.Count == 0 ? 0 : modelToValidate.MembersToValidate.Max(static vm => vm.ValidationAttributes.Count);
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
            GenValidateOptionsResultReturn("builder");
            OutCloseBrace();
        }

        // Whether the generated ValidateAsync body for this member's local validation function will contain any
        // await. When it won't (e.g. a member only has a transitive/enumerated validator that resolves to its
        // synchronous Validate method), the local function needs a CS1998 suppression since it is still declared
        // async so its exceptions (including a canceled token) surface on the returned Task instead of throwing
        // synchronously.
        private bool MemberAsyncValidationHasAwait(ValidatedMember vm)
            => (_symbolHolder.HasTryValidateValueAsyncMethod && vm.ValidationAttributes.Count > 0)
                || vm.TransValidatorEmitsAsync
                || vm.EnumerationValidatorEmitsAsync;

        // Whether this member can actually suspend. Synchronous validation attributes are still invoked through
        // TryValidateValueAsync, but that call completes synchronously and does not justify the per-member allocations
        // required by the concurrent validation path.
        private bool MemberAsyncValidationCanSuspend(ValidatedMember vm)
            => (_symbolHolder.HasTryValidateValueAsyncMethod && vm.ValidationAttributes.Any(static attr => attr.IsAsyncValidationAttribute))
                || vm.TransValidatorEmitsAsync
                || vm.EnumerationValidatorEmitsAsync;

        private void GenAsyncModelValidationMethod(
            ValidatedModel modelToValidate,
            bool makeStatic,
            ref Dictionary<string, StaticFieldInfo> staticValidationAttributesDict,
            ref Dictionary<string, StaticFieldInfo> staticValidatorsDict)
        {
            bool concurrentMembers = modelToValidate.MembersToValidate.Count >= 2
                && modelToValidate.MembersToValidate.Any(MemberAsyncValidationCanSuspend);

            // Determine whether the generated method body will contain any await. The method is always declared
            // async regardless (so that cancellationToken.ThrowIfCancellationRequested() below surfaces as a
            // faulted/canceled Task instead of throwing synchronously to the caller - a plain non-async method
            // would throw directly). When the body genuinely contains no await (e.g. the model only self-validates
            // synchronously via IValidatableObject and has no members), we suppress the resulting CS1998 warning.
            // Running two or more members concurrently always awaits Task.WhenAll.
            bool willAwait = concurrentMembers || modelToValidate.SelfValidatesAsync;
            if (!willAwait)
            {
                foreach (var vm in modelToValidate.MembersToValidate)
                {
                    if (MemberAsyncValidationHasAwait(vm))
                    {
                        willAwait = true;
                        break;
                    }
                }
            }

            OutLn($"/// <summary>");
            OutLn($"/// Validates a specific named options instance asynchronously (or all when <paramref name=\"name\"/> is <see langword=\"null\" />).");
            OutLn($"/// </summary>");
            OutLn($"/// <param name=\"name\">The name of the options instance being validated.</param>");
            OutLn($"/// <param name=\"options\">The options instance.</param>");
            OutLn($"/// <param name=\"cancellationToken\">The <see cref=\"global::System.Threading.CancellationToken\"/> to monitor for cancellation requests.</param>");
            OutLn($"/// <returns>A task representing the asynchronous validation operation, containing the validation result.</returns>");
            OutGeneratedCodeAttribute();

            if (_symbolHolder.UnconditionalSuppressMessageAttributeSymbol is not null)
            {
                // We disable the warning on `new ValidationContext(object)` usage as we use it in a safe way that not require executing the reflection code.
                // This is done by initializing the DisplayName in the context which is the part trigger reflection if it is not initialized. For
                // projects targeting .NET 10 and above, we can avoid the suppression since we use the new trim-safe constructor.
                OutLn("#if !NET");
                OutLn($"[global::System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(\"Trimming\", \"IL2026:RequiresUnreferencedCode\",");
                OutLn($"     Justification = \"The created ValidationContext object is used in a way that never call reflection\")]");
                OutLn("#endif");
            }

            if (!willAwait)
            {
                OutLn("#pragma warning disable CS1998 // The method is declared async so cancellationToken.ThrowIfCancellationRequested() below surfaces on the returned Task rather than throwing synchronously.");
            }

            OutLn($"public {(makeStatic ? "static " : string.Empty)}async global::System.Threading.Tasks.Task<global::Microsoft.Extensions.Options.ValidateOptionsResult> ValidateAsync(string? name, {modelToValidate.Name} options, global::System.Threading.CancellationToken cancellationToken = default)");
            OutOpenBrace();
            OutLn($"cancellationToken.ThrowIfCancellationRequested();");
            OutLn();
            OutLn($"global::Microsoft.Extensions.Options.ValidateOptionsResultBuilder? builder = null;");

            bool contextDeclared = false;
            if (concurrentMembers)
            {
                GenConcurrentAsyncMemberValidations(modelToValidate, ref staticValidationAttributesDict, ref staticValidatorsDict);
            }
            else
            {
                OutLn("#if NET");
                OutLn($"var context = new {StaticValidationContextType}(options, \"{modelToValidate.SimpleName}\", null, null);");
                OutLn("#else");
                OutLn($"var context = new {StaticValidationContextType}(options);");
                OutLn("#endif");
                contextDeclared = true;

                int capacity = modelToValidate.MembersToValidate.Count == 0 ? 0 : modelToValidate.MembersToValidate.Max(static vm => vm.ValidationAttributes.Count);
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
                        GenAsyncMemberValidation(vm, ref staticValidationAttributesDict, cleanListsBeforeUse);
                        cleanListsBeforeUse = true;
                        OutLn();
                    }

                    if (vm.TransValidatorType is not null)
                    {
                        GenAsyncTransitiveValidation(vm, ref staticValidatorsDict);
                        OutLn();
                    }

                    if (vm.EnumerationValidatorType is not null)
                    {
                        GenAsyncEnumerationValidation(vm, ref staticValidatorsDict);
                        OutLn();
                    }
                }
            }

            if (!contextDeclared && (modelToValidate.SelfValidatesAsync || modelToValidate.SelfValidates))
            {
                OutLn("#if NET");
                OutLn($"var context = new {StaticValidationContextType}(options, \"{modelToValidate.SimpleName}\", null, null);");
                OutLn("#else");
                OutLn($"var context = new {StaticValidationContextType}(options);");
                OutLn("#endif");
            }

            GenAsyncModelSelfValidationIfNecessary(modelToValidate);

            GenValidateOptionsResultReturn("builder");
            OutCloseBrace();

            if (!willAwait)
            {
                OutLn("#pragma warning restore CS1998");
            }
        }

        // Emits one async local function per member (each owning its own ValidationContext, ValidationResult list,
        // ValidationAttribute list, and ValidateOptionsResultBuilder - no mutable state is shared between members),
        // starts them all, and awaits their completion together via Task.WhenAll so member validation for a model
        // with 2+ MembersToValidate runs concurrently (see dotnet/runtime issue #128882). The per-member results are
        // then merged into the outer builder in original declaration order, regardless of completion order, so
        // existing failure ordering remains deterministic.
        private void GenConcurrentAsyncMemberValidations(
            ValidatedModel modelToValidate,
            ref Dictionary<string, StaticFieldInfo> staticValidationAttributesDict,
            ref Dictionary<string, StaticFieldInfo> staticValidatorsDict)
        {
            var members = modelToValidate.MembersToValidate;
            var taskVarNames = new string[members.Count];

            for (int i = 0; i < members.Count; i++)
            {
                var vm = members[i];
                bool memberHasAwait = MemberAsyncValidationHasAwait(vm);
                string builderVar = $"memberBuilder{i}";
                string contextVar = $"memberContext{i}";
                string resultsVar = $"memberValidationResults{i}";
                string attributesVar = $"memberValidationAttributes{i}";

                if (!memberHasAwait)
                {
                    OutLn("#pragma warning disable CS1998 // This member's validation never awaits, but the local function is declared async to participate uniformly in Task.WhenAll below.");
                }

                OutLn($"async global::System.Threading.Tasks.Task<global::Microsoft.Extensions.Options.ValidateOptionsResult> Validate{i}Async()");
                OutOpenBrace();
                OutLn($"global::Microsoft.Extensions.Options.ValidateOptionsResultBuilder? {builderVar} = null;");

                if (vm.ValidationAttributes.Count > 0)
                {
                    OutLn("#if NET");
                    OutLn($"var {contextVar} = new {StaticValidationContextType}(options, \"{modelToValidate.SimpleName}\", null, null);");
                    OutLn("#else");
                    OutLn($"var {contextVar} = new {StaticValidationContextType}(options);");
                    OutLn("#endif");
                    OutLn($"var {resultsVar} = new {StaticListType}<{StaticValidationResultType}>();");
                    OutLn($"var {attributesVar} = new {StaticListType}<{StaticValidationAttributeType}>({vm.ValidationAttributes.Count});");

                    GenAsyncMemberValidation(vm, ref staticValidationAttributesDict, cleanListsBeforeUse: false, contextVar, builderVar, resultsVar, attributesVar);
                }

                if (vm.TransValidatorType is not null)
                {
                    GenAsyncTransitiveValidation(vm, ref staticValidatorsDict, builderVar);
                }

                if (vm.EnumerationValidatorType is not null)
                {
                    GenAsyncEnumerationValidation(vm, ref staticValidatorsDict, builderVar);
                }

                GenValidateOptionsResultReturn(builderVar);
                OutCloseBrace();

                if (!memberHasAwait)
                {
                    OutLn("#pragma warning restore CS1998");
                }

                OutLn();

                taskVarNames[i] = $"memberTask{i}";
            }

            for (int i = 0; i < members.Count; i++)
            {
                OutLn($"var {taskVarNames[i]} = Validate{i}Async();");
            }

            OutLn($"var memberResults = await global::System.Threading.Tasks.Task.WhenAll({string.Join(", ", taskVarNames)}).ConfigureAwait(false);");
            OutLn();

            for (int i = 0; i < members.Count; i++)
            {
                OutLn($"if (memberResults[{i}].Failed)");
                OutOpenBrace();
                OutLn($"(builder ??= new()).AddResult(memberResults[{i}]);");
                OutCloseBrace();
            }
        }

        private void GenValidateOptionsResultReturn(string builderVariable)
        {
            OutLn($"return {builderVariable} is null ? global::Microsoft.Extensions.Options.ValidateOptionsResult.Success : {builderVariable}.Build();");
        }

        private void GenAsyncMemberValidation(
            ValidatedMember vm,
            ref Dictionary<string, StaticFieldInfo> staticValidationAttributesDict,
            bool cleanListsBeforeUse,
            string contextVar = "context",
            string builderVar = "builder",
            string resultsVar = "validationResults",
            string attributesVar = "validationAttributes")
        {
            OutLn($"{contextVar}.MemberName = \"{vm.Name}\";");
            OutLn($"{contextVar}.DisplayName = string.IsNullOrEmpty(name) ? \"{vm.Name}\" : $\"{{name}}.{vm.Name}\";");

            if (cleanListsBeforeUse)
            {
                OutLn($"{resultsVar}.Clear();");
                OutLn($"{attributesVar}.Clear();");
            }

            foreach (var attr in vm.ValidationAttributes)
            {
                var staticValidationAttributeInstance = GetOrAddStaticValidationAttribute(ref staticValidationAttributesDict, attr);
                OutLn($"{attributesVar}.Add({_staticValidationAttributeHolderClassFQN}.{staticValidationAttributeInstance.FieldName});");
            }

            if (_symbolHolder.HasTryValidateValueAsyncMethod)
            {
                OutLn($"if (!await global::System.ComponentModel.DataAnnotations.Validator.TryValidateValueAsync(options.{EscapeIdentifier(vm.Name)}{_TryGetValueNullableAnnotation}, {contextVar}, {resultsVar}, {attributesVar}, cancellationToken).ConfigureAwait(false))");
            }
            else
            {
                OutLn($"if (!global::System.ComponentModel.DataAnnotations.Validator.TryValidateValue(options.{EscapeIdentifier(vm.Name)}{_TryGetValueNullableAnnotation}, {contextVar}, {resultsVar}, {attributesVar}))");
            }

            OutOpenBrace();
            OutLn($"({builderVar} ??= new()).AddResults({resultsVar});");
            OutCloseBrace();
        }

        private void GenAsyncTransitiveValidation(ValidatedMember vm, ref Dictionary<string, StaticFieldInfo> staticValidatorsDict, string builderVar = "builder")
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

            var baseName = $"string.IsNullOrEmpty(name) ? \"{vm.Name}\" : $\"{{name}}.{vm.Name}\"";
            var memberAccess = $"options.{EscapeIdentifier(vm.Name)}";
            string asyncCallSequence = vm.TransValidatorAsyncInterfaceType is null
                ? callSequence
                : $"(({vm.TransValidatorAsyncInterfaceType}){callSequence})";

            // A nested validator emits an awaited ValidateAsync call only when it was synthesized for an async context or
            // when it implements IAsyncValidateOptions<T> for the member type.
            // Otherwise we fall back to its synchronous Validate method to guarantee the generated code compiles.
            string resultExpression = vm.TransValidatorEmitsAsync
                ? $"await {asyncCallSequence}.ValidateAsync({baseName}, {memberAccess}{valueAccess}, cancellationToken).ConfigureAwait(false)"
                : $"{callSequence}.Validate({baseName}, {memberAccess}{valueAccess})";

            if (vm.IsNullable)
            {
                OutLn($"if ({memberAccess} is not null)");
                OutOpenBrace();
                OutLn($"({builderVar} ??= new()).AddResult({resultExpression});");
                OutCloseBrace();
            }
            else
            {
                OutLn($"({builderVar} ??= new()).AddResult({resultExpression});");
            }
        }

        private void GenAsyncEnumerationValidation(ValidatedMember vm, ref Dictionary<string, StaticFieldInfo> staticValidatorsDict, string builderVar = "builder")
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

            bool awaitItem = vm.EnumerationValidatorEmitsAsync;
            var memberAccess = $"options.{EscapeIdentifier(vm.Name)}";
            string asyncCallSequence = vm.EnumerationValidatorAsyncInterfaceType is null
                ? callSequence
                : $"(({vm.EnumerationValidatorAsyncInterfaceType}){callSequence})";

            if (vm.IsNullable)
            {
                OutLn($"if ({memberAccess} is not null)");
            }

            OutOpenBrace();

            OutLn($"var count = 0;");
            OutLn($"foreach (var o in {memberAccess}{valueAccess})");
            OutOpenBrace();

            if (vm.EnumeratedIsNullable)
            {
                OutLn($"if (o is not null)");
                OutOpenBrace();
                var propertyName = $"string.IsNullOrEmpty(name) ? $\"{vm.Name}[{{count}}]\" : $\"{{name}}.{vm.Name}[{{count}}]\"";
                string itemResult = awaitItem
                    ? $"await {asyncCallSequence}.ValidateAsync({propertyName}, o{enumeratedValueAccess}, cancellationToken).ConfigureAwait(false)"
                    : $"{callSequence}.Validate({propertyName}, o{enumeratedValueAccess})";
                OutLn($"({builderVar} ??= new()).AddResult({itemResult});");
                OutCloseBrace();

                if (!vm.EnumeratedMayBeNull)
                {
                    OutLn($"else");
                    OutOpenBrace();
                    var error = $"string.IsNullOrEmpty(name) ? $\"{vm.Name}[{{count}}] is null\" : $\"{{name}}.{vm.Name}[{{count}}] is null\"";
                    OutLn($"({builderVar} ??= new()).AddError({error});");
                    OutCloseBrace();
                }

                OutLn($"count++;");
            }
            else
            {
                var propertyName = $"string.IsNullOrEmpty(name) ? $\"{vm.Name}[{{count++}}]\" : $\"{{name}}.{vm.Name}[{{count++}}]\"";
                string itemResult = awaitItem
                    ? $"await {asyncCallSequence}.ValidateAsync({propertyName}, o{enumeratedValueAccess}, cancellationToken).ConfigureAwait(false)"
                    : $"{callSequence}.Validate({propertyName}, o{enumeratedValueAccess})";
                OutLn($"({builderVar} ??= new()).AddResult({itemResult});");
            }

            OutCloseBrace();
            OutCloseBrace();
        }

        private void GenAsyncModelSelfValidationIfNecessary(ValidatedModel modelToValidate)
        {
            if (modelToValidate.SelfValidatesAsync)
            {
                OutLn($"context.MemberName = \"ValidateAsync\";");
                OutLn($"context.DisplayName = string.IsNullOrEmpty(name) ? \"ValidateAsync\" : $\"{{name}}.ValidateAsync\";");
                OutLn($"var asyncSelfValidationResults = ((global::System.ComponentModel.DataAnnotations.IAsyncValidatableObject)options).ValidateAsync(context, cancellationToken);");
                OutLn($"if (asyncSelfValidationResults is not null)");
                OutOpenBrace();
                OutLn($"await foreach (var asyncValidationResult in global::System.Threading.Tasks.TaskAsyncEnumerableExtensions.ConfigureAwait(asyncSelfValidationResults, false))");
                OutOpenBrace();
                OutLn($"(builder ??= new()).AddResult(asyncValidationResult);");
                OutCloseBrace();
                OutCloseBrace();
                OutLn();
            }
            else if (modelToValidate.SelfValidates)
            {
                OutLn($"context.MemberName = \"Validate\";");
                OutLn($"context.DisplayName = string.IsNullOrEmpty(name) ? \"Validate\" : $\"{{name}}.Validate\";");
                OutLn($"(builder ??= new()).AddResults(((global::System.ComponentModel.DataAnnotations.IValidatableObject)options).Validate(context));");
                OutLn();
            }
        }

        private void GenMemberValidation(ValidatedMember vm, ref Dictionary<string, StaticFieldInfo> staticValidationAttributesDict, bool cleanListsBeforeUse)
        {
            OutLn($"context.MemberName = \"{vm.Name}\";");
            OutLn($"context.DisplayName = string.IsNullOrEmpty(name) ? \"{vm.Name}\" : $\"{{name}}.{vm.Name}\";");

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

            OutLn($"if (!global::System.ComponentModel.DataAnnotations.Validator.TryValidateValue(options.{EscapeIdentifier(vm.Name)}{_TryGetValueNullableAnnotation}, context, validationResults, validationAttributes))");
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

                callSequence = $"{_staticValidatorHolderClassFQN}.{staticValidatorInstance.FieldName}";
            }

            var valueAccess = (vm.IsNullable && vm.IsValueType) ? ".Value" : string.Empty;

            var baseName = $"string.IsNullOrEmpty(name) ? \"{vm.Name}\" : $\"{{name}}.{vm.Name}\"";
            var memberAccess = $"options.{EscapeIdentifier(vm.Name)}";

            if (vm.IsNullable)
            {
                OutLn($"if ({memberAccess} is not null)");
                OutOpenBrace();
                OutLn($"(builder ??= new()).AddResult({callSequence}.Validate({baseName}, {memberAccess}{valueAccess}));");
                OutCloseBrace();
            }
            else
            {
                OutLn($"(builder ??= new()).AddResult({callSequence}.Validate({baseName}, {memberAccess}{valueAccess}));");
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

                callSequence = $"{_staticValidatorHolderClassFQN}.{staticValidatorInstance.FieldName}";
            }

            var memberAccess = $"options.{EscapeIdentifier(vm.Name)}";

            if (vm.IsNullable)
            {
                OutLn($"if ({memberAccess} is not null)");
            }

            OutOpenBrace();

            OutLn($"var count = 0;");
            OutLn($"foreach (var o in {memberAccess}{valueAccess})");
            OutOpenBrace();

            if (vm.EnumeratedIsNullable)
            {
                OutLn($"if (o is not null)");
                OutOpenBrace();
                var propertyName = $"string.IsNullOrEmpty(name) ? $\"{vm.Name}[{{count}}]\" : $\"{{name}}.{vm.Name}[{{count}}]\"";
                OutLn($"(builder ??= new()).AddResult({callSequence}.Validate({propertyName}, o{enumeratedValueAccess}));");
                OutCloseBrace();

                if (!vm.EnumeratedMayBeNull)
                {
                    OutLn($"else");
                    OutOpenBrace();
                    var error = $"string.IsNullOrEmpty(name) ? $\"{vm.Name}[{{count}}] is null\" : $\"{{name}}.{vm.Name}[{{count}}] is null\"";
                    OutLn($"(builder ??= new()).AddError({error});");
                    OutCloseBrace();
                }

                OutLn($"count++;");
            }
            else
            {
                var propertyName = $"string.IsNullOrEmpty(name) ? $\"{vm.Name}[{{count++}}]\" : $\"{{name}}.{vm.Name}[{{count++}}]\"";
                OutLn($"(builder ??= new()).AddResult({callSequence}.Validate({propertyName}, o{enumeratedValueAccess}));");
            }

            OutCloseBrace();
            OutCloseBrace();
        }

        /// <summary>
        /// Prefixes an identifier with "@" when it would otherwise be parsed as a keyword (e.g. a member declared as <c>@class</c>).
        /// </summary>
        private static string EscapeIdentifier(string identifier)
            => SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None || SyntaxFacts.GetContextualKeywordKind(identifier) != SyntaxKind.None
                ? "@" + identifier
                : identifier;

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
