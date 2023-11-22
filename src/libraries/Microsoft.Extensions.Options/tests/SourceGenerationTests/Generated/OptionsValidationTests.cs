// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Options;
using TestClasses.OptionsValidation;
using Xunit;

namespace Microsoft.Gen.OptionsValidation.Test;

public class OptionsValidationTests
{
    [Fact]
    public void RequiredAttributeValid()
    {
        var validModel = new RequiredAttributeModel
        {
            Val = "val"
        };

        var modelValidator = new RequiredAttributeModelValidator();
        var result = modelValidator.Validate(nameof(validModel), validModel);

        Assert.Equal(ValidateOptionsResult.Success, result);
    }

    [Fact]
    public void RequiredAttributeInvalid()
    {
        var validModel = new RequiredAttributeModel
        {
            Val = null
        };

        var modelValidator = new RequiredAttributeModelValidator();
        Utils.VerifyValidateOptionsResult(modelValidator.Validate(nameof(validModel), validModel), 1);
    }

    [Fact]
    public void RegularExpressionAttributeValid()
    {
        var validModel = new RegularExpressionAttributeModel
        {
            Val = " "
        };

        var modelValidator = new RegularExpressionAttributeModelValidator();
        var result = modelValidator.Validate(nameof(validModel), validModel);

        Assert.Equal(ValidateOptionsResult.Success, result);
    }

    [Fact]
    public void RegularExpressionAttributeInvalid()
    {
        var validModel = new RegularExpressionAttributeModel
        {
            Val = "Not Space"
        };

        var modelValidator = new RegularExpressionAttributeModelValidator();
        Utils.VerifyValidateOptionsResult(modelValidator.Validate(nameof(validModel), validModel), 1);
    }

    [Fact]
    public void EmailAttributeValid()
    {
        var validModel = new EmailAttributeModel
        {
            Val = "abc@xyz.com"
        };

        var modelValidator = new EmailAttributeModelValidator();
        var result = modelValidator.Validate(nameof(validModel), validModel);

        Assert.Equal(ValidateOptionsResult.Success, result);
    }

    [Fact]
    public void EmailAttributeInvalid()
    {
        var validModel = new EmailAttributeModel
        {
            Val = "Not Email Address"
        };

        var modelValidator = new EmailAttributeModelValidator();
        Utils.VerifyValidateOptionsResult(modelValidator.Validate(nameof(validModel), validModel), 1);
    }

    [Fact]
    public void CustomValidationAttributeValid()
    {
        var validModel = new CustomValidationAttributeModel
        {
            Val = "Pass"
        };

        var modelValidator = new CustomValidationAttributeModelValidator();
        var result = modelValidator.Validate(nameof(validModel), validModel);

        Assert.Equal(ValidateOptionsResult.Success, result);
    }

    [Fact]
    public void CustomValidationAttributeInvalid()
    {
        var validModel = new CustomValidationAttributeModel
        {
            Val = "NOT PASS"
        };

        var modelValidator = new CustomValidationAttributeModelValidator();
        Assert.Throws<ValidationException>(() => modelValidator.Validate(nameof(validModel), validModel));
    }

    [Fact]
    public void DataTypeAttributeValid()
    {
        var validModel = new DataTypeAttributeModel
        {
            Val = "ABC"
        };

        var modelValidator = new DataTypeAttributeModelValidator();
        var result = modelValidator.Validate(nameof(validModel), validModel);

        Assert.Equal(ValidateOptionsResult.Success, result);
    }

    [Fact]
    public void RangeAttributeModelIntValid()
    {
        var validModel = new RangeAttributeModelInt
        {
            Val = 1
        };

        var modelValidator = new RangeAttributeModelIntValidator();
        var result = modelValidator.Validate(nameof(validModel), validModel);

        Assert.Equal(ValidateOptionsResult.Success, result);
    }

    [Fact]
    public void RangeAttributeModelIntInvalid()
    {
        var validModel = new RangeAttributeModelInt
        {
            Val = 0
        };

        var modelValidator = new RangeAttributeModelIntValidator();
        Utils.VerifyValidateOptionsResult(modelValidator.Validate(nameof(validModel), validModel), 1);
    }

    [Fact]
    public void RangeAttributeModelDoubleValid()
    {
        var validModel = new RangeAttributeModelDouble
        {
            Val = 0.6
        };

        var modelValidator = new RangeAttributeModelDoubleValidator();
        var result = modelValidator.Validate(nameof(validModel), validModel);

        Assert.Equal(ValidateOptionsResult.Success, result);
    }

    [Fact]
    public void RangeAttributeModelDoubleInvalid()
    {
        var validModel = new RangeAttributeModelDouble
        {
            Val = 0.1
        };

        var modelValidator = new RangeAttributeModelDoubleValidator();
        Utils.VerifyValidateOptionsResult(modelValidator.Validate(nameof(validModel), validModel), 1);
    }

