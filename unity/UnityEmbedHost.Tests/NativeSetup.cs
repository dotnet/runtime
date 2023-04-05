// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using NUnit.Framework;
using Unity.CoreCLRHelpers;

namespace UnityEmbedHost.Tests;

#if TESTING_UNITY_CORECLR
[SetUpFixture]
#endif
public class NativeSetup
{
#if TESTING_UNITY_CORECLR
    [OneTimeSetUp]
#endif
    public void Initialize()
    {
        CoreCLRHostNative.InitializeNative();
    }
}
