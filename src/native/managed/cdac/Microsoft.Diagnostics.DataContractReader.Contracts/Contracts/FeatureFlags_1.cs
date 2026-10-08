// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct FeatureFlags_1 : IFeatureFlags
{
    private readonly Target _target;

    public FeatureFlags_1(Target target)
    {
        _target = target;
    }

    bool IFeatureFlags.IsEnabled(RuntimeFeature feature)
    {
        string? globalName = feature switch
        {
            RuntimeFeature.COMInterop => Constants.Globals.FeatureCOMInterop,
            RuntimeFeature.ComWrappers => Constants.Globals.FeatureComWrappers,
            RuntimeFeature.ObjCMarshal => Constants.Globals.FeatureObjCMarshal,
            RuntimeFeature.JavaMarshal => Constants.Globals.FeatureJavaMarshal,
            RuntimeFeature.OnStackReplacement => Constants.Globals.FeatureOnStackReplacement,
            RuntimeFeature.PortableEntrypoints => Constants.Globals.FeaturePortableEntrypoints,
            RuntimeFeature.Webcil => Constants.Globals.FeatureWebcil,
            _ => null,
        };
        return globalName is not null
            && _target.TryReadGlobal<byte>(globalName, out byte? value)
            && value != 0;
    }
}
