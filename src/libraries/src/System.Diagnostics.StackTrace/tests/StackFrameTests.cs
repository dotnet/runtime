// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Diagnostics.Tests
{
    [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "StackFrame is not supported in uapaot.")]
    public class StackFrameTests
    {
        [Fact]
        public void OffsetUnknown_Get_ReturnsNegativeOne()
        {
            Assert.Equal(-1, StackFrame.OFFSET_UNKNOWN);
        }

        [Fact]
        public void Ctor_Default()
        {
            var stackFrame = new StackFrame();
            VerifyStackFrame(stackFrame, false, 0, typeof(StackFrameTests).GetMethod(nameof(Ctor_Default)), isCurrentFrame: true);
        }

        [ActiveIssue(23796, TargetFrameworkMonikers.NetFramework)]
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_FNeedFileInfo(bool fNeedFileInfo)
        {
            var stackFrame = new StackFrame(fNeedFileInfo);
            VerifyStackFrame(stackFrame, fNeedFileInfo, 0, typeof(StackFrameTests).GetMethod(nameof(Ctor_FNeedFileInfo)));
        }

        [Theory]
        [InlineData(StackFrame.OFFSET_UNKNOWN)]
        [InlineData(0)]
        [InlineData(1)]
        public void Ctor_SkipFrames(int skipFrames)
        {
            var stackFrame = new StackFrame(skipFrames);
            VerifyStackFrame(stackFrame, true, skipFrames, typeof(StackFrameTests).GetMethod(nameof(Ctor_SkipFrames)), isCurrentFrame: skipFrames == 0);
        }

        [ActiveIssue(23796, TargetFrameworkMonikers.NetFramework)]
        [Theory]
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
        public void SkipFrames_CallMethod_ReturnsExpected()
        {
            StackFrame stackFrame = CallMethod(1);
            MethodInfo expectedMethod = typeof(StackFrameTests).GetMethod(nameof(SkipFrames_CallMethod_ReturnsExpected));
#if DEBUG
            Assert.Equal(expectedMethod, stackFrame.GetMethod());
#else
            Assert.NotEqual(expectedMethod, stackFrame.GetMethod());
#endif
        }

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
#if DEBUG
            yield return new object[] { new StackFrame(), "MoveNext at offset {offset} in file:line:column {fileName}:{lineNumber}:{column}" + Environment.NewLine };
            yield return new object[] { new StackFrame("FileName", 1, 2), "MoveNext at offset {offset} in file:line:column FileName:1:2" + Environment.NewLine };
            yield return new object[] { new StackFrame(int.MaxValue), "<null>" + Environment.NewLine };
            yield return new object[] { GenericMethod<string>(), "GenericMethod<T> at offset {offset} in file:line:column {fileName}:{lineNumber}:{column}" + Environment.NewLine };
            yield return new object[] { GenericMethod<string, int>(), "GenericMethod<T,U> at offset {offset} in file:line:column {fileName}:{lineNumber}:{column}" + Environment.NewLine };
            yield return new object[] { new ClassWithConstructor().StackFrame, ".ctor at offset {offset} in file:line:column {fileName}:{lineNumber}:{column}" + Environment.NewLine };
#else
            yield break;
#endif
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public void ToString_Invoke_ReturnsExpected(StackFrame stackFrame, string expectedToString)
        {
            expectedToString = expectedToString.Replace("{offset}", stackFrame.GetNativeOffset().ToString())
                                               .Replace("{fileName}", stackFrame.GetFileName() ?? "<filename unknown>")
                                               .Replace("{lineNumber}", stackFrame.GetFileLineNumber().ToString())
                                               .Replace("{column}", stackFrame.GetFileLineNumber().ToString());
            Assert.Equal(expectedToString, stackFrame.ToString());
        }

        private static StackFrame GenericMethod<T>() => new StackFrame();
        private static StackFrame GenericMethod<T, U>() => new StackFrame();

        private class ClassWithConstructor
        {
            public StackFrame StackFrame { get; }
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
#if !DEBUG
            else if ((!PlatformDetection.IsFullFramework && isFileConstructor) || (PlatformDetection.IsFullFramework && !isFileConstructor && !isCurrentFrame && skipFrames == 0))
            {
                Assert.Equal(0, stackFrame.GetILOffset());
            }
#endif
            else
            {
                Assert.True(stackFrame.GetILOffset() > 0, $"Expected GetILOffset() {stackFrame.GetILOffset()} for {stackFrame} to be greater than zero.");
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
            // GetNativeOffset returns 0 for known frames with a positive skipFrames.
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
                Assert.Equal(0, stackFrame.GetNativeOffset());
            }
        }
    }
}
