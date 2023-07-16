// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Enumeration;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Gen.OptionsValidation.Test;

public class EnumerationTests
{
    [Fact]
    public void Invalid()
    {
        var secondModelC = new SecondModel
        {
            P6 = "1234",
        };

        var secondModelB = new SecondModel
        {
            P6 = "12345",
        };

        var secondModel = new SecondModel
        {
            P6 = "1234",
        };

        ThirdModel? thirdModel = new ThirdModel
        {
            Value = 11
        };

        var firstModel = new FirstModel
        {
            P1 = new[] { secondModel },
            P2 = new[] { secondModel, secondModelB, secondModelC },
            P51 = new[] { thirdModel }
        };

        var validator = default(FirstValidator);
        var vr = validator.Validate("Enumeration", firstModel);

        Utils.VerifyValidateOptionsResult(vr, 4, "P1[0].P6", "P2[0].P6", "P2[2].P6", "P51[0].Value");
    }

    [Fact]
    public void NullElement()
    {
        var firstModel = new FirstModel
        {
            P1 = new[] { (SecondModel)null! },
        };

        var validator = default(FirstValidator);
        var vr = validator.Validate("Enumeration", firstModel);

        Utils.VerifyValidateOptionsResult(vr, 1, "P1[0]");
    }

    [Fact]
    public void Valid()
    {
        var secondModel = new SecondModel
        {
            P6 = "12345",
        };

        var thirdModelA = new ThirdModel
        {
            Value = 2
        };

        var thirdModelB = new ThirdModel
        {
            Value = 9
        };

        var firstModel = new FirstModel
        {
            P1 = new[] { secondModel },
            P2 = new[] { secondModel },
            P3 = new[] { (SecondModel?)null },
            P4 = new[] { thirdModelA, thirdModelB },
            P5 = new ThirdModel?[] { thirdModelA, default },
            P51 = new ThirdModel?[] { thirdModelB, default }
        };

        var validator = default(FirstValidator);
        Assert.Equal(ValidateOptionsResult.Success, validator.Validate("Enumeration", firstModel));
    }
}
