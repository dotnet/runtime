// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using Microsoft.Gen.OptionsValidation.Test;

#pragma warning disable SA1649
#pragma warning disable SA1402

namespace TestClasses.OptionsValidation
{
    // ValidationAttribute without parameter
    public class RequiredAttributeModel
    {
        [Required]
        public string? Val { get; set; }
    }

    // ValidationAttribute with string parameter
    public class RegularExpressionAttributeModel
    {
        [RegularExpression("\\s")]
        public string Val { get; set; } = string.Empty;
    }

    // DataTypeAttribute
    public class EmailAttributeModel
    {
        [EmailAddress]
        public string Val { get; set; } = string.Empty;
    }

    // ValidationAttribute with System.Type parameter
    public class CustomValidationAttributeModel
    {
        [CustomValidation(typeof(CustomValidationTest), "TestMethod")]
        public string Val { get; set; } = string.Empty;
    }

#pragma warning disable SA1204 // Static elements should appear before instance elements
    public static class CustomValidationTest
#pragma warning restore SA1204 // Static elements should appear before instance elements
    {
        public static ValidationResult? TestMethod(string val, ValidationContext _)
        {
            if (val.Equals("Pass", StringComparison.Ordinal))
            {
                return ValidationResult.Success;
            }

            throw new ValidationException();
        }
    }

    // ValidationAttribute with DataType parameter
    public class DataTypeAttributeModel
    {
        [DataType(DataType.Text)]
        public string Val { get; set; } = string.Empty;
    }

    // ValidationAttribute with type, double, int parameters
    public class RangeAttributeModelInt
    {
        [Range(1, 3)]
        public int Val { get; set; }
    }

    public class RangeAttributeModelDouble
    {
        [Range(0.5, 0.9)]
        public double Val { get; set; }
    }

    public class RangeAttributeModelDate
    {
#if NETCOREAPP3_1_OR_GREATER
        [Range(typeof(DateTime), "1/2/2004", "3/4/2004", ParseLimitsInInvariantCulture = true)]
#else
        [Range(typeof(DateTime), "1/2/2004", "3/4/2004")]
#endif
        public DateTime Val { get; set; }
    }

    public class MultipleAttributeModel
    {
        [Required]
        [DataType(DataType.Password)]
        public string Val1 { get; set; } = string.Empty;

        [Range(1, 3)]
        public int Val2 { get; set; }

        [Range(3, 5)]
        public int Val3 { get; set; }

        [Range(5, 9)]
        public int Val4 { get; set; }
    }

    public class CustomTypeCustomValidationAttributeModel
    {
        [CustomValidation(typeof(CustomTypeCustomValidationTest), "TestMethod")]
        public CustomType? Val { get; set; }
    }

    public class CustomType
    {
        public string Val1 { get; set; } = string.Empty;
        public string Val2 { get; set; } = string.Empty;
    }

#pragma warning disable SA1204 // Static elements should appear before instance elements
    public static class CustomTypeCustomValidationTest
#pragma warning restore SA1204 // Static elements should appear before instance elements
    {
        public static ValidationResult? TestMethod(CustomType val, ValidationContext _)
        {
            if (val.Val1.Equals("Pass", StringComparison.Ordinal) && val.Val2.Equals("Pass", StringComparison.Ordinal))
            {
                return ValidationResult.Success;
            }

            throw new ValidationException();
        }
    }

    public class AttributePropertyModel
    {
        [Range(1, 3, ErrorMessage = "ErrorMessage")]
        public int Val1 { get; set; }

        [Range(1, 3, ErrorMessageResourceType = typeof(SR), ErrorMessageResourceName = "ErrorMessageResourceName")]
        public int Val2 { get; set; }
    }

    public class TypeWithoutOptionsValidator
    {
        [Required]
        public string? Val1 { get; set; }

        [Range(typeof(DateTime), "1/2/2004", "3/4/2004")]
        public DateTime Val2 { get; set; }

        [Microsoft.Extensions.Options.ValidateObjectMembers]
        public RangeAttributeModelDouble? YetAnotherComplexVal { get; set; }
    }

    public class DerivedModel : RequiredAttributeModel
    {
        [Required]
        public string? DerivedVal { get; set; }

        [Required]
        public virtual int? VirtualValWithAttr { get; set; }

        public virtual int? VirtualValWithoutAttr { get; set; }

        [Required]
        public new int? Val { get; set; }
    }

    public class LeafModel : DerivedModel
    {
        public override int? VirtualValWithAttr { get; set; }

        [Required]
        public override int? VirtualValWithoutAttr { get; set; }
    }

    public class ComplexModel
    {
        [Microsoft.Extensions.Options.ValidateObjectMembers]
        public RequiredAttributeModel? ComplexVal { get; set; }

        [Microsoft.Extensions.Options.ValidateObjectMembers]
        public TypeWithoutOptionsValidator? ValWithoutOptionsValidator { get; set; }
    }

    public class OptionsUsingRangeWithTimeSpan
    {
        [Range(typeof(TimeSpan), "00:00:00", "00:00:10")]
        public TimeSpan P1 { get; set; }

        [Range(typeof(TimeSpan), "00:00:00", "00:00:10")]
        public TimeSpan? P2 { get; set; }

        [Range(typeof(TimeSpan), "00:00:00", "00:00:10")]
        public string P3 { get; set; }

        [Range(typeof(TimeSpan), "00:00:00", "00:00:10")]
        public string? P4 { get; set; }
    }

    [OptionsValidator]
    public partial class RequiredAttributeModelValidator : IValidateOptions<RequiredAttributeModel>
    {
    }

    [OptionsValidator]
    public partial class RegularExpressionAttributeModelValidator : IValidateOptions<RegularExpressionAttributeModel>
    {
    }

    [OptionsValidator]
    public partial class EmailAttributeModelValidator : IValidateOptions<EmailAttributeModel>
    {
    }

    [OptionsValidator]
    public partial class CustomValidationAttributeModelValidator : IValidateOptions<CustomValidationAttributeModel>
    {
    }

    [OptionsValidator]
    public partial class DataTypeAttributeModelValidator : IValidateOptions<DataTypeAttributeModel>
    {
    }

    [OptionsValidator]
    public partial class RangeAttributeModelIntValidator : IValidateOptions<RangeAttributeModelInt>
    {
    }

    [OptionsValidator]
    public partial class RangeAttributeModelDoubleValidator : IValidateOptions<RangeAttributeModelDouble>
    {
    }

    [OptionsValidator]
    public partial class RangeAttributeModelDateValidator : IValidateOptions<RangeAttributeModelDate>
    {
    }

    [OptionsValidator]
    public partial class MultipleAttributeModelValidator : IValidateOptions<MultipleAttributeModel>
    {
    }

    [OptionsValidator]
    public partial class CustomTypeCustomValidationAttributeModelValidator : IValidateOptions<CustomTypeCustomValidationAttributeModel>
    {
    }

    [OptionsValidator]
    public partial class AttributePropertyModelValidator : IValidateOptions<AttributePropertyModel>
    {
    }

    [OptionsValidator]
    public partial class DerivedModelValidator : IValidateOptions<DerivedModel>
    {
    }

    [OptionsValidator]
    public partial class LeafModelValidator : IValidateOptions<LeafModel>
    {
    }

    [OptionsValidator]
    internal sealed partial class ComplexModelValidator : IValidateOptions<ComplexModel>
    {
    }

    [OptionsValidator]
    internal sealed partial class OptionsUsingRangeWithTimeSpanValidator : IValidateOptions<OptionsUsingRangeWithTimeSpan>
    {
    }
}
