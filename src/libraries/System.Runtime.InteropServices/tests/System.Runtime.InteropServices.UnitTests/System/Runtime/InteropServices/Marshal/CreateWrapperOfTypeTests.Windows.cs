// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class CreateWrapperOfTypeTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void CreateWrapperOfType_SameType_ReturnsSameInstance()
        {
            var comObject = new ComImportObject();
            Assert.Same(comObject, Marshal.CreateWrapperOfType(comObject, typeof(ComImportObject)));
            Assert.Same(comObject, Marshal.CreateWrapperOfType<ComImportObject, ComImportObject>(comObject));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void CreateWrapperOfType_NullObject_ReturnsNull()
        {
            Assert.Null(Marshal.CreateWrapperOfType(null, typeof(ComImportObject)));
            Assert.Null(Marshal.CreateWrapperOfType<ComImportObject, ComImportObject>(null));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        [InlineData(typeof(int))]
        [InlineData(typeof(GenericSubComImportObject<string>))]
        [InlineData(typeof(GenericSubComImportObject<>))]
        public void CreateWrapperOfType_InvalidComObjectType_ThrowsArgumentException(Type t)
        {
            AssertExtensions.Throws<ArgumentException>("t", () => Marshal.CreateWrapperOfType(new ComImportObject(), t));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void CreateWrappedOfType_ObjectNotComObject_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("o", () => Marshal.CreateWrapperOfType(10, typeof(ComImportObject)));
            AssertExtensions.Throws<ArgumentException>("o", () => Marshal.CreateWrapperOfType<int, ComImportObject>(10));
        }
    }
}
