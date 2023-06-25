// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Fields;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Gen.OptionsValidation.Test;

public class FieldTests
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
            P4 = "1234",
        };

        var firstModel = new FirstModel
        {
            P1 = "1234",
            P2 = secondModel,
            P3 = thirdModel,
        };

        var validator = default(FirstValidator);
        var vr = validator.Validate("Fields", firstModel);

        Utils.VerifyValidateOptionsResult(vr, 3, "P1", "P2.P4", "P3.P5");
    }

    [Fact]
    public void Valid()
    {
        var thirdModel = new ThirdModel
        {
            P5 = "12345",
            P6 = 1
        };

        var secondModel = new SecondModel
        {
            P4 = "12345",
        };

        var firstModel = new FirstModel
        {
            P1 = "12345",
            P2 = secondModel,
            P3 = thirdModel,
        };

        var validator = default(FirstValidator);
        Assert.Equal(ValidateOptionsResult.Success, validator.Validate("Fields", firstModel));
    }
}
