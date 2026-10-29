// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading;
using Xunit;
using System.Diagnostics.CodeAnalysis;
#pragma warning disable xUnit1026 // Theory methods should use all of their parameters

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    [ConditionalClass(typeof(WorkerInteropTest), nameof(IsBrowserDomSupportedOrWorker))]
    public partial class WorkerInteropTest
    {
        [JSImport("echo", nameof(WorkerInteropTest))]
        private static partial nint EchoPointerOnUI(nint value);
        [JSImport("echo", nameof(WorkerInteropTest))]
        [return: JSMarshalAs<JSType.Date>]
        private static partial DateTime EchoDateOnUI([JSMarshalAs<JSType.Date>] DateTime value);
        [JSImport("echo", nameof(WorkerInteropTest))]
        [return: JSMarshalAs<JSType.Date>]
        private static partial DateTimeOffset EchoOffsetOnUI([JSMarshalAs<JSType.Date>] DateTimeOffset value);
        [JSImport("echo", nameof(WorkerInteropTest))]
        private static partial int? EchoNullableOnUI(int? value);
        [JSImport("echo", nameof(WorkerInteropTest))]
        private static partial bool? EchoNullableBooleanOnUI(bool? value);
        [JSImport("echo", nameof(WorkerInteropTest))]
        private static partial double? EchoNullableDoubleOnUI(double? value);
        [JSImport("echo", nameof(WorkerInteropTest))]
        private static partial byte? EchoNullableByteOnUI(byte? value);
        [JSImport("echo", nameof(WorkerInteropTest))]
        private static partial char? EchoNullableCharOnUI(char? value);
        [JSImport("echo", nameof(WorkerInteropTest))]
        private static partial short? EchoNullableInt16OnUI(short? value);
        [JSImport("echo", nameof(WorkerInteropTest))]
        private static partial float? EchoNullableSingleOnUI(float? value);
        [JSImport("echo", nameof(WorkerInteropTest))]
        private static partial nint? EchoNullablePointerOnUI(nint? value);
        [JSImport("echo", nameof(WorkerInteropTest))]
        [return: JSMarshalAs<JSType.Number>]
        private static partial long? EchoNullableNumberOnUI([JSMarshalAs<JSType.Number>] long? value);
        [JSImport("echo", nameof(WorkerInteropTest))]
        [return: JSMarshalAs<JSType.Date>]
        private static partial DateTimeOffset? EchoNullableOffsetOnUI([JSMarshalAs<JSType.Date>] DateTimeOffset? value);
        [JSImport("echo", nameof(WorkerInteropTest))]
        [return: JSMarshalAs<JSType.BigInt>]
        private static partial long? EchoNullableBigIntOnUI([JSMarshalAs<JSType.BigInt>] long? value);
        [JSImport("echo", nameof(WorkerInteropTest))]
        [return: JSMarshalAs<JSType.Date>]
        private static partial DateTime? EchoNullableDateOnUI([JSMarshalAs<JSType.Date>] DateTime? value);
        [JSImport("echo", nameof(WorkerInteropTest))]
        private static partial byte[] EchoBytesOnUI(byte[] value);
        [JSImport("echo", nameof(WorkerInteropTest))]
        private static partial int[] EchoIntegersOnUI(int[] value);
        [JSImport("echo", nameof(WorkerInteropTest))]
        private static partial float[] EchoSinglesOnUI(float[] value);
        [JSImport("echo", nameof(WorkerInteropTest))]
        private static partial double[] EchoDoublesOnUI(double[] value);
        [JSImport("echo", nameof(WorkerInteropTest))]
        private static partial string[] EchoStringsOnUI(string[] value);
        [JSImport("echo", nameof(WorkerInteropTest))]
        [return: JSMarshalAs<JSType.Any>]
        private static partial object EchoObjectOnUI([JSMarshalAs<JSType.Any>] object value);
        [JSImport("echo", nameof(WorkerInteropTest))]
        private static partial Exception EchoExceptionOnUI(Exception value);
        [JSImport("echo", nameof(WorkerInteropTest))]
        private static partial JSObject EchoJSObjectOnUI(JSObject value);
        [JSImport("echo", nameof(WorkerInteropTest))]
        private static partial JSObject[] EchoJSObjectsOnUI(JSObject[] value);
        [JSImport("echo", nameof(WorkerInteropTest))]
        [return: JSMarshalAs<JSType.Array<JSType.Any>>]
        private static partial object[] EchoObjectsOnUI([JSMarshalAs<JSType.Array<JSType.Any>>] object[] value);
        [JSImport("newObject", nameof(WorkerInteropTest))]
        private static partial JSObject NewObjectOnUI();
        [JSImport("echoAsync", nameof(WorkerInteropTest))]
        [return: JSMarshalAs<JSType.Promise<JSType.Any>>]
        private static partial Task<object> EchoArrayAsyncOnUI(int[] value);
        [JSImport("echoAsync", nameof(WorkerInteropTest))]
        [return: JSMarshalAs<JSType.Promise<JSType.Any>>]
        private static partial Task<object> EchoObjectAsyncOnUI([JSMarshalAs<JSType.Any>] object value);
        [JSImport("callFunction", nameof(WorkerInteropTest))]
        private static partial int CallFunctionOnUI([JSMarshalAs<JSType.Function<JSType.Number, JSType.Number>>] Func<int, int> function, int value);
        [JSImport("echo", nameof(WorkerInteropTest))]
        [return: JSMarshalAs<JSType.Function<JSType.Number, JSType.Number>>]
        private static partial Func<int, int> EchoFunctionOnUI([JSMarshalAs<JSType.Function<JSType.Number, JSType.Number>>] Func<int, int> function);
        [JSImport("callAction", nameof(WorkerInteropTest))]
        private static partial void CallActionOnUI([JSMarshalAs<JSType.Function>] Action action);
        [JSImport("echoTask", nameof(WorkerInteropTest))]
        private static partial Task<int> EchoTaskOnUI(Task<int> value);
        [JSImport("editView", nameof(WorkerInteropTest))]
        [return: JSMarshalAs<JSType.MemoryView>]
        private static partial Span<byte> EditSpanOnUI([JSMarshalAs<JSType.MemoryView>] Span<byte> value, bool fail);
        [JSImport("editView", nameof(WorkerInteropTest))]
        [return: JSMarshalAs<JSType.MemoryView>]
        private static partial ArraySegment<int> EditSegmentOnUI([JSMarshalAs<JSType.MemoryView>] ArraySegment<int> value, bool fail);
        [JSImport("editView", nameof(WorkerInteropTest))]
        [return: JSMarshalAs<JSType.MemoryView>]
        private static partial Span<int> EditInt32SpanOnUI([JSMarshalAs<JSType.MemoryView>] Span<int> value, bool fail);
        [JSImport("editView", nameof(WorkerInteropTest))]
        [return: JSMarshalAs<JSType.MemoryView>]
        private static partial Span<float> EditSingleSpanOnUI([JSMarshalAs<JSType.MemoryView>] Span<float> value, bool fail);
        [JSImport("editView", nameof(WorkerInteropTest))]
        [return: JSMarshalAs<JSType.MemoryView>]
        private static partial Span<double> EditDoubleSpanOnUI([JSMarshalAs<JSType.MemoryView>] Span<double> value, bool fail);
        [JSImport("editView", nameof(WorkerInteropTest))]
        [return: JSMarshalAs<JSType.MemoryView>]
        private static partial ArraySegment<byte> EditByteSegmentOnUI([JSMarshalAs<JSType.MemoryView>] ArraySegment<byte> value, bool fail);
        [JSImport("editView", nameof(WorkerInteropTest))]
        [return: JSMarshalAs<JSType.MemoryView>]
        private static partial ArraySegment<float> EditSingleSegmentOnUI([JSMarshalAs<JSType.MemoryView>] ArraySegment<float> value, bool fail);
        [JSImport("editView", nameof(WorkerInteropTest))]
        [return: JSMarshalAs<JSType.MemoryView>]
        private static partial ArraySegment<double> EditDoubleSegmentOnUI([JSMarshalAs<JSType.MemoryView>] ArraySegment<double> value, bool fail);
        [JSImport("disposeView", nameof(WorkerInteropTest))]
        private static partial bool DisposeLastViewOnUI();
        [JSImport("disposeManaged", nameof(WorkerInteropTest))]
        private static partial bool DisposeManagedOnUI([JSMarshalAs<JSType.Any>] object value);
        [JSImport("disposeManaged", nameof(WorkerInteropTest))]
        private static partial bool DisposeExceptionOnUI(Exception value);
        [JSImport("voidArgument", nameof(WorkerInteropTest))]
        [return: JSMarshalAs<JSType.Discard>]
        private static partial void DiscardOnUI(string value);
        [JSImport("voidArgument", nameof(WorkerInteropTest))]
        [return: JSMarshalAs<JSType.DiscardNoWait>]
        private static partial void DiscardNoWaitOnUI(string value);
        [JSImport("discardContracts", nameof(WorkerInteropTest))]
        private static partial int DiscardContractsOnUI();

        [JSExport]
        [return: JSMarshalAs<JSType.Discard>]
        public static void DiscardExport(string value) => DiscardOnUI(value);
        [JSExport]
        [return: JSMarshalAs<JSType.DiscardNoWait>]
        public static void DiscardNoWaitExport(string value) => DiscardNoWaitOnUI(value);
        [JSImport("remainingContracts", nameof(WorkerInteropTest))]
        private static partial Task<int> RemainingContractsOnUI();
        [JSImport("viewDisposed", nameof(WorkerInteropTest))]
        private static partial bool LastViewDisposedOnUI();

        [JSExport] public static nint EchoPointer(nint value) => value;
        [JSExport]
        [return: JSMarshalAs<JSType.Date>]
        public static DateTime EchoDate([JSMarshalAs<JSType.Date>] DateTime value) => value;
        [JSExport]
        [return: JSMarshalAs<JSType.Date>]
        public static DateTimeOffset EchoOffset([JSMarshalAs<JSType.Date>] DateTimeOffset value) => value;
        [JSExport] public static int? EchoNullable(int? value) => value;
        [JSExport] public static bool? EchoNullableBoolean(bool? value) => value;
        [JSExport] public static double? EchoNullableDouble(double? value) => value;
        [JSExport]
        [return: JSMarshalAs<JSType.BigInt>]
        public static long? EchoNullableBigInt([JSMarshalAs<JSType.BigInt>] long? value) => value;
        [JSExport]
        [return: JSMarshalAs<JSType.Date>]
        public static DateTime? EchoNullableDate([JSMarshalAs<JSType.Date>] DateTime? value) => value;
        [JSExport] public static byte[] EchoBytes(byte[] value) => value;
        [JSExport] public static int[] EchoIntegers(int[] value) => value;
        [JSExport] public static float[] EchoSingles(float[] value) => value;
        [JSExport] public static double[] EchoDoubles(double[] value) => value;
        [JSExport] public static string[] EchoStrings(string[] value) => value;
        [JSExport]
        [return: JSMarshalAs<JSType.Any>]
        public static object EchoObject([JSMarshalAs<JSType.Any>] object value) => value;
        [JSExport] public static Exception EchoException(Exception value) => value;
        [JSExport] public static Exception CreateException() => new InvalidOperationException("managed exception identity");
        [JSExport] public static void ThrowException(Exception value) => throw value;
        [JSExport]
        public static async Task ThrowExceptionAsync(Exception value)
        {
            await Task.Yield();
            throw value;
        }
        [JSExport] public static JSObject EchoJSObject(JSObject value) => value;
        [JSExport]
        public static int ApplyFunction([JSMarshalAs<JSType.Function<JSType.Number, JSType.Number>>] Func<int, int> function, int value) => function(value);
        [JSExport]
        public static void ApplyAction([JSMarshalAs<JSType.Function>] Action action) => action();
        [JSExport] public static async Task<int> AwaitTask(Task<int> value) => await value;
        [JSExport]
        [return: JSMarshalAs<JSType.Promise<JSType.Any>>]
        public static async Task<object> EchoArrayAsync(int[] value) => await EchoArrayAsyncOnUI(value);
        [JSExport]
        [return: JSMarshalAs<JSType.Promise<JSType.Any>>]
        public static async Task<object> EchoObjectAsync([JSMarshalAs<JSType.Any>] object value) => await EchoObjectAsyncOnUI(value);

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        [InlineData(int.MaxValue)]
        public void PointerInterop(int value) => Assert.Equal((nint)value, EchoPointerOnUI(value));

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void NullableInterop(bool hasValue)
        {
            Assert.Equal(hasValue ? 42 : null, EchoNullableOnUI(hasValue ? 42 : null));
            Assert.Equal(hasValue ? false : null, EchoNullableBooleanOnUI(hasValue ? false : null));
            Assert.Equal(hasValue ? -1.25 : null, EchoNullableDoubleOnUI(hasValue ? -1.25 : null));
            Assert.Equal(hasValue ? (byte)255 : null, EchoNullableByteOnUI(hasValue ? (byte)255 : null));
            Assert.Equal(hasValue ? '\uD800' : null, EchoNullableCharOnUI(hasValue ? '\uD800' : null));
            Assert.Equal(hasValue ? short.MinValue : null, EchoNullableInt16OnUI(hasValue ? short.MinValue : null));
            Assert.Equal(hasValue ? 1.25f : null, EchoNullableSingleOnUI(hasValue ? 1.25f : null));
            Assert.Equal(hasValue ? (nint)(-1) : null, EchoNullablePointerOnUI(hasValue ? (nint)(-1) : null));
            Assert.Equal(hasValue ? 9007199254740991L : null, EchoNullableNumberOnUI(hasValue ? 9007199254740991L : null));
            Assert.Equal(hasValue ? long.MaxValue : null, EchoNullableBigIntOnUI(hasValue ? long.MaxValue : null));
            DateTime? date = hasValue ? DateTime.UnixEpoch.AddMilliseconds(123456) : null;
            Assert.Equal(date, EchoNullableDateOnUI(date));
            DateTimeOffset? offset = hasValue ? new DateTimeOffset(date.Value) : null;
            Assert.Equal(offset, EchoNullableOffsetOnUI(offset));
        }

        [Theory]
        [InlineData(-62135596800000L)]
        [InlineData(0L)]
        [InlineData(253402300799999L)]
        public void DateInterop(long milliseconds)
        {
            DateTime date = DateTime.UnixEpoch.AddMilliseconds(milliseconds);
            Assert.Equal(date, EchoDateOnUI(date));
            Assert.Equal(new DateTimeOffset(date), EchoOffsetOnUI(new DateTimeOffset(date)));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(1000)]
        public async Task ArrayInterop(int length)
        {
            byte[] bytes = new byte[length];
            int[] integers = new int[length];
            float[] singles = new float[length];
            double[] doubles = new double[length];
            string[] strings = new string[length];
            for (int i = 0; i < length; i++)
            {
                bytes[i] = (byte)i;
                integers[i] = i - 500;
                singles[i] = i + 0.25f;
                doubles[i] = i + 0.125;
                strings[i] = i % 2 is 0 ? null : "text\0\u0130\U0001F600";
            }
            Assert.Equal(bytes, EchoBytesOnUI(bytes));
            Assert.Equal(integers, EchoIntegersOnUI(integers));
            Assert.Equal(singles, EchoSinglesOnUI(singles));
            Assert.Equal(doubles, EchoDoublesOnUI(doubles));
            Assert.Equal(strings, EchoStringsOnUI(strings));
            Assert.Equal(integers, Assert.IsType<int[]>(await EchoArrayAsyncOnUI(integers)));
        }

        [Fact]
        public async Task ReferenceInterop()
        {
            object value = new object();
            Assert.Same(value, EchoObjectOnUI(value));
            Assert.Same(value, await EchoObjectAsyncOnUI(value));
            Exception exception = new InvalidOperationException("reference exception");
            Assert.Same(exception, EchoExceptionOnUI(exception));
            using JSObject jsObject = NewObjectOnUI();
            Assert.True(jsObject.HasProperty("value"));
            Assert.False(jsObject.HasProperty("missing"));
            Assert.Equal(42, jsObject.GetPropertyAsInt32("value"));
            jsObject.SetProperty("value", 43);
            using JSObject echoed = EchoJSObjectOnUI(jsObject);
            Assert.Same(jsObject, echoed);
            Assert.Equal(43, echoed.GetPropertyAsInt32("value"));
            JSObject[] objects = EchoJSObjectsOnUI([jsObject, null, jsObject]);
            Assert.Same(jsObject, objects[0]);
            Assert.Null(objects[1]);
            Assert.Same(jsObject, objects[2]);
            object[] values = EchoObjectsOnUI([value, "text\0", true, 42.5, null, exception, jsObject]);
            Assert.Same(value, values[0]);
            Assert.Equal("text\0", values[1]);
            Assert.Equal(true, values[2]);
            Assert.Equal(42.5, values[3]);
            Assert.Null(values[4]);
            Assert.Same(exception, values[5]);
            Assert.Same(jsObject, values[6]);
        }

        [Fact]
        public void DelegateInterop()
        {
            Assert.Equal(43, CallFunctionOnUI(value => value + 1, 42));
            Func<int, int> echoed = EchoFunctionOnUI(value => value + 2);
            Assert.Equal(44, echoed(42));
            int count = 0;
            CallActionOnUI(() => count++);
            Assert.Equal(1, count);
        }

        [Fact]
        public void ReferenceDisposal()
        {
            Assert.True(DisposeManagedOnUI(new object()));
            Assert.True(DisposeExceptionOnUI(new InvalidOperationException("disposed reference exception")));
        }

        [Fact]
        public void DiscardInterop() => Assert.Equal(42, DiscardContractsOnUI());

        [Fact]
        public async Task TaskArgumentInterop() => Assert.Equal(42, await EchoTaskOnUI(Task.FromResult(42)));

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void MemoryViewInterop(bool fail)
        {
            byte[] bytes = [1, 2, 3];
            if (fail)
            {
                JSException exception = Assert.Throws<JSException>(() => EditSpanOnUI(bytes, true));
                Assert.Contains("view failure", exception.Message);
                _ = exception.StackTrace;
            }
            else
            {
                Assert.Equal(new byte[] { 9, 2, 3 }, EditSpanOnUI(bytes, false).ToArray());
            }
            Assert.Equal(9, bytes[0]);
            Assert.True(LastViewDisposedOnUI());
            int[] integers = [1, 2, 3];
            ArraySegment<int> segment = EditSegmentOnUI(new ArraySegment<int>(integers), false);
            Assert.Equal(9, segment[0]);
            Assert.Equal(9, integers[0]);
            Assert.True(DisposeLastViewOnUI());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void MemoryViewElementTypes(int type)
        {
            switch (type)
            {
                case 0:
                    byte[] bytes = [1, 2, 3];
                    Assert.Equal(9, EditSpanOnUI(bytes, false)[0]);
                    Assert.Equal(9, EditByteSegmentOnUI(new ArraySegment<byte>(bytes), false)[0]);
                    break;
                case 1:
                    int[] integers = [1, 2, 3];
                    Assert.Equal(9, EditInt32SpanOnUI(integers, false)[0]);
                    Assert.Equal(9, EditSegmentOnUI(new ArraySegment<int>(integers), false)[0]);
                    break;
                case 2:
                    float[] singles = [1, 2, 3];
                    Assert.Equal(9, EditSingleSpanOnUI(singles, false)[0]);
                    Assert.Equal(9, EditSingleSegmentOnUI(new ArraySegment<float>(singles), false)[0]);
                    break;
                default:
                    double[] doubles = [1, 2, 3];
                    Assert.Equal(9, EditDoubleSpanOnUI(doubles, false)[0]);
                    Assert.Equal(9, EditDoubleSegmentOnUI(new ArraySegment<double>(doubles), false)[0]);
                    break;
            }
            Assert.True(DisposeLastViewOnUI());
        }

        [Fact]
        public async Task RemainingExportContracts() => Assert.Equal(42, await RemainingContractsOnUI());

        public static bool IsBrowserDomSupportedOrWorker => PlatformDetection.IsBrowserDomSupported
            || Environment.GetEnvironmentVariable("DOTNET_TEST_WITH_WORKER") == "1";

        [JSImport("echo", nameof(WorkerInteropTest))]
        private static partial int EchoOnUI(int value);

        [JSImport("invokeEcho", nameof(WorkerInteropTest))]
        private static partial int InvokeEchoOnUI(int value);

        [JSImport("invokeNested", nameof(WorkerInteropTest))]
        private static partial int InvokeNestedOnUI(int value);

        [JSImport("echoAsync", nameof(WorkerInteropTest))]
        private static partial Task<int> EchoAsyncOnUI(int value);

        [JSImport("invokeAsync", nameof(WorkerInteropTest))]
        private static partial Task<int> InvokeAsyncOnUI(int value);

        [JSImport("fail", nameof(WorkerInteropTest))]
        private static partial int FailOnUI(int value);

        [JSImport("mode", nameof(WorkerInteropTest))]
        private static partial int GetModeOnUI(int value);

        [JSImport("echoString", nameof(WorkerInteropTest))]
        private static partial string EchoStringOnUI(string value);

        [JSImport("invokeString", nameof(WorkerInteropTest))]
        private static partial string InvokeStringOnUI(string value);

        [JSImport("echoStringAsync", nameof(WorkerInteropTest))]
        private static partial Task<string> EchoStringAsyncOnUI(string value);

        [JSImport("invokeStringAsync", nameof(WorkerInteropTest))]
        private static partial Task<string> InvokeStringAsyncOnUI(string value);

        [JSImport("nestedString", nameof(WorkerInteropTest))]
        private static partial string NestedStringOnUI(string value);

        [JSImport("failString", nameof(WorkerInteropTest))]
        private static partial string FailStringOnUI(string value);

        [JSImport("failStringAsync", nameof(WorkerInteropTest))]
        private static partial Task<string> FailStringAsyncOnUI(string value);

        [JSImport("stringLength", nameof(WorkerInteropTest))]
        private static partial int StringLengthOnUI(string value);

        [JSImport("largeString", nameof(WorkerInteropTest))]
        private static partial string LargeStringOnUI(int length);

        [JSImport("growString", nameof(WorkerInteropTest))]
        private static partial string GrowStringOnUI(int length);

        [JSImport("echoBoolean", nameof(WorkerInteropTest))]
        private static partial bool EchoBooleanOnUI(bool value);

        [JSImport("invokeBoolean", nameof(WorkerInteropTest))]
        private static partial bool InvokeBooleanOnUI(bool value);

        [JSImport("echoBooleanAsync", nameof(WorkerInteropTest))]
        private static partial Task<bool> EchoBooleanAsyncOnUI(bool value);

        [JSImport("invokeBooleanAsync", nameof(WorkerInteropTest))]
        private static partial Task<bool> InvokeBooleanAsyncOnUI(bool value);

        [JSImport("echoNumber", nameof(WorkerInteropTest))]
        private static partial double EchoDoubleOnUI(double value);

        [JSImport("invokeDouble", nameof(WorkerInteropTest))]
        private static partial double InvokeDoubleOnUI(double value);

        [JSImport("echoNumberAsync", nameof(WorkerInteropTest))]
        private static partial Task<double> EchoDoubleAsyncOnUI(double value);

        [JSImport("invokeDoubleAsync", nameof(WorkerInteropTest))]
        private static partial Task<double> InvokeDoubleAsyncOnUI(double value);

        [JSImport("echoNumber", nameof(WorkerInteropTest))]
        private static partial float EchoSingleOnUI(float value);

        [JSImport("invokeSingle", nameof(WorkerInteropTest))]
        private static partial float InvokeSingleOnUI(float value);

        [JSImport("echoNumberAsync", nameof(WorkerInteropTest))]
        private static partial Task<float> EchoSingleAsyncOnUI(float value);

        [JSImport("invokeSingleAsync", nameof(WorkerInteropTest))]
        private static partial Task<float> InvokeSingleAsyncOnUI(float value);

        [JSImport("primitiveContracts", nameof(WorkerInteropTest))]
        private static partial int CheckPrimitiveContractsOnUI(int value);

        [JSImport("nullPrimitive", nameof(WorkerInteropTest))]
        private static partial bool NullBooleanOnUI(int value);

        [JSImport("nullPrimitive", nameof(WorkerInteropTest))]
        private static partial double NullDoubleOnUI(int value);

        [JSImport("nullPrimitive", nameof(WorkerInteropTest))]
        private static partial float NullSingleOnUI(int value);

        [JSImport("nullPrimitiveAsync", nameof(WorkerInteropTest))]
        private static partial Task<bool> NullBooleanAsyncOnUI(int value);

        [JSImport("nullPrimitiveAsync", nameof(WorkerInteropTest))]
        private static partial Task<double> NullDoubleAsyncOnUI(int value);

        [JSImport("nullPrimitiveAsync", nameof(WorkerInteropTest))]
        private static partial Task<float> NullSingleAsyncOnUI(int value);

        [JSImport("invalidPrimitive", nameof(WorkerInteropTest))]
        private static partial bool InvalidBooleanOnUI(int value);

        [JSImport("invalidPrimitive", nameof(WorkerInteropTest))]
        private static partial double InvalidDoubleOnUI(int value);

        [JSImport("invalidPrimitive", nameof(WorkerInteropTest))]
        private static partial float InvalidSingleOnUI(int value);

        [JSImport("noArgumentValue", nameof(WorkerInteropTest))]
        private static partial int NoArgumentValueOnUI();

        [JSImport("noArgumentString", nameof(WorkerInteropTest))]
        private static partial string NoArgumentStringOnUI();

        [JSImport("noArgumentValueAsync", nameof(WorkerInteropTest))]
        private static partial Task<int> NoArgumentValueAsyncOnUI();

        [JSImport("noArgumentStringAsync", nameof(WorkerInteropTest))]
        private static partial Task<string> NoArgumentStringAsyncOnUI();

        [JSImport("void", nameof(WorkerInteropTest))]
        private static partial void VoidOnUI();

        [JSImport("voidArgument", nameof(WorkerInteropTest))]
        private static partial void VoidArgumentOnUI(string value);

        [JSImport("voidAsync", nameof(WorkerInteropTest))]
        private static partial Task VoidAsyncOnUI();

        [JSImport("voidAsyncArgument", nameof(WorkerInteropTest))]
        private static partial Task VoidAsyncArgumentOnUI(string value);

        [JSImport("voidTaskMode", nameof(WorkerInteropTest))]
        private static partial Task VoidTaskModeOnUI(int value);

        [JSImport("voidContracts", nameof(WorkerInteropTest))]
        private static partial int VoidContractsOnUI(int value);

        [JSImport("voidAsyncContracts", nameof(WorkerInteropTest))]
        private static partial Task<int> VoidAsyncContractsOnUI(int value);

        [JSImport("sumTwo", nameof(WorkerInteropTest))]
        private static partial int SumTwoOnUI(int first, int second);

        [JSImport("joinThree", nameof(WorkerInteropTest))]
        private static partial string JoinThreeOnUI(string first, string second, string third);

        [JSImport("mixedSix", nameof(WorkerInteropTest))]
        private static partial string MixedSixOnUI(string first, int number, double fractional, bool flag, float single, string last);

        [JSImport("mixedSixAsync", nameof(WorkerInteropTest))]
        private static partial Task<string> MixedSixAsyncOnUI(string first, int number, double fractional, bool flag, float single, string last);

        [JSImport("mixedSixVoid", nameof(WorkerInteropTest))]
        private static partial void MixedSixVoidOnUI(string first, int number, double fractional, bool flag, float single, string last);

        [JSImport("sixStrings", nameof(WorkerInteropTest))]
        private static partial string SixStringsOnUI(string first, string second, string third, string fourth, string fifth, string sixth);

        [JSImport("nestedMultiple", nameof(WorkerInteropTest))]
        private static partial string NestedMultipleOnUI(string first, int depth, string last);

        [JSImport("multiContracts", nameof(WorkerInteropTest))]
        private static partial int CheckMultiContractsOnUI(int value);

        [JSImport("overLimit", nameof(WorkerInteropTest))]
        private static partial int OverLimitOnUI(int a, int b, int c, int d, int e, int f, int g);

        [JSImport("echoInteger", nameof(WorkerInteropTest))]
        private static partial byte EchoByteOnUI(byte value);
        [JSImport("invokeByte", nameof(WorkerInteropTest))]
        private static partial byte InvokeByteOnUI(byte value);
        [JSImport("echoIntegerAsync", nameof(WorkerInteropTest))]
        private static partial Task<byte> EchoByteAsyncOnUI(byte value);
        [JSImport("invokeByteAsync", nameof(WorkerInteropTest))]
        private static partial Task<byte> InvokeByteAsyncOnUI(byte value);

        [JSImport("echoInteger", nameof(WorkerInteropTest))]
        private static partial char EchoCharOnUI(char value);
        [JSImport("invokeChar", nameof(WorkerInteropTest))]
        private static partial char InvokeCharOnUI(char value);
        [JSImport("echoIntegerAsync", nameof(WorkerInteropTest))]
        private static partial Task<char> EchoCharAsyncOnUI(char value);
        [JSImport("invokeCharAsync", nameof(WorkerInteropTest))]
        private static partial Task<char> InvokeCharAsyncOnUI(char value);

        [JSImport("echoInteger", nameof(WorkerInteropTest))]
        private static partial short EchoInt16OnUI(short value);
        [JSImport("invokeInt16", nameof(WorkerInteropTest))]
        private static partial short InvokeInt16OnUI(short value);
        [JSImport("echoIntegerAsync", nameof(WorkerInteropTest))]
        private static partial Task<short> EchoInt16AsyncOnUI(short value);
        [JSImport("invokeInt16Async", nameof(WorkerInteropTest))]
        private static partial Task<short> InvokeInt16AsyncOnUI(short value);

        [JSImport("echoInteger", nameof(WorkerInteropTest))]
        [return: JSMarshalAs<JSType.Number>]
        private static partial long EchoInt52OnUI([JSMarshalAs<JSType.Number>] long value);
        [JSImport("invokeInt52", nameof(WorkerInteropTest))]
        [return: JSMarshalAs<JSType.Number>]
        private static partial long InvokeInt52OnUI([JSMarshalAs<JSType.Number>] long value);
        [JSImport("echoIntegerAsync", nameof(WorkerInteropTest))]
        [return: JSMarshalAs<JSType.Promise<JSType.Number>>]
        private static partial Task<long> EchoInt52AsyncOnUI([JSMarshalAs<JSType.Number>] long value);
        [JSImport("invokeInt52Async", nameof(WorkerInteropTest))]
        [return: JSMarshalAs<JSType.Promise<JSType.Number>>]
        private static partial Task<long> InvokeInt52AsyncOnUI([JSMarshalAs<JSType.Number>] long value);

        [JSImport("echoInteger", nameof(WorkerInteropTest))]
        [return: JSMarshalAs<JSType.BigInt>]
        private static partial long EchoBigInt64OnUI([JSMarshalAs<JSType.BigInt>] long value);
        [JSImport("invokeBigInt64", nameof(WorkerInteropTest))]
        [return: JSMarshalAs<JSType.BigInt>]
        private static partial long InvokeBigInt64OnUI([JSMarshalAs<JSType.BigInt>] long value);
        [JSImport("echoIntegerAsync", nameof(WorkerInteropTest))]
        [return: JSMarshalAs<JSType.Promise<JSType.BigInt>>]
        private static partial Task<long> EchoBigInt64AsyncOnUI([JSMarshalAs<JSType.BigInt>] long value);
        [JSImport("invokeBigInt64Async", nameof(WorkerInteropTest))]
        [return: JSMarshalAs<JSType.Promise<JSType.BigInt>>]
        private static partial Task<long> InvokeBigInt64AsyncOnUI([JSMarshalAs<JSType.BigInt>] long value);

        [JSImport("integerContracts", nameof(WorkerInteropTest))]
        private static partial int CheckIntegerContractsOnUI(int value);
        [JSImport("invalidInteger", nameof(WorkerInteropTest))]
        private static partial byte InvalidByteOnUI(int value);
        [JSImport("invalidInteger", nameof(WorkerInteropTest))]
        private static partial char InvalidCharOnUI(int value);
        [JSImport("invalidInteger", nameof(WorkerInteropTest))]
        private static partial short InvalidInt16OnUI(int value);
        [JSImport("invalidInteger", nameof(WorkerInteropTest))]
        [return: JSMarshalAs<JSType.Number>]
        private static partial long InvalidInt52OnUI(int value);
        [JSImport("invalidInteger", nameof(WorkerInteropTest))]
        [return: JSMarshalAs<JSType.BigInt>]
        private static partial long InvalidBigInt64OnUI(int value);

        [JSImport("mixedIntegers", nameof(WorkerInteropTest))]
        private static partial string MixedIntegersOnUI(byte b, char c, short s,
            [JSMarshalAs<JSType.Number>] long number, [JSMarshalAs<JSType.BigInt>] long big, string text);
        [JSImport("mixedIntegersAsync", nameof(WorkerInteropTest))]
        private static partial Task<string> MixedIntegersAsyncOnUI(byte b, char c, short s,
            [JSMarshalAs<JSType.Number>] long number, [JSMarshalAs<JSType.BigInt>] long big, string text);

        [JSExport]
        internal static byte EchoByte(byte value) => value;
        [JSExport]
        internal static Task<byte> EchoByteAsync(byte value) => Task.FromResult(value);
        [JSExport]
        internal static char EchoChar(char value) => value;
        [JSExport]
        internal static Task<char> EchoCharAsync(char value) => Task.FromResult(value);
        [JSExport]
        internal static short EchoInt16(short value) => value;
        [JSExport]
        internal static Task<short> EchoInt16Async(short value) => Task.FromResult(value);

        [JSExport]
        [return: JSMarshalAs<JSType.Number>]
        internal static long EchoInt52([JSMarshalAs<JSType.Number>] long value) => value;
        [JSExport]
        [return: JSMarshalAs<JSType.Promise<JSType.Number>>]
        internal static Task<long> EchoInt52Async([JSMarshalAs<JSType.Number>] long value) => Task.FromResult(value);
        [JSExport]
        [return: JSMarshalAs<JSType.BigInt>]
        internal static long EchoBigInt64([JSMarshalAs<JSType.BigInt>] long value) => value;
        [JSExport]
        [return: JSMarshalAs<JSType.Promise<JSType.BigInt>>]
        internal static Task<long> EchoBigInt64Async([JSMarshalAs<JSType.BigInt>] long value) => Task.FromResult(value);

        [JSExport]
        internal static string MixedIntegers(byte b, char c, short s,
            [JSMarshalAs<JSType.Number>] long number, [JSMarshalAs<JSType.BigInt>] long big, string text)
            => FormattableString.Invariant($"{b}|{(int)c}|{s}|{number}|{big}|{text}");

        private static string s_lastMixed;

        private static string FormatMixed(string first, int number, double fractional, bool flag, float single, string last)
            => FormattableString.Invariant($"{first ?? "<null>"}|{number}|{fractional}|{(flag ? "true" : "false")}|{single}|{last ?? "<null>"}");

        [JSExport]
        internal static int SumTwo(int first, int second) => first + second;

        [JSExport]
        internal static string JoinThree(string first, string second, string third) => string.Join("|", first, second, third);

        [JSExport]
        internal static string MixedSix(string first, int number, double fractional, bool flag, float single, string last)
            => FormatMixed(first, number, fractional, flag, single, last);

        [JSExport]
        internal static async Task<string> MixedSixAsync(string first, int number, double fractional, bool flag, float single, string last)
        {
            await Task.Delay(1);
            return FormatMixed(first, number, fractional, flag, single, last);
        }

        [JSExport]
        internal static void MixedSixVoid(string first, int number, double fractional, bool flag, float single, string last)
        {
            s_lastMixed = FormatMixed(first, number, fractional, flag, single, last);
        }

        [JSExport]
        internal static string SixStrings(string first, string second, string third, string fourth, string fifth, string sixth)
            => string.Join("|", first, second, third, fourth, fifth, sixth);

        [JSExport]
        internal static int OverLimit(int a, int b, int c, int d, int e, int f, int g) => a + b + c + d + e + f + g;

        [JSExport]
        internal static string NestedMultiple(string first, int depth, string last)
            => depth == 0 ? first + "|" + last : NestedMultipleOnUI(first + "L", depth - 1, "R" + last);

        private static int s_voidCalls;

        [JSExport]
        internal static int NoArgumentValue() => NoArgumentValueOnUI();

        [JSExport]
        internal static string NoArgumentString() => NoArgumentStringOnUI();

        [JSExport]
        internal static Task<int> NoArgumentValueAsync() => NoArgumentValueAsyncOnUI();

        [JSExport]
        internal static Task<string> NoArgumentStringAsync() => NoArgumentStringAsyncOnUI();

        [JSExport]
        internal static void VoidExport()
        {
            s_voidCalls++;
            VoidOnUI();
        }

        [JSExport]
        internal static void VoidStringExport(string value)
        {
            s_voidCalls++;
            VoidArgumentOnUI(value);
        }

        [JSExport]
        internal static int VoidCallCount() => s_voidCalls;

        [JSExport]
        internal static async Task VoidAsyncExport()
        {
            s_voidCalls++;
            await VoidAsyncOnUI();
        }

        [JSExport]
        internal static async Task VoidStringAsyncExport(string value)
        {
            s_voidCalls++;
            await VoidAsyncArgumentOnUI(value);
        }

        [JSExport]
        internal static Task VoidTaskModeExport(int value)
        {
            return value switch
            {
                0 => null,
                1 => Task.CompletedTask,
                2 => Task.FromException(new InvalidOperationException("Managed void Task fault")),
                3 => throw new InvalidOperationException("Managed void prefix fault"),
                _ => VoidTaskModeOnUI(value),
            };
        }

        [JSExport]
        internal static bool EchoBoolean(bool value) => value;

        [JSExport]
        internal static Task<bool> EchoBooleanAsync(bool value) => Task.FromResult(value);

        [JSExport]
        internal static double EchoDouble(double value) => value;

        [JSExport]
        internal static Task<double> EchoDoubleAsync(double value) => Task.FromResult(value);

        [JSExport]
        internal static float EchoSingle(float value) => value;

        [JSExport]
        internal static Task<float> EchoSingleAsync(float value) => Task.FromResult(value);

        [JSExport]
        internal static string EchoString(string value) => value;

        [JSExport]
        internal static async Task<string> EchoStringAsync(string value)
        {
            await Task.Delay(1);
            return await EchoStringAsyncOnUI(value);
        }

        [JSExport]
        internal static string NestedString(string value) => value.Length == 0 ? "" : NestedStringOnUI(value.Substring(1)) + value[0];

        [JSExport]
        internal static string ThrowString(string value) => throw new InvalidOperationException("Managed string failure");

        [JSExport]
        internal static int StringLength(string value) => value is null ? -1 : value.Length;

        [JSExport]
        internal static string LargeString(int length) => new string('z', length);

        [JSExport]
        internal static int Echo(int value) => value;

        [JSExport]
        internal static int Nested(int value) => value == 0 ? 0 : InvokeNestedOnUI(value - 1) + 1;

        [JSExport]
        internal static async Task<int> EchoAsync(int value)
        {
            await Task.Delay(1);
            return await EchoAsyncOnUI(value);
        }

        [JSExport]
        internal static int HasDocument(int value) => JSHost.GlobalThis.HasProperty("document") ? 1 : 0;

        [Fact]
        public void ExecutionRealm()
        {
            int expected = Environment.GetEnvironmentVariable("DOTNET_TEST_WITH_WORKER") == "1" ? 1 : 0;
            Assert.Equal(expected, GetModeOnUI(0));
        }

        [Theory]
        [MemberData(nameof(JSInteropTestBase.MarshalInt32Cases), MemberType = typeof(JSInteropTestBase))]
        public void ScalarImportAndExport(int value)
        {
            Assert.Equal(value, EchoOnUI(value));
            Assert.Equal(value, InvokeEchoOnUI(value));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(4)]
        [InlineData(15)]
        public void NestedSynchronousCalls(int depth)
        {
            Assert.Equal(depth, InvokeNestedOnUI(depth));
        }

        [Theory]
        [MemberData(nameof(JSInteropTestBase.MarshalInt32Cases), MemberType = typeof(JSInteropTestBase))]
        public async Task AsynchronousImportAndExport(int value)
        {
            Assert.Equal(value, await EchoAsyncOnUI(value));
            Assert.Equal(value, await InvokeAsyncOnUI(value));
        }

        [Fact]
        public async Task ConcurrentAsynchronousCalls()
        {
            Task<int>[] pending = new Task<int>[70];
            for (int value = 0; value < pending.Length; value++)
                pending[value] = InvokeAsyncOnUI(value);
            int[] results = await Task.WhenAll(pending);
            for (int value = 0; value < results.Length; value++)
                Assert.Equal(value, results[value]);
        }

        [Fact]
        public void ImportFailureAndRecovery()
        {
            JSException error = Assert.Throws<JSException>(() => FailOnUI(-1));
            Assert.Contains("Worker UI import failure", error.Message);
            Assert.Contains(nameof(ImportFailureAndRecovery), error.StackTrace);
            Assert.Equal(42, FailOnUI(42));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        public async Task StringImportAndExport(int scenario)
        {
            string value = scenario switch
            {
                0 => null,
                1 => "",
                2 => "hello",
                3 => "I\u0131\u0130i",
                4 => "a\0b",
                5 => "\ud800",
                6 => "\udfff",
                7 => "a\ud83d\ude00z",
                8 => new string('x', 400000),
                _ => throw new ArgumentOutOfRangeException(nameof(scenario))
            };
            string expected = scenario is 5 or 6 ? "\ufffd" : value;
            Assert.Equal(expected, EchoStringOnUI(value));
            Assert.Equal(expected, InvokeStringOnUI(value));
            Assert.Equal(expected, await EchoStringAsyncOnUI(value));
            Assert.Equal(expected, await InvokeStringAsyncOnUI(value));
            Assert.Equal(value is null ? -1 : value.Length, StringLengthOnUI(value));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(4)]
        [InlineData(15)]
        public void NestedStringCalls(int depth)
        {
            string value = new string('x', depth);
            Assert.Equal(value, NestedStringOnUI(value));
        }

        [Fact]
        public async Task StringFailureAndRecovery()
        {
            Exception error = Assert.ThrowsAny<Exception>(() => FailStringOnUI("value"));
            Assert.IsType<InvalidOperationException>(error);
            Assert.Contains("Managed string failure", error.Message);
            Assert.Contains(nameof(StringFailureAndRecovery), error.StackTrace);
            JSException asyncError = await Assert.ThrowsAsync<JSException>(() => FailStringAsyncOnUI("value"));
            Assert.Contains("UI async string failure", asyncError.Message);
            Assert.NotEmpty(asyncError.StackTrace);
            Assert.Equal("recovered", InvokeStringOnUI("recovered"));
        }

        [Fact]
        public async Task ConcurrentStringCalls()
        {
            Task<string>[] tasks = new Task<string>[70];
            for (int i = 0; i < tasks.Length; i++)
                tasks[i] = InvokeStringAsyncOnUI(i.ToString(Globalization.CultureInfo.InvariantCulture));
            string[] results = await Task.WhenAll(tasks);
            for (int i = 0; i < results.Length; i++)
                Assert.Equal(i.ToString(Globalization.CultureInfo.InvariantCulture), results[i]);
        }

        [Fact]
        public void LargeStringResultsAndRepeatedCleanup()
        {
            Assert.Equal(new string('z', 400000), LargeStringOnUI(400000));
            for (int i = 0; i < 32; i++)
            {
                string value = new string('x', 16384);
                Assert.Equal(value, InvokeStringOnUI(value));
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Assert.Equal("after GC", InvokeStringOnUI("after GC"));
        }

        [Fact]
        public void StringTransportAcrossMemoryGrowth()
        {
            const int Length = 40_000_000;
            string value = GrowStringOnUI(Length);
            Assert.Equal(Length, value.Length);
            Assert.Equal('z', value[0]);
            Assert.Equal('z', value[^1]);
            Assert.Equal("after growth", InvokeStringOnUI("after growth"));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task BooleanImportAndExport(bool value)
        {
            Assert.Equal(value, EchoBooleanOnUI(value));
            Assert.Equal(value, InvokeBooleanOnUI(value));
            Assert.Equal(value, await EchoBooleanAsyncOnUI(value));
            Assert.Equal(value, await InvokeBooleanAsyncOnUI(value));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        [InlineData(9)]
        public async Task FloatingPointImportAndExport(int scenario)
        {
            double value = scenario switch
            {
                0 => 0.0,
                1 => -0.0,
                2 => Math.PI,
                3 => double.NaN,
                4 => double.PositiveInfinity,
                5 => double.NegativeInfinity,
                6 => double.MaxValue,
                7 => double.Epsilon,
                8 => -double.Epsilon,
                9 => 1.0 + Math.Pow(2, -24),
                _ => throw new ArgumentOutOfRangeException(nameof(scenario))
            };
            double[] doubleResults = [EchoDoubleOnUI(value), InvokeDoubleOnUI(value),
                await EchoDoubleAsyncOnUI(value), await InvokeDoubleAsyncOnUI(value)];
            foreach (double actual in doubleResults)
            {
                Assert.Equal(value, actual);
                if (value == 0.0)
                    Assert.Equal(BitConverter.DoubleToInt64Bits(value), BitConverter.DoubleToInt64Bits(actual));
            }
            float single = (float)value;
            float[] singleResults = [EchoSingleOnUI(single), InvokeSingleOnUI(single),
                await EchoSingleAsyncOnUI(single), await InvokeSingleAsyncOnUI(single)];
            foreach (float actual in singleResults)
            {
                Assert.Equal(single, actual);
                if (single == 0.0f)
                    Assert.Equal(BitConverter.SingleToInt32Bits(single), BitConverter.SingleToInt32Bits(actual));
            }
        }

        [Fact]
        public async Task PrimitiveDefaultsAndValidation()
        {
            Assert.Equal(42, CheckPrimitiveContractsOnUI(42));
            Assert.False(NullBooleanOnUI(0));
            Assert.Equal(0.0, NullDoubleOnUI(0));
            Assert.Equal(0.0f, NullSingleOnUI(0));
            Assert.False(await NullBooleanAsyncOnUI(0));
            Assert.Equal(0.0, await NullDoubleAsyncOnUI(0));
            Assert.Equal(0.0f, await NullSingleAsyncOnUI(0));
            JSException booleanError = Assert.Throws<JSException>(() => InvalidBooleanOnUI(0));
            JSException doubleError = Assert.Throws<JSException>(() => InvalidDoubleOnUI(0));
            JSException singleError = Assert.Throws<JSException>(() => InvalidSingleOnUI(0));
            Assert.Contains("Boolean", booleanError.Message);
            Assert.Contains("Number", doubleError.Message);
            Assert.Contains("Number", singleError.Message);
            Assert.NotEmpty(booleanError.StackTrace);
            Assert.NotEmpty(doubleError.StackTrace);
            Assert.NotEmpty(singleError.StackTrace);
            Assert.True(InvokeBooleanOnUI(true));
            Assert.Equal(42.5, InvokeDoubleOnUI(42.5));
        }

        [Fact]
        public async Task ZeroArgumentValues()
        {
            Assert.Equal(42, NoArgumentValueOnUI());
            Assert.Equal("zero\0argument", NoArgumentStringOnUI());
            Assert.Equal(43, await NoArgumentValueAsyncOnUI());
            Assert.Equal("zero\0async", await NoArgumentStringAsyncOnUI());
        }

        [Fact]
        public void SynchronousVoidCalls()
        {
            VoidOnUI();
            VoidArgumentOnUI("direct");
            Assert.Equal(42, VoidContractsOnUI(42));
            JSException error = Assert.Throws<JSException>(() => VoidArgumentOnUI("throw"));
            Assert.Contains("Synchronous UI void failure", error.Message);
            Assert.NotEmpty(error.StackTrace);
            VoidArgumentOnUI("recovered");
        }

        [Fact]
        public async Task AsynchronousVoidCalls()
        {
            await VoidAsyncOnUI();
            await VoidAsyncArgumentOnUI("direct async");
            Assert.Equal(42, await VoidAsyncContractsOnUI(42));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void NullTaskImports(int mode)
        {
            Assert.Null(VoidTaskModeOnUI(mode));
        }

        [Theory]
        [InlineData(2, "UI void prefix fault")]
        [InlineData(3, "Value is not a Promise")]
        public void TaskImportPrefixFailures(int mode, string message)
        {
            JSException error = Assert.Throws<JSException>(() => { _ = VoidTaskModeOnUI(mode); });
            Assert.Contains(message, error.Message);
            Assert.NotEmpty(error.StackTrace);
            Assert.Equal(42, NoArgumentValueOnUI());
        }

        [Fact]
        public async Task TaskImportRejection()
        {
            JSException error = await Assert.ThrowsAsync<JSException>(() => VoidTaskModeOnUI(4));
            Assert.Contains("UI void Task fault", error.Message);
            Assert.NotEmpty(error.StackTrace);
            await VoidTaskModeOnUI(5);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        public async Task MultipleMixedArguments(int scenario)
        {
            string first = scenario switch
            {
                0 => null,
                1 => "",
                2 => "first\0value",
                3 => "I\u0131\u0130i",
                4 => "a\ud83d\ude00z",
                _ => new string('x', 400000),
            };
            string last = scenario == 0 ? null : "last\0" + scenario.ToString(Globalization.CultureInfo.InvariantCulture);
            int number = scenario * -17;
            double fractional = scenario + 0.5;
            bool flag = scenario % 2 == 0;
            float single = scenario + 0.25f;
            string expected = FormatMixed(first, number, fractional, flag, single, last);
            Assert.Equal(expected, MixedSixOnUI(first, number, fractional, flag, single, last));
            Assert.Equal(expected, await MixedSixAsyncOnUI(first, number, fractional, flag, single, last));
            MixedSixVoidOnUI(first, number, fractional, flag, single, last);
            Assert.Equal(expected, s_lastMixed);
            Assert.Equal("1st|2nd|3rd", JoinThreeOnUI("1st", "2nd", "3rd"));
            Assert.Equal(scenario + 42, SumTwoOnUI(scenario, 42));
            Assert.Equal("a||c|d|e|last", SixStringsOnUI("a", null, "c", "d", "e", "last"));
        }

        [Fact]
        public async Task ConcurrentMultipleArguments()
        {
            Task<string>[] tasks = new Task<string>[70];
            for (int i = 0; i < tasks.Length; i++)
                tasks[i] = MixedSixAsyncOnUI("left", i, 2.5, false, 1.25f, "right");
            string[] results = await Task.WhenAll(tasks);
            for (int i = 0; i < results.Length; i++)
                Assert.Equal(FormatMixed("left", i, 2.5, false, 1.25f, "right"), results[i]);
        }

        [Fact]
        public void MultipleArgumentValidationAndLimits()
        {
            Assert.Equal(42, CheckMultiContractsOnUI(42));
            if (Environment.GetEnvironmentVariable("DOTNET_TEST_WITH_WORKER") == "1")
            {
                JSException error = Assert.Throws<JSException>(() => OverLimitOnUI(1, 2, 3, 4, 5, 6, 7));
                Assert.Contains("at most 6", error.Message);
                Assert.NotEmpty(error.StackTrace);
            }
            else
            {
                Assert.Equal(28, OverLimitOnUI(1, 2, 3, 4, 5, 6, 7));
            }
            Assert.Equal(42, SumTwoOnUI(12, 30));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(4)]
        public void NestedMultipleArguments(int depth)
        {
            Assert.Equal("a" + new string('L', depth) + "|" + new string('R', depth) + "z", NestedMultipleOnUI("a", depth, "z"));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        public async Task IntegerImportAndExport(int scenario)
        {
            byte b = new byte[] { 0, 1, 42, 255, 128, 254, 17, 127 }[scenario];
            char c = new char[] { '\0', '\u0001', '\u0130', '\ud800', '\udfff', '\uffff', '\u1234', '\u7fff' }[scenario];
            short s = new short[] { short.MinValue, -1, 0, 1, short.MaxValue, short.MinValue + 1, 1234, -1234 }[scenario];
            long number = new long[] { -9007199254740991, -4294967419, -1, 0, 1, 4294967419, 9007199254740991, 2147483648 }[scenario];
            long big = new long[] { long.MinValue, -9007199254740993, -1, 0, 1, 9007199254740993, long.MaxValue, long.MinValue + 1 }[scenario];
            Assert.Equal(b, EchoByteOnUI(b));
            Assert.Equal(b, InvokeByteOnUI(b));
            Assert.Equal(b, await EchoByteAsyncOnUI(b));
            Assert.Equal(b, await InvokeByteAsyncOnUI(b));
            Assert.Equal(c, EchoCharOnUI(c));
            Assert.Equal(c, InvokeCharOnUI(c));
            Assert.Equal(c, await EchoCharAsyncOnUI(c));
            Assert.Equal(c, await InvokeCharAsyncOnUI(c));
            Assert.Equal(s, EchoInt16OnUI(s));
            Assert.Equal(s, InvokeInt16OnUI(s));
            Assert.Equal(s, await EchoInt16AsyncOnUI(s));
            Assert.Equal(s, await InvokeInt16AsyncOnUI(s));
            Assert.Equal(number, EchoInt52OnUI(number));
            Assert.Equal(number, InvokeInt52OnUI(number));
            Assert.Equal(number, await EchoInt52AsyncOnUI(number));
            Assert.Equal(number, await InvokeInt52AsyncOnUI(number));
            Assert.Equal(big, EchoBigInt64OnUI(big));
            Assert.Equal(big, InvokeBigInt64OnUI(big));
            Assert.Equal(big, await EchoBigInt64AsyncOnUI(big));
            Assert.Equal(big, await InvokeBigInt64AsyncOnUI(big));
            string expected = FormattableString.Invariant($"{b}|{(int)c}|{s}|{number}|{big}|mixed");
            Assert.Equal(expected, MixedIntegersOnUI(b, c, s, number, big, "mixed"));
            Assert.Equal(expected, await MixedIntegersAsyncOnUI(b, c, s, number, big, "mixed"));
        }

        [Fact]
        public void IntegerValidationAndRecovery()
        {
            Assert.Equal(42, CheckIntegerContractsOnUI(42));
            foreach (Action invoke in new Action[] {
                () => InvalidByteOnUI(0), () => InvalidCharOnUI(1), () => InvalidInt16OnUI(2),
                () => InvalidInt52OnUI(3), () => InvalidBigInt64OnUI(4)
            })
            {
                JSException error = Assert.Throws<JSException>(invoke);
                Assert.NotEmpty(error.Message);
                Assert.NotEmpty(error.StackTrace);
            }
            Assert.Equal(byte.MaxValue, InvokeByteOnUI(byte.MaxValue));
            Assert.Equal(long.MaxValue, InvokeBigInt64OnUI(long.MaxValue));
        }

        [Fact]
        public async Task ConcurrentBigInt64Calls()
        {
            Task<long>[] tasks = new Task<long>[70];
            for (int i = 0; i < tasks.Length; i++)
                tasks[i] = InvokeBigInt64AsyncOnUI(long.MaxValue - i);
            long[] results = await Task.WhenAll(tasks);
            for (int i = 0; i < results.Length; i++)
                Assert.Equal(long.MaxValue - i, results[i]);
        }
    }

    public class JSImportTest : JSInteropTestBase, IAsyncLifetime
    {
        [Fact]
        public unsafe void StructSize()
        {
            Assert.Equal(32, sizeof(JSMarshalerArgument));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBrowserDomSupportedOrNodeJS))] // not V8 shell
        public unsafe void PrototypeNotEqual()
        {
            using var temp1 = JSHost.GlobalThis.GetPropertyAsJSObject("EventTarget");
            using var temp2 = JSHost.GlobalThis.GetPropertyAsJSObject("Window");
            Assert.NotEqual(temp1, temp2);
        }

        [Fact]
        public async Task MultipleImportAsync()
        {
            var first = await JSHost.ImportAsync("JavaScriptTestHelper", "../JavaScriptTestHelper.mjs");
            var second = await JSHost.ImportAsync("JavaScriptTestHelper", "../JavaScriptTestHelper.mjs");
            Assert.NotNull(first);
            Assert.NotNull(second);
            Assert.Equal("object", first.GetTypeOfProperty("instance"));
            var instance1 = first.GetPropertyAsJSObject("instance");
            var instance2 = second.GetPropertyAsJSObject("instance");
            Assert.Same(instance1, instance2);
            first.Dispose();
            second.Dispose();
            instance1.Dispose();
        }

        [Fact]
        public void MissingImport()
        {
            var ex = Assert.Throws<JSException>(() => JavaScriptTestHelper.IntentionallyMissingImport());
            Assert.Contains("intentionallyMissingImport must be a Function but was undefined", ex.Message);
        }

        [Fact]
        public async Task MissingImportAsync()
        {
            var ex = await Assert.ThrowsAsync<JSException>(() => JavaScriptTestHelper.IntentionallyMissingImportAsync());
            Assert.Contains("intentionallyMissingImportAsync must be a Function but was undefined", ex.Message);
        }

        // because in MT JSHost.ImportAsync is really async, it will finish before the caller could cancel it
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMultithreadingSupported))]
        public async Task CancelableImportAsync()
        {
            var cts = new CancellationTokenSource();
            var exTask = Assert.ThrowsAsync<JSException>(async () => await JSHost.ImportAsync("JavaScriptTestHelper", "../JavaScriptTestHelper.mjs", cts.Token));
            cts.Cancel();
            var actualEx2 = await exTask;
            Assert.Equal("Error: OperationCanceledException", actualEx2.Message);

            var actualEx = await Assert.ThrowsAsync<JSException>(async () => await JSHost.ImportAsync("JavaScriptTestHelper", "../JavaScriptTestHelper.mjs", new CancellationToken(true)));
            Assert.Equal("Error: OperationCanceledException", actualEx.Message);
        }

        [Fact]
        public unsafe void GlobalThis()
        {
            var globalThis = JSHost.GlobalThis;
            Assert.Null(globalThis.GetPropertyAsString("dummy"));
            Assert.False(globalThis.HasProperty("dummy"));
            Assert.Equal("undefined", globalThis.GetTypeOfProperty("dummy"));
            Assert.Equal("function", globalThis.GetTypeOfProperty("Array"));
            var javaScriptTestHelper = globalThis.GetPropertyAsJSObject("javaScriptTestHelper");
            Assert.NotNull(javaScriptTestHelper);
            globalThis.Dispose();
            javaScriptTestHelper.Dispose();
        }

        [Fact]
        public unsafe void DotnetInstance()
        {
            JSHost.DotnetInstance.SetProperty("testBool", true);
            Assert.Equal("boolean", JSHost.DotnetInstance.GetTypeOfProperty("testBool"));

            JSHost.DotnetInstance.SetProperty("testInt", 42);
            Assert.Equal("number", JSHost.DotnetInstance.GetTypeOfProperty("testInt"));
            Assert.Equal(42, JSHost.DotnetInstance.GetPropertyAsInt32("testInt"));

            JSHost.DotnetInstance.SetProperty("testDouble", 3.14);
            Assert.Equal("number", JSHost.DotnetInstance.GetTypeOfProperty("testDouble"));
            Assert.Equal(3.14, JSHost.DotnetInstance.GetPropertyAsDouble("testDouble"));

            JSHost.DotnetInstance.SetProperty("testString", "Yoda");
            Assert.Equal("string", JSHost.DotnetInstance.GetTypeOfProperty("testString"));
            Assert.Equal("Yoda", JSHost.DotnetInstance.GetPropertyAsString("testString"));
        }

        [Fact]
        public async Task RejectString()
        {
            var ex = await Assert.ThrowsAsync<JSException>(() => JavaScriptTestHelper.Reject("noodles"));
            Assert.Contains("noodles", ex.Message);
        }

        [Fact]
        public async Task RejectException()
        {
            var expected = new Exception("noodles");
            var actual = await Assert.ThrowsAsync<Exception>(() => JavaScriptTestHelper.Reject(expected));
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task RejectNull()
        {
            var ex = await Assert.ThrowsAsync<JSException>(() => JavaScriptTestHelper.Reject(null));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMultithreadingSupported))]
        public unsafe void OptimizedPaths()
        {
            JavaScriptTestHelper.optimizedReached = 0;
            JavaScriptTestHelper.invoke0V();
            Assert.Equal(1, JavaScriptTestHelper.optimizedReached);
            JavaScriptTestHelper.invoke1V(42);
            Assert.Equal(43, JavaScriptTestHelper.optimizedReached);
            Assert.Equal(124, JavaScriptTestHelper.invoke1R(123));
            Assert.Equal(43 + 123, JavaScriptTestHelper.optimizedReached);
            Assert.Equal(32, JavaScriptTestHelper.invoke2R(15, 16));
            Assert.Equal(43 + 123 + 31, JavaScriptTestHelper.optimizedReached);
        }

        #region Assertion Errors
        [Fact]
        public unsafe void BadCast()
        {
            JSException ex;
            JSHost.DotnetInstance.SetProperty("testBool", true);
            ex = Assert.Throws<JSException>(() => JSHost.DotnetInstance.GetPropertyAsInt32("testBool"));
            Assert.Contains("Value is not an integer", ex.Message);
            ex = Assert.Throws<JSException>(() => JSHost.DotnetInstance.GetPropertyAsDouble("testBool"));
            Assert.Contains("Value is not a Number", ex.Message);
            ex = Assert.Throws<JSException>(() => JSHost.DotnetInstance.GetPropertyAsString("testBool"));
            Assert.Contains("Value is not a String", ex.Message);
            ex = Assert.Throws<JSException>(() => JSHost.DotnetInstance.GetPropertyAsJSObject("testBool"));
            Assert.Contains("JSObject proxy of boolean is not supported", ex.Message);
            ex = Assert.Throws<JSException>(() => JSHost.DotnetInstance.GetPropertyAsByteArray("testBool"));
            Assert.Contains("Value is not an Array or Uint8Array", ex.Message);
            JSHost.DotnetInstance.SetProperty("testInt", 42);
            ex = Assert.Throws<JSException>(() => JSHost.DotnetInstance.GetPropertyAsBoolean("testInt"));
            Assert.Contains("Value is not a Boolean", ex.Message);
        }

        [Fact]
        public unsafe void OutOfRange()
        {
            JSException ex;
            JSHost.DotnetInstance.SetProperty("testDouble", 9007199254740991L);
            ex = Assert.Throws<JSException>(() => JSHost.DotnetInstance.GetPropertyAsInt32("testDouble"));
            Assert.Contains("Overflow: value 9007199254740991 is out of -2147483648 2147483647 range", ex.Message);
        }

        [Fact]
        public async Task TaskOfShortOutOfRange_ThrowsAssertionInTaskContinuation()
        {
            Task<short> res = JavaScriptTestHelper.ReturnResolvedPromiseWithIntMaxValue_AsShortToBeOutOfRange();
            JSException ex = await Assert.ThrowsAsync<JSException>(() => res);
            Assert.Equal("Error: Assert failed: Overflow: value 2147483647 is out of -32768 32767 range", ex.Message);
        }

        [Fact]
        public async Task TaskOfByteOutOfRange_ThrowsAssertionInTaskContinuation()
        {
            Task<byte> res = JavaScriptTestHelper.ReturnResolvedPromiseWithIntMaxValue_AsByteToBeOutOfRange();
            JSException ex = await Assert.ThrowsAsync<JSException>(() => res);
            Assert.Equal("Error: Assert failed: Overflow: value 2147483647 is out of 0 255 range", ex.Message);
        }

        [Fact]
        public async Task TaskOfDateTimeOutOfRange_ThrowsAssertionInTaskContinuation()
        {
            Task<DateTime> res = JavaScriptTestHelper.ReturnResolvedPromiseWithDateMaxValue();
            JSException ex = await Assert.ThrowsAsync<JSException>(() => res);
            Assert.Equal("Error: Assert failed: Overflow: value +275760-09-13T00:00:00.000Z is out of 0001-01-01T00:00:00.000Z 9999-12-31T23:59:59.999Z range", ex.Message);
        }

        [Fact]
        public async Task DateTimeMaxValueBoundaryCondition()
        {
            DateTime t = JavaScriptTestHelper.ReturnDateTimeWithOffset(DateTime.MaxValue, 0);
            Assert.Equal(DateTime.MaxValue.AddTicks(-9_999), t); // Microseconds are lost during marshalling
            JSException ex = Assert.Throws<JSException>(() => JavaScriptTestHelper.ReturnDateTimeWithOffset(DateTime.MaxValue, 1));
            Assert.Equal("Error: Assert failed: Overflow: value +010000-01-01T00:00:00.000Z is out of 0001-01-01T00:00:00.000Z 9999-12-31T23:59:59.999Z range", ex.Message);
        }

        [Fact]
        public async Task DateTimeMinValueBoundaryCondition()
        {
            DateTime t = JavaScriptTestHelper.ReturnDateTimeWithOffset(DateTime.MinValue, 0);
            Assert.Equal(DateTime.MinValue, t);
            JSException ex = Assert.Throws<JSException>(() => JavaScriptTestHelper.ReturnDateTimeWithOffset(DateTime.MinValue, -1));
            Assert.Equal("Error: Assert failed: Overflow: value 0000-12-31T23:59:59.999Z is out of 0001-01-01T00:00:00.000Z 9999-12-31T23:59:59.999Z range", ex.Message);
        }

        [Fact]
        public async Task Int32ArrayWithOutOfRangeValues()
        {
            int[] arr = JavaScriptTestHelper.getInt32ArrayWithOutOfRangeValues();
            Assert.Equal(0, arr[0]);
            Assert.Equal(1, arr[1]);
            Assert.Equal(-2147483648, arr[2]);
            Assert.Equal(-1147483648, arr[3]);
            Assert.Equal(-1, arr[4]);
            Assert.Equal(0, arr[5]);
            // Currently, values > int32.MaxValue are wrapped around when marshaled from JS to C#.
            // TODO: Instead throw OverflowException when out of range value is encountered.
        }

        #endregion

        #region Get/Set Property

        [Fact]
        public unsafe void JSObjectGetSet()
        {
            Func<double, JSObject> createObject = Utils.CreateFunctionDoubleJSObject("a", @"
                var x = {a, x:42 };
                return x;
                ");
            JSObject obj = createObject(1);
            Assert.NotNull(obj);
            double? a = obj.GetPropertyAsDouble("a");
            Assert.Equal(1, a);

            double? x = obj.GetPropertyAsDouble("x");
            Assert.Equal(42, x);

            int? xi = obj.GetPropertyAsInt32("x");
            Assert.Equal(42, xi);

            /*
            obj.GetProperty("x", out string? xs);
            Assert.Equal("42", xs);
            */

            obj.SetProperty("b", 3);
            double? b = obj.GetPropertyAsDouble("b");
            Assert.Equal(3, b);

            obj.SetProperty("c", "test");
            string? c = obj.GetPropertyAsString("c");
            Assert.Equal("test", c);

            obj.SetProperty("c", (string)null);
            string? d = obj.GetPropertyAsString("c");
            Assert.Null(d);
        }

        #endregion

        #region CreateFunction

        [Fact]
        public unsafe void CreateFunctionDouble()
        {
            Func<double, double, double> doublePlus = Utils.CreateFunctionDoubleDoubleDouble("a", "b", "return a+b");
            Assert.Equal(3, doublePlus(1, 2));
            Assert.Equal(Math.PI * 2, doublePlus(Math.PI, Math.PI));
        }

        [Fact]
        public unsafe void CreateFunctionDoubleThrow()
        {
            Func<double, double, double> doubleThrows = Utils.CreateFunctionDoubleDoubleDouble("a", "b", "throw Error('test '+a+' '+b);");
            var ex = Assert.Throws<JSException>(() => doubleThrows(1, 2));
            Assert.Equal("Error: test 1 2", ex.Message);

            if (!PlatformDetection.IsMultithreadingSupported)
            {
                Assert.Contains("create_function", ex.StackTrace);
            }
            else
            {
                Assert.Contains("omitted JavaScript stack trace", ex.StackTrace);
            }
        }

        [Fact]
        public unsafe void CreateFunctionString()
        {
            Func<string, string, string> stringPlus = Utils.CreateFunctionStringStringString("a", "b", "return a+b");
            Assert.Equal("hello world", stringPlus("hello ", "world"));
            Assert.Equal("hellonull", stringPlus("hello", null));
        }

        [Fact]
        public unsafe void CreateFunctionInternal()
        {
            Func<bool> internals = Utils.CreateFunctionBool("return INTERNAL.getGlobalThis() === globalThis;");
            Assert.True(internals());
        }

        #endregion

        #region Arrays

        [Theory]
        [MemberData(nameof(MarshalByteArrayCases))]
        public unsafe void JsImportByteArray(byte[]? expected)
        {
            var actual = JavaScriptTestHelper.echo1_ByteArray(expected);
            Assert.Equal(expected, actual);
            if (expected != null) for (int i = 0; i < expected.Length; i++)
                {
                    var actualI = JavaScriptTestHelper.store_ByteArray(expected, i);
                    Assert.Equal(expected[i], actualI);
                }
        }

        [Theory]
        [MemberData(nameof(MarshalIntArrayCases))]
        public unsafe void JsImportIntArray(int[]? expected)
        {
            var actual = JavaScriptTestHelper.echo1_Int32Array(expected);
            Assert.Equal(expected, actual);
            if (expected != null) for (int i = 0; i < expected.Length; i++)
                {
                    var actualI = JavaScriptTestHelper.store_Int32Array(expected, i);
                    Assert.Equal(expected[i], actualI);
                }
        }

        [Theory]
        [MemberData(nameof(MarshalDoubleArrayCases))]
        public unsafe void JsImportDoubleArray(double[]? expected)
        {
            var actual = JavaScriptTestHelper.echo1_DoubleArray(expected);
            Assert.Equal(expected, actual);
            if (expected != null) for (int i = 0; i < expected.Length; i++)
                {
                    var actualI = JavaScriptTestHelper.store_DoubleArray(expected, i);
                    Assert.Equal(expected[i], actualI);
                }
        }

        [Theory]
        [MemberData(nameof(MarshalDoubleArrayCases))]
        public unsafe void JsImportDoubleArray_NoAttributes(double[]? expected)
        {
            var actual = JavaScriptTestHelper.echo1_DoubleArray_NoAttributes(expected);
            Assert.Equal(expected, actual);
            if (expected != null) for (int i = 0; i < expected.Length; i++)
                {
                    var actualI = JavaScriptTestHelper.store_DoubleArray_NoAttributes(expected, i);
                    Assert.Equal(expected[i], actualI);
                }
        }

        [Theory]
        [MemberData(nameof(MarshalSingleArrayCases))]
        public unsafe void JsImportSingleArray(float[]? expected)
        {
            var actual = JavaScriptTestHelper.echo1_SingleArray(expected);
            Assert.Equal(expected, actual);
            if (expected != null) for (int i = 0; i < expected.Length; i++)
                {
                    var actualI = JavaScriptTestHelper.store_SingleArray(expected, i);
                    Assert.Equal(expected[i], actualI);
                }
        }

        [Theory]
        [MemberData(nameof(MarshalSingleArrayCases))]
        public unsafe void JsImportSingleArray_NoAttributes(float[]? expected)
        {
            var actual = JavaScriptTestHelper.echo1_SingleArray_NoAttributes(expected);
            Assert.Equal(expected, actual);
            if (expected != null) for (int i = 0; i < expected.Length; i++)
                {
                    var actualI = JavaScriptTestHelper.store_SingleArray_NoAttributes(expected, i);
                    Assert.Equal(expected[i], actualI);
                }
        }

        [Theory]
        [MemberData(nameof(MarshalStringArrayCases))]
        public unsafe void JsImportStringArray(string[]? expected)
        {
            var actual = JavaScriptTestHelper.echo1_StringArray(expected);
            Assert.Equal(expected, actual);

            if (expected != null) for (int i = 0; i < expected.Length; i++)
                {
                    var actualI = JavaScriptTestHelper.store_StringArray(expected, i);
                    Assert.Equal(expected[i], actualI);
                }
        }

        [Theory]
        [MemberData(nameof(MarshalObjectArrayCases))]
        public unsafe void JsImportObjectArray(object[]? expected)
        {
            if (expected?.Length == 1 && expected[0] is string s && s == "JSData")
            {
                expected = new object[] { new object[] { JavaScriptTestHelper.createData("test"), JavaScriptTestHelper.createException("test") } };
            }
            var actual = JavaScriptTestHelper.echo1_ObjectArray(expected);
            Assert.Equal(expected, actual);

            if (expected != null) for (int i = 0; i < expected.Length; i++)
                {
                    var actualI = JavaScriptTestHelper.store_ObjectArray(expected, i);
                    Assert.Equal(expected[i], actualI);
                }
        }

        [Theory]
        [MemberData(nameof(MarshalObjectArrayCasesToDouble))]
        public unsafe void JsImportObjectArrayToDouble(object[]? expected)
        {
            if (expected != null) for (int i = 0; i < expected.Length; i++)
                {
                    var actualI = JavaScriptTestHelper.store_ObjectArray(expected, i);
                    if (expected[i].GetType() == typeof(char))
                    {
                        Assert.Equal((double)(int)(char)expected[i], actualI);
                    }
                    else
                    {
                        Assert.Equal(Convert.ToDouble(expected[i]), actualI);
                    }
                }
        }

        [Theory]
        [MemberData(nameof(MarshalObjectArrayCasesThrow))]
        public void JsImportObjectArrayThrows(object[]? expected)
        {
            Assert.Throws<NotSupportedException>(() => JavaScriptTestHelper.echo1_ObjectArray(expected));
        }

        [Fact]
        public async Task JsImportObjectArrayTask()
        {
            object[] expected = new object[] { Task.CompletedTask };
            var actual = JavaScriptTestHelper.echo1_ObjectArray(expected);
            Assert.True(typeof(Task).IsAssignableFrom(actual[0].GetType()));
            await Task.Delay(100);
            await Task.Yield();
            var actual0 = actual[0] as Task;
            Assert.True(actual0.IsCompleted);
            Assert.True(actual0.IsCompletedSuccessfully);
            var actualT = JavaScriptTestHelper.store_ObjectArray(expected, 0);
            await Task.Delay(100);
            await Task.Yield();
            var actualT0 = actualT as Task;
            Assert.True(actualT0.IsCompletedSuccessfully);
        }

        [Fact]
        public async Task JsImportObjectArrayTaskObject()
        {
            object[] expected = new object[] { Task.FromResult((object)42) };
            var actual = JavaScriptTestHelper.echo1_ObjectArray(expected);
            var actual0 = Assert.IsType<Task<object>>(actual[0]);
            await Task.Delay(100);
            await Task.Yield();
            Assert.True(actual0.IsCompleted);
            Assert.True(actual0.IsCompletedSuccessfully);
            Assert.Equal(42.0d, actual0.Result);
        }

        [Fact]
        public async Task JsImportObjectArrayTaskObjectFail()
        {
            var exex = new Exception("test");
            object[] expected = new object[] { Task.FromException(exex) };
            var actual = JavaScriptTestHelper.echo1_ObjectArray(expected);
            var actual0 = Assert.IsType<Task<object>>(actual[0]);
            await Task.Delay(100);
            await Task.Yield();
            Assert.True(actual0.IsCompleted);
            Assert.True(actual0.IsFaulted);
            var actualEx = await Assert.ThrowsAsync<Exception>(async () => await actual0);
            Assert.Same(exex, actualEx);
        }

        #endregion

        #region Views

        [Fact]
        public unsafe void JsImportSpanOfByte()
        {
            var expectedBytes = stackalloc byte[] { 1, 2, 42, 0, 127, 255 };
            Span<byte> expected = new Span<byte>(expectedBytes, 6);
            Assert.True(Unsafe.AsPointer(ref expected.GetPinnableReference()) == expectedBytes);
            Span<byte> actual = JavaScriptTestHelper.echo1_SpanOfByte(expected, false);
            Assert.Equal(expected.Length, actual.Length);
            Assert.NotEqual(expected[0], expected[1]);
            Assert.Equal(expected.GetPinnableReference(), actual.GetPinnableReference());
            Assert.True(actual.SequenceCompareTo(expected) == 0);
            Assert.Equal(expected.ToArray(), actual.ToArray());
            actual = JavaScriptTestHelper.echo1_SpanOfByte(expected, true);
            Assert.Equal(expected[0], expected[1]);
            Assert.Equal(actual[0], actual[1]);
        }

        [Fact]
        public unsafe void JsImportSpanOfInt32()
        {
            var expectedBytes = stackalloc int[] { 0, 1, -2, 42, int.MaxValue, int.MinValue };
            Span<int> expected = new Span<int>(expectedBytes, 6);
            Assert.True(Unsafe.AsPointer(ref expected.GetPinnableReference()) == expectedBytes);
            Span<int> actual = JavaScriptTestHelper.echo1_SpanOfInt32(expected, false);
            Assert.Equal(expected.Length, actual.Length);
            Assert.NotEqual(expected[0], expected[1]);
            Assert.Equal(expected.GetPinnableReference(), actual.GetPinnableReference());
            Assert.True(actual.SequenceCompareTo(expected) == 0);
            Assert.Equal(expected.ToArray(), actual.ToArray());
            actual = JavaScriptTestHelper.echo1_SpanOfInt32(expected, true);
            Assert.Equal(expected[0], expected[1]);
            Assert.Equal(actual[0], actual[1]);
        }

        [Fact]
        public unsafe void JsImportSpanOfDouble()
        {
            var expectedBytes = stackalloc double[] { 0, 1, -1, double.Pi, 42, double.MaxValue, double.MinValue, double.NaN, double.PositiveInfinity, double.NegativeInfinity };
            Span<double> expected = new Span<double>(expectedBytes, 10);
            Assert.True(Unsafe.AsPointer(ref expected.GetPinnableReference()) == expectedBytes);
            Span<double> actual = JavaScriptTestHelper.echo1_SpanOfDouble(expected, false);
            Assert.Equal(expected.Length, actual.Length);
            Assert.NotEqual(expected[0], expected[1]);
            Assert.Equal(expected.GetPinnableReference(), actual.GetPinnableReference());
            Assert.True(actual.SequenceCompareTo(expected) == 0);
            Assert.Equal(expected.ToArray(), actual.ToArray());
            actual = JavaScriptTestHelper.echo1_SpanOfDouble(expected, true);
            Assert.Equal(expected[0], expected[1]);
            Assert.Equal(actual[0], actual[1]);
        }

        [Fact]
        public unsafe void JsImportSpanOfSingle()
        {
            var expectedFloats = stackalloc float[] { 0, 1, -1, float.Pi, 42, float.MaxValue, float.MinValue, float.NaN, float.PositiveInfinity, float.NegativeInfinity };
            Span<float> expected = new Span<float>(expectedFloats, 10);
            Assert.True(Unsafe.AsPointer(ref expected.GetPinnableReference()) == expectedFloats);
            Span<float> actual = JavaScriptTestHelper.echo1_SpanOfSingle(expected, false);
            Assert.Equal(expected.Length, actual.Length);
            Assert.NotEqual(expected[0], expected[1]);
            Assert.Equal(expected.GetPinnableReference(), actual.GetPinnableReference());
            Assert.True(actual.SequenceCompareTo(expected) == 0);
            Assert.Equal(expected.ToArray(), actual.ToArray());
            actual = JavaScriptTestHelper.echo1_SpanOfSingle(expected, true);
            Assert.Equal(expected[0], expected[1]);
            Assert.Equal(actual[0], actual[1]);
        }

        [Fact]
        public unsafe void JsImportArraySegmentOfByte()
        {
            var expectedBytes = new byte[] { 88, 1, 2, 42, 0, 127, 255 };
            ArraySegment<byte> expected = new ArraySegment<byte>(expectedBytes, 1, 6);
            ArraySegment<byte> actual = JavaScriptTestHelper.echo1_ArraySegmentOfByte(expected, false);
            Assert.Equal(expected.Count, actual.Count);
            Assert.NotEqual(expected[0], expected[1]);
            Assert.Equal(expected.Array, actual.Array);
            actual = JavaScriptTestHelper.echo1_ArraySegmentOfByte(expected, true);
            Assert.Equal(expected[0], expected[1]);
            Assert.Equal(actual[0], actual[1]);
        }

        [Fact]
        public unsafe void JsImportArraySegmentOfInt32()
        {
            var expectedInts = new int[] { 88, 0, 1, -2, 42, int.MaxValue, int.MinValue };
            ArraySegment<int> expected = new ArraySegment<int>(expectedInts, 1, 6);
            ArraySegment<int> actual = JavaScriptTestHelper.echo1_ArraySegmentOfInt32(expected, false);
            Assert.Equal(expected.Count, actual.Count);
            Assert.NotEqual(expected[0], expected[1]);
            Assert.Equal(expected.Array, actual.Array);
            actual = JavaScriptTestHelper.echo1_ArraySegmentOfInt32(expected, true);
            Assert.Equal(expected[0], expected[1]);
            Assert.Equal(actual[0], actual[1]);
        }

        [Fact]
        public unsafe void JsImportArraySegmentOfDouble()
        {
            var expectedDoubles = new double[] { 88.88, 0, 1, -1, double.Pi, 42, double.MaxValue, double.MinValue, double.NaN, double.PositiveInfinity, double.NegativeInfinity };
            ArraySegment<double> expected = new ArraySegment<double>(expectedDoubles, 1, 10);
            ArraySegment<double> actual = JavaScriptTestHelper.echo1_ArraySegmentOfDouble(expected, false);
            Assert.Equal(expected.Count, actual.Count);
            Assert.NotEqual(expected[0], expected[1]);
            Assert.Equal(expected.Array, actual.Array);
            actual = JavaScriptTestHelper.echo1_ArraySegmentOfDouble(expected, true);
            Assert.Equal(expected[0], expected[1]);
            Assert.Equal(actual[0], actual[1]);
        }

        [Fact]
        public unsafe void JsImportArraySegmentOfSingle()
        {
            var expectedFloats = new float[] { 88.88F, 0, 1, -1, float.Pi, 42, float.MaxValue, float.MinValue, float.NaN, float.PositiveInfinity, float.NegativeInfinity };
            ArraySegment<float> expected = new ArraySegment<float>(expectedFloats, 1, 10);
            ArraySegment<float> actual = JavaScriptTestHelper.echo1_ArraySegmentOfSingle(expected, false);
            Assert.Equal(expected.Count, actual.Count);
            Assert.NotEqual(expected[0], expected[1]);
            Assert.Equal(expected.Array, actual.Array);
            actual = JavaScriptTestHelper.echo1_ArraySegmentOfSingle(expected, true);
            Assert.Equal(expected[0], expected[1]);
            Assert.Equal(actual[0], actual[1]);
        }

        #endregion

        #region Boolean

        [Theory]
        [MemberData(nameof(MarshalBooleanCases))]
        public void JsImportBoolean(bool value)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_Boolean,
                JavaScriptTestHelper.retrieve1_Boolean,
                JavaScriptTestHelper.echo1_Boolean,
                JavaScriptTestHelper.throw1_Boolean,
                JavaScriptTestHelper.identity1_Boolean,
                "boolean");
        }

        #endregion Boolean

        #region Char
        [Theory]
        [MemberData(nameof(MarshalCharCases))]
        public void JsImportChar(char value)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_Char,
                JavaScriptTestHelper.retrieve1_Char,
                JavaScriptTestHelper.echo1_Char,
                JavaScriptTestHelper.throw1_Char,
                JavaScriptTestHelper.identity1_Char,
                "number");
        }

        #endregion Char

        #region Byte
        [Theory]
        [MemberData(nameof(MarshalByteCases))]
        public void JsImportByte(byte value)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_Byte,
                JavaScriptTestHelper.retrieve1_Byte,
                JavaScriptTestHelper.echo1_Byte,
                JavaScriptTestHelper.throw1_Byte,
                JavaScriptTestHelper.identity1_Byte,
                "number");
        }

        [Theory]
        [MemberData(nameof(OutOfRangeCases))]
        public void ByteOutOfRange(double value, string message)
        {
            JavaScriptTestHelper.store1_Double(value);
            var ex = Assert.Throws<JSException>(() => JavaScriptTestHelper.retrieve1_Byte());
            Assert.Contains(message, ex.Message);
        }

        #endregion Byte

        #region Int16
        [Theory]
        [MemberData(nameof(MarshalInt16Cases))]
        public void JsImportInt16(short value)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_Int16,
                JavaScriptTestHelper.retrieve1_Int16,
                JavaScriptTestHelper.echo1_Int16,
                JavaScriptTestHelper.throw1_Int16,
                JavaScriptTestHelper.identity1_Int16,
                "number");
        }

        #endregion Int16

        #region Int32
        [Theory]
        [MemberData(nameof(MarshalInt32Cases))]
        public async Task JsImportInt32DiscardNoWait(int value)
        {
            JavaScriptTestHelper.store1DiscardNoWait_Int32(value);
            await JavaScriptTestHelper.Delay(0);
            Assert.Equal(value, JavaScriptTestHelper.retrieve1_Int32());
        }

        [Theory]
        [MemberData(nameof(MarshalInt32Cases))]
        public void JsImportInt32(int value)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_Int32,
                JavaScriptTestHelper.retrieve1_Int32,
                JavaScriptTestHelper.echo1_Int32,
                JavaScriptTestHelper.throw1_Int32,
                JavaScriptTestHelper.identity1_Int32,
                "number");
        }

        [Theory]
        [MemberData(nameof(OutOfRangeCases))]
        public void Int32OutOfRange(double value, string message)
        {
            JavaScriptTestHelper.store1_Double(value);
            var ex = Assert.Throws<JSException>(() => JavaScriptTestHelper.retrieve1_Int32());
            Assert.Contains(message, ex.Message);
        }

        #endregion Int32

        #region Int52

        [Theory]
        [MemberData(nameof(MarshalInt52Cases))]
        public void JsImportInt52(long value)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_Int52,
                JavaScriptTestHelper.retrieve1_Int52,
                JavaScriptTestHelper.echo1_Int52,
                JavaScriptTestHelper.throw1_Int52,
                JavaScriptTestHelper.identity1_Int52,
                "number");
        }

        #endregion Int52

        #region BigInt64
        [Theory]
        [MemberData(nameof(MarshalBigInt64Cases))]
        public void JsImportBigInt64(long value)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_BigInt64,
                JavaScriptTestHelper.retrieve1_BigInt64,
                JavaScriptTestHelper.echo1_BigInt64,
                JavaScriptTestHelper.throw1_BigInt64,
                JavaScriptTestHelper.identity1_BigInt64,
                "bigint");
        }

        #endregion BigInt64

        #region Double
        [Theory]
        [MemberData(nameof(MarshalDoubleCases))]
        public void JsImportDouble(double value)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_Double,
                JavaScriptTestHelper.retrieve1_Double,
                JavaScriptTestHelper.echo1_Double,
                JavaScriptTestHelper.throw1_Double,
                JavaScriptTestHelper.identity1_Double,
                "number");
        }

        #endregion Double

        #region Single
        [Theory]
        [MemberData(nameof(MarshalSingleCases))]
        public void JsImportSingle(float value)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_Single,
                JavaScriptTestHelper.retrieve1_Single,
                JavaScriptTestHelper.echo1_Single,
                JavaScriptTestHelper.throw1_Single,
                JavaScriptTestHelper.identity1_Single,
                "number");
        }

        #endregion Single

        #region IntPtr
        [Theory]
        [MemberData(nameof(MarshalIntPtrCases))]
        public void JsImportIntPtr(IntPtr value)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_IntPtr,
                JavaScriptTestHelper.retrieve1_IntPtr,
                JavaScriptTestHelper.echo1_IntPtr,
                JavaScriptTestHelper.throw1_IntPtr,
                JavaScriptTestHelper.identity1_IntPtr,
                "number");
        }

        #endregion IntPtr

        #region VoidPtr

        [Theory]
        [MemberData(nameof(MarshalIntPtrCases))]
        public unsafe void JsImportVoidPtr(IntPtr xvalue)
        {
            void* value = (void*)xvalue;

            JavaScriptTestHelper.store1_VoidPtr(value);
            void* res = JavaScriptTestHelper.retrieve1_VoidPtr();
            Assert.True(value == res);
            res = JavaScriptTestHelper.echo1_VoidPtr(value);
            Assert.True(value == res);

            var actualJsType = JavaScriptTestHelper.getType1();
            Assert.Equal("number", actualJsType);
        }

        #endregion VoidPtr

        #region Datetime
        [Theory]
        [MemberData(nameof(MarshalDateTimeCases))]
        public void JSImportDateTime(DateTime value)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_DateTime,
                JavaScriptTestHelper.retrieve1_DateTime,
                JavaScriptTestHelper.echo1_DateTime,
                JavaScriptTestHelper.throw1_DateTime,
                JavaScriptTestHelper.identity1_DateTime,
                "object", "Date");
        }

        [Fact] // JavaScript Date has millisecond precision, the microseconds are lost during marshalling
        public async Task DateTimeMarshallingLosesMicrosecondComponentPrecisionLoss()
        {
            DateTime now = new DateTime(1995, 4, 1, 10, 43, 6, 94, microsecond: 20);
            DateTime t = JavaScriptTestHelper.ReturnDateTimeWithOffset(now, 0);
            Assert.NotEqual(now, t);
            DateTime nowWithJSPrecision = now.AddMicroseconds(-now.Microsecond);
            Assert.Equal(nowWithJSPrecision, t);
        }
        #endregion Datetime

        #region DateTimeOffset
        [Theory]
        [MemberData(nameof(MarshalDateTimeOffsetCases))]
        public void JSImportDateTimeOffset(DateTimeOffset value)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_DateTimeOffset,
                JavaScriptTestHelper.retrieve1_DateTimeOffset,
                JavaScriptTestHelper.echo1_DateTimeOffset,
                JavaScriptTestHelper.throw1_DateTimeOffset,
                JavaScriptTestHelper.identity1_DateTimeOffset,
                "object", "Date");
        }

        #endregion DateTimeOffset

        #region NullableBoolean
        [Theory]
        [MemberData(nameof(MarshalNullableBooleanCases))]
        public void JsImportNullableBoolean(bool? value)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_NullableBoolean,
                JavaScriptTestHelper.retrieve1_NullableBoolean,
                JavaScriptTestHelper.echo1_NullableBoolean,
                JavaScriptTestHelper.throw1_NullableBoolean,
                JavaScriptTestHelper.identity1_NullableBoolean,
                "boolean");
        }

        #endregion NullableBoolean

        #region NullableInt32
        [Theory]
        [MemberData(nameof(MarshalNullableInt32Cases))]
        public void JsImportNullableInt32(int? value)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_NullableInt32,
                JavaScriptTestHelper.retrieve1_NullableInt32,
                JavaScriptTestHelper.echo1_NullableInt32,
                JavaScriptTestHelper.throw1_NullableInt32,
                JavaScriptTestHelper.identity1_NullableInt32,
                "number");
        }

        #endregion NullableInt32

        #region NullableBigInt64
        [Theory]
        [MemberData(nameof(MarshalNullableBigInt64Cases))]
        public void JsImportNullableBigInt64(long? value)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_NullableBigInt64,
                JavaScriptTestHelper.retrieve1_NullableBigInt64,
                JavaScriptTestHelper.echo1_NullableBigInt64,
                JavaScriptTestHelper.throw1_NullableBigInt64,
                JavaScriptTestHelper.identity1_NullableBigInt64,
                "bigint");
        }

        #endregion NullableBigInt64

        #region NullableIntPtr
        [Theory]
        [MemberData(nameof(MarshalNullableIntPtrCases))]
        public void JsImportNullableIntPtr(IntPtr? value)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_NullableIntPtr,
                JavaScriptTestHelper.retrieve1_NullableIntPtr,
                JavaScriptTestHelper.echo1_NullableIntPtr,
                JavaScriptTestHelper.throw1_NullableIntPtr,
                JavaScriptTestHelper.identity1_NullableIntPtr,
                "number");
        }

        #endregion NullableIntPtr

        #region NullableDouble
        [Theory]
        [MemberData(nameof(MarshalNullableDoubleCases))]
        public void JsImportNullableDouble(double? value)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_NullableDouble,
                JavaScriptTestHelper.retrieve1_NullableDouble,
                JavaScriptTestHelper.echo1_NullableDouble,
                JavaScriptTestHelper.throw1_NullableDouble,
                JavaScriptTestHelper.identity1_NullableDouble,
                "number");
        }

        #endregion NullableDouble

        #region NullableDateTime
        [Theory]
        [MemberData(nameof(MarshalNullableDateTimeCases))]
        public void JsImportNullableDateTime(DateTime? value)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_NullableDateTime,
                JavaScriptTestHelper.retrieve1_NullableDateTime,
                JavaScriptTestHelper.echo1_NullableDateTime,
                JavaScriptTestHelper.throw1_NullableDateTime,
                JavaScriptTestHelper.identity1_NullableDateTime,
                "object");
        }

        #endregion NullableDateTime

        #region String
        [Theory]
        [MemberData(nameof(MarshalStringCases))]
        public void JsImportString(string value)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_String,
                JavaScriptTestHelper.retrieve1_String,
                JavaScriptTestHelper.echo1_String,
                JavaScriptTestHelper.throw1_String,
                JavaScriptTestHelper.identity1_String
                , "string");
        }

        [Fact]
        public void JsImportNative()
        {
            if (JSHost.GlobalThis.HasProperty("window"))
            {
                var actual = JavaScriptTestHelper.NativeFunctionToString();
                Assert.StartsWith("http", actual);
            }
        }

        [Fact]
        public void JsImportInstanceMember()
        {
            var actual = JavaScriptTestHelper.MemberEcho("t-e-s-t");
            Assert.StartsWith("t-e-s-t-w-i-t-h-i-n-s-t-a-n-c-e", actual);
        }

        [Fact]
        public void JsImportReboundInstanceMember()
        {
            var actual = JavaScriptTestHelper.ReboundMemberEcho("t-e-s-t");
            Assert.StartsWith("t-e-s-t-w-i-t-h-i-n-s-t-a-n-c-e", actual);
        }

        #endregion String

        #region Object
        [Theory]
        [MemberData(nameof(MarshalObjectCases))]
        public void JSImportObject(object value, string clazz)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_Object,
                JavaScriptTestHelper.retrieve1_Object,
                JavaScriptTestHelper.echo1_Object,
                JavaScriptTestHelper.throw1_Object,
                JavaScriptTestHelper.identity1_Object,
                "object", clazz);
        }

        #endregion Object

        #region Exception
        [Theory]
        [MemberData(nameof(MarshalExceptionCases))]
        public void JSImportException(Exception value, string clazz)
        {
            if (clazz == "JSTestError")
            {
                value = JavaScriptTestHelper.createException("!CreateEx!");
            }

            JsImportTest(value,
                JavaScriptTestHelper.store1_Exception,
                JavaScriptTestHelper.retrieve1_Exception,
                JavaScriptTestHelper.echo1_Exception,
                JavaScriptTestHelper.throw1_Exception,
                JavaScriptTestHelper.identity1_Exception,
                "object", clazz);
        }

        [Fact]
        public void JSImportReturnError()
        {
            var err = JavaScriptTestHelper.returnError() as Exception;
            Assert.NotNull(err);
            Assert.Contains("this-is-error", err.Message);
        }

        #endregion Exception

        #region JSObject
        [Theory]
        [MemberData(nameof(MarshalIJSObjectCases))]
        public void JSImportIJSObject(JSObject value, string clazz)
        {
            if (clazz == "JSData")
            {
                value = JavaScriptTestHelper.createData("!CreateJS!");
            }

            JsImportTest(value,
                JavaScriptTestHelper.store1_JSObject,
                JavaScriptTestHelper.retrieve1_JSObject,
                JavaScriptTestHelper.echo1_JSObject,
                JavaScriptTestHelper.throw1_JSObject,
                JavaScriptTestHelper.identity1_JSObject,
                "object", clazz);
        }

        #endregion JSObject

        #region ProxyOfProxy
        [Fact]
        public void ProxyOfProxyThrows()
        {
            // proxy of proxy should throw
            JavaScriptTestHelper.store1_Object(new object());
            Assert.Throws<JSException>(() => JavaScriptTestHelper.retrieve1_JSObject());
        }


        [Fact]
        public void ProxyOfIntThrows()
        {
            // JSObject proxy of int should throw
            JavaScriptTestHelper.store1_Int32(13);
            Assert.Throws<JSException>(() => JavaScriptTestHelper.retrieve1_JSObject());
        }
        #endregion

        #region Task

        [Fact]
        public async Task JsImportSleep()
        {
            await JavaScriptTestHelper.sleep(100);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMultithreadingSupported))] // slow
        public async Task JsImportTaskTypes()
        {
            for (int i = 0; i < 100; i++)
            {
                object a = new object();
                Exception e = new Exception();
                JSObject j = JSHost.GlobalThis;
                Assert.Equal("test", await JavaScriptTestHelper.echopromise_String("test"));
                Assert.Same(a, await JavaScriptTestHelper.echopromise_Object(a));
                Assert.Same(e, await JavaScriptTestHelper.echopromise_Exception(e));
                Assert.Same(j, await JavaScriptTestHelper.echopromise_JSObject(j));
                GC.Collect();
                await Task.Delay(10);
            }
        }

        [Fact]
        public async Task JsImportThenVoid()
        {
            TaskCompletionSource tcs = new TaskCompletionSource();
            JavaScriptTestHelper.thenvoid(tcs.Task);
            GC.Collect();
            tcs.SetResult();

            GC.Collect();

            await Task.Yield();
        }

        [Fact]
        [OuterLoop]
        public async Task JsImportForeverMany()
        {
            for (int i = 0; i < 1000; i++)
            {
                if (i % 100 == 0)
                {
                    GC.Collect();
                    await Task.Yield();
                }
                var forever = JavaScriptTestHelper.forever();
                Assert.False(forever.IsCompleted);
            }
        }

        [Fact]
        public async Task JsImportVoidTaskPending()
        {
            GC.Collect();
            var pending = Task.Delay(1000);
            var res = JavaScriptTestHelper.await2(pending);
            GC.Collect();
            Assert.False(res.IsCompleted);
            await Task.Yield();
            GC.Collect();
            await res;
            GC.Collect();
            Assert.True(res.IsCompleted);
            GC.Collect();
        }

        [Fact]
        public async Task JsImportVoidTaskComplete()
        {
            GC.Collect();
            var resComplete = JavaScriptTestHelper.await2(Task.CompletedTask);
            GC.Collect();
            await Task.Delay(100);
            GC.Collect();
            await Task.Yield();
            GC.Collect();
            Assert.True(resComplete.IsCompleted);
            GC.Collect();
            await resComplete;
            GC.Collect();
        }

        [Fact]
        public async Task JsImportSleep2()
        {
            int ms = await JavaScriptTestHelper.sleep_Int(100);
            Assert.Equal(100, ms);
        }


        [Fact]
        public async Task JsImportTaskEchoComplete()
        {
            var task = JavaScriptTestHelper.echo1_Task(Task.CompletedTask);
            Assert.NotEqual(Task.CompletedTask, task);
            // yield to main loop, because "the respective handler function will be called asynchronously"
            // https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Promise/then#return_value
            await Task.Delay(100);
            Assert.True(task.IsCompleted);
        }

        [Fact]
        public async Task JsImportTaskEchoPendingResult()
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            var task = JavaScriptTestHelper.echo1_Task(tcs.Task);
            Assert.NotEqual(tcs.Task, task);
            Assert.False(task.IsCompleted);

            tcs.SetResult("test");
            // yield to main loop, because "the respective handler function will be called asynchronously"
            await Task.Delay(100);
            Assert.True(task.IsCompleted);
            Assert.Equal(typeof(Task), task.GetType());
        }

        [Fact]
        public async Task JsImportTaskEchoPendingException()
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            var task = JavaScriptTestHelper.echo1_Task(tcs.Task);
            Assert.NotEqual(tcs.Task, task);
            Assert.False(task.IsCompleted);

            tcs.SetException(new Exception("Test"));
            // yield to main loop, because "the respective handler function will be called asynchronously"
            await Task.Delay(100);
            Assert.True(task.IsFaulted);
            await Assert.ThrowsAsync<Exception>(async () => await task);
        }

        [Theory]
        [MemberData(nameof(TaskCases))]
        public async Task JsImportTaskAwaitPendingResult(object result)
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            var task = JavaScriptTestHelper.await1(tcs.Task);
            Assert.NotEqual(tcs.Task, task);
            Assert.False(task.IsCompleted);

            tcs.SetResult(result);
            // yield to main loop, because "the respective handler function will be called asynchronously"
            await Task.Delay(100);
            Assert.True(task.IsCompleted);
            var res = await task;
            if (result != null && result.GetType() == typeof(int))
            {
                Assert.Equal(result, Convert.ToInt32(res));
            }
            else
            {
                Assert.Equal(result, res);
            }
        }

        [Fact]
        public async Task JsImportTaskAwaitPendingException()
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
            var task = JavaScriptTestHelper.await1(tcs.Task);
            Assert.NotEqual(tcs.Task, task);
            Assert.False(task.IsCompleted);

            tcs.SetException(new Exception("Test"));
            // yield to main loop, because "the respective handler function will be called asynchronously"
            await Task.Delay(100);
            Assert.True(task.IsFaulted);
            await Assert.ThrowsAsync<Exception>(async () => await task);
        }

        [Fact]
        public async Task JsImportTaskAwaitPendingExceptionValue()
        {
            TaskCompletionSource<Exception> tcs = new TaskCompletionSource<Exception>();
            var task = JavaScriptTestHelper.await1_TaskOfException(tcs.Task);
            Assert.NotEqual(tcs.Task, task);
            Assert.False(task.IsCompleted);

            tcs.SetResult(new Exception("Test"));
            // yield to main loop, because "the respective handler function will be called asynchronously"
            await Task.Delay(100);
            Assert.True(task.IsCompletedSuccessfully);
            Assert.Equal("Test", task.Result.Message);
        }


        [Fact]
        public async Task JsImportTaskAwait()
        {
            var task = JavaScriptTestHelper.awaitvoid(Task.CompletedTask);
            await Task.Delay(100);
            Assert.True(task.IsCompleted);
            await task;
        }

        [Fact]
        public async Task JsImportResolvedPromiseReturnsCompletedTask()
        {
            var promise = JavaScriptTestHelper.ReturnResolvedPromise();

            if (!PlatformDetection.IsMultithreadingSupported)
            {
                Assert.False(promise.IsCompleted);
            }

            await promise;
            Assert.True(promise.IsCompleted);
        }

        #endregion

        #region Action

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMultithreadingSupported))]
        public void JsImportCallback_EchoAction()
        {
            bool called = false;
            Action expected = () =>
            {
                called = true;
            };
            var actual = JavaScriptTestHelper.echo1_ActionAction(expected);
            Assert.NotEqual(expected, actual);
            Assert.False(called);
            actual();
            Assert.True(called);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
        public void JsImportCallback_EchoActionThrows_MT()
        {
            bool called = false;
            Action expected = () =>
            {
                called = true;
            };
            var actual = JavaScriptTestHelper.echo1_ActionAction(expected);
            Assert.NotEqual(expected, actual);
            Assert.False(called);
            // with deputy thread, call back to C# from synchronous JS function is not allowed
            Assert.Throws<JSException>(()=>actual());
            Assert.False(called);
        }

        [Fact]
        public async Task JsImportCallback_Async()
        {
            bool called = false;
            var promise = JavaScriptTestHelper.backback_FuncIntIntFuncIntIntAsync((a,b) =>
            {
                called = true;
                return a + b;
            }, 123, 321);
            Assert.False(called);
            var actual = await promise;
            Assert.True(called);
            Assert.Equal(444, actual);
        }


        [Fact]
        [OuterLoop]
        public async Task JsImportCallback_EchoActionMany()
        {
            int a = 1;
            for (int i = 0; i < 1000; i++)
            {
                Action expected = () =>
                {
                    a += i;
                };
                var actual = JavaScriptTestHelper.echo1large_ActionAction(expected);
                Assert.NotEqual(expected, actual);
                if (i % 100 == 0)
                {
                    await Task.Yield();
                    GC.Collect();
                }
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMultithreadingSupported))]
        public void JsImportCallback_Action()
        {
            bool called = false;
            JavaScriptTestHelper.back3_Action(() =>
            {
                called = true;
            });
            Assert.True(called);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMultithreadingSupported))]
        public void JsImportEcho_ActionAction()
        {
            bool called = false;
            Action res = JavaScriptTestHelper.echo1_ActionAction(() =>
            {
                called = true;
            });
            Assert.False(called);
            res.Invoke();
            Assert.True(called);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMultithreadingSupported))]
        public void JsImportEcho_ActionIntActionInt()
        {
            int calledA = -1;
            Action<int> res = JavaScriptTestHelper.echo1_ActionIntActionInt((a) =>
            {
                calledA = a;
            });
            Assert.Equal(-1, calledA);
            res.Invoke(42);
            Assert.Equal(42, calledA);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMultithreadingSupported))]
        public void JsImportCallback_ActionInt()
        {
            int called = -1;
            JavaScriptTestHelper.back3_ActionInt((a) =>
            {
                called = a;
            }, 42);
            Assert.Equal(42, called);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMultithreadingSupported))]
        public void JsImportCallback_FunctionIntInt()
        {
            int called = -1;
            int res = JavaScriptTestHelper.back3_FunctionIntInt((a) =>
            {
                called = a;
                return a;
            }, 42);
            Assert.Equal(42, called);
            Assert.Equal(42, res);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMultithreadingSupported))]
        public void JsImportBackCallback_FunctionIntInt()
        {
            int called = -1;
            Func<int, int> res = JavaScriptTestHelper.backback_FuncIntFuncInt((a) =>
            {
                called = a;
                return a;
            }, 42);
            Assert.Equal(-1, called);
            int actual = res.Invoke(42);
            Assert.Equal(84, actual);
            Assert.Equal(84, called);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMultithreadingSupported))]
        public void JsImportBackCallback_FunctionIntIntIntInt()
        {
            int calledA = -1;
            int calledB = -1;
            Func<int, int, int> res = JavaScriptTestHelper.backback_FuncIntIntFuncIntInt((a, b) =>
            {
                calledA = a;
                calledB = b;
                return a + b;
            }, 42, 43);
            Assert.Equal(-1, calledA);
            Assert.Equal(-1, calledB);
            int actual = res.Invoke(40, 41);
            Assert.Equal(166, actual);
            Assert.Equal(82, calledA);
            Assert.Equal(84, calledB);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMultithreadingSupported))]
        public void JsImportCallback_ActionIntInt()
        {
            int calledA = -1;
            int calledB = -1;
            JavaScriptTestHelper.back3_ActionIntInt((a, b) =>
            {
                calledA = a;
                calledB = b;
            }, 42, 43);
            Assert.Equal(42, calledA);
            Assert.Equal(43, calledB);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMultithreadingSupported))]
        public void JsImportCallback_ActionLongLong()
        {
            long calledA = -1;
            long calledB = -1;
            JavaScriptTestHelper.back3_ActionLongLong((a, b) =>
            {
                calledA = a;
                calledB = b;
            }, 42, 43);
            Assert.Equal(42, calledA);
            Assert.Equal(43, calledB);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMultithreadingSupported))]
        public void JsImportCallback_ActionIntLong()
        {
            int calledA = -1;
            long calledB = -1;
            JavaScriptTestHelper.back3_ActionIntLong((a, b) =>
            {
                calledA = a;
                calledB = b;
            }, 42, 43);
            Assert.Equal(42, calledA);
            Assert.Equal(43, calledB);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMultithreadingSupported))]
        public void JsImportCallback_ActionIntLongDouble()
        {
            int calledA = -1;
            long calledB = -1;
            double calledC = -1;
            JavaScriptTestHelper.back4_ActionIntLongDouble((a, b, c) =>
            {
                calledA = a;
                calledB = b;
                calledC = c;
            }, 42, 43, 44.5);
            Assert.Equal(42, calledA);
            Assert.Equal(43, calledB);
            Assert.Equal(44.5, calledC);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMultithreadingSupported))]
        public void JsImportCallback_ActionIntThrow()
        {
            int called = -1;
            Exception expected = new Exception("test!!");
            Exception actual = Assert.Throws<Exception>(() => JavaScriptTestHelper.back3_ActionInt((a) =>
            {
                called = a;
                throw expected;
            }, 42));
            Assert.Equal(42, called);
            Assert.Same(expected, actual);
        }

        [Fact]
        public void JsImportMath()
        {
            Func<int, int, int> plus = Utils.CreateFunctionIntIntInt("a", "b", @"return a+b");
            Assert.Equal(3, plus(1, 2));
        }

        #endregion

        private void JsImportTest<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>(T value
            , Action<T> store1
            , Func<T> retrieve1
            , Func<T, T> echo1
            , Func<T, T> throw1
            , Func<T, bool> identity1
            , string jsType, string? jsClass = null)
        {
            if (value == null)
            {
                jsClass = null;
                jsType = "object";
            }

            // invoke 
            store1(value);
            var res = retrieve1();
            Assert.Equal(value, res);
            res = echo1(value);
            Assert.Equal(value, res);
            var equals = identity1(value);
            Assert.True(equals, "value not equals");

            var actualJsType = JavaScriptTestHelper.getType1();
            Assert.Equal(jsType, actualJsType);

            if (jsClass != null)
            {
                var actualJsClass = JavaScriptTestHelper.getClass1();
                Assert.Equal(jsClass, actualJsClass);
            }
            var exThrow0 = Assert.Throws<JSException>(() => JavaScriptTestHelper.throw0());
            Assert.Contains("throw-0-msg", exThrow0.Message);
            Assert.DoesNotContain(" at ", exThrow0.Message);
            if (!PlatformDetection.IsMultithreadingSupported)
            {
                Assert.Contains("throw0fn", exThrow0.StackTrace);
            }

            var exThrow1 = Assert.Throws<JSException>(() => throw1(value));
            Assert.Contains("throw1-msg", exThrow1.Message);
            Assert.DoesNotContain(" at ", exThrow1.Message);
            if (!PlatformDetection.IsMultithreadingSupported)
            {
                Assert.Contains("throw1fn", exThrow1.StackTrace);
            }
            else
            {
                Assert.Contains("omitted JavaScript stack trace", exThrow0.StackTrace);
            }

            // anything is a system.object, sometimes it would be JSObject wrapper
            if (typeof(T).IsPrimitive)
            {
                if (typeof(T) != typeof(long))
                {

                    object resBoxed = JavaScriptTestHelper.echo1_Object(value);
                    // js Number always boxes as double
                    if (typeof(T) == typeof(IntPtr))
                    {
                        //TODO Assert.Equal((IntPtr)(object)value, (IntPtr)(int)(double)resBoxed);
                    }
                    else if (typeof(T) == typeof(bool))
                    {
                        Assert.Equal((bool)(object)value, (bool)resBoxed);
                    }
                    else if (typeof(T) == typeof(char))
                    {
                        Assert.Equal((char)(object)value, (char)(double)resBoxed);
                    }
                    else
                    {
                        Assert.Equal(Convert.ToDouble(value), resBoxed);
                    }
                }

                //TODO var task = JavaScriptTestHelper.await1(Task.FromResult((object)value));
            }
            else if (typeof(T) == typeof(DateTime))
            {
                var resBoxed = JavaScriptTestHelper.echo1_Object(value);
                Assert.Equal(value, resBoxed);
            }
            else if (typeof(T) == typeof(DateTimeOffset))
            {
                var resBoxed = JavaScriptTestHelper.echo1_Object(value);
                Assert.Equal(((DateTimeOffset)(object)value).UtcDateTime, resBoxed);
            }
            else if (Nullable.GetUnderlyingType(typeof(T)) != null)
            {
                var vt = Nullable.GetUnderlyingType(typeof(T));
                if (vt != typeof(long))
                {
                    var resBoxed = JavaScriptTestHelper.echo1_Object(value);
                    if (resBoxed != null)
                    {
                        if (vt == typeof(bool))
                        {
                            Assert.Equal(((bool?)(object)value).Value, (bool)resBoxed);
                        }
                        else if (vt == typeof(char))
                        {
                            Assert.Equal(((char?)(object)value).Value, (char)resBoxed);
                        }
                        else if (vt == typeof(DateTime))
                        {
                            Assert.Equal(((DateTime?)(object)value).Value, resBoxed);
                        }
                        else if (vt == typeof(DateTimeOffset))
                        {
                            Assert.Equal(((DateTimeOffset?)(object)value).Value.UtcDateTime, resBoxed);
                        }
                        else if (vt == typeof(IntPtr))
                        {
                            // TODO Assert.Equal((double)((IntPtr?)(object)value).Value, resBoxed);
                        }
                        else
                        {
                            Assert.Equal(Convert.ToDouble(value), resBoxed);
                        }
                    }
                    else
                    {
                        Assert.Equal(value, default(T));
                    }
                }
            }
            else
            {
                var resObj = JavaScriptTestHelper.retrieve1_Object();
                if (resObj == null || resObj.GetType() != typeof(JSObject))
                {
                    Assert.Equal(value, resObj);
                }
            }

            if (typeof(Exception).IsAssignableFrom(typeof(T)))
            {
                // all exceptions are Exception
                var resEx = JavaScriptTestHelper.retrieve1_Exception();
                Assert.Equal((Exception)(object)value, resEx);
            }
        }
    }
}
