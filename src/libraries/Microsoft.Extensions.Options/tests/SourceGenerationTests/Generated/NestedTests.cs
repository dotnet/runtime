// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if ROSLYN_4_0_OR_GREATER

using Microsoft.Extensions.Options;
using Nested;
using Xunit;

namespace Microsoft.Gen.OptionsValidation.Test;

public class NestedTests
{
    [Fact]
    public void Invalid()
    {
        var thirdModel = new Container1.ThirdModel
        {
            P6 = "1234",
        };

        var secondModel = new Container1.SecondModel
        {
            P5 = "1234",
        };

        var firstModel = new Container1.FirstModel
        {
            P1 = "1234",
            P2 = secondModel,
            P3 = thirdModel,
            P4 = secondModel,
        };

        var validator = default(Container2.Container3.FirstValidator);
        var vr = validator.Validate("Nested", firstModel);

        Utils.VerifyValidateOptionsResult(vr, 4, "P1", "P2.P5", "P3.P6", "P4.P5");
    }

    [Fact]
    public void Valid()
    {
        var thirdModel = new Container1.ThirdModel
        {
            P6 = "12345",
        };

        var secondModel = new Container1.SecondModel
        {
            P5 = "12345",
        };

        var firstModel = new Container1.FirstModel
        {
            P1 = "12345",
            P2 = secondModel,
            P3 = thirdModel,
            P4 = secondModel,
        };

        var validator = default(Container2.Container3.FirstValidator);
        Assert.Equal(ValidateOptionsResult.Success, validator.Validate("Nested", firstModel));
    }
}

#endif
