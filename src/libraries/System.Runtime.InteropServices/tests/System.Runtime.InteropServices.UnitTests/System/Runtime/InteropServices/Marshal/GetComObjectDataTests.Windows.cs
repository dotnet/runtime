// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class GetComObjectDataTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void GetComObjectData_ValidObject_ReturnsExpected()
        {
            var comObject = new ComImportObject();

            Assert.Null(Marshal.GetComObjectData(comObject, "key"));

            Marshal.SetComObjectData(comObject, "key", 1);
            Assert.Equal(1, Marshal.GetComObjectData(comObject, "key"));
            Assert.Null(Marshal.GetComObjectData(comObject, "noSuchKey"));
        }
    }
}
