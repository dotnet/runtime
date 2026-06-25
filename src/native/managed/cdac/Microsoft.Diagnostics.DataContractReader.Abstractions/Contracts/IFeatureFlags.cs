// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

/// <summary>
/// Feature flags that a runtime may or may not support.
/// </summary>
public enum RuntimeFeature
{
    COMInterop,
    ComWrappers,
    ObjCMarshal,
    JavaMarshal,
    OnStackReplacement,
    PortableEntrypoints,
    Webcil,
}

public interface IFeatureFlags : IContract
{
    static string IContract.Name { get; } = nameof(FeatureFlags);

    /// <summary>
    /// Returns <see langword="true"/> if the target runtime has the given feature enabled.
    /// </summary>
    bool IsEnabled(RuntimeFeature feature) => throw new NotImplementedException();
}

public readonly struct FeatureFlags : IFeatureFlags { }
