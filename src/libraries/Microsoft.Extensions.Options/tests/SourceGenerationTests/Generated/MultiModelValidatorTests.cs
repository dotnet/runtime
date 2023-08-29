// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;
using MultiModelValidator;
using Xunit;

namespace Microsoft.Gen.OptionsValidation.Test;

public class MultiModelValidatorTests
{
    [Fact]
    public void Invalid()
    {
        var secondModel = new SecondModel
        {
            P3 = "1234",
        };

        var firstModel = new FirstModel
        {
            P1 = "1234",
            P2 = secondModel,
        };

        var validator = default(MultiValidator);
        var vr = validator.Validate("MultiModelValidator", firstModel);

        Utils.VerifyValidateOptionsResult(vr, 2, "P1", "P2.P3");
    }

    [Fact]
    public void Valid()
    {
        var secondModel = new SecondModel
        {
            P3 = "12345",
        };

        var firstModel = new FirstModel
        {
            P1 = "12345",
            P2 = secondModel,
        };

        var validator = default(MultiValidator);
        Assert.Equal(ValidateOptionsResult.Success, validator.Validate("MultiModelValidator", firstModel));
    }
}
