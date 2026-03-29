// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Security.Cryptography.Tests;
using Xunit.Sdk;

// Apply to all tests in the assembly when compiled for Android.
[assembly: AndroidGarbageCollect]

namespace System.Security.Cryptography.Tests
{
    /// <summary>
    /// Forces a garbage collection after each test method on Android.
    /// On Android, crypto objects wrap JNI global refs backed by Java/BoringSSL
    /// native memory that the .NET GC cannot see.  Forcing a collection after
    /// each test ensures SafeHandles are finalized and JNI roots are removed
    /// promptly, giving the ART GC a chance to reclaim the native memory
    /// before the low-memory killer intervenes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class AndroidGarbageCollectAttribute : BeforeAfterTestAttribute
    {
        public override void After(MethodInfo methodUnderTest)
        {
            if (OperatingSystem.IsAndroid())
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Interop.AndroidCrypto.TriggerJavaGarbageCollection();
            }
        }
    }
}
