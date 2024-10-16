// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Linq
{
    /// <summary>
    /// Defines options for how iterators behave.
    /// </summary>
    internal static class IteratorOptions
    {
        [FeatureSwitchDefinition("System.Linq.ValueTypeTrimFriendlySelect")]
        public static bool ValueTypeTrimFriendlySelect { get; } = AppContext.TryGetSwitch("System.Linq.ValueTypeTrimFriendlySelect", out bool isEnabled) ? isEnabled : true;
    }
}
