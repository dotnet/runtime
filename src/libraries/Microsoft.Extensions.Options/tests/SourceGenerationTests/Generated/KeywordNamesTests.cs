// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using KeywordNames;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Gen.OptionsValidation.Test;

public class KeywordNamesTests
{
    [Fact]
    public void Invalid()
    {
        var model = new FirstModel
        {
            @namespace = "XXX",
            @if = "YYY",
            @event = new @class(),
            @const = new List<@class> { new @class { @string = "XXX" } },
        };

        var validator = new FirstValidator();
        var vr = validator.Validate("KeywordNames", model);

        Utils.VerifyValidateOptionsResult(vr, 4, "namespace", "if", "event", "const");
    }

    [Fact]
    public void Valid()
    {
        var model = new FirstModel
        {
            @namespace = "ABCDE",
            @if = "ABCDE",
            @event = new @class { @string = "ABCDE" },
            @const = new List<@class> { new @class { @string = "ABCDE" } },
        };

        var validator = new FirstValidator();
        Assert.Equal(ValidateOptionsResult.Success, validator.Validate("KeywordNames", model));
    }

    [Fact]
    public void KeywordValidatorTypeInvalid()
    {
        var model = new @class { @string = "XXX" };

        var validator = new KeywordNamesNested.@base.@void();
        var vr = validator.Validate("KeywordValidator", model);

        Utils.VerifyValidateOptionsResult(vr, 1, "string");
    }

    [Fact]
    public void KeywordValidatorTypeValid()
    {
        var model = new @class { @string = "ABCDE" };

        var validator = new KeywordNamesNested.@base.@void();
        Assert.Equal(ValidateOptionsResult.Success, validator.Validate("KeywordValidator", model));
    }

    [Fact]
    public void KeywordNamespaceInvalid()
    {
        var model = new @struct.@interface.SecondModel
        {
            @public = "XXX",
            @return = new @struct.@interface.@sealed { @string = "XXX" },
        };

        var validator = new @struct.@interface.SecondValidator();
        var vr = validator.Validate("KeywordNamespace", model);

        Utils.VerifyValidateOptionsResult(vr, 2, "public", "return");
    }

    [Fact]
    public void KeywordNamespaceValid()
    {
        var model = new @struct.@interface.SecondModel
        {
            @public = "ABCDE",
            @return = new @struct.@interface.@sealed { @string = "ABCDE" },
        };

        var validator = new @struct.@interface.SecondValidator();
        Assert.Equal(ValidateOptionsResult.Success, validator.Validate("KeywordNamespace", model));
    }
}
