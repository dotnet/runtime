// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Tests
{
    public static partial class ModuleTests
    {
        [Fact]
        public static void CannotDoWithReflectionOnlyModule()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                // Storing as ICustomAttributeProvider so we don't accidentally pick up the CustomAttributeExtensions extension methods.
                ICustomAttributeProvider icp = lc.LoadFromByteArray(TestData.s_SimpleAssemblyImage).ManifestModule;

                Assert.Throws<InvalidOperationException>(() => icp.GetCustomAttributes(inherit: false));
                Assert.Throws<InvalidOperationException>(() => icp.GetCustomAttributes(null, inherit: false));
                Assert.Throws<InvalidOperationException>(() => icp.IsDefined(null, inherit: false));
            }
        }
    }
}
