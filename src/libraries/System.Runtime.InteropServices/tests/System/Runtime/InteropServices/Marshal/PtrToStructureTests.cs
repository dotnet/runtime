// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class PtrToStructureTests
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct SomeTestStruct
        {
            public int i;
            public String s;
        }

        [Fact]
        public void PtrToStructureTest()
        {
            IntPtr ip;
            SomeTestStruct someTestStruct = new SomeTestStruct();
            
            ip = IntPtr.Zero;
            AssertExtensions.Throws<ArgumentNullException>("ptr", () => Marshal.PtrToStructure(ip, someTestStruct));

            ip = new IntPtr(123);
            AssertExtensions.Throws<ArgumentNullException>("structureType", () => Marshal.PtrToStructure(ip, null));

            ip = new IntPtr(123);
            Assert.Throws<ArgumentException>(() => Marshal.PtrToStructure<SomeTestStruct>(ip, someTestStruct));
        }
    }
}
