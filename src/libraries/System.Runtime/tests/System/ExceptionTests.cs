// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace System.Tests
{
    public static class ExceptionTests
    {
        [Fact]
        public static void Exception_GetType()
        {
            Assert.Equal(typeof(Exception), (new Exception()).GetType());
            Assert.Equal(typeof(NullReferenceException), (new NullReferenceException()).GetType());
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Exception.TargetSite always returns null on UapAot.")]
        public static void Exception_TargetSite_Jit()
        {
            bool caught = false;

            try
            {
                throw new Exception();
            }
            catch (Exception ex)
            {
                caught = true;

                Assert.Equal(MethodInfo.GetCurrentMethod(), ex.TargetSite);
            }

            Assert.True(caught);
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.UapAot, "Exception.TargetSite always returns null on UapAot.")]
        public static void Exception_TargetSite_Aot()
        {
            bool caught = false;

            try
            {
                throw new Exception();
            }
            catch (Exception ex)
            {
                caught = true;

                Assert.Null(ex.TargetSite);
            }

            Assert.True(caught);
        }

        [Fact]
        public static void ThrowStatementDoesNotResetExceptionStackLineSameMethod()
        {
            (string, string, int) rethrownExceptionStackFrame = (null, null, 0);

            try
            {
                ThrowAndRethrowSameMethod(out rethrownExceptionStackFrame);
            }
            catch (Exception ex)
            {
                VerifyCallStack(rethrownExceptionStackFrame, ex.StackTrace, 0);
            }
        }

        private static (string, string, int) ThrowAndRethrowSameMethod(out (string, string, int) rethrownExceptionStackFrame)
        {
            try
            {
                if (!PlatformDetection.IsFullFramework)
                    rethrownExceptionStackFrame = GetSourceInformation(1);
                throw new Exception("Boom!");
            }
            catch
            {
                if (PlatformDetection.IsFullFramework)
                    rethrownExceptionStackFrame = GetSourceInformation(1);
                throw;
            }
        }

        [Fact]
        public static void ThrowStatementDoesNotResetExceptionStackLineOtherMethod()
        {
            (string, string, int) rethrownExceptionStackFrame = (null, null, 0);

            try
            {
                ThrowAndRethrowOtherMethod(out rethrownExceptionStackFrame);
            }
            catch (Exception ex)
            {
                VerifyCallStack(rethrownExceptionStackFrame, ex.StackTrace, 1);
            }
        }

        private static void ThrowAndRethrowOtherMethod(out (string, string, int) rethrownExceptionStackFrame)
        {
            try
            {
                if (!PlatformDetection.IsFullFramework)
                    rethrownExceptionStackFrame = GetSourceInformation(1);
                ThrowException(); Assert.True(false, "Workaround for Linux Release builds (https://github.com/dotnet/corefx/pull/28059#issuecomment-378335456)");
            }
            catch
            {
                if (PlatformDetection.IsFullFramework)
                    rethrownExceptionStackFrame = GetSourceInformation(1);
                throw;
            }
            rethrownExceptionStackFrame = (null, null, 0);
        }

        private static void ThrowException()
        {
            throw new Exception("Boom!");
        }

        private static void VerifyCallStack(
            (string CallerMemberName, string SourceFilePath, int SourceLineNumber) expectedStackFrame,
            string reportedCallStack, int skipFrames)
        {
            Console.WriteLine("* ExceptionTests - reported call stack:\n{0}", reportedCallStack);
            const string frameParserRegex = @"\s+at\s.+\.(?<memberName>[^(.]+)\([^)]*\)\sin\s(?<filePath>.*)\:line\s(?<lineNumber>[\d]+)";

            using (var sr = new StringReader(reportedCallStack))
            {
                for (int i = 0; i < skipFrames; i++)
                    sr.ReadLine();
                string frame = sr.ReadLine();
                Assert.NotNull(frame);
                var match = Regex.Match(frame, frameParserRegex);
                Assert.True(match.Success);
                Assert.Equal(expectedStackFrame.CallerMemberName, match.Groups["memberName"].Value);
                Assert.Equal(expectedStackFrame.SourceFilePath, match.Groups["filePath"].Value);
                Assert.Equal(expectedStackFrame.SourceLineNumber, Convert.ToInt32(match.Groups["lineNumber"].Value));
            }
        }

        private static (string, string, int) GetSourceInformation(
            int offset,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            return (memberName, sourceFilePath, sourceLineNumber + offset);
        }
    }

    public class ExceptionDerivedTests: Exception
    {
        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public static void Exception_SerializeObjectState()
        {
            var excp = new ExceptionDerivedTests();
            Assert.Throws<PlatformNotSupportedException>( () => excp.SerializeObjectState += (exception, eventArgs) => eventArgs.AddSerializedState(null));
            Assert.Throws<PlatformNotSupportedException>( () => excp.SerializeObjectState -= (exception, eventArgs) => eventArgs.AddSerializedState(null));
        }
    }
}
