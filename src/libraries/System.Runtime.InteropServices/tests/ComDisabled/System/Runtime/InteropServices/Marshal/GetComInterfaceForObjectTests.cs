// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class GetComInterfaceForObjectTests
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetComInterfaceForObject_GenericWithValidClass_ReturnsExpected()
        {
            var o = new ClassWithInterface();
            Assert.Throws<NotSupportedException>(() => Marshal.GetComInterfaceForObject<ClassWithInterface, INonGenericInterface>(o));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetComInterfaceForObject_GenericWithValidStruct_ReturnsExpected()
        {
            var o = new StructWithInterface();
            Assert.Throws<NotSupportedException>(() => Marshal.GetComInterfaceForObject<StructWithInterface, INonGenericInterface>(o));
        }

        public class ClassWithInterface : INonGenericInterface { }
        public struct StructWithInterface : INonGenericInterface { }

    }
}