    [Fact]
    public void RangeAttributeModelDateValid()
    {
#if NETCOREAPP3_1_OR_GREATER
        // Setting non-invariant culture to check that
        // attribute's "ParseLimitsInInvariantCulture" property
        // was set up correctly in the validator:
        CultureInfo.CurrentCulture = new CultureInfo("cs");
#else
        // Setting invariant culture to avoid DateTime parsing discrepancies:
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
#endif
        var validModel = new RangeAttributeModelDate
        {
            Val = new DateTime(day: 3, month: 1, year: 2004)
        };

        var modelValidator = new RangeAttributeModelDateValidator();
        var result = modelValidator.Validate(nameof(validModel), validModel);

        Assert.Equal(ValidateOptionsResult.Success, result);
    }

    [Fact]
    public void RangeAttributeModelDateInvalid()
    {
        var validModel = new RangeAttributeModelDate
        {
            Val = new DateTime(day: 1, month: 1, year: 2004)
        };

        var modelValidator = new RangeAttributeModelDateValidator();
        Utils.VerifyValidateOptionsResult(modelValidator.Validate(nameof(validModel), validModel), 1);
    }

    [Fact]
    public void MultipleAttributeModelValid()
    {
        var validModel = new MultipleAttributeModel
        {
            Val1 = "abc",
            Val2 = 2,
            Val3 = 4,
            Val4 = 6
        };

        var modelValidator = new MultipleAttributeModelValidator();
        var result = modelValidator.Validate(nameof(validModel), validModel);

        Assert.Equal(ValidateOptionsResult.Success, result);
    }

    [Theory]
    [InlineData("", 2, 4, 7)]
    [InlineData(null, 2, 4, 7)]
    [InlineData("abc", 0, 4, 9)]
    [InlineData("abc", 2, 8, 8)]
    [InlineData("abc", 2, 4, 10)]
    public void MultipleAttributeModelInvalid(string val1, int val2, int val3, int val4)
    {
        var validModel = new MultipleAttributeModel
        {
            Val1 = val1,
            Val2 = val2,
            Val3 = val3,
            Val4 = val4
        };

        var modelValidator = new MultipleAttributeModelValidator();
        Utils.VerifyValidateOptionsResult(modelValidator.Validate(nameof(validModel), validModel), 1);
    }

    [Fact]
    public void CustomTypeCustomValidationAttributeModelValid()
    {
        var validModel = new CustomTypeCustomValidationAttributeModel
        {
            Val = new CustomType { Val1 = "Pass", Val2 = "Pass" }
        };

        var modelValidator = new CustomTypeCustomValidationAttributeModelValidator();
        var result = modelValidator.Validate(nameof(validModel), validModel);

        Assert.Equal(ValidateOptionsResult.Success, result);
    }

    [Fact]
    public void CustomTypeCustomValidationAttributeModelInvalid()
    {
        var validModel = new CustomTypeCustomValidationAttributeModel
        {
            Val = new CustomType { Val1 = "Pass", Val2 = "Not Pass" }
        };

        var modelValidator = new CustomTypeCustomValidationAttributeModelValidator();
        Assert.Throws<ValidationException>(() => modelValidator.Validate(nameof(validModel), validModel));
    }

    [Fact]
    public void DerivedModelIsValid()
    {
        var validModel = new DerivedModel
        {
            Val = 1,
            DerivedVal = "Valid",
            VirtualValWithAttr = 1,
            VirtualValWithoutAttr = null
        };

        ((RequiredAttributeModel)validModel).Val = "Valid hidden member from base class";

        var validator = new DerivedModelValidator();
        var result = validator.Validate(nameof(validModel), validModel);
        Assert.Equal(ValidateOptionsResult.Success, result);
    }

    [Theory]
    [InlineData(0, "", 1, null, "Valid hidden member from base class")]
    [InlineData(null, "Valid", 1, null, "Valid hidden member from base class")]
    [InlineData(1, "Valid", null, null, "Valid hidden member from base class")]
    public void DerivedModelIsInvalid(int? val, string? derivedVal, int? virtValAttr, int? virtVal, string? hiddenValBaseClass)
    {
        var invalidModel = new DerivedModel
        {
            Val = val,
            DerivedVal = derivedVal,
            VirtualValWithAttr = virtValAttr,
            VirtualValWithoutAttr = virtVal
        };

        ((RequiredAttributeModel)invalidModel).Val = hiddenValBaseClass;

        var validator = new DerivedModelValidator();
        Utils.VerifyValidateOptionsResult(validator.Validate(nameof(invalidModel), invalidModel), 1);
    }

    [Fact]
    public void LeafModelIsValid()
    {
        var validModel = new LeafModel
        {
            Val = 1,
            DerivedVal = "Valid",
            VirtualValWithAttr = null,
            VirtualValWithoutAttr = 1
        };

        ((RequiredAttributeModel)validModel).Val = "Valid hidden member from base class";

        var validator = new LeafModelValidator();
        var result = validator.Validate(nameof(validModel), validModel);
        Assert.Equal(ValidateOptionsResult.Success, result);
    }

