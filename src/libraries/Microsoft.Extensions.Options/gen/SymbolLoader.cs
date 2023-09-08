// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Options.Generators
{
    internal static class SymbolLoader
    {
        public const string OptionsValidatorAttribute = "Microsoft.Extensions.Options.OptionsValidatorAttribute";
        internal const string ValidationAttribute = "System.ComponentModel.DataAnnotations.ValidationAttribute";
        internal const string DataTypeAttribute = "System.ComponentModel.DataAnnotations.DataTypeAttribute";
        internal const string IValidatableObjectType = "System.ComponentModel.DataAnnotations.IValidatableObject";
        internal const string IValidateOptionsType = "Microsoft.Extensions.Options.IValidateOptions`1";
        internal const string TypeOfType = "System.Type";
        internal const string ValidateObjectMembersAttribute = "Microsoft.Extensions.Options.ValidateObjectMembersAttribute";
        internal const string ValidateEnumeratedItemsAttribute = "Microsoft.Extensions.Options.ValidateEnumeratedItemsAttribute";
        internal const string GenericIEnumerableType = "System.Collections.Generic.IEnumerable`1";

        public static bool TryLoad(Compilation compilation, out SymbolHolder? symbolHolder)
        {
            INamedTypeSymbol? GetSymbol(string metadataName) => compilation.GetTypeByMetadataName(metadataName);

            // required
            var optionsValidatorSymbol = GetSymbol(OptionsValidatorAttribute);
            var validationAttributeSymbol = GetSymbol(ValidationAttribute);
            var dataTypeAttributeSymbol = GetSymbol(DataTypeAttribute);
            var ivalidatableObjectSymbol = GetSymbol(IValidatableObjectType);
            var validateOptionsSymbol = GetSymbol(IValidateOptionsType);
            var genericIEnumerableSymbol = GetSymbol(GenericIEnumerableType);
            var typeSymbol = GetSymbol(TypeOfType);
            var validateObjectMembersAttribute = GetSymbol(ValidateObjectMembersAttribute);
            var validateEnumeratedItemsAttribute = GetSymbol(ValidateEnumeratedItemsAttribute);

    #pragma warning disable S1067 // Expressions should not be too complex
            if (optionsValidatorSymbol == null ||
                validationAttributeSymbol == null ||
                dataTypeAttributeSymbol == null ||
                ivalidatableObjectSymbol == null ||
                validateOptionsSymbol == null ||
                genericIEnumerableSymbol == null ||
                typeSymbol == null ||
                validateObjectMembersAttribute == null ||
                validateEnumeratedItemsAttribute == null)
            {
                symbolHolder = default;
                return false;
            }
    #pragma warning restore S1067 // Expressions should not be too complex

            symbolHolder = new(
                optionsValidatorSymbol,
                validationAttributeSymbol,
                dataTypeAttributeSymbol,
                validateOptionsSymbol,
                ivalidatableObjectSymbol,
                genericIEnumerableSymbol,
                typeSymbol,
                validateObjectMembersAttribute,
                validateEnumeratedItemsAttribute);

            return true;
        }
    }
}
