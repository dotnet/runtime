// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Diagnostics.Tests
{
    [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "StackFrame is not supported in uapaot.")]
    public class StackFrameExtensionsTests
    {
        public static IEnumerable<object[]> StackFrame_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new StackFrame() };
            yield return new object[] { new StackFrame(int.MaxValue) };
        }

        [Theory]
        [MemberData(nameof(StackFrame_TestData))]
        public void HasNativeImage_StackFrame_ReturnsFalse(StackFrame stackFrame)
        {
            Assert.False(stackFrame.HasNativeImage());
        }

        [Theory]
        [MemberData(nameof(StackFrame_TestData))]
        public void GetNativeIP_StackFrame_ReturnsZero(StackFrame stackFrame)
        {
            Assert.Equal(IntPtr.Zero, stackFrame.GetNativeIP());
        }

        [Theory]
        [MemberData(nameof(StackFrame_TestData))]
        public void GetNativeImageBase_StackFrame_ReturnsZero(StackFrame stackFrame)
        {
            Assert.Equal(IntPtr.Zero, stackFrame.GetNativeImageBase());
        }

        public static IEnumerable<object[]> HasMethod_TestData()
        {
            yield return new object[] { new StackFrame(), true };
            yield return new object[] { new StackFrame(int.MaxValue), false };
        }

        [Theory]
        [MemberData(nameof(HasMethod_TestData))]
        public void HasILOffset_Invoke_ReturnsExpected(StackFrame stackFrame, bool expected)
        {
            Assert.Equal(expected, stackFrame.HasILOffset());
        }

        [Fact]
        public void HasILOffset_NullStackFrame_ThrowsNullReferenceException()
        {
            Assert.Throws<NullReferenceException>(() => StackFrameExtensions.HasILOffset(null));
        }

        [Theory]
        [MemberData(nameof(HasMethod_TestData))]
        public void HasMethod_Invoke_ReturnsExpected(StackFrame stackFrame, bool expected)
        {
            Assert.Equal(expected, stackFrame.HasMethod());
        }

        [Fact]
        public void HasMethod_NullStackFrame_ThrowsNullReferenceException()
        {
            Assert.Throws<NullReferenceException>(() => StackFrameExtensions.HasMethod(null));
        }

        public static IEnumerable<object[]> HasSource_TestData()
        {
            yield return new object[] { new StackFrame(), false };
            yield return new object[] { new StackFrame("FileName", 1), true };
            yield return new object[] { new StackFrame(int.MaxValue), false };
        }

        [Theory]
        [MemberData(nameof(HasSource_TestData))]
        public void HasSource_Invoke_ReturnsExpected(StackFrame stackFrame, bool expected)
        {
            Assert.Equal(expected, stackFrame.HasSource());
        }

        [Fact]
        public void HasSource_NullStackFrame_ThrowsNullReferenceException()
        {
            Assert.Throws<NullReferenceException>(() => StackFrameExtensions.HasSource(null));
        }
    }
}
