// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FunnyStrings;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Gen.OptionsValidation.Test;

public class FunnyStringsTests
{
    [Fact]
    public void Invalid()
    {
        var firstModel = new FirstModel
        {
            P1 = "XXX",
        };

        var validator = default(FirstValidator);
        var vr = validator.Validate("FunnyStrings", firstModel);

        Utils.VerifyValidateOptionsResult(vr, 1, "P1");
    }

    [Fact]
    public void Valid()
    {
        var firstModel = new FirstModel
        {
            P1 = "\"\r\n\\",
        };

        var validator = default(FirstValidator);
        Assert.Equal(ValidateOptionsResult.Success, validator.Validate("FunnyStrings", firstModel));
    }
}
