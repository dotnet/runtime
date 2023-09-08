// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using CustomAttr;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Gen.OptionsValidation.Test;

public class CustomAttrTests
{
    [Fact]
    public void Invalid()
    {
        var firstModel = new FirstModel
        {
            P1 = 'a',
            P2 = 'x',
        };

        var validator = new FirstValidator();
        var vr = validator.Validate("CustomAttr", firstModel);

        Utils.VerifyValidateOptionsResult(vr, 2, "P1", "P2");
    }

    [Fact]
    public void Valid()
    {
        var firstModel = new FirstModel
        {
            P1 = 'A',
            P2 = 'A',
        };

        var validator = new FirstValidator();
        Assert.Equal(ValidateOptionsResult.Success, validator.Validate("CustomAttr", firstModel));
    }
}
