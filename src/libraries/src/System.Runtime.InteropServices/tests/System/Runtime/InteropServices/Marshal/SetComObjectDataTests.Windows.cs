// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class SetComObjectDataTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Not approved COM object for app")]
        public void SetComObjectData_NonNullValue_Sucesss()
        {
            var comObject = new ComImportObject();

            Assert.True(Marshal.SetComObjectData(comObject, "key", 1));
            Assert.Equal(1, Marshal.GetComObjectData(comObject, "key"));

            Assert.False(Marshal.SetComObjectData(comObject, "key", 2));
            Assert.Equal(1, Marshal.GetComObjectData(comObject, "key"));

            Assert.True(Marshal.SetComObjectData(comObject, "otherKey", 2));
            Assert.Equal(2, Marshal.GetComObjectData(comObject, "otherKey"));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Not approved COM object for app")]
        public void SetComObjectData_NullValue_Sucesss()
        {
            var comObject = new ComImportObject();

            Assert.True(Marshal.SetComObjectData(comObject, "key", null));
            Assert.Null(Marshal.GetComObjectData(comObject, "key"));

            Assert.True(Marshal.SetComObjectData(comObject, "key", null));
            Assert.Null(Marshal.GetComObjectData(comObject, "key"));

            Assert.True(Marshal.SetComObjectData(comObject, "key", 1));
            Assert.Equal(1, Marshal.GetComObjectData(comObject, "key"));

            Assert.True(Marshal.SetComObjectData(comObject, "otherKey", null));
            Assert.Null(Marshal.GetComObjectData(comObject, "otherKey"));
        }
    }
}
