// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Tests;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace System.Tests
{
    public static class ExceptionTests
    {
        private const int COR_E_EXCEPTION = unchecked((int)0x80131500);

        [Fact]
        public static void Ctor_Empty()
        {
            var exception = new Exception();
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_EXCEPTION, validateMessage: false);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "something went wrong";
            var exception = new Exception(message);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_EXCEPTION, message: message);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "something went wrong";
            var innerException = new Exception("Inner exception");
            var exception = new Exception(message, innerException);
            ExceptionUtility.ValidateExceptionProperties(exception, hResult: COR_E_EXCEPTION, innerException: innerException, message: message);
        }

        [Fact]
        public static void Exception_GetType()
        {
            Assert.Equal(typeof(Exception), (new Exception()).GetType());
            Assert.Equal(typeof(NullReferenceException), (new NullReferenceException()).GetType());
        }

        [Fact]
        public static void Exception_GetBaseException()
        {
            var ex = new Exception();
            Assert.Same(ex.GetBaseException(), ex);

            var ex1 = new Exception("One level wrapper", ex);
            Assert.Same(ex1.GetBaseException(), ex);

            var ex2 = new Exception("Two level wrapper", ex);
            Assert.Same(ex2.GetBaseException(), ex);
        }

        [Fact]
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
                rethrownExceptionStackFrame = GetSourceInformation(1);
                throw new Exception("Boom!");
            }
            catch
            {
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
                rethrownExceptionStackFrame = GetSourceInformation(1);
                ThrowException(); Assert.True(false, "Workaround for Linux Release builds (https://github.com/dotnet/corefx/pull/28059#issuecomment-378335456)");
            }
            catch
            {
                throw;
            }
            rethrownExceptionStackFrame = (null, null, 0);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowException()
        {
            throw new Exception("Boom!");
        }

        private static void VerifyCallStack(
            (string CallerMemberName, string SourceFilePath, int SourceLineNumber) expectedStackFrame,
            string reportedCallStack, int skipFrames)
        {
            try
            {
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
            catch
            {
                Console.WriteLine("* ExceptionTests - reported call stack:\n{0}", reportedCallStack);
                throw;
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

    public class ExceptionDerivedTests : Exception
    {
        [Fact]
        public static void Exception_SerializeObjectState()
        {
            var excp = new ExceptionDerivedTests();
            Assert.Throws<PlatformNotSupportedException>(() => excp.SerializeObjectState += (exception, eventArgs) => eventArgs.AddSerializedState(null));
            Assert.Throws<PlatformNotSupportedException>(() => excp.SerializeObjectState -= (exception, eventArgs) => eventArgs.AddSerializedState(null));
        }
    }

    public class ExceptionDataTests : IDictionary_NonGeneric_Tests
    {
        protected override IDictionary NonGenericIDictionaryFactory() => new Exception().Data;

        protected override Type ICollection_NonGeneric_CopyTo_NonZeroLowerBound_ThrowType => typeof(IndexOutOfRangeException);
        protected override Type ICollection_NonGeneric_CopyTo_ArrayOfIncorrectReferenceType_ThrowType => typeof(InvalidCastException);
        protected override Type ICollection_NonGeneric_CopyTo_ArrayOfIncorrectValueType_ThrowType => typeof(InvalidCastException);
        protected override Type ICollection_NonGeneric_CopyTo_ArrayOfEnumType_ThrowType => typeof(InvalidCastException);

        public override void ICollection_NonGeneric_CopyTo_NonZeroLowerBound(int count)
        {
            if (!PlatformDetection.IsNonZeroLowerBoundArraySupported)
                return;

            if (count == 0)
            {
                ICollection collection = NonGenericICollectionFactory(count);
                Array arr = Array.CreateInstance(typeof(object), new int[1] { count }, new int[1] { 2 });
                collection.CopyTo(arr, 0);
                return;
            }

            base.ICollection_NonGeneric_CopyTo_NonZeroLowerBound(count);
        }
    }
}
