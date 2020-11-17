// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Tests;
using Xunit;

namespace System.Tests
{
    public class UnitySerializationHolderTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBinaryFormatterSupported))]
        public void UnitySerializationHolderWithAssemblySingleton()
        {
            const string UnitySerializationHolderAssemblyBase64String = "AAEAAAD/////AQAAAAAAAAAEAQAAAB9TeXN0ZW0uVW5pdHlTZXJpYWxpemF0aW9uSG9sZGVyAwAAAAREYXRhCVVuaXR5VHlwZQxBc3NlbWJseU5hbWUBAAEIBgIAAABLbXNjb3JsaWIsIFZlcnNpb249NC4wLjAuMCwgQ3VsdHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj1iNzdhNWM1NjE5MzRlMDg5BgAAAAkCAAAACw==";
            SerializationException se = AssertExtensions.Throws<SerializationException>(() =>
              BinaryFormatterHelpers.FromBase64String(UnitySerializationHolderAssemblyBase64String));
            Assert.IsAssignableFrom<ArgumentException>(se.InnerException);
        }
    }
}
