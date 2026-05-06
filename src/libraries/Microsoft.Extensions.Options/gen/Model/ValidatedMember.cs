// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.Options.Generators
{
    internal sealed record class ValidatedMember(
        string Name,
        List<ValidationAttributeInfo> ValidationAttributes,
        string? TransValidatorType,
        bool TransValidateTypeIsSynthetic,
        string? EnumerationValidatorType,
        bool EnumerationValidatorTypeIsSynthetic,
        bool IsNullable,
        bool IsValueType,
        bool EnumeratedIsNullable,
        bool EnumeratedIsValueType,
        bool EnumeratedMayBeNull);
}
