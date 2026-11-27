// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
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
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_EXCEPTION, validateMessage: false);
        }

        [Fact]
        public static void Ctor_String()
        {
            string message = "something went wrong";
            var exception = new Exception(message);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_EXCEPTION, message: message);
        }

        [Fact]
        public static void Ctor_String_Exception()
        {
            string message = "something went wrong";
            var innerException = new Exception("Inner exception");
            var exception = new Exception(message, innerException);
            ExceptionHelpers.ValidateExceptionProperties(exception, hResult: COR_E_EXCEPTION, innerException: innerException, message: message);
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
        public static void Exception_TargetSite()
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

        static void RethrowException()
        {
            try
            {
                ThrowException();
            }
            catch
            {
                throw;
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50957", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsMonoAOT))]
        public static void Exception_TargetSite_OtherMethod()
        {
            Exception ex = Assert.ThrowsAny<Exception>(() => ThrowException());
            Assert.Equal(nameof(ThrowException), ex.TargetSite.Name);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50957", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsMonoAOT))]
        public static void Exception_TargetSite_Rethrow()
        {
            Exception ex = Assert.ThrowsAny<Exception>(() => RethrowException());
            Assert.Equal(nameof(ThrowException), ex.TargetSite.Name);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.HasAssemblyFiles))]
        [ActiveIssue("https://github.com/mono/mono/issues/15140", TestRuntimes.Mono)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/124344", typeof(PlatformDetection), nameof(PlatformDetection.IsAppleMobile), nameof(PlatformDetection.IsCoreCLR))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/130796", typeof(PlatformDetection), nameof(PlatformDetection.IsWasi), nameof(PlatformDetection.IsCoreCLR))]
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotArm64Process), nameof(PlatformDetection.HasAssemblyFiles))]
        // [ActiveIssue(https://github.com/dotnet/runtime/issues/1871)] can't use ActiveIssue for archs
        [ActiveIssue("https://github.com/mono/mono/issues/15141", TestRuntimes.Mono)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/124344", typeof(PlatformDetection), nameof(PlatformDetection.IsAppleMobile), nameof(PlatformDetection.IsX64Process), nameof(PlatformDetection.IsCoreCLR))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/130796", typeof(PlatformDetection), nameof(PlatformDetection.IsWasi), nameof(PlatformDetection.IsCoreCLR))]
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
                ThrowException(); Assert.Fail("Workaround for Linux Release builds (https://github.com/dotnet/corefx/pull/28059#issuecomment-378335456)");
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

        [Theory]
        [InlineData(nameof(ArgumentAfterFilteredCatch), "name: InvalidDataException")]
        [InlineData(nameof(ArgumentAfterPlainCatch), "name: InvalidDataException")]
        [InlineData(nameof(ExceptionFilterProbe.LocalsSetInFilteredCatch), "0 0 True")]
        [InlineData(nameof(LocalBeforeFilteredCatch), 28)]
        [InlineData(nameof(IntegerArgumentAfterFilteredCatch), 42)]
        [InlineData(nameof(ArgumentInFilter), true)]
        [InlineData(nameof(ArgumentInsideFilteredCatch), "name: InvalidDataException")]
        [InlineData(nameof(ArgumentInOuterFilteredCatch), "name x")]
        public static void ExceptionFilter_PreservesArgumentsAndLocals(string scenario, object expected)
        {
            object actual = scenario switch
            {
                nameof(ArgumentAfterFilteredCatch) => ArgumentAfterFilteredCatch("name"),
                nameof(ArgumentAfterPlainCatch) => ArgumentAfterPlainCatch("name"),
                nameof(ExceptionFilterProbe.LocalsSetInFilteredCatch) => new ExceptionFilterProbe { Flag = true }.LocalsSetInFilteredCatch(),
                nameof(LocalBeforeFilteredCatch) => LocalBeforeFilteredCatch(7),
                nameof(IntegerArgumentAfterFilteredCatch) => IntegerArgumentAfterFilteredCatch(42),
                nameof(ArgumentInFilter) => ArgumentInFilter("x"),
                nameof(ArgumentInsideFilteredCatch) => ArgumentInsideFilteredCatch("name"),
                nameof(ArgumentInOuterFilteredCatch) => ArgumentInOuterFilteredCatch("name"),
                _ => throw new ArgumentException($"Unknown scenario: {scenario}", nameof(scenario))
            };

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void ExceptionFilter_PreservesArgumentChanges(bool matches)
        {
            Assert.Equal("49: name-filter", ChangedArgumentsInFilter(42, "name", matches));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowForFilter() => throw new InvalidDataException("x");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string ArgumentAfterFilteredCatch(string name)
        {
            string result;
            try
            {
                ThrowForFilter();
                result = "ok";
            }
            catch (Exception e) when (e is InvalidDataException or NotSupportedException)
            {
                result = e.GetType().Name;
            }
            return name + ": " + result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string ArgumentAfterPlainCatch(string name)
        {
            string result;
            try
            {
                ThrowForFilter();
                result = "ok";
            }
            catch (Exception e)
            {
                if (e is not InvalidDataException)
                    throw;
                result = e.GetType().Name;
            }
            return name + ": " + result;
        }

        private sealed class ExceptionFilterProbe
        {
            public bool Flag { get; set; }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public string LocalsSetInFilteredCatch()
            {
                List<int> a;
                List<int> b;
                bool lost = false;
                try
                {
                    ThrowForFilter();
                    a = [1];
                    b = [2];
                }
                catch (InvalidDataException) when (Flag)
                {
                    (a, b, lost) = ([], [], true);
                }
                return $"{a.Count} {b.Count} {lost}";
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int LocalBeforeFilteredCatch(int value)
        {
            int local = value * 3;
            try
            {
                ThrowForFilter();
            }
            catch (Exception e) when (e is InvalidDataException)
            {
            }
            return local + value;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int IntegerArgumentAfterFilteredCatch(int value)
        {
            try
            {
                ThrowForFilter();
            }
            catch (Exception e) when (e is InvalidDataException)
            {
            }
            return value;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool ArgumentInFilter(string value)
        {
            try
            {
                ThrowForFilter();
                return false;
            }
            catch (Exception) when (value == "x")
            {
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string ArgumentInsideFilteredCatch(string name)
        {
            try
            {
                ThrowForFilter();
                return "";
            }
            catch (Exception e) when (e is InvalidDataException or NotSupportedException)
            {
                return $"{name}: {e.GetType().Name}";
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string ArgumentInOuterFilteredCatch(string name)
        {
            try
            {
                return RejectedInnerFilter();
            }
            catch (Exception e) when (e is InvalidDataException)
            {
                return name + " " + e.Message;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string RejectedInnerFilter()
        {
            try
            {
                ThrowForFilter();
                return "";
            }
            catch (Exception e) when (e is NotSupportedException)
            {
                return "wrong";
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string ChangedArgumentsInFilter(int value, string name, bool matches)
        {
            try
            {
                ThrowForFilter();
            }
            catch (InvalidDataException) when (ChangeFilterArguments(ref value, ref name, matches))
            {
            }
            catch (InvalidDataException)
            {
            }
            return $"{value}: {name}";
        }

        private static bool ChangeFilterArguments(ref int value, ref string name, bool matches)
        {
            value += 7;
            name += "-filter";
            return matches;
        }

        [Fact]
        public static void FirstCatch_ThrowFromCatch_HandledByOuterCatch()
        {
            // Seed the frame slot with a valid resume ID from a previous invocation.
            ThrowFromCatch(catchCount: 1, throwFromCatch: null);

            bool caught = false;

            try
            {
                ThrowFromCatch(catchCount: 1, throwFromCatch: 0);
            }
            catch (InvalidOperationException)
            {
                caught = true;
            }

            Assert.True(caught);
        }

        [Fact]
        public static void RepeatedCatch_ThrowFromSecondCatch_HandledByOuterCatch()
        {
            bool caught = false;

            try
            {
                ThrowFromCatch(catchCount: 2, throwFromCatch: 1);
            }
            catch (InvalidOperationException)
            {
                caught = true;
            }

            Assert.True(caught);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowFromCatch(int catchCount, int? throwFromCatch)
        {
            for (int i = 0; i < catchCount; i++)
            {
                try
                {
                    throw new Exception();
                }
                catch
                {
                    if (i == throwFromCatch)
                    {
                        throw new InvalidOperationException();
                    }
                }
            }
        }

        private static void VerifyCallStack(
            (string CallerMemberName, string SourceFilePath, int SourceLineNumber) expectedStackFrame,
            string reportedCallStack, int skipFrames)
        {
            try
            {
                string frameParserRegex = @"\s+at\s.+\.(?<memberName>[^(.]+)\([^)]*\)\sin\s(?<filePath>.*)\:line\s(?<lineNumber>[\d]+)";

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

    public class DerivedException : Exception
    {
        public override string Message
        {
            get => "DerivedException.Message";
        }

        public override string ToString()
        {
            return "DerivedException.ToString()";
        }

#pragma warning disable SYSLIB0011 // BinaryFormatter serialization is obsolete and should not be used.
        [Fact]
        public static void Exception_SerializeObjectState()
        {
            var excp = new DerivedException();
            Assert.Throws<PlatformNotSupportedException>(() => excp.SerializeObjectState += (exception, eventArgs) => eventArgs.AddSerializedState(null));
            Assert.Throws<PlatformNotSupportedException>(() => excp.SerializeObjectState -= (exception, eventArgs) => eventArgs.AddSerializedState(null));
        }
#pragma warning restore SYSLIB0011

        [Fact]
        public static void Exception_OverriddenToStringOnInnerException()
        {
            var inner = new DerivedException();
            var excp = new Exception("msg", inner);

            Assert.Contains("DerivedException.ToString()", excp.ToString());
            Assert.DoesNotContain("DerivedException.Message", excp.ToString());
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
