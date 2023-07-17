// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

#pragma warning disable CA1716
namespace Microsoft.Shared.Data.Validation;
#pragma warning restore CA1716

#if !SHARED_PROJECT
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif

internal static class ValidationContextExtensions
{
    public static string[]? GetMemberName(this ValidationContext? validationContext)
    {
#pragma warning disable S1168 // Empty arrays and collections should be returned instead of null
        return validationContext?.MemberName is { } memberName
            ? new[] { memberName }
            : null;
#pragma warning restore S1168 // Empty arrays and collections should be returned instead of null
    }

    public static string GetDisplayName(this ValidationContext? validationContext)
    {
        return validationContext?.DisplayName ?? string.Empty;
    }
}
