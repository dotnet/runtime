// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System;

namespace Microsoft.Extensions.Options.Tests
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class FromAttribute : ValidationAttribute
    {
        public string Accepted { get; set; }

        public override bool IsValid(object value)
            => value == null || value.ToString() == Accepted;
    }
}
