// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.DotNet.RemoteExecutor;
using System.Threading.Tasks;
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

    public static class V1Methods
    {
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(false)]
#line 1 "Test0.cs"
        public static async Task Test0(Func<int, Task> method)
        {
            await Test1(method);
            await Task.Yield();
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(false)]
#line 1 "Test1.cs"
        public static async Task Test1(Func<int, Task> method)
        {
            try
            {
                await method(3);
            }
            catch (Exception ex) when (ex.Message.Contains("404"))
            {
                Console.WriteLine($"Caught exception in Test1 with: {ex}");
            }
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(false)]
#line 1 "Test2.cs"
        public static async Task Test2(int i)
        {
            throw new NullReferenceException("Exception from Test2");
        }
    }

    public class V2Methods
    {
        // v2 -> v1 -> v2 -> v1
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
#line 1 "Foo.cs"
        public static async Task Foo()
        {
            await Task.Yield();
            try
            {
                await V1Methods.Test0(Foo1);
            }
            catch (NotImplementedException)
            {
                throw;
            }
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
#line 1 "Foo1.cs"
        private static async Task<int> Foo1(int i)
        {
            await Task.Yield();
            try
            {
                await Foo2(i);
                return i * 2;
            }
            catch (NotImplementedException)
            {
                throw;
            }
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
#line 1 "Foo2.cs"
        private static async Task<int> Foo2(int i)
        {
            try
            {
                await Task.Yield();
                await V1Methods.Test2(i);
            }
            finally
            {
                throw new NotImplementedException("Exception from Foo2");
            }
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
#line 1 "Bar.cs"
        public static async Task Bar(int i)
        {
            if (i == 0)
                throw new Exception("Exception from Bar");
            await Bar(i - 1);
        }

        // also v2 v1 chaining but this time we don't have finally
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
#line 1 "Quux.cs"
        public static async Task Quux()
        {
            await Task.Yield();
            try
            {
                await V1Methods.Test0(Quux1);
            }
            catch (NotImplementedException)
            {
                throw;
            }
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
#line 1 "Quux1.cs"
        private static async Task<int> Quux1(int i)
        {
            try
            {
                await Task.Delay(10);
                throw new NotImplementedException("Exception from Quux1");
            }
            catch (NotImplementedException)
            {
                throw;
            }
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
#line 1 "Quuux.cs"
        public static async Task<int> Quuux()
        {
            var task = Quuux2();
            await Task.Yield();
            return await task;
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
#line 1 "Quuux2.cs"
        private static async Task<int> Quuux2()
        {
            await Task.Yield();
            throw new Exception("Exception from Quuux2");
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
#line 1 "Bux.cs"
        public static async Task Bux()
        {
            await Task.Yield();
            try
            {
                Baz().Wait();
            }
            catch (Exception)
            {
                throw;
            }
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        [System.Runtime.CompilerServices.RuntimeAsyncMethodGeneration(true)]
#line 1 "Baz.cs"
        public static async Task Baz()
        {
            if (Random.Shared.Next(1) == 100) await Task.Yield();
            throw new Exception("Exception from Baz method.");
        }
    }
}
#line default

namespace System.Diagnostics.Tests
{
    public class StackTraceTests
    {
        [Fact]
        public void MethodsToSkip_Get_ReturnsZero()
        {
            Assert.Equal(0, StackTrace.METHODS_TO_SKIP);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50957", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsMonoAOT))]
        public void Ctor_Default()
        {
            var stackTrace = new StackTrace();
            VerifyFrames(stackTrace, false, 0);
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50957", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsMonoAOT))]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_FNeedFileInfo(bool fNeedFileInfo)
        {
            var stackTrace = new StackTrace(fNeedFileInfo);
            VerifyFrames(stackTrace, fNeedFileInfo, 0);
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50957", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsMonoAOT))]
        [InlineData(0)]
        [InlineData(1)]
        public void Ctor_SkipFrames(int skipFrames)
        {
            var emptyStackTrace = new StackTrace();
            IEnumerable<MethodBase> expectedMethods = emptyStackTrace.GetFrames().Skip(skipFrames).Select(f => f.GetMethod());

            var stackTrace = new StackTrace(skipFrames);
            Assert.Equal(emptyStackTrace.FrameCount - skipFrames, stackTrace.FrameCount);
            Assert.Equal(expectedMethods, stackTrace.GetFrames().Select(f => f.GetMethod()));

            VerifyFrames(stackTrace, false, skipFrames);
        }

        [Fact]
        public void Ctor_LargeSkipFrames_GetFramesReturnsEmpty()
        {
            var stackTrace = new StackTrace(int.MaxValue);
            Assert.Equal(0, stackTrace.FrameCount);
            Assert.Empty(stackTrace.GetFrames());
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50957", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsMonoAOT))]
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

            VerifyFrames(stackTrace, fNeedFileInfo, skipFrames);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_LargeSkipFramesFNeedFileInfo_GetFramesReturnsEmpty(bool fNeedFileInfo)
        {
            var stackTrace = new StackTrace(int.MaxValue, fNeedFileInfo);
            Assert.Equal(0, stackTrace.FrameCount);
            Assert.Empty(stackTrace.GetFrames());
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50957", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsMonoAOT))]
        public void Ctor_ThrownException_GetFramesReturnsExpected()
        {
            var stackTrace = new StackTrace(InvokeException());
            VerifyFrames(stackTrace, false, 0);
        }

        [Fact]
        public void Ctor_EmptyException_GetFramesReturnsEmpty()
        {
            var exception = new Exception();
            var stackTrace = new StackTrace(exception);
            Assert.Equal(0, stackTrace.FrameCount);
            Assert.Empty(stackTrace.GetFrames());
            Assert.Null(stackTrace.GetFrame(0));
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50957", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsMonoAOT))]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_Bool_ThrownException_GetFramesReturnsExpected(bool fNeedFileInfo)
        {
            var stackTrace = new StackTrace(InvokeException(), fNeedFileInfo);
            VerifyFrames(stackTrace, fNeedFileInfo, 0);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_EmptyException_FNeedFileInfo(bool fNeedFileInfo)
        {
            var exception = new Exception();
            var stackTrace = new StackTrace(exception, fNeedFileInfo);
            Assert.Equal(0, stackTrace.FrameCount);
            Assert.Empty(stackTrace.GetFrames());
            Assert.Null(stackTrace.GetFrame(0));
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/31796", TestRuntimes.Mono)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50957", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsMonoAOT))]
        [InlineData(0)]
        [InlineData(1)]
        public void Ctor_Exception_SkipFrames(int skipFrames)
        {
            Exception ex = InvokeException();
            var exceptionStackTrace = new StackTrace(ex);
            IEnumerable<MethodBase> expectedMethods = exceptionStackTrace.GetFrames().Skip(skipFrames).Select(f => f.GetMethod());

            var stackTrace = new StackTrace(ex, skipFrames);
            Assert.Equal(exceptionStackTrace.FrameCount - skipFrames, stackTrace.FrameCount);

            // .NET Framework has null Frames if skipping frames in Release mode.
            StackFrame[] frames = stackTrace.GetFrames();
            Assert.Equal(expectedMethods, frames.Select(f => f.GetMethod()));
            if (frames != null)
            {
                VerifyFrames(stackTrace, false, skipFrames);
            }
        }

        [Fact]
        public void Ctor_Exception_LargeSkipFrames()
        {
            var stackTrace = new StackTrace(InvokeException(), int.MaxValue);
            Assert.Equal(0, stackTrace.FrameCount);
            Assert.Empty(stackTrace.GetFrames());
        }

        [Fact]
        public void Ctor_EmptyException_SkipFrames()
        {
            var stackTrace = new StackTrace(new Exception(), 0);
            Assert.Equal(0, stackTrace.FrameCount);
            Assert.Empty(stackTrace.GetFrames());
            Assert.Null(stackTrace.GetFrame(0));
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/31796", TestRuntimes.Mono)]
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

            // .NET Framework has null Frames if skipping frames in Release mode.
            StackFrame[] frames = stackTrace.GetFrames();
            Assert.Equal(expectedMethods, frames.Select(f => f.GetMethod()));
            if (frames != null)
            {
                VerifyFrames(stackTrace, fNeedFileInfo, skipFrames);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_Exception_LargeSkipFrames_FNeedFileInfo(bool fNeedFileInfo)
        {
            var stackTrace = new StackTrace(InvokeException(), int.MaxValue, fNeedFileInfo);
            Assert.Equal(0, stackTrace.FrameCount);
            Assert.Empty(stackTrace.GetFrames());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor_EmptyException_SkipFrames_FNeedFileInfo(bool fNeedFileInfo)
        {
            var stackTrace = new StackTrace(new Exception(), 0, fNeedFileInfo);
            Assert.Equal(0, stackTrace.FrameCount);
            Assert.Empty(stackTrace.GetFrames());
            Assert.Null(stackTrace.GetFrame(0));
        }

        [Fact]
        public void Ctor_NullException_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("e", () => new StackTrace((Exception)null));
            AssertExtensions.Throws<ArgumentNullException>("e", () => new StackTrace((Exception)null, false));
            AssertExtensions.Throws<ArgumentNullException>("e", () => new StackTrace(null, 1));
        }

        [Fact]
        public void Ctor_NullMultiFrame_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("frames", () => new StackTrace((IEnumerable<StackFrame>)null));
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

        [Fact]
        public void Ctor_MultiFrame()
        {
            var stackFrames = new[] { new StackFrame(), new StackFrame() };
            var stackTrace = new StackTrace(stackFrames);
            Assert.Equal(stackFrames.Length, stackTrace.FrameCount);

            for (var i = 0; i < stackFrames.Length; ++i)
            {
                Assert.Equal(stackFrames[i], stackTrace.GetFrame(i));
            }
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            yield return new object[] { new StackTrace(InvokeException()), "System.Diagnostics.Tests.StackTraceTests.ThrowException()" };
            yield return new object[] { new StackTrace(new Exception()), "" };
            yield return new object[] { NoParameters(), "System.Diagnostics.Tests.StackTraceTests.NoParameters()" };
            yield return new object[] { OneParameter(1), "System.Diagnostics.Tests.StackTraceTests.OneParameter(Int32 x)" };
            yield return new object[] { TwoParameters(1, null), "System.Diagnostics.Tests.StackTraceTests.TwoParameters(Int32 x, String y)" };
            yield return new object[] { Generic<int>(), "System.Diagnostics.Tests.StackTraceTests.Generic[T]()" };
            yield return new object[] { Generic<int, string>(), "System.Diagnostics.Tests.StackTraceTests.Generic[T,U]()" };
            yield return new object[] { new ClassWithConstructor().StackTrace, "System.Diagnostics.Tests.StackTraceTests.ClassWithConstructor..ctor()" };

            // Methods belonging to the System.Diagnostics namespace are ignored.
            yield return new object[] { InvokeIgnoredMethod(), "System.Diagnostics.Tests.StackTraceTests.InvokeIgnoredMethod()" };

            yield return new object[] { InvokeIgnoredMethodWithException(), "System.Diagnostics.Ignored.MethodWithException()" };
        }

        [Fact]
        public void GetFrame_InvalidIndex_ReturnsNull()
        {
            var stackTrace = new StackTrace();
            Assert.Null(stackTrace.GetFrame(-1));
            Assert.Null(stackTrace.GetFrame(stackTrace.FrameCount));
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/31797", TestRuntimes.Mono)]
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
                Assert.Contains(expectedToString, toString);
                Assert.EndsWith(Environment.NewLine, toString);

                string[] frames = toString.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                // StackTrace pretty printer omits uninteresting frames from the formatted stacktrace
                AssertExtensions.LessThanOrEqualTo(frames.Length, stackTrace.FrameCount);
            }
        }

        [Fact]
        public void ToString_NullFrame_ThrowsNullReferenceException()
        {
            var stackTrace = new StackTrace((StackFrame)null);
            Assert.Equal(Environment.NewLine, stackTrace.ToString());
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/11354", TestRuntimes.Mono)]
        public unsafe void ToString_FunctionPointerSignature()
        {
            // This is separate from ToString_Invoke_ReturnsExpected since unsafe cannot be used for iterators
            var stackTrace = FunctionPointerParameter(null);
            // Function pointers have no Name.
            Assert.Contains("System.Diagnostics.Tests.StackTraceTests.FunctionPointerParameter( x)", stackTrace.ToString());
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ToString_ShowILOffset()
        {
            string AssemblyName = "ExceptionTestAssembly.dll";
            string SourceTestAssemblyPath = Path.Combine(Environment.CurrentDirectory, AssemblyName);
            string regPattern = @":token 0x([a-f0-9]*)\+0x([a-f0-9]*)";

            // Normal loading case
            RemoteExecutor.Invoke((asmPath, asmName, p) =>
            {
                AppContext.SetSwitch("Switch.System.Diagnostics.StackTrace.ShowILOffsets", true);
                var asm = Assembly.LoadFrom(asmPath);
                try
                {
                    asm.GetType("Program").GetMethod("Foo").Invoke(null, null);
                }
                catch (Exception e)
                {
                    Assert.Contains(asmName, e.InnerException.StackTrace);
                    Assert.Matches(p, e.InnerException.StackTrace);
                }
            }, SourceTestAssemblyPath, AssemblyName, regPattern).Dispose();

            // Assembly.Load(Byte[]) case
            RemoteExecutor.Invoke((asmPath, asmName, p) =>
            {
                AppContext.SetSwitch("Switch.System.Diagnostics.StackTrace.ShowILOffsets", true);
                var inMemBlob = File.ReadAllBytes(asmPath);
                var asm2 = Assembly.Load(inMemBlob);
                try
                {
                    asm2.GetType("Program").GetMethod("Foo").Invoke(null, null);
                }
                catch (Exception e)
                {
                    Assert.Contains(asmName, e.InnerException.StackTrace);
                    Assert.Matches(p, e.InnerException.StackTrace);
                }
            }, SourceTestAssemblyPath, AssemblyName, regPattern).Dispose();

            // AssmblyBuilder.DefineDynamicAssembly() case
            RemoteExecutor.Invoke((p) =>
            {
                AppContext.SetSwitch("Switch.System.Diagnostics.StackTrace.ShowILOffsets", true);
                AssemblyName asmName = new AssemblyName("ExceptionTestAssembly");
                AssemblyBuilder asmBldr = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
                ModuleBuilder modBldr = asmBldr.DefineDynamicModule(asmName.Name);
                TypeBuilder tBldr = modBldr.DefineType("Program");
                MethodBuilder mBldr = tBldr.DefineMethod("Foo", MethodAttributes.Public | MethodAttributes.Static, null, null);
                ILGenerator ilGen = mBldr.GetILGenerator();
                ilGen.ThrowException(typeof(NullReferenceException));
                ilGen.Emit(OpCodes.Ret);
                Type t = tBldr.CreateType();
                try
                {
                    t.InvokeMember("Foo", BindingFlags.InvokeMethod, null, null, null);
                }
                catch (Exception e)
                {
                    Assert.Contains("RefEmit_InMemoryManifestModule", e.InnerException.StackTrace);
                    Assert.Matches(p, e.InnerException.StackTrace);
                }
            }, regPattern).Dispose();
        }

        public static Dictionary<string, string[]> MethodExceptionStrings = new()
        {
            { "Foo", new[] {
                @"Exception from Foo2",
                @"V2Methods\.Foo2\(Int32.*Foo2.*\.cs:line 10",
                @"V2Methods\.Foo1\(Int32.*Foo1.*\.cs:line 6",
                @"V1Methods.*Test1",
                @"V1Methods.*Test0",
                @"V2Methods\.Foo\(\).*Foo.*\.cs:line 6"
            }},
            { "Bar", new[] {
                @"Exception from Bar",
                @"V2Methods\.Bar\(Int32.*Bar.*\.cs:line 4",
                @"V2Methods\.Bar\(Int32.*Bar.*\.cs:line 5"
            }},
            {"Quux", new[] {
                @"Exception from Quux1",
                @"V2Methods\.Quux1\(Int32.*Quux1.*\.cs:line 6",
                @"V1Methods.*Test1",
                @"V1Methods.*Test0",
                @"V2Methods\.Quux\(\).*Quux.*\.cs:line 6"
            }},
            { "Quuux", new[] {
                @"Exception from Quuux2",
                @"V2Methods\.Quuux2\(\).*Quuux2.*\.cs:line 4",
                @"V2Methods\.Quuux\(\).*Quuux.*\.cs:line [35]" // if yield finishes before Task is awaited, line 3 else line 5. Either is ok.
            }},
            {"Bux", new[] {
                @"Exception from Baz method.",
                @"V2Methods\.Baz\(\).*Baz.*\.cs:line 4",
                @"V2Methods\.Bux\(\).*Bux.*\.cs:line 6"
            }},
        };

        public static IEnumerable<object[]> Ctor_Async_TestData()
        {
            yield return new object[] { () => V2Methods.Foo(), MethodExceptionStrings["Foo"] };
            yield return new object[] { () => V2Methods.Bar(3), MethodExceptionStrings["Bar"] };
            yield return new object[] { () => V2Methods.Quux(), MethodExceptionStrings["Quux"] };
            yield return new object[] { () => V2Methods.Quuux(), MethodExceptionStrings["Quuux"] };
            yield return new object[] { () => V2Methods.Bux(), MethodExceptionStrings["Bux"] };
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/123979", typeof(PlatformDetection), nameof(PlatformDetection.IsArmProcess))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/124015", typeof(PlatformDetection), nameof(PlatformDetection.IsArm64Process))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/124044", typeof(PlatformDetection), nameof(PlatformDetection.IsCoreClrInterpreter))]
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsRuntimeAsyncSupported))]
        [MemberData(nameof(Ctor_Async_TestData))]
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public async Task ToString_Async(Func<Task> asyncMethod, string[] expectedPatterns)
        {
            Exception? caughtException = null;
            try
            {
                await asyncMethod();
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }

            Assert.NotNull(caughtException);
            string exceptionText = caughtException.ToString();
            int startIndex = 0;
            foreach (string pattern in expectedPatterns)
            {
                Regex regex = new(pattern, RegexOptions.None, TimeSpan.FromSeconds(10));
                Match match = regex.Match(exceptionText, startIndex);
                Assert.True(match.Success, $"Could not find expected pattern '{pattern}' in exception text:\n{exceptionText} starting at index {startIndex}.");
                startIndex = match.Index + match.Length;
            }
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private static StackTrace NoParameters() => new StackTrace();
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private static StackTrace OneParameter(int x) => new StackTrace();
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private static StackTrace TwoParameters(int x, string y) => new StackTrace();

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private unsafe static StackTrace FunctionPointerParameter(delegate*<void> x) => new StackTrace();

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private static StackTrace Generic<T>() => new StackTrace();
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private static StackTrace Generic<T, U>() => new StackTrace();

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private static StackTrace InvokeIgnoredMethod() => Ignored.Method();
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private static StackTrace InvokeIgnoredMethodWithException() => Ignored.MethodWithException();

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
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

            [MethodImpl(MethodImplOptions.NoInlining)]
            public ClassWithConstructor() => StackTrace = new StackTrace();
        }

        private static void VerifyFrames(StackTrace stackTrace, bool hasFileInfo, int skippedFrames)
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

                // On native AOT, the reflection invoke infrastructure uses compiler-generated code
                // that doesn't have a reflection method associated. Limit the checks.
                if (!PlatformDetection.IsNativeAot || (i + skippedFrames) == 0)
                    Assert.NotNull(stackFrame.GetMethod());
            }
        }
    }
}
