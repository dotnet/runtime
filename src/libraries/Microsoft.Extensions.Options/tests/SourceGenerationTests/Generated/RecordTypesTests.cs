// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if ROSLYN_4_0_OR_GREATER

using Microsoft.Extensions.Options;
using RecordTypes;
using Xunit;

namespace Microsoft.Gen.OptionsValidation.Test;

public class RecordTypesTests
{
    [Fact]
    public void Invalid()
    {
        var thirdModel = new ThirdModel
        {
            P6 = "1234",
        };

        var secondModel = new SecondModel
        {
            P5 = "1234",
        };

        var firstModel = new FirstModel
        {
            P1 = "1234",
            P2 = secondModel,
            P3 = secondModel,
            P4 = thirdModel,
        };

        var validator = default(FirstValidator);
        var vr = validator.Validate("RecordTypes", firstModel);

        Utils.VerifyValidateOptionsResult(vr, 4, "P1", "P2.P5", "P3.P5", "P4.P6");
    }

    [Fact]
    public void Valid()
    {
        var thirdModel = new ThirdModel
        {
            P6 = "12345",
        };

        var secondModel = new SecondModel
        {
            P5 = "12345",
        };

        var firstModel = new FirstModel
        {
            P1 = "12345",
            P2 = secondModel,
            P3 = secondModel,
            P4 = thirdModel,
        };

        var validator = default(FirstValidator);
        Assert.Equal(ValidateOptionsResult.Success, validator.Validate("RecordTypes", firstModel));
    }
}

#endif
