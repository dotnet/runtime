// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Generics;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Gen.OptionsValidation.Test;

public class GenericsTests
{
    [Fact]
    public void Invalid()
    {
        var secondModel = new SecondModel
        {
            P4 = "1234",
        };

        var firstModel = new FirstModel<int>
        {
            P1 = "1234",
            P3 = secondModel,
        };

        var validator = new FirstValidator<int>();
        var vr = validator.Validate("Generics", firstModel);

        Utils.VerifyValidateOptionsResult(vr, 2, "P1", "P3.P4");
    }

    [Fact]
    public void Valid()
    {
        var secondModel = new SecondModel
        {
            P4 = "12345",
        };

        var firstModel = new FirstModel<int>
        {
            P1 = "12345",
            P3 = secondModel,
        };

        var validator = new FirstValidator<int>();
        Assert.Equal(ValidateOptionsResult.Success, validator.Validate("Generics", firstModel));
    }
}
