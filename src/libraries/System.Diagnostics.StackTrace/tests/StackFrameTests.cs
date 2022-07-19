// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class StackFrameTests
    {
        [Fact]
        public void OffsetUnknown_Get_ReturnsNegativeOne()
        {
            Assert.Equal(-1, StackFrame.OFFSET_UNKNOWN);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50957", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsMonoAOT))]
        public void Ctor_Default()
        {
            var stackFrame = new StackFrame();
            VerifyStackFrame(stackFrame, false, 0, typeof(StackFrameTests).GetMethod(nameof(Ctor_Default)), isCurrentFrame: true);
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50957", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsMonoAOT))]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_FNeedFileInfo(bool fNeedFileInfo)
        {
            var stackFrame = new StackFrame(fNeedFileInfo);
            VerifyStackFrame(stackFrame, fNeedFileInfo, 0, typeof(StackFrameTests).GetMethod(nameof(Ctor_FNeedFileInfo)));
        }

        [Theory]
        [ActiveIssue("https://github.com/mono/mono/issues/15183", TestRuntimes.Mono)]
        [InlineData(StackFrame.OFFSET_UNKNOWN)]
        [InlineData(0)]
        [InlineData(1)]
        public void Ctor_SkipFrames(int skipFrames)
        {
            var stackFrame = new StackFrame(skipFrames);
            VerifyStackFrame(stackFrame, true, skipFrames, typeof(StackFrameTests).GetMethod(nameof(Ctor_SkipFrames)), isCurrentFrame: skipFrames == 0);
        }

        [Theory]
        [ActiveIssue("https://github.com/mono/mono/issues/15187", TestRuntimes.Mono)]
        [InlineData(StackFrame.OFFSET_UNKNOWN, true)]
        [InlineData(0, true)]
        [InlineData(1, true)]
        [InlineData(StackFrame.OFFSET_UNKNOWN, false)]
        [InlineData(0, false)]
        [InlineData(1, false)]
        public void Ctor_SkipFrames_FNeedFileInfo(int skipFrames, bool fNeedFileInfo)
        {
            var stackFrame = new StackFrame(skipFrames, fNeedFileInfo);
            VerifyStackFrame(stackFrame, fNeedFileInfo, skipFrames, typeof(StackFrameTests).GetMethod(nameof(Ctor_SkipFrames_FNeedFileInfo)));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50957", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsMonoAOT))]
        public void SkipFrames_CallMethod_ReturnsExpected()
        {
            StackFrame stackFrame = CallMethod(1);
            MethodInfo expectedMethod = typeof(StackFrameTests).GetMethod(nameof(SkipFrames_CallMethod_ReturnsExpected));
            Assert.Equal(expectedMethod, stackFrame.GetMethod());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public StackFrame CallMethod(int skipFrames) => new StackFrame(skipFrames);

        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(int.MaxValue)]
        public void SkipFrames_ManyFrames_HasNoMethod(int skipFrames)
        {
            var stackFrame = new StackFrame(skipFrames);
            VerifyStackFrame(stackFrame, true, skipFrames, null);
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50957", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsMonoAOT))]
        [InlineData(null, StackFrame.OFFSET_UNKNOWN)]
        [InlineData("", 0)]
        [InlineData("FileName", 1)]
        public void Ctor_Filename_LineNumber(string fileName, int lineNumber)
        {
            var stackFrame = new StackFrame(fileName, lineNumber);
            Assert.Equal(fileName, stackFrame.GetFileName());
            Assert.Equal(lineNumber, stackFrame.GetFileLineNumber());
            Assert.Equal(0, stackFrame.GetFileColumnNumber());

            VerifyStackFrameSkipFrames(stackFrame, true, 0, typeof(StackFrameTests).GetMethod(nameof(Ctor_Filename_LineNumber)));
        }

        [Theory]
        [ActiveIssue("https://github.com/mono/mono/issues/15184", TestRuntimes.Mono)]
        [InlineData(null, StackFrame.OFFSET_UNKNOWN, 0)]
        [InlineData("", 0, StackFrame.OFFSET_UNKNOWN)]
        [InlineData("FileName", 1, 2)]
        public void Ctor_Filename_LineNumber_ColNumber(string fileName, int lineNumber, int columnNumber)
        {
            var stackFrame = new StackFrame(fileName, lineNumber, columnNumber);
            Assert.Equal(fileName, stackFrame.GetFileName());
            Assert.Equal(lineNumber, stackFrame.GetFileLineNumber());
            Assert.Equal(columnNumber, stackFrame.GetFileColumnNumber());

            VerifyStackFrameSkipFrames(stackFrame, true, 0, typeof(StackFrameTests).GetMethod(nameof(Ctor_Filename_LineNumber_ColNumber)));
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            yield return new object[] { new StackFrame(), "MoveNext at offset {offset} in file:line:column {fileName}:{lineNumber}:{column}" + Environment.NewLine };
            yield return new object[] { new StackFrame("FileName", 1, 2), "MoveNext at offset {offset} in file:line:column FileName:1:2" + Environment.NewLine };
            yield return new object[] { new StackFrame(int.MaxValue), "<null>" + Environment.NewLine };
            yield return new object[] { GenericMethod<string>(), "GenericMethod<T> at offset {offset} in file:line:column {fileName}:{lineNumber}:{column}" + Environment.NewLine };
            yield return new object[] { GenericMethod<string, int>(), "GenericMethod<T,U> at offset {offset} in file:line:column {fileName}:{lineNumber}:{column}" + Environment.NewLine };
            yield return new object[] { new ClassWithConstructor().StackFrame, ".ctor at offset {offset} in file:line:column {fileName}:{lineNumber}:{column}" + Environment.NewLine };
        }

        [Theory]
        [ActiveIssue("https://github.com/mono/mono/issues/15186", TestRuntimes.Mono)]
        [MemberData(nameof(ToString_TestData))]
        public void ToString_Invoke_ReturnsExpected(StackFrame stackFrame, string expectedToString)
        {
            expectedToString = expectedToString.Replace("{offset}", stackFrame.GetNativeOffset().ToString())
                                               .Replace("{fileName}", stackFrame.GetFileName() ?? "<filename unknown>")
                                               .Replace("{lineNumber}", stackFrame.GetFileLineNumber().ToString())
                                               .Replace("{column}", stackFrame.GetFileLineNumber().ToString());
            Assert.Equal(expectedToString, stackFrame.ToString());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static StackFrame GenericMethod<T>() => new StackFrame();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static StackFrame GenericMethod<T, U>() => new StackFrame();

        private class ClassWithConstructor
        {
            public StackFrame StackFrame { get; }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public ClassWithConstructor() => StackFrame = new StackFrame();
        }

        private static void VerifyStackFrame(StackFrame stackFrame, bool hasFileInfo, int skipFrames, MethodInfo expectedMethod, bool isCurrentFrame = false)
        {
            if (!hasFileInfo)
            {
                Assert.Null(stackFrame.GetFileName());
                Assert.Equal(0, stackFrame.GetFileLineNumber());
                Assert.Equal(0, stackFrame.GetFileColumnNumber());
            }

            VerifyStackFrameSkipFrames(stackFrame, false, skipFrames, expectedMethod, isCurrentFrame);
        }

        private static void VerifyStackFrameSkipFrames(StackFrame stackFrame, bool isFileConstructor, int skipFrames, MethodInfo expectedMethod, bool isCurrentFrame = false)
        {
            // GetILOffset returns StackFrame.OFFSET_UNKNOWN for unknown frames.
            if (skipFrames == int.MinValue || skipFrames > 0)
            {
                Assert.Equal(StackFrame.OFFSET_UNKNOWN, stackFrame.GetILOffset());
            }
            else
            {
                Assert.True(stackFrame.GetILOffset() >= 0, $"Expected GetILOffset() {stackFrame.GetILOffset()} for {stackFrame} to be greater or equal to zero.");
            }

            // GetMethod returns null for unknown frames.
            if (expectedMethod == null)
            {
                Assert.Null(stackFrame.GetMethod());
            }
            else if (skipFrames == 0)
            {
                Assert.Equal(expectedMethod, stackFrame.GetMethod());
            }
            else
            {
                Assert.NotEqual(expectedMethod, stackFrame.GetMethod());
            }

            // GetNativeOffset returns StackFrame.OFFSET_UNKNOWN for unknown frames.
            // For a positive skipFrame, the GetNativeOffset return value is dependent upon the implementation of reflection
            // Invoke() which can be native (where the value would be zero) or managed (where the value is likely non-zero).
            if (skipFrames == int.MaxValue || skipFrames == int.MinValue)
            {
                Assert.Equal(StackFrame.OFFSET_UNKNOWN, stackFrame.GetNativeOffset());
            }
            else if (skipFrames <= 0)
            {
                Assert.True(stackFrame.GetNativeOffset() > 0, $"Expected GetNativeOffset() {stackFrame.GetNativeOffset()} for {stackFrame} to be greater than zero.");
                Assert.True(stackFrame.GetNativeOffset() > 0);
            }
            else
            {
                Assert.True(stackFrame.GetNativeOffset() >= 0);
            }
        }
    }
}
