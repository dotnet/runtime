// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Unity.CoreCLRHelpers;

public enum ManagedWrapperOptions
{
    /// <summary>
    /// Some types are converted to easier to use types in the C# wrappers.
    /// </summary>
    Default,

    /// <summary>
    /// Exclude from the managed wrappers
    /// </summary>
    Exclude,

    /// <summary>
    /// The C# wrappers will use the same type as the attribute owners type.  Logic to generate conversions to easier to use types is disabled
    /// </summary>
    AsIs,

    /// <summary>
    /// A custom type name is provided for the C# wrappers
    /// </summary>
    Custom
}
