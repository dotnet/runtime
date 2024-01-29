// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;
using ValueTypes;
using Xunit;

namespace Microsoft.Gen.OptionsValidation.Test;

public class ValueTypesTests
{
    [Fact]
    public void Invalid()
    {
        var secondModel = new SecondModel
        {
            P4 = "1234",
        };

        var firstModel = new FirstModel
        {
            P1 = "1234",
            P3 = secondModel,
            P2 = secondModel,
            P4 = default,
        };

        var validator = default(FirstValidator);
        var vr = validator.Validate("ValueTypes", firstModel);

        Utils.VerifyValidateOptionsResult(vr, 3, "P1", "P2.P4", "P3.P4");
    }

    [Fact]
    public void Valid()
    {
        var secondModel = new SecondModel
        {
            P4 = "12345",
        };

        var firstModel = new FirstModel
        {
            P1 = "12345",
            P3 = secondModel,
            P2 = secondModel,
            P4 = default,
        };

        var validator = default(FirstValidator);
        Assert.Equal(ValidateOptionsResult.Success, validator.Validate("ValueTypes", firstModel));
    }
}
