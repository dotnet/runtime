// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization.Formatters.Tests;
using Xunit;

namespace System.Tests
{
    public class UnitySerializationHolderTests
    {
        [Fact]
        public void UnitySerializationHolderWithAssemblySingleton()
        {
            const string UnitySerializationHolderAssemblyBase64String = "AAEAAAD/////AQAAAAAAAAAEAQAAAB9TeXN0ZW0uVW5pdHlTZXJpYWxpemF0aW9uSG9sZGVyAwAAAAREYXRhCVVuaXR5VHlwZQxBc3NlbWJseU5hbWUBAAEIBgIAAABLbXNjb3JsaWIsIFZlcnNpb249NC4wLjAuMCwgQ3VsdHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj1iNzdhNWM1NjE5MzRlMDg5BgAAAAkCAAAACw==";
            AssertExtensions.ThrowsIf<ArgumentException>(!PlatformDetection.IsFullFramework,
                () => BinaryFormatterHelpers.FromBase64String(UnitySerializationHolderAssemblyBase64String));
        }
    }
}
