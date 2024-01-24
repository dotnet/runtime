// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
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
            P2 = new SecondModel
            {
                P3 = "5678"
            }
        };

        var validator = default(FirstValidator);
        var vr = validator.Validate("SelfValidation", firstModel);

        Utils.VerifyValidateOptionsResult(vr, 2, "P3", "P1");
    }

    [Fact]
    public void Valid()
    {
        var firstModel = new FirstModel
        {
            P1 = "12345",
            P2 = new SecondModel
            {
                P3 = "67890"
            }
        };

        var validator = default(FirstValidator);
        Assert.Equal(ValidateOptionsResult.Success, validator.Validate("SelfValidation", firstModel));
    }

    [Fact]
    public void SelfValidateOptionsTest()
    {
        SelfValidateOptions validator = new();
        ValidateOptionsResult vr = validator.Validate("SelfValidation", validator);
        Assert.Equal(1, vr.Failures.Count());
        Assert.Equal($"Display: SelfValidation.Validate, Member: Validate", vr.Failures.First());
    }
}
