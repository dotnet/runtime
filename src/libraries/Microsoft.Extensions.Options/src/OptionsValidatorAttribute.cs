// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Triggers the automatic generation of validation members for <see cref="Microsoft.Extensions.Options.IValidateOptions{T}" /> at compile time.
    /// </summary>
    /// <remarks>
    /// When the annotated partial type implements
    /// <see cref="Microsoft.Extensions.Options.IAsyncValidateOptions{T}" /> for an options type,
    /// the generator emits both the synchronous <c>Validate</c> method and the asynchronous <c>ValidateAsync</c>
    /// method for that type.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class OptionsValidatorAttribute : Attribute
    {
    }
}
