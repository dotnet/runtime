// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Gen.OptionsValidation.Test;

public class NoNamespaceTests
{
    [Fact]
    public void Invalid()
    {
        var thirdModel = new ThirdModelNoNamespace
        {
            P5 = "1234",
        };

        var secondModel = new SecondModelNoNamespace
        {
            P4 = "1234",
        };

        var firstModel = new FirstModelNoNamespace
        {
            P1 = "1234",
            P2 = secondModel,
            P3 = thirdModel,
        };

        var validator = new FirstValidatorNoNamespace();
        var vr = validator.Validate("NoNamespace", firstModel);

        Utils.VerifyValidateOptionsResult(vr, 3, "P1", "P2.P4", "P3.P5");
    }

    [Fact]
    public void Valid()
    {
        var thirdModel = new ThirdModelNoNamespace
        {
            P5 = "12345",
        };

        var secondModel = new SecondModelNoNamespace
        {
            P4 = "12345",
        };

        var firstModel = new FirstModelNoNamespace
        {
            P1 = "12345",
            P2 = secondModel,
            P3 = thirdModel,
        };

        var validator = new FirstValidatorNoNamespace();
        Assert.Equal(ValidateOptionsResult.Success, validator.Validate("NoNamespace", firstModel));
    }
}
