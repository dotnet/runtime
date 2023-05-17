// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharedTypes.ComInterfaces;
using Xunit;

namespace ComInterfaceGenerator.Tests
{
    public unsafe partial class StringMarshallingTests
    {
        [LibraryImport(NativeExportsNE.NativeExportsNE_Binary, EntryPoint = "new_utf8_marshalling")]
        public static partial void* NewIUtf8Marshalling();

        [LibraryImport(NativeExportsNE.NativeExportsNE_Binary, EntryPoint = "new_utf16_marshalling")]
        public static partial void* NewIUtf16Marshalling();

        [GeneratedComClass]
        internal partial class Utf8MarshalledClass : IUTF8Marshalling
        {
            string _data = "Hello, World!";

            public string GetString() => _data;
            public void SetString(string value) => _data = value;
        }

        [GeneratedComClass]
        internal partial class Utf16MarshalledClass : IUTF16Marshalling
        {
            string _data = "Hello, World!";

            public string GetString() => _data;
            public void SetString(string value) => _data = value;
        }

        [GeneratedComClass]
        internal partial class CustomUtf16MarshalledClass : ICustomStringMarshallingUtf16
        {
            string _data = "Hello, World!";

            public string GetString() => _data;
            public void SetString(string value) => _data = value;
        }

        [Fact]
        public void ValidateStringMarshallingRCW()
        {
            var cw = new StrategyBasedComWrappers();
            var utf8 = NewIUtf8Marshalling();
            IUTF8Marshalling obj8 = (IUTF8Marshalling)cw.GetOrCreateObjectForComInstance((nint)utf8, CreateObjectFlags.None);
            string value = obj8.GetString();
            Assert.Equal("Hello, World!", value);
            obj8.SetString("TestString");
            value = obj8.GetString();
            Assert.Equal("TestString", value);

            var utf16 = NewIUtf16Marshalling();
            IUTF16Marshalling obj16 = (IUTF16Marshalling)cw.GetOrCreateObjectForComInstance((nint)utf16, CreateObjectFlags.None);
            Assert.Equal("Hello, World!", obj16.GetString());
            obj16.SetString("TestString");
            Assert.Equal("TestString", obj16.GetString());

            var utf16custom = NewIUtf16Marshalling();
            ICustomStringMarshallingUtf16 objCustom = (ICustomStringMarshallingUtf16)cw.GetOrCreateObjectForComInstance((nint)utf16custom, CreateObjectFlags.None);
            Assert.Equal("Hello, World!", objCustom.GetString());
            objCustom.SetString("TestString");
            Assert.Equal("TestString", objCustom.GetString());
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/85795", TargetFrameworkMonikers.Any)]
        public void RcwToCcw()
        {
            var cw = new StrategyBasedComWrappers();

            var utf8 = new Utf8MarshalledClass();
            var utf8ComInstance = cw.GetOrCreateComInterfaceForObject(utf8, CreateComInterfaceFlags.None);
            var utf8ComObject = (IUTF8Marshalling)cw.GetOrCreateObjectForComInstance(utf8ComInstance, CreateObjectFlags.None);
            Assert.Equal(utf8.GetString(), utf8ComObject.GetString());
            utf8.SetString("Set from CLR object");
            Assert.Equal(utf8.GetString(), utf8ComObject.GetString());
            utf8ComObject.SetString("Set from COM object");
            Assert.Equal(utf8.GetString(), utf8ComObject.GetString());

            var utf16 = new Utf16MarshalledClass();
            var utf16ComInstance = cw.GetOrCreateComInterfaceForObject(utf16, CreateComInterfaceFlags.None);
            var utf16ComObject = (IUTF16Marshalling)cw.GetOrCreateObjectForComInstance(utf16ComInstance, CreateObjectFlags.None);
            Assert.Equal(utf16.GetString(), utf16ComObject.GetString());
            utf16.SetString("Set from CLR object");
            Assert.Equal(utf16.GetString(), utf16ComObject.GetString());
            utf16ComObject.SetString("Set from COM object");
            Assert.Equal(utf16.GetString(), utf16ComObject.GetString());

            var customUtf16 = new CustomUtf16MarshalledClass();
            var customUtf16ComInstance = cw.GetOrCreateComInterfaceForObject(customUtf16, CreateComInterfaceFlags.None);
            var customUtf16ComObject = (ICustomStringMarshallingUtf16)cw.GetOrCreateObjectForComInstance(customUtf16ComInstance, CreateObjectFlags.None);
            Assert.Equal(customUtf16.GetString(), customUtf16ComObject.GetString());
            customUtf16.SetString("Set from CLR object");
            Assert.Equal(customUtf16.GetString(), customUtf16ComObject.GetString());
            customUtf16ComObject.SetString("Set from COM object");
            Assert.Equal(customUtf16.GetString(), customUtf16ComObject.GetString());
        }
    }
}