    [Fact]
    public void ComplexModelValid()
    {
        var validModel = new ComplexModel
        {
            ComplexVal = new RequiredAttributeModel { Val = "Valid" }
        };

        var modelValidator = new ComplexModelValidator();
        var result = modelValidator.Validate(nameof(validModel), validModel);
        Assert.Equal(ValidateOptionsResult.Success, result);

        validModel = new ComplexModel
        {
            ValWithoutOptionsValidator = new TypeWithoutOptionsValidator
            {
                Val1 = "Valid",
                Val2 = new DateTime(day: 3, month: 1, year: 2004)
            }
        };

        // Setting invariant culture to avoid DateTime parsing discrepancies:
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        result = modelValidator.Validate(nameof(validModel), validModel);
        Assert.Equal(ValidateOptionsResult.Success, result);

        validModel = new ComplexModel
        {
            ValWithoutOptionsValidator = new TypeWithoutOptionsValidator
            {
                Val1 = "A",
                Val2 = new DateTime(day: 2, month: 2, year: 2004),
                YetAnotherComplexVal = new RangeAttributeModelDouble { Val = 0.7 }
            }
        };

        result = modelValidator.Validate(nameof(validModel), validModel);
        Assert.Equal(ValidateOptionsResult.Success, result);
    }

    [Fact]
    public void ComplexModelInvalid()
    {
        var invalidModel = new ComplexModel
        {
            ComplexVal = new RequiredAttributeModel { Val = null }
        };

        var modelValidator = new ComplexModelValidator();
        Utils.VerifyValidateOptionsResult(modelValidator.Validate(nameof(invalidModel), invalidModel), 1);

        invalidModel = new ComplexModel
        {
            ValWithoutOptionsValidator = new TypeWithoutOptionsValidator { Val1 = "Valid", Val2 = new DateTime(2003, 3, 3) }
        };

        Utils.VerifyValidateOptionsResult(modelValidator.Validate(nameof(invalidModel), invalidModel), 1);

        invalidModel = new ComplexModel
        {
            ValWithoutOptionsValidator = new TypeWithoutOptionsValidator { Val1 = string.Empty, Val2 = new DateTime(2004, 3, 3) }
        };

        Utils.VerifyValidateOptionsResult(modelValidator.Validate(nameof(invalidModel), invalidModel), 1);

        invalidModel = new ComplexModel
        {
            ValWithoutOptionsValidator = new TypeWithoutOptionsValidator
            {
                Val1 = "A",
                Val2 = new DateTime(2004, 2, 2),
                YetAnotherComplexVal = new RangeAttributeModelDouble { Val = 0.4999 }
            }
        };

        Utils.VerifyValidateOptionsResult(modelValidator.Validate(nameof(invalidModel), invalidModel), 1);
    }

    [Fact]
    public void AttributePropertyModelTestOnErrorMessage()
    {
        var validModel = new AttributePropertyModel
        {
            Val1 = 5,
            Val2 = 1
        };

        var modelValidator = new AttributePropertyModelValidator();
        Utils.VerifyValidateOptionsResult(modelValidator.Validate(nameof(validModel), validModel), 1);
    }

    [Fact]
    public void AttributePropertyModelTestOnErrorMessageResource()
    {
        var validModel = new AttributePropertyModel
        {
            Val1 = 1,
            Val2 = 5
        };

        var modelValidator = new AttributePropertyModelValidator();
        Utils.VerifyValidateOptionsResult(modelValidator.Validate(nameof(validModel), validModel), 1);
    }

    [Fact]
    public void OptionsUsingRangeWithTimeSpanValid()
    {
        var validModel = new OptionsUsingRangeWithTimeSpan
        {
            P1 = TimeSpan.FromSeconds(1),
            P2 = TimeSpan.FromSeconds(2),
            P3 = "00:00:03",
            P4 = "00:00:04",
        };

        var modelValidator = new OptionsUsingRangeWithTimeSpanValidator();
        ValidateOptionsResult result = modelValidator.Validate(nameof(validModel), validModel);
        Assert.Equal(ValidateOptionsResult.Success, result);

        var invalidModel = new OptionsUsingRangeWithTimeSpan
        {
            P1 = TimeSpan.FromSeconds(11),
            P2 = TimeSpan.FromSeconds(-2),
            P3 = "01:00:03",
            P4 = "02:00:04",
        };
        result = modelValidator.Validate(nameof(invalidModel), invalidModel);
        Assert.Equal(4, result.Failures.Count());

        // null values pass the validation!
        invalidModel = new OptionsUsingRangeWithTimeSpan
        {
            P1 = TimeSpan.FromSeconds(100),
            P2 = null,
            P3 = "00:01:00",
            P4 = null,
        };
        result = modelValidator.Validate(nameof(invalidModel), invalidModel);
        Assert.Equal(2, result.Failures.Count());
    }
}
