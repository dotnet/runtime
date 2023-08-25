// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;
using SelfValidation;
using Xunit;

namespace Microsoft.Gen.OptionsValidation.Test;

public class SelfValidationTests
{
    [Fact]
    public void Invalid()
    {
        var firstModel = new FirstModel
        {
            P1 = "1234",
        };

        var validator = default(FirstValidator);
        var vr = validator.Validate("SelfValidation", firstModel);

        Utils.VerifyValidateOptionsResult(vr, 1, "P1");
    }

    [Fact]
    public void Valid()
    {
        var firstModel = new FirstModel
        {
            P1 = "12345",
        };

        var validator = default(FirstValidator);
        Assert.Equal(ValidateOptionsResult.Success, validator.Validate("SelfValidation", firstModel));
    }
}
