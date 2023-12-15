// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Options.Generators
{
    internal static class SymbolLoader
    {
        public const string OptionsValidatorAttribute = "Microsoft.Extensions.Options.OptionsValidatorAttribute";
        internal const string ValidationAttribute = "System.ComponentModel.DataAnnotations.ValidationAttribute";
        internal const string MaxLengthAttribute = "System.ComponentModel.DataAnnotations.MaxLengthAttribute";
        internal const string MinLengthAttribute = "System.ComponentModel.DataAnnotations.MinLengthAttribute";
        internal const string CompareAttribute = "System.ComponentModel.DataAnnotations.CompareAttribute";
        internal const string LengthAttribute = "System.ComponentModel.DataAnnotations.LengthAttribute";
        internal const string RangeAttribute = "System.ComponentModel.DataAnnotations.RangeAttribute";
        internal const string ICollectionType = "System.Collections.ICollection";
        internal const string DataTypeAttribute = "System.ComponentModel.DataAnnotations.DataTypeAttribute";
        internal const string IValidatableObjectType = "System.ComponentModel.DataAnnotations.IValidatableObject";
        internal const string IValidateOptionsType = "Microsoft.Extensions.Options.IValidateOptions`1";
        internal const string TypeOfType = "System.Type";
        internal const string TimeSpanType = "System.TimeSpan";
        internal const string ValidateObjectMembersAttribute = "Microsoft.Extensions.Options.ValidateObjectMembersAttribute";
        internal const string ValidateEnumeratedItemsAttribute = "Microsoft.Extensions.Options.ValidateEnumeratedItemsAttribute";
        internal const string GenericIEnumerableType = "System.Collections.Generic.IEnumerable`1";
        internal const string UnconditionalSuppressMessageAttributeType = "System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessageAttribute";

        public static bool TryLoad(Compilation compilation, out SymbolHolder? symbolHolder)
        {
            INamedTypeSymbol? GetSymbol(string metadataName) => compilation.GetTypeByMetadataName(metadataName);

            // required
            var optionsValidatorSymbol = GetSymbol(OptionsValidatorAttribute);
            var validationAttributeSymbol = GetSymbol(ValidationAttribute);
            var maxLengthAttributeSymbol = GetSymbol(MaxLengthAttribute);
            var minLengthAttributeSymbol = GetSymbol(MinLengthAttribute);
            var compareAttributeSymbol = GetSymbol(CompareAttribute);
            var lengthAttributeSymbol = GetSymbol(LengthAttribute);
            var rangeAttributeSymbol = GetSymbol(RangeAttribute);
            var iCollectionSymbol = GetSymbol(ICollectionType);
            var dataTypeAttributeSymbol = GetSymbol(DataTypeAttribute);
            var ivalidatableObjectSymbol = GetSymbol(IValidatableObjectType);
            var validateOptionsSymbol = GetSymbol(IValidateOptionsType);
            var genericIEnumerableSymbol = GetSymbol(GenericIEnumerableType);
            var typeSymbol = GetSymbol(TypeOfType);
            var timeSpanSymbol = GetSymbol(TimeSpanType);
            var validateObjectMembersAttribute = GetSymbol(ValidateObjectMembersAttribute);
            var validateEnumeratedItemsAttribute = GetSymbol(ValidateEnumeratedItemsAttribute);
            var unconditionalSuppressMessageAttributeSymbol = GetSymbol(UnconditionalSuppressMessageAttributeType);
            if (unconditionalSuppressMessageAttributeSymbol is not null)
            {
                var containingAssemblyName = unconditionalSuppressMessageAttributeSymbol.ContainingAssembly.Identity.Name;
                if (!containingAssemblyName.Equals("System.Private.CoreLib", System.StringComparison.OrdinalIgnoreCase) &&
                    !containingAssemblyName.Equals("System.Runtime", System.StringComparison.OrdinalIgnoreCase))
                {
                    // The compilation returns UnconditionalSuppressMessageAttribute symbol even if the attribute is not available like the case when running on .NET Framework.
                    // We need to make sure that the attribute is really available by checking the containing assembly which in .NET Core will be either System.Private.CoreLib or System.Runtime.
                    unconditionalSuppressMessageAttributeSymbol = null;
                }
            }

    #pragma warning disable S1067 // Expressions should not be too complex
            if (optionsValidatorSymbol == null ||
                validationAttributeSymbol == null ||
                maxLengthAttributeSymbol == null ||
                minLengthAttributeSymbol == null ||
                compareAttributeSymbol == null ||
                rangeAttributeSymbol == null ||
                iCollectionSymbol == null ||
                dataTypeAttributeSymbol == null ||
                ivalidatableObjectSymbol == null ||
                validateOptionsSymbol == null ||
                genericIEnumerableSymbol == null ||
                typeSymbol == null ||
                timeSpanSymbol == null ||
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
                maxLengthAttributeSymbol,
                minLengthAttributeSymbol,
                compareAttributeSymbol,
                lengthAttributeSymbol,
                unconditionalSuppressMessageAttributeSymbol,
                rangeAttributeSymbol,
                iCollectionSymbol,
                dataTypeAttributeSymbol,
                validateOptionsSymbol,
                ivalidatableObjectSymbol,
                genericIEnumerableSymbol,
                typeSymbol,
                timeSpanSymbol,
                validateObjectMembersAttribute,
                validateEnumeratedItemsAttribute);

            return true;
        }
    }
}
