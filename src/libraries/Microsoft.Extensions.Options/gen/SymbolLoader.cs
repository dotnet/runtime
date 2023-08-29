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

        public static bool TryLoad(Compilation compilation, out SymbolHolder? symbolHolder)
        {
            INamedTypeSymbol? GetSymbol(string metadataName, bool optional = false)
            {
                var symbol = compilation.GetTypeByMetadataName(metadataName);
                if (symbol == null && !optional)
                {
                    return null;
                }

                return symbol;
            }

            // required
            var optionsValidatorSymbol = GetSymbol(OptionsValidatorAttribute);
            var validationAttributeSymbol = GetSymbol(ValidationAttribute);
            var dataTypeAttributeSymbol = GetSymbol(DataTypeAttribute);
            var ivalidatableObjectSymbol = GetSymbol(IValidatableObjectType);
            var validateOptionsSymbol = GetSymbol(IValidateOptionsType);
            var typeSymbol = GetSymbol(TypeOfType);

    #pragma warning disable S1067 // Expressions should not be too complex
            if (optionsValidatorSymbol == null ||
                validationAttributeSymbol == null ||
                dataTypeAttributeSymbol == null ||
                ivalidatableObjectSymbol == null ||
                validateOptionsSymbol == null ||
                typeSymbol == null)
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
                typeSymbol,

                // optional
                GetSymbol(ValidateObjectMembersAttribute, optional: true),
                GetSymbol(ValidateEnumeratedItemsAttribute, optional: true));

            return true;
        }
    }
}
