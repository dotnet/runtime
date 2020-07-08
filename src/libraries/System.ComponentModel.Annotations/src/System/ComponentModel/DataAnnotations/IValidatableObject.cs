// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.ComponentModel.DataAnnotations
{
    public interface IValidatableObject
    {
        IEnumerable<ValidationResult> Validate(ValidationContext validationContext);
    }
}
