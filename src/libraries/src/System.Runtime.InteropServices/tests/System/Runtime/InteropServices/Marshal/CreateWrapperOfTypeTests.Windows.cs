// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class CreateWrapperOfTypeTests
    {
        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework, "Marshal.CreateWrapperOfType is not supported on .NET Core.")]
        public void CreateWrapperOfType_NonGenericHasNoWrapper_ReturnsExpected()
        {
            var comObject = new ComImportObject();
            object wrapper = Marshal.CreateWrapperOfType(comObject, typeof(WrapperComImportObject));
            Assert.Same(wrapper, Marshal.GetComObjectData(comObject, typeof(WrapperComImportObject)));
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework, "Marshal.CreateWrapperOfType is not supported on .NET Core.")]
        public void CreateWrapperOfType_NonGenericHasNoWrapperWithInterfaces_ReturnsExpected()
        {
            var comObject = new ComImportObject();
            object wrapper = Marshal.CreateWrapperOfType(comObject, typeof(HasNonCOMInterfaces));
            Assert.Same(wrapper, Marshal.GetComObjectData(comObject, typeof(HasNonCOMInterfaces)));
        }

        [ComImport]
        [Guid("927971f5-0939-11d1-8be1-00c04fd8d503")]
        public class HasNonCOMInterfaces : NonGenericInterface, GenericInterface<string> { }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework, "Marshal.CreateWrapperOfType is not supported on .NET Core.")]
        public void CreateWrapperOfType_GenericHasNoWrapper_ReturnsExpected()
        {
            var comObject = new ComImportObject();
            object wrapper = Marshal.CreateWrapperOfType<ComImportObject, HasNonCOMInterfaces>(comObject);
            Assert.Same(wrapper, Marshal.GetComObjectData(comObject, typeof(HasNonCOMInterfaces)));
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework, "Marshal.CreateWrapperOfType is not supported on .NET Core.")]
        public void CreateWrapperOfType_GenericHasNoWrapperWithInterfaces_ReturnsExpected()
        {
            var comObject = new ComImportObject();
            object wrapper = Marshal.CreateWrapperOfType<ComImportObject, HasNonCOMInterfaces>(comObject);
            Assert.Same(wrapper, Marshal.GetComObjectData(comObject, typeof(HasNonCOMInterfaces)));
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework, "Marshal.CreateWrapperOfType is not supported on .NET Core.")]
        public void CreateWrapperOfType_AlreadyHasWrapper_ReturnsExpected()
        {
            var comObject = new ComImportObject();
            Marshal.SetComObjectData(comObject, typeof(WrapperComImportObject), "data");

            Assert.Same("data", Marshal.CreateWrapperOfType(comObject, typeof(WrapperComImportObject)));
        }

        [ComImport]
        [Guid("927971f5-0939-11d1-8be1-00c04fd8d503")]
        public class WrapperComImportObject { }
        
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Not approved COM object for app")]
        public void CreateWrapperOfType_SameType_ReturnsSameInstance()
        {
            var comObject = new ComImportObject();
            Assert.Same(comObject, Marshal.CreateWrapperOfType(comObject, typeof(ComImportObject)));
            Assert.Same(comObject, Marshal.CreateWrapperOfType<ComImportObject, ComImportObject>(comObject));
        }

        [Fact]
        public void CreateWrapperOfType_NullObject_ReturnsNull()
        {
            Assert.Null(Marshal.CreateWrapperOfType(null, typeof(ComImportObject)));
            Assert.Null(Marshal.CreateWrapperOfType<ComImportObject, ComImportObject>(null));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [InlineData(typeof(int))]
        [InlineData(typeof(GenericSubComImportObject<string>))]
        [InlineData(typeof(GenericSubComImportObject<>))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Not approved COM object for app")]
        public void CreateWrapperOfType_InvalidComObjectType_ThrowsArgumentException(Type t)
        {
            AssertExtensions.Throws<ArgumentException>("t", () => Marshal.CreateWrapperOfType(new ComImportObject(), t));
        }

        [Fact]
        public void CreateWrappedOfType_ObjectNotComObject_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("o", () => Marshal.CreateWrapperOfType(10, typeof(ComImportObject)));
            AssertExtensions.Throws<ArgumentException>("o", () => Marshal.CreateWrapperOfType<int, ComImportObject>(10));
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework, "Marshal.CreateWrapperOfType is not supported on .NET Core.")]
        public void CreateWrapperOfType_AlreadyHasWrapperOfBadType_ThrowsInvalidCastException()
        {
            var comObject = new ComImportObject();
            Marshal.SetComObjectData(comObject, typeof(WrapperComImportObject), "data");

            Assert.Throws<InvalidCastException>(() => Marshal.CreateWrapperOfType<ComImportObject, WrapperComImportObject>(comObject));
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework, "Marshal.CreateWrapperOfType is not supported on .NET Core.")]
        public void CreateWrapperOfType_CantAssignInterfaces_ThrowsInvalidCastException()
        {
            var comObject = new ComImportObject();
            Assert.Throws<InvalidCastException>(() => Marshal.CreateWrapperOfType(comObject, typeof(HasCOMInterfaces)));
        }

        public class HasCOMInterfaces : ComImportObject, IComImportObject { }
    }
}
