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
#if TESTING_UNITY_CORECLR
        CoreCLRHostNative.InitializeNative();
#else
        CoreCLRHost.InitState();
#endif
    }
}
