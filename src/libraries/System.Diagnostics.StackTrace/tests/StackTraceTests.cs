// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Diagnostics
{
    public static class Ignored
    {
        public static StackTrace Method() => new StackTrace();
        public static StackTrace MethodWithException()
        {
            try
            {
                throw new Exception();
            }
            catch (Exception exception)
            {
                return new StackTrace(exception);
            }
        }
    }
}

namespace System.Diagnostics.Tests
{
    [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "StackTrace is not supported in uapaot.")]
    public class StackTraceTests
    {
        [Fact]
        public void MethodsToSkip_Get_ReturnsZero()
        {
            Assert.Equal(0, StackTrace.METHODS_TO_SKIP);
        }

        [Fact]
        public void Ctor_Default()
        {
            var stackTrace = new StackTrace();
            VerifyFrames(stackTrace, false);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_FNeedFileInfo(bool fNeedFileInfo)
        {
            var stackTrace = new StackTrace(fNeedFileInfo);
            VerifyFrames(stackTrace, fNeedFileInfo);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void Ctor_SkipFrames(int skipFrames)
        {
            var emptyStackTrace = new StackTrace();
            IEnumerable<MethodBase> expectedMethods = emptyStackTrace.GetFrames().Skip(skipFrames).Select(f => f.GetMethod());

            var stackTrace = new StackTrace(skipFrames);
            Assert.Equal(emptyStackTrace.FrameCount - skipFrames, stackTrace.FrameCount);
            Assert.Equal(expectedMethods, stackTrace.GetFrames().Select(f => f.GetMethod()));

            VerifyFrames(stackTrace, false);
        }

        [Fact]
        public void Ctor_LargeSkipFrames_GetFramesReturnsNull()
        {
            var stackTrace = new StackTrace(int.MaxValue);
            Assert.Equal(0, stackTrace.FrameCount);
            Assert.Null(stackTrace.GetFrames());
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(1, true)]
        [InlineData(0, false)]
        [InlineData(1, false)]
        public void Ctor_SkipFrames_FNeedFileInfo(int skipFrames, bool fNeedFileInfo)
        {
            var emptyStackTrace = new StackTrace();
            IEnumerable<MethodBase> expectedMethods = emptyStackTrace.GetFrames().Skip(skipFrames).Select(f => f.GetMethod());

            var stackTrace = new StackTrace(skipFrames, fNeedFileInfo);
            Assert.Equal(emptyStackTrace.FrameCount - skipFrames, stackTrace.FrameCount);
            Assert.Equal(expectedMethods, stackTrace.GetFrames().Select(f => f.GetMethod()));

            VerifyFrames(stackTrace, fNeedFileInfo);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_LargeSkipFramesFNeedFileInfo_GetFramesReturnsNull(bool fNeedFileInfo)
        {
            var stackTrace = new StackTrace(int.MaxValue, fNeedFileInfo);
            Assert.Equal(0, stackTrace.FrameCount);
            Assert.Null(stackTrace.GetFrames());
        }

        [Fact]
        public void Ctor_ThrownException_GetFramesReturnsExpected()
        {
            var stackTrace = new StackTrace(InvokeException());
            VerifyFrames(stackTrace, false);
        }

        [Fact]
        public void Ctor_EmptyException_GetFramesReturnsNull()
        {
            var exception = new Exception();
            var stackTrace = new StackTrace(exception);
            Assert.Equal(0, stackTrace.FrameCount);
            Assert.Null(stackTrace.GetFrames());
            Assert.Null(stackTrace.GetFrame(0));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_ThrownException_GetFramesReturnsExpected(bool fNeedFileInfo)
        {
            var stackTrace = new StackTrace(InvokeException(), fNeedFileInfo);
            VerifyFrames(stackTrace, fNeedFileInfo);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_EmptyException_FNeedFileInfo(bool fNeedFileInfo)
        {
            var exception = new Exception();
            var stackTrace = new StackTrace(exception, fNeedFileInfo);
            Assert.Equal(0, stackTrace.FrameCount);
            Assert.Null(stackTrace.GetFrames());
            Assert.Null(stackTrace.GetFrame(0));
        }

        [ActiveIssue(23796, TargetFrameworkMonikers.NetFramework)]
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void Ctor_Exception_SkipFrames(int skipFrames)
        {
            Exception ex = InvokeException();
            var exceptionStackTrace = new StackTrace(ex);
            IEnumerable<MethodBase> expectedMethods = exceptionStackTrace.GetFrames().Skip(skipFrames).Select(f => f.GetMethod());

            var stackTrace = new StackTrace(ex, skipFrames);
            Assert.Equal(exceptionStackTrace.FrameCount - skipFrames, stackTrace.FrameCount);

            // Netfx has null Frames if skipping frames in Release mode.
            StackFrame[] frames = stackTrace.GetFrames();
#if DEBUG
            Assert.Equal(expectedMethods, frames.Select(f => f.GetMethod()));
#else
            if (PlatformDetection.IsFullFramework && skipFrames > 0)
            {
                Assert.Null(frames);
            }
            else
            {
                Assert.Equal(expectedMethods, frames.Select(f => f.GetMethod()));
            }
#endif
            if (frames != null)
            {
                VerifyFrames(stackTrace, false);
            }
        }

        [Fact]
        public void Ctor_Exception_LargeSkipFrames()
        {
            var stackTrace = new StackTrace(InvokeException(), int.MaxValue);
            Assert.Equal(0, stackTrace.FrameCount);
            Assert.Null(stackTrace.GetFrames());
        }

        [Fact]
        public void Ctor_EmptyException_SkipFrames()
        {
            var stackTrace = new StackTrace(new Exception(), 0);
            Assert.Equal(0, stackTrace.FrameCount);
            Assert.Null(stackTrace.GetFrames());
            Assert.Null(stackTrace.GetFrame(0));
        }

        [ActiveIssue(23796, TargetFrameworkMonikers.NetFramework)]
        [Theory]
        [InlineData(0, true)]
        [InlineData(1, true)]
        [InlineData(0, false)]
        [InlineData(1, false)]
        public void Ctor_Exception_SkipFrames_FNeedFileInfo(int skipFrames, bool fNeedFileInfo)
        {
            Exception ex = InvokeException();
            var exceptionStackTrace = new StackTrace(ex);
            IEnumerable<MethodBase> expectedMethods = exceptionStackTrace.GetFrames().Skip(skipFrames).Select(f => f.GetMethod());

            var stackTrace = new StackTrace(ex, skipFrames, fNeedFileInfo);

            // Netfx has null Frames if skipping frames in Release mode.
            StackFrame[] frames = stackTrace.GetFrames();
#if DEBUG
            Assert.Equal(expectedMethods, frames.Select(f => f.GetMethod()));
#else
            if (PlatformDetection.IsFullFramework && skipFrames > 0)
            {
                Assert.Null(frames);
            }
            else
            {
                Assert.Equal(expectedMethods, frames.Select(f => f.GetMethod()));
            }
#endif
            if (frames != null)
            {
                VerifyFrames(stackTrace, fNeedFileInfo);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_Exception_LargeSkipFrames_FNeedFileInfo(bool fNeedFileInfo)
        {
            var stackTrace = new StackTrace(InvokeException(), int.MaxValue);
            Assert.Equal(0, stackTrace.FrameCount);
            Assert.Null(stackTrace.GetFrames());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_EmptyException_SkipFrames_FNeedFileInfo(bool fNeedFileInfo)
        {
            var stackTrace = new StackTrace(new Exception(), 0, fNeedFileInfo);
            Assert.Equal(0, stackTrace.FrameCount);
            Assert.Null(stackTrace.GetFrames());
            Assert.Null(stackTrace.GetFrame(0));
        }

        [Fact]
        public void Ctor_NullException_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("e", () => new StackTrace((Exception)null));
            AssertExtensions.Throws<ArgumentNullException>("e", () => new StackTrace((Exception)null, false));
            AssertExtensions.Throws<ArgumentNullException>("e", () => new StackTrace(null, 1));
        }

        public static IEnumerable<object[]> Ctor_Frame_TestData()
        {
            yield return new object[] { new StackFrame() };
            yield return new object[] { null };
        }

        [Theory]
        [MemberData(nameof(Ctor_Frame_TestData))]
        public void Ctor_Frame(StackFrame stackFrame)
        {
            var stackTrace = new StackTrace(stackFrame);
            Assert.Equal(1, stackTrace.FrameCount);
            Assert.Equal(new StackFrame[] { stackFrame }, stackTrace.GetFrames());
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            // Debug mode and Release mode give different results.
#if DEBUG
            yield return new object[] { new StackTrace(InvokeException()), "   at System.Diagnostics.Tests.StackTraceTests.ThrowException()" };
            yield return new object[] { new StackTrace(new Exception()), "" };
            yield return new object[] { NoParameters(), "   at System.Diagnostics.Tests.StackTraceTests.NoParameters()" };
            yield return new object[] { OneParameter(1), "   at System.Diagnostics.Tests.StackTraceTests.OneParameter(Int32 x)" };
            yield return new object[] { TwoParameters(1, null), "   at System.Diagnostics.Tests.StackTraceTests.TwoParameters(Int32 x, String y)" };
            yield return new object[] { Generic<int>(), "   at System.Diagnostics.Tests.StackTraceTests.Generic[T]()" };
            yield return new object[] { Generic<int, string>(), "   at System.Diagnostics.Tests.StackTraceTests.Generic[T,U]()" };
            yield return new object[] { new ClassWithConstructor().StackTrace, "   at System.Diagnostics.Tests.StackTraceTests.ClassWithConstructor..ctor()" };

            // Methods belonging to the System.Diagnostics namespace are ignored.
            yield return new object[] { InvokeIgnoredMethod(), "   at System.Diagnostics.Tests.StackTraceTests.InvokeIgnoredMethod()" };
#endif

            yield return new object[] { InvokeIgnoredMethodWithException(), "   at System.Diagnostics.Ignored.MethodWithException()" };
        }

        [Fact]
        public void GetFrame_InvalidIndex_ReturnsNull()
        {
            var stackTrace = new StackTrace();
            Assert.Null(stackTrace.GetFrame(-1));
            Assert.Null(stackTrace.GetFrame(stackTrace.FrameCount));
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public void ToString_Invoke_ReturnsExpected(StackTrace stackTrace, string expectedToString)
        {
            if (expectedToString.Length == 0)
            {
                Assert.Equal(Environment.NewLine, stackTrace.ToString());
            }
            else
            {
                string toString = stackTrace.ToString();
                Assert.StartsWith(expectedToString, toString);
                Assert.EndsWith(Environment.NewLine, toString);

                string[] frames = toString.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                Assert.Equal(frames.Length, stackTrace.FrameCount);
            }
        }

        [Fact]
        public void ToString_NullFrame_ThrowsNullReferenceException()
        {
            var stackTrace = new StackTrace((StackFrame)null);
            Assert.Throws<NullReferenceException>(() => stackTrace.ToString());
        }

        private static StackTrace NoParameters() => new StackTrace();
        private static StackTrace OneParameter(int x) => new StackTrace();
        private static StackTrace TwoParameters(int x, string y) => new StackTrace();

        private static StackTrace Generic<T>() => new StackTrace();
        private static StackTrace Generic<T, U>() => new StackTrace();

        private static StackTrace InvokeIgnoredMethod() => Ignored.Method();
        private static StackTrace InvokeIgnoredMethodWithException() => Ignored.MethodWithException();

        private static Exception InvokeException()
        {
            try
            {
                ThrowException();
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        private static void ThrowException() => throw new Exception();

        private class ClassWithConstructor
        {
            public StackTrace StackTrace { get; }
            public ClassWithConstructor() => StackTrace = new StackTrace();
        }

        private static void VerifyFrames(StackTrace stackTrace, bool hasFileInfo)
        {
            Assert.True(stackTrace.FrameCount > 0);

            StackFrame[] stackFrames = stackTrace.GetFrames();
            Assert.Equal(stackTrace.FrameCount, stackFrames.Length);

            for (int i = 0; i < stackFrames.Length; i++)
            {
                StackFrame stackFrame = stackFrames[i];

                if (!hasFileInfo)
                {
                    Assert.Null(stackFrame.GetFileName());
                    Assert.Equal(0, stackFrame.GetFileLineNumber());
                    Assert.Equal(0, stackFrame.GetFileColumnNumber());
                }
                Assert.NotNull(stackFrame.GetMethod());
            }
        }
    }
}
