// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class FinalReleaseComObjectTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void FinalReleaseComObject_ValidComObject_Success()
        {
            var comObject = new ComImportObject();
            Assert.Equal(0, Marshal.FinalReleaseComObject(comObject));
        }
    }
}
