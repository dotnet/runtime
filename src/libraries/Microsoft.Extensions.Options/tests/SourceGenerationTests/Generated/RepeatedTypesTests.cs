// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;
using RepeatedTypes;
using Xunit;

namespace Microsoft.Gen.OptionsValidation.Test;

public class RepeatedTypesTests
{
    [Fact]
    public void Invalid()
    {
        var thirdModel = new ThirdModel
        {
            P5 = "1234",
        };

        var secondModel = new SecondModel
        {
            P4 = thirdModel,
        };

        var firstModel = new FirstModel
        {
            P1 = secondModel,
            P2 = secondModel,
            P3 = thirdModel,
        };

        var validator = new FirstValidator();
        var vr = validator.Validate("RepeatedTypes", firstModel);

        Utils.VerifyValidateOptionsResult(vr, 3, "P1.P4.P5", "P2.P4.P5", "P3.P5");
    }

    [Fact]
    public void Valid()
    {
        var thirdModel = new ThirdModel
        {
            P5 = "12345",
        };

        var secondModel = new SecondModel
        {
            P4 = thirdModel,
        };

        var firstModel = new FirstModel
        {
            P1 = secondModel,
            P2 = secondModel,
            P3 = thirdModel,
        };

        var validator = new FirstValidator();
        Assert.Equal(ValidateOptionsResult.Success, validator.Validate("RepeatedTypes", firstModel));
    }
}
