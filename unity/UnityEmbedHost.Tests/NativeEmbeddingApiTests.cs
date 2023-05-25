// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using NUnit.Framework;
using Unity.CoreCLRHelpers;

namespace UnityEmbedHost.Tests;

#if !TESTING_UNITY_CORECLR
[Ignore("This suite can only be ran against unity coreclr")]
#endif
[TestFixture]
public class NativeEmbeddingApiTests : BaseEmbeddingApiTests
{
    internal override ICoreCLRHostWrapper ClrHost { get; } = new CoreCLRHostNativeWrappers();

    [Test]
    [Category("ExcludeFromNormalTestRun")]
    public void ExceptionDoesNotCauseACrash()
    {
        var fieldInfo = typeof(Cat).GetField(nameof(Cat.EarCount));
        // Pass the wrong type to cause an exception
        var result = ClrHost.field_get_object(typeof(Mammal), fieldInfo!.FieldHandle);
        Assert.IsNull(result);
    }
}
