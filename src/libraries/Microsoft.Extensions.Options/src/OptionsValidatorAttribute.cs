// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Triggers the automatic generation of the implementation of <see cref="Microsoft.Extensions.Options.IValidateOptions{T}" /> at compile time.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class OptionsValidatorAttribute : Attribute
    {
    }
}
