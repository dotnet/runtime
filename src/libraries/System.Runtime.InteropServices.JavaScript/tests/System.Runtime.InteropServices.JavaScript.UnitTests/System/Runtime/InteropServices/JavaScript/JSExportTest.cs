// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading;
using Xunit;
using System.Diagnostics.CodeAnalysis;
#pragma warning disable xUnit1026 // Theory methods should use all of their parameters

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    public class JSExportAsyncTest : JSInteropTestBase, IAsyncLifetime
    {
        [Fact]
        public async Task FirstAsyncExportBindsInsideInvocation()
        {
            Assert.Equal(42, await JavaScriptTestHelper.InvokeDeferredFirstTask());
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
        public void SyncJsImportJsExportThrows()
        {
            var ex = Assert.Throws<JSException>(()=>JavaScriptTestHelper.invoke1_Boolean(true, nameof(JavaScriptTestHelper.EchoBoolean)));
            Assert.Contains("Cannot call synchronous C# method", ex.Message);
        }

        [Theory]
        [MemberData(nameof(MarshalBooleanCases))]
        public async Task JsExportBooleanAsync(bool value)
        {
            await JsExportTestAsync(value,
                JavaScriptTestHelper.invoke1_BooleanAsync,
                nameof(JavaScriptTestHelper.EchoBoolean),
                "boolean");
        }

        [Theory]
        [MemberData(nameof(MarshalInt32Cases))]
        public async Task JsExportInt32DiscardNoWait(int value)
        {
            JavaScriptTestHelper.optimizedReached=0;
            JavaScriptTestHelper.invoke1O(value);
            await JavaScriptTestHelper.Delay(50);
            Assert.Equal(value, JavaScriptTestHelper.optimizedReached);
        }

        private async Task JsExportTestAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>(T value
        , Func<T, string, Task<T>> invoke, string echoName, string jsType, string? jsClass = null)
        {
            T res;
            res = await invoke(value, echoName);
            Assert.Equal<T>(value, res);
        }
    }

    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMultithreadingSupported))]
    public class JSExportTest : JSInteropTestBase, IAsyncLifetime
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public unsafe void FailedBindingReleasesUnconsumedPayloads(bool reentrant)
        {
            Type hostType = typeof(JSExportProvider).Assembly.GetType("System.Runtime.InteropServices.JavaScript.JSHostImplementation", throwOnError: true)!;
            Type contextType = typeof(JSExportProvider).Assembly.GetType("System.Runtime.InteropServices.JavaScript.JSProxyContext", throwOnError: true)!;
            Type pendingType = contextType.GetNestedType("PendingJSExport", BindingFlags.NonPublic)!;
            object context = contextType.GetMethod("AssertIsInteropThread", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, null)!;
            var provider = new PayloadFailureProvider("PayloadFailure", isAsync: false);
            JSFunctionBinding signature = PayloadFailureProvider.CreateSignature(
                [JSMarshalerType.Int32, JSMarshalerType.Object, JSMarshalerType.Span(JSMarshalerType.Byte)]);
            object pending = Activator.CreateInstance(pendingType, BindingFlags.Instance | BindingFlags.NonPublic, null,
                [provider, "0", provider.Name, 1, signature], null)!;
            int handle = (int)contextType.GetMethod("AllocJSExportHandle", BindingFlags.Instance | BindingFlags.NonPublic, null, [pendingType], null)!.Invoke(context, [pending])!;
            FieldInfo isBinding = pendingType.GetField("IsBinding", BindingFlags.Instance | BindingFlags.NonPublic)!;
            isBinding.SetValue(pending, reentrant);
            PayloadFailureProvider.FreeSignature(signature);
            MethodInfo bind = hostType.GetMethod("TryBindJSExport", BindingFlags.Static | BindingFlags.NonPublic)!;
            JSMarshalerArgument* arguments = stackalloc JSMarshalerArgument[4];
            Span<byte> borrowed = stackalloc byte[] { 17, 25 };
            try
            {
                for (int attempt = 0; attempt < 2; attempt++)
                {
                    arguments[0].Initialize();
                    arguments[1].Initialize();
                    arguments[2].Initialize();
                    arguments[3].Initialize();
                    arguments[2].ToJS(new object[] { new byte[] { 1, 2, 3 }, "payload", new object[] { new int[] { 4, 5 } } });
                    arguments[3].ToJS(borrowed);
                    Assert.False((bool)bind.Invoke(null, [context, handle, Pointer.Box(arguments, typeof(JSMarshalerArgument*)), null])!);
                    arguments[0].ToManaged(out Exception? error);
                    Assert.Contains(reentrant ? "cannot be bound recursively" : "Payload binding failure", error!.Message);
                    arguments[2].ToManaged(out object? payload);
                    Assert.Null(payload);
                    Assert.Equal(17, borrowed[0]);
                    Assert.Equal(25, borrowed[1]);
                }
                Assert.Equal(reentrant ? 0 : 2, provider.BindAttempts);
            }
            finally
            {
                contextType.GetMethod("FreeJSExportHandle", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(context, [handle]);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task AllocatedArrayBindingFailureCanRetry(bool isAsync)
        {
            string assemblyName = "PayloadFailure" + Guid.NewGuid().ToString("N");
            PayloadFailureProvider.Register(assemblyName, isAsync);
            Assert.Equal(42, await JavaScriptTestHelper.InvokePayloadFailure(assemblyName, isAsync));
        }

        private sealed unsafe class PayloadFailureProvider(string assemblyName, bool isAsync, bool probeReentry = false) : JSExportProvider
        {
            internal string Name => $"[{assemblyName}]Payload:Invoke";
            internal int BindAttempts;
            public override ReadOnlySpan<string> GetExportNames() => new string[] { Name + "#1" };

            public override void Bind(string methodId)
            {
                if (++BindAttempts <= 2)
                    throw new InvalidOperationException("Payload binding failure");

                if (probeReentry)
                    JavaScriptTestHelper.ProbePayloadReentry(assemblyName);
                JSFunctionBinding.BindManagedFunction(Name, 1,
                    [isAsync ? JSMarshalerType.Task(JSMarshalerType.Int32) : JSMarshalerType.Int32, JSMarshalerType.Array(JSMarshalerType.Byte)],
                    isAsync ? &InvokeAsync : &Invoke);
                if (probeReentry)
                    JavaScriptTestHelper.ProbePayloadReentry(assemblyName);
            }

            private static void Invoke(JSMarshalerArgument* arguments)
            {
                arguments[2].ToManaged(out byte[]? bytes);
                arguments[1].ToJS(bytes![0] + bytes[1]);
            }

            private static void InvokeAsync(JSMarshalerArgument* arguments)
            {
                arguments[2].ToManaged(out byte[]? bytes);
                arguments[1].ToJS(Task.FromResult(bytes![0] + bytes[1]),
                    static (ref JSMarshalerArgument argument, int value) => argument.ToJS(value));
            }

            internal static JSFunctionBinding CreateSignature(ReadOnlySpan<JSMarshalerType> types) => GetMethodSignature(null, types, null, null);
            internal static void FreeSignature(JSFunctionBinding signature) => FreeMethodSignatureBuffer(null, signature);

            internal static void Register(string assemblyName, bool isAsync)
            {
                Type contextType = typeof(JSExportProvider).Assembly.GetType("System.Runtime.InteropServices.JavaScript.JSProxyContext", throwOnError: true)!;
                Type pendingType = contextType.GetNestedType("PendingJSExport", BindingFlags.NonPublic)!;
                object context = contextType.GetMethod("AssertIsInteropThread", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, null)!;
                var provider = new PayloadFailureProvider(assemblyName, isAsync, probeReentry: true);
                JSFunctionBinding signature = CreateSignature(
                    [isAsync ? JSMarshalerType.Task(JSMarshalerType.Int32) : JSMarshalerType.Int32, JSMarshalerType.Array(JSMarshalerType.Byte)]);
                try
                {
                    object pending = Activator.CreateInstance(pendingType, BindingFlags.Instance | BindingFlags.NonPublic, null,
                        [provider, "0", provider.Name, 1, signature], null)!;
                    int handle = (int)contextType.GetMethod("AllocJSExportHandle", BindingFlags.Instance | BindingFlags.NonPublic, null, [pendingType], null)!.Invoke(context, [pending])!;
                    void* header = Pointer.Unbox(typeof(JSFunctionBinding).GetField("Header", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(signature)!);
                    JavaScriptTestHelper.RegisterPayloadFailure(assemblyName, handle, header);
                }
                finally
                {
                    FreeSignature(signature);
                }
            }

            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "GetMethodSignature")]
            private static extern JSFunctionBinding GetMethodSignature(
                [UnsafeAccessorType("System.Runtime.InteropServices.JavaScript.JSHostImplementation, System.Runtime.InteropServices.JavaScript")] object? target,
                ReadOnlySpan<JSMarshalerType> types, string? functionName, string? moduleName);

            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "FreeMethodSignatureBuffer")]
            private static extern void FreeMethodSignatureBuffer(
                [UnsafeAccessorType("System.Runtime.InteropServices.JavaScript.JSHostImplementation, System.Runtime.InteropServices.JavaScript")] object? target,
                JSFunctionBinding signature);

        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsCoreCLR))]
        public void PublicTypeMapStillFollowsAssemblyTargets()
        {
            IReadOnlyDictionary<string, Type> map = TypeMapping.GetOrCreateExternalTypeMapping<JSExportProvider>();
            Assert.True(map.TryGetValue("JavaScriptLibrary", out Type? libraryProvider));
            Assert.True(typeof(JSExportProvider).IsAssignableFrom(libraryProvider));
            Assert.True(map.TryGetValue("System.Runtime.InteropServices.JavaScript.Tests", out Type? applicationProvider));
            Assert.True(typeof(JSExportProvider).IsAssignableFrom(applicationProvider));
        }

        [Theory]
        [InlineData("Exports")]
        [InlineData("Catalog.Namespace.Exports/Nested")]
        public async Task RawExportCatalogPreservesAliases(string typeName)
        {
            string assemblyName = "RawCatalog" + Guid.NewGuid().ToString("N");
            JavaScriptTestHelper.RegisterRawExportCatalog(assemblyName,
                [
                    $"[{assemblyName}]{typeName}:Echo#-17",
                    $"[{assemblyName}]{typeName}:Echo#23",
                    $"[{assemblyName}]{typeName}:Unused#-2147483648"
                ]);

            Assert.Equal("Unable to resolve the JavaScript export binding.|Unable to resolve the JavaScript export binding.|Unable to resolve the JavaScript export binding.",
                await JavaScriptTestHelper.InspectRawExportCatalog(assemblyName, typeName));
        }

        [Theory]
        [InlineData("[Other]Exports:Echo#1")]
        [InlineData("[RawCatalogInvalid]Exports:Echo")]
        [InlineData("[RawCatalogInvalid]Exports:Echo#")]
        [InlineData("[RawCatalogInvalid]Exports:Echo#2147483648")]
        [InlineData("[RawCatalogInvalid]Exports:#1")]
        public void RawExportCatalogRejectsInvalidNames(string name)
        {
            JSException exception = Assert.Throws<JSException>(() =>
                JavaScriptTestHelper.RegisterRawExportCatalog("RawCatalogInvalid", [name]));
            Assert.Contains($"Invalid JSExport catalog key '{name}'", exception.Message);
        }

        [Theory]
        [MemberData(nameof(MarshalBooleanCases))]
        public void JsExportBoolean(bool value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_Boolean,
                nameof(JavaScriptTestHelper.EchoBoolean),
                "boolean");
        }

        [Theory]
        [MemberData(nameof(MarshalCharCases))]
        public void JsExportChar(char value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_Char,
                nameof(JavaScriptTestHelper.EchoChar),
                "number");
        }

        [Theory]
        [MemberData(nameof(MarshalByteCases))]
        public void JsExportByte(byte value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_Byte,
                nameof(JavaScriptTestHelper.EchoByte),
                "number");
        }

        [Theory]
        [MemberData(nameof(MarshalInt16Cases))]
        public void JsExportInt16(short value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_Int16,
                nameof(JavaScriptTestHelper.EchoInt16),
                "number");
        }

        [Theory]
        [MemberData(nameof(MarshalInt32Cases))]
        public void JsExportInt32(int value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_Int32,
                nameof(JavaScriptTestHelper.EchoInt32),
                "number");
        }

        [Theory]
        [MemberData(nameof(MarshalInt52Cases))]
        public void JsExportInt52(long value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_Int52,
                nameof(JavaScriptTestHelper.EchoInt52),
                "number");
        }

        [Theory]
        [MemberData(nameof(MarshalBigInt64Cases))]
        public void JsExportBigInt64(long value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_BigInt64,
                nameof(JavaScriptTestHelper.EchoBigInt64),
                "bigint");
        }

        [Theory]
        [MemberData(nameof(MarshalDoubleCases))]
        public void JsExportDouble(double value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_Double,
                nameof(JavaScriptTestHelper.EchoDouble),
                "number");
        }

        [Theory]
        [MemberData(nameof(MarshalSingleCases))]
        public void JsExportSingle(float value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_Single,
                nameof(JavaScriptTestHelper.EchoSingle),
                "number");
        }

        [Theory]
        [MemberData(nameof(MarshalIntPtrCases))]
        public void JsExportIntPtr(IntPtr value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_IntPtr,
                nameof(JavaScriptTestHelper.EchoIntPtr),
                "number");
        }

        [Theory]
        [MemberData(nameof(MarshalIntPtrCases))]
        public unsafe void JsExportVoidPtr(IntPtr xvalue)
        {
            void* value = (void*)xvalue;
            void* res = JavaScriptTestHelper.invoke1_VoidPtr(value, nameof(JavaScriptTestHelper.EchoVoidPtr));
            Assert.True(value == res);
        }

        [Theory]
        [MemberData(nameof(MarshalDateTimeCases))]
        public void JsExportDateTime(DateTime value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_DateTime,
                nameof(JavaScriptTestHelper.EchoDateTime),
                "object", "Date");
        }


        [Theory]
        [MemberData(nameof(MarshalDateTimeOffsetCases))]
        public void JsExportDateTimeOffset(DateTimeOffset value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_DateTimeOffset,
                nameof(JavaScriptTestHelper.EchoDateTimeOffset),
                "object", "Date");
        }

        [Theory]
        [MemberData(nameof(MarshalNullableBooleanCases))]
        public void JsExportNullableBoolean(bool? value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_NullableBoolean,
                nameof(JavaScriptTestHelper.EchoNullableBoolean),
                "boolean");
        }

        [Theory]
        [MemberData(nameof(MarshalNullableInt32Cases))]
        public void JsExportNullableInt32(int? value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_NullableInt32,
                nameof(JavaScriptTestHelper.EchoNullableInt32),
                "number");
        }

        [Theory]
        [MemberData(nameof(MarshalNullableBigInt64Cases))]
        public void JsExportNullableBigInt64(long? value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_NullableBigInt64,
                nameof(JavaScriptTestHelper.EchoNullableBigInt64),
                "bigint");
        }

        [Theory]
        [MemberData(nameof(MarshalNullableIntPtrCases))]
        public void JsExportNullableIntPtr(IntPtr? value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_NullableIntPtr,
                nameof(JavaScriptTestHelper.EchoNullableIntPtr),
                "number");
        }

        [Theory]
        [MemberData(nameof(MarshalNullableDoubleCases))]
        public void JsExportNullableDouble(double? value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_NullableDouble,
                nameof(JavaScriptTestHelper.EchoNullableDouble),
                "number");
        }

        [Theory]
        [MemberData(nameof(MarshalNullableDateTimeCases))]
        public void JsExportNullableDateTime(DateTime? value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_NullableDateTime,
                nameof(JavaScriptTestHelper.EchoNullableDateTime),
                "object");
        }

        [Theory]
        [MemberData(nameof(MarshalStringCases))]
        public void JsExportString(string value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_String,
                nameof(JavaScriptTestHelper.EchoString),
                "string");
        }

        [Fact]
        public void JsExportArraySegmentOfDouble()
        {
            ArraySegment<double> segment = new ArraySegment<double>([1, 2, 3, double.MaxValue, double.MinValue, double.Pi, double.NegativeInfinity, double.PositiveInfinity, double.NaN]);
            ArraySegment<double> res = JavaScriptTestHelper.invoke1_ArraySegmentOfDouble(segment, nameof(JavaScriptTestHelper.EchoArraySegmentOfDouble));
            Assert.Equal(segment.Count, res.Count);
            Assert.Equal(segment.Array, res.Array);
        }

        [Fact]
        public void JsExportArraySegmentOfSingle()
        {
            ArraySegment<float> segment = new ArraySegment<float>([1, 2, 3, float.MaxValue, float.MinValue, float.Pi, float.NegativeInfinity, float.PositiveInfinity, float.NaN]);
            ArraySegment<float> res = JavaScriptTestHelper.invoke1_ArraySegmentOfSingle(segment, nameof(JavaScriptTestHelper.EchoArraySegmentOfSingle));
            Assert.Equal(segment.Count, res.Count);
            Assert.Equal(segment.Array, res.Array);
        }

        [Theory]
        [MemberData(nameof(MarshalDoubleArrayCases))]
        public void JsExportSpanOfDouble(double[] value)
        {
            Span<double> span = new Span<double>(value);
            Span<double> res = JavaScriptTestHelper.invoke1_SpanDouble(span, nameof(JavaScriptTestHelper.EchoSpanDouble));
            Assert.Equal(value, res);
        }

        [Theory]
        [MemberData(nameof(MarshalSingleArrayCases))]
        public void JsExportSpanOfSingle(float[] value)
        {
            Span<float> span = new Span<float>(value);
            Span<float> res = JavaScriptTestHelper.invoke1_SpanSingle(span, nameof(JavaScriptTestHelper.EchoSpanSingle));
            Assert.Equal(value, res);
        }

        [Fact]
        public void JsExportStringNoNs()
        {
            var actual = JavaScriptTestHelper.invoke2_String("test", nameof(JavaScriptTestHelperNoNamespace.EchoString));
            Assert.Equal("test51", actual);
        }

        [Fact]
        public void JsExportStructClassRecords()
        {
            var actual = JavaScriptTestHelper.invokeStructClassRecords("test");
            Assert.Equal(48, actual.Length);
            Assert.Equal("test11", actual[0]);
            Assert.Equal("test12", actual[1]);
            Assert.Equal("test13", actual[2]);
            Assert.Equal("test14", actual[3]);
            Assert.Equal("test15", actual[4]);
            Assert.Equal("test16", actual[5]);
            Assert.Equal("test17", actual[6]);
            Assert.Equal("test18", actual[7]);
            Assert.Equal("test19", actual[8]);
            Assert.Equal("test21", actual[9]);
            Assert.Equal("test22", actual[10]);
            Assert.Equal("test23", actual[11]);
            Assert.Equal("test24", actual[12]);
            Assert.Equal("test25", actual[13]);
            Assert.Equal("test31", actual[14]);
            Assert.Equal("test32", actual[15]);
            Assert.Equal("test33", actual[16]);
            Assert.Equal("test34", actual[17]);
            Assert.Equal("test35", actual[18]);
            Assert.Equal("test41", actual[19]);
            Assert.Equal("test42", actual[20]);
            Assert.Equal("test43", actual[21]);
            Assert.Equal("test44", actual[22]);
            Assert.Equal("test45", actual[23]);
            Assert.Equal("test51", actual[24]);
            Assert.Equal("test52", actual[25]);
            Assert.Equal("test53", actual[26]);
            Assert.Equal("test54", actual[27]);
            Assert.Equal("test55", actual[28]);
            Assert.Equal("test56", actual[29]);
            Assert.Equal("test57", actual[30]);
            Assert.Equal("test58", actual[31]);
            Assert.Equal("test59", actual[32]);
            Assert.Equal("test61", actual[33]);
            Assert.Equal("test62", actual[34]);
            Assert.Equal("test63", actual[35]);
            Assert.Equal("test64", actual[36]);
            Assert.Equal("test65", actual[37]);
            Assert.Equal("test71", actual[38]);
            Assert.Equal("test72", actual[39]);
            Assert.Equal("test73", actual[40]);
            Assert.Equal("test74", actual[41]);
            Assert.Equal("test75", actual[42]);
            Assert.Equal("test81", actual[43]);
            Assert.Equal("test82", actual[44]);
            Assert.Equal("test83", actual[45]);
            Assert.Equal("test84", actual[46]);
            Assert.Equal("test85", actual[47]);
        }

        [Theory]
        [MemberData(nameof(MarshalObjectCases))]
        public void JsExportObject(object value, string clazz)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_Object,
                nameof(JavaScriptTestHelper.EchoObject),
                "object", clazz);
        }

        [Theory]
        [MemberData(nameof(MarshalExceptionCases))]
        public void JsExportException(Exception value, string clazz)
        {
            if (clazz == "JSTestError")
            {
                value = JavaScriptTestHelper.createException("!CreateEx!");
            }

            JsExportTest(value,
                JavaScriptTestHelper.invoke1_Exception,
                nameof(JavaScriptTestHelper.EchoException),
                "object", clazz);
        }

        [Fact]
        public void JsExportCatchToString()
        {
            var toString = JavaScriptTestHelper.catch1toString("-t-e-s-t-", nameof(JavaScriptTestHelper.ThrowFromJSExport));
            Assert.DoesNotContain("Unexpected error", toString);
            Assert.Contains("-t-e-s-t-", toString);
            Assert.DoesNotContain(nameof(JavaScriptTestHelper.ThrowFromJSExport), toString);
        }

        [Fact]
        public void JsExportCatchStack()
        {
            var stack = JavaScriptTestHelper.catch1stack("-t-e-s-t-", nameof(JavaScriptTestHelper.ThrowFromJSExport));
            Assert.Contains(nameof(JavaScriptTestHelper.ThrowFromJSExport), stack);
            if (PlatformDetection.IsBrowserDomSupportedOrNodeJS)
            {
                Assert.Contains("catch1stack", stack);
            }
        }

        [Theory]
        [MemberData(nameof(MarshalIJSObjectCases))]
        public void JsExportIJSObject(JSObject value, string clazz)
        {
            if (clazz == "JSData")
            {
                value = JavaScriptTestHelper.createData("!CreateJS!");
            }

            JsExportTest(value,
                JavaScriptTestHelper.invoke1_JSObject,
                nameof(JavaScriptTestHelper.EchoIJSObject),
                "object", clazz);
        }

        [Theory]
        [MemberData(nameof(MarshalInt32Cases))]
        public async Task JsExportTaskOfInt(int value)
        {
            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();

            var res = JavaScriptTestHelper.invoke1_TaskOfInt(tcs.Task, nameof(JavaScriptTestHelper.AwaitTaskOfObject));
            tcs.SetResult(value);
            await Task.Yield();
            var rr = await res;
            await Task.Yield();
            Assert.Equal(value, rr);
            //GC.Collect();
        }

        [Theory]
        [MemberData(nameof(MarshalBigInt64Cases))]
        public async Task JsExportTaskOfBigLong(long value)
        {
            TaskCompletionSource<long> tcs = new TaskCompletionSource<long>();
            var res = JavaScriptTestHelper.invoke1_TaskOfBigLong(tcs.Task, nameof(JavaScriptTestHelper.AwaitTaskOfInt64));
            tcs.SetResult(value); // unresolved task marshalls promise and resolves on completion
            await Task.Yield();
            var rr = await res;
            await Task.Yield();
            Assert.Equal(value, rr);
        }

        [Theory]
        [MemberData(nameof(MarshalBigInt64Cases))]
        public async Task JsExportCompletedTaskOfBigLong(long value)
        {
            TaskCompletionSource<long> tcs = new TaskCompletionSource<long>();
            tcs.SetResult(value); // completed task marshalls value immediately
            var res = JavaScriptTestHelper.invoke1_TaskOfBigLong(tcs.Task, nameof(JavaScriptTestHelper.AwaitTaskOfInt64));
            await Task.Yield();
            var rr = await res;
            await Task.Yield();
            Assert.Equal(value, rr);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMultithreadingSupported))]
        public void JsExportCallback_FunctionIntInt()
        {
            int called = -1;
            var chain = JavaScriptTestHelper.invoke1_FuncOfIntInt((int a) =>
            {
                called = a;
                return a;
            }, nameof(JavaScriptTestHelper.BackFuncOfIntInt));

            Assert.Equal(-1, called);
            var actual = chain(42);
            Assert.Equal(42, actual);
            Assert.Equal(42, called);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMultithreadingSupported))]
        public void JsExportCallback_FunctionIntIntThrow()
        {
            int called = -1;
            var expected = new Exception("test!!");
            var chain = JavaScriptTestHelper.invoke1_FuncOfIntInt((int a) =>
            {
                called = a;
                throw expected;
            }, nameof(JavaScriptTestHelper.BackFuncOfIntInt));

            Assert.Equal(-1, called);
            var actual = Assert.Throws<Exception>(() => chain(42));
            Assert.Equal(42, called);
            Assert.Same(expected, actual);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMultithreadingSupported))]
        public async Task JsExportFunctionDateTimeDateTime()
        {
            DateTime input = DateTime.Now;
            DateTime receivedArg = DateTime.MinValue;
            DateTime returnVal = JavaScriptTestHelper.invokeDelegateOfDateTime((DateTime date) => {
                return receivedArg = date;
            }, input, 0);
            Assert.Equal(input, receivedArg);
            Assert.Equal(input, returnVal);
        }

        private void JsExportTest<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>(T value
        , Func<T, string, T> invoke, string echoName, string jsType, string? jsClass = null)
        {
            T res;
            res = invoke(value, echoName);
            Assert.Equal<T>(value, res);
        }

        [Fact]
        public async Task InternalsVisibleToDoesntBreak()
        {
            Assert.Equal(JavaScriptLibrary.JavaScriptInterop.ValidationMethod(5, 6), await JavaScriptTestHelper.callJavaScriptLibrary(5, 6));
        }

        [Fact]
        public async Task JSExportCompletedTaskReturnsResolvedPromise()
        {
            string result = await JavaScriptTestHelper.InvokeReturnCompletedTask();
            Assert.Equal("resolved", result);
        }

        #region Assertion Errors
        [Fact]
        public async Task JsExportTaskOfShortOutOfRange_ThrowsAssertionInTaskContinuation()
        {
            // 1<<16 is out of range, passed to js and back, marshalling ts code asserts out of range and throws
            Task<short> res = JavaScriptTestHelper.invoke1_TaskOfOutOfRangeShort(Task.FromResult(1 << 16), nameof(JavaScriptTestHelper.AwaitTaskOfShort));
            JSException ex = await Assert.ThrowsAsync<JSException>(() => res);
            Assert.Equal("Error: Assert failed: Overflow: value 65536 is out of -32768 32767 range", ex.Message);
        }

        [Fact]
        public async Task JsExportTaskOfStringTypeAssertion_ThrowsAssertionInTaskContinuation()
        {
            // long value cannot be converted to string, error thrown through continuation in CS
            Task<string> res = JavaScriptTestHelper.invoke1_TaskOfLong_ExceptionReturnTypeAssert(Task.FromResult(1L << 32), nameof(JavaScriptTestHelper.AwaitTaskOfString));
            JSException ex = await Assert.ThrowsAsync<JSException>(() => res);
            Assert.Equal("Error: Assert failed: Value is not a String", ex.Message);
        }

        [Fact]
        public async Task JsExportTaskOfLong_TaskReturnValue_OverflowInt52()
        {
            long value = 1L << 53;
            TaskCompletionSource<long> tcs = new TaskCompletionSource<long>();
            var res = JavaScriptTestHelper.invoke1_TaskOfLong(tcs.Task, nameof(JavaScriptTestHelper.AwaitTaskOfInt64));
            tcs.SetResult(value);
            JSException ex = await Assert.ThrowsAsync<JSException>(() => res);
            Assert.Equal("Error: Assert failed: Value is not an integer: 9007199254740992 (bigint)", ex.Message);
        }

        [Fact]
        public async Task JsExportTaskOfDateTime_TaskReturnValue_OverflowNETDateTime()
        {
            var res = JavaScriptTestHelper.invokeExportWithTaskOfMaxJSDateTime(nameof(JavaScriptTestHelper.AwaitTaskOfDateTime));
            JSException ex = await Assert.ThrowsAsync<JSException>(() => res);
            Assert.Equal("Error: Assert failed: Overflow: value +275760-09-13T00:00:00.000Z is out of 0001-01-01T00:00:00.000Z 9999-12-31T23:59:59.999Z range", ex.Message);
        }

        [Fact]
        public async Task JsExportDateTime_ReturnValue_OverflowNETDateTime()
        {
            JSException ex = Assert.Throws<JSException>(() => JavaScriptTestHelper.invokeExportWithMaxJSDateTime(nameof(JavaScriptTestHelper.EchoDateTime)));
            Assert.Equal("Error: Assert failed: Overflow: value +275760-09-13T00:00:00.000Z is out of 0001-01-01T00:00:00.000Z 9999-12-31T23:59:59.999Z range", ex.Message);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMultithreadingSupported))]
        public async Task JsExportFuncOfDateTime_Argument_OverflowNETDateTime()
        {
            DateTime receivedArg = DateTime.MinValue;
            JSException ex = Assert.Throws<JSException>(() => JavaScriptTestHelper.invokeDelegateOfDateTime((DateTime date) => {
                return receivedArg = date;
            }, DateTime.MaxValue, 60_001));
            Assert.Equal("Error: Assert failed: Overflow: value +010000-01-01T00:01:00.000Z is out of 0001-01-01T00:00:00.000Z 9999-12-31T23:59:59.999Z range", ex.Message);
            Assert.Equal(DateTime.MinValue, receivedArg); // delegate invoke failed, no change to arg
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMultithreadingSupported))]
        public void JsExportCallback_FunctionLongLong_OverflowInt52_JSSide()
        {
            long called = -1;
            Assert.Equal(-1, called);
            JSException ex = Assert.Throws<JSException>(() => JavaScriptTestHelper.invokeFuncOfLongLong((long a) =>
            {
                return called = a;
            }, 9007199254740991, offset: 1));
            Assert.Equal(-1, called);
            Assert.Equal("Error: Assert failed: Value is not an integer: 9007199254740992 (number)", ex.Message);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMultithreadingSupported))]
        public void JsExportCallback_FunctionLongLong_OverflowInt52_NETSide()
        {
            long called = -1;
            var chain = JavaScriptTestHelper.invoke1_FuncOfLongLong((long a) =>
            {
                return called = a;
            }, nameof(JavaScriptTestHelper.BackFuncOfLongLong));

            Assert.Equal(-1, called);
            OverflowException ex = Assert.Throws<OverflowException>(() => chain(long.MaxValue));
            Assert.Equal(-1, called);
            Assert.Equal("Overflow: value 9223372036854775807 is out of -9007199254740991 9007199254740991 range.", ex.Message);
        }
        #endregion
    }
}
