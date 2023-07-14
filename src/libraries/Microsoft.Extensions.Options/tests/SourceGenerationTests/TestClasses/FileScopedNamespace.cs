// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace FileScopedNamespace;

#pragma warning disable SA1649 // File name should match first type name

public class FirstModel
{
    [Required]
    [MinLength(5)]
    public string P1 = string.Empty;
}

[OptionsValidator]
public partial struct FirstValidator : IValidateOptions<FirstModel>
{
}
