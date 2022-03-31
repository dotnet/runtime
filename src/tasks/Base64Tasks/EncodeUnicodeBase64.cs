// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
namespace Base64Tasks;

public class EncodeUnicodeBase64 : Task
{
    [Required]
    public string? Input { get; set; }

    [Output]
    public string? Base64 { get; set; }

    public override bool Execute()
    {
        Base64 = Convert.ToBase64String(Encoding.Unicode.GetBytes(Input!));
        return true;
    }
}
