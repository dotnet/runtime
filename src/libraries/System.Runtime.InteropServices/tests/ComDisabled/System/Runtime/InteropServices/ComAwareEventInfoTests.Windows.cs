// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class ComAwareEventInfoTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void AddEventHandler_ComObjectWithMultipleComEventInterfaceAttribute_ThrowsAmbiguousMatchException()
        {
            Assert.Throws<NotSupportedException>(() => new ComImportObject());

        }

    }
}
