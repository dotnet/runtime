// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.WindowsRuntime.Tests
{
    public class UnixTests
    {
        [Fact]
        public void DefaultInterfaceAttribute_Ctor_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => new DefaultInterfaceAttribute(typeof(int)));
        }

        [Fact]
        public void InterfaceImplementedInVersionAttribute_Ctor_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => new InterfaceImplementedInVersionAttribute(typeof(int), 1, 2, 3, 4));
        }

        [Fact]
        public void ReadOnlyArrayAttribute_Ctor_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => new ReadOnlyArrayAttribute());
        }

        [Fact]
        public void ReturnValueNameAttribute_Ctor_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => new ReturnValueNameAttribute("Name"));
        }

        [Fact]
        public void WriteOnlyArrayAttribute_Ctor_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => new WriteOnlyArrayAttribute());
        }

        [Fact]
        public void EventRegistrationTokenTake_Ctor_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => new EventRegistrationTokenTable<Delegate>());
        }

        [Fact]
        public void WindowsRuntimeMarshal_AddEventHandler_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => WindowsRuntimeMarshal.AddEventHandler(null, null, 0));
        }

        [Fact]
        public void WindowsRuntimeMarshal_RemoveEventHandler_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => WindowsRuntimeMarshal.RemoveEventHandler(null, 0));
        }

        [Fact]
        public void WindowsRuntimeMarshal_RemoveAllEventHandlers_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => WindowsRuntimeMarshal.RemoveAllEventHandlers(null));
        }

        [Fact]
        public void WindowsRuntimeMarshal_StringToHString_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => WindowsRuntimeMarshal.StringToHString(null));
        }

        [Fact]
        public void WindowsRuntimeMarshal_PtrToStringHString_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => WindowsRuntimeMarshal.PtrToStringHString(IntPtr.Zero));
        }

        [Fact]
        public void WindowsRuntimeMarshal_FreeHString_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => WindowsRuntimeMarshal.FreeHString(IntPtr.Zero));
        }

        [Fact]
        public void WindowsRuntimeMarshal_GetActivationFactory_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => WindowsRuntimeMarshal.GetActivationFactory(null));
        }
    }
}
