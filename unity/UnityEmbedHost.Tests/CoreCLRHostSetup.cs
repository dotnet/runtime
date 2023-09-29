// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using NUnit.Framework;
using Unity.CoreCLRHelpers;

namespace UnityEmbedHost.Tests;

[SetUpFixture]
public class CoreCLRHostSetup
{

    [OneTimeSetUp]
    public void Initialize()
    {
        // Need to use handles to ensure the managed tests that run through native are GC safe now that WorkaroundToGetGCSafety has been removed
        var returnHandlesFromAPI = true;
#if TESTING_UNITY_CORECLR
        CoreCLRHostNative.InitializeNative();
        CoreCLRHost.OverrideNativeOptions(returnHandlesFromAPI: returnHandlesFromAPI);
#else
        CoreCLRHost.InitState(returnHandlesFromAPI: returnHandlesFromAPI);
#endif
    }
}
