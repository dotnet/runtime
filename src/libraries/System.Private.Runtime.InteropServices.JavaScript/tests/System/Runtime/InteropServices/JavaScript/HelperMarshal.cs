// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    public static class HelperMarshal
    {
        internal const string INTEROP_CLASS = "[System.Private.Runtime.InteropServices.JavaScript.Tests]System.Runtime.InteropServices.JavaScript.Tests.HelperMarshal:";
        internal static int _i32Value;
        private static void InvokeI32(int a, int b)
        {
            _i32Value = a + b;
        }

        internal static float _f32Value;
        private static void InvokeFloat(float f)
        {
            _f32Value = f;
        }

        internal static double _f64Value;
        private static void InvokeDouble(double d)
        {
            _f64Value = d;
        }

        internal static long _i64Value;
        private static void InvokeLong(long l)
        {
            _i64Value = l;
        }

        internal static byte[] _byteBuffer;
        private static void MarshalArrayBuffer(ArrayBuffer buffer)
        {
            using (var bytes = new Uint8Array(buffer))
                _byteBuffer = bytes.ToArray();
        }

        private static void MarshalByteBuffer(Uint8Array buffer)
        {
            _byteBuffer = buffer.ToArray();
        }

        internal static string _stringResource;
        private static void InvokeString(string s)
        {
            _stringResource = s;
        }

        internal static string _stringResource2;
        private static void InvokeString2(string s)
        {
            _stringResource2 = s;
        }

        private static string StoreArgumentAndReturnLiteral(string s)
        {
            _stringResource = $"s: {s} length: {s?.Length}";
            return "1";
        }

        private static string StoreAndReturnNew(string s)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("Got:");
            sb.Append(' ');
            sb.Append(s);
            _stringResource = sb.ToString();
            return _stringResource;
        }

        internal static string _marshaledString;
        private static string InvokeMarshalString()
        {
            _marshaledString = "Hic Sunt Dracones";
            return _marshaledString;
        }

        internal static object _object1;
        private static object InvokeObj1(object obj)
        {
            _object1 = obj;
            return obj;
        }

        internal static object _object2;
        private static object InvokeObj2(object obj)
        {
            _object2 = obj;
            return obj;
        }

        internal static object _marshaledObject;
        private static object InvokeMarshalObj()
        {
            _marshaledObject = new object();
            return _marshaledObject;
        }

        private static object InvokeReturnMarshalObj()
        {
            return _marshaledObject;
        }

        internal static int _valOne, _valTwo;
        private static void ManipulateObject(JSObject obj)
        {
            _valOne = (int)obj.Invoke("inc");
            _valTwo = (int)obj.Invoke("add", 20);
        }

        internal static object[] _jsObjects;
        private static void MinipulateObjTypes(JSObject obj)
        {
            _jsObjects = new object[4];
            _jsObjects[0] = obj.Invoke("return_int");
            _jsObjects[1] = obj.Invoke("return_double");
            _jsObjects[2] = obj.Invoke("return_string");
            _jsObjects[3] = obj.Invoke("return_bool");
        }

        internal static int _jsAddFunctionResult;
        private static void UseFunction(JSObject obj)
        {
            _jsAddFunctionResult = (int)obj.Invoke("call", null, 10, 20);
        }

        internal static int _jsAddAsFunctionResult;
        private static void UseAsFunction(Function func)
        {
            _jsAddAsFunctionResult = (int)func.Call(null, 20, 30);
        }

        internal static int _intValue;
        private static void InvokeInt(int value)
        {
            _intValue = value;
        }

        internal static IntPtr _intPtrValue;
        private static void InvokeIntPtr(IntPtr i)
        {
            _intPtrValue = i;
        }

        internal static IntPtr _marshaledIntPtrValue;
        private static IntPtr InvokeMarshalIntPtr()
        {
            _marshaledIntPtrValue = (IntPtr)42;
            return _marshaledIntPtrValue;
        }

        internal static object[] _jsProperties;
        private static void RetrieveObjectProperties(JSObject obj)
        {
            _jsProperties = new object[4];
            _jsProperties[0] = obj.GetObjectProperty("myInt");
            _jsProperties[1] = obj.GetObjectProperty("myDouble");
            _jsProperties[2] = obj.GetObjectProperty("myString");
            _jsProperties[3] = obj.GetObjectProperty("myBoolean");
        }

        private static void PopulateObjectProperties(JSObject obj, bool createIfNotExist)
        {
            _jsProperties = new object[4];
            obj.SetObjectProperty("myInt", 100, createIfNotExist);
            obj.SetObjectProperty("myDouble", 4.5, createIfNotExist);
            obj.SetObjectProperty("myString", "qwerty", createIfNotExist);
            obj.SetObjectProperty("myBoolean", true, createIfNotExist);
        }

        private static void SetTypedArrayByte(JSObject obj)
        {
            var dragons = "hic sunt dracones";
            byte[] buffer = System.Text.Encoding.ASCII.GetBytes(dragons);
            obj.SetObjectProperty("dracones", Uint8Array.From(buffer));
        }

        internal static byte[] _taByte;
        private static void GetTypedArrayByte(JSObject obj)
        {
            _taByte = ((Uint8Array)obj.GetObjectProperty("dracones")).ToArray();
        }

        private static Function _sumFunction;
        private static void CreateFunctionSum()
        {
            _sumFunction = new Function("a", "b", "return a + b");
        }

        internal static int _sumValue = 0;
        private static void CallFunctionSum()
        {
            if (_sumFunction == null)
                throw new Exception("_sumFunction is null");
            _sumValue = (int)_sumFunction.Call(null, 3, 5);
        }

        private static Function _mathMinFunction;
        private static void CreateFunctionApply()
        {
            var math = (JSObject)Runtime.GetGlobalObject("Math");
            if (math == null)
                throw new Exception("Runtime.GetGlobalObject(Math) returned null");
            _mathMinFunction = (Function)math.GetObjectProperty("min");

        }

        internal static int _minValue = 0;
        private static void CallFunctionApply()
        {
            if (_mathMinFunction == null)
                throw new Exception("_mathMinFunction is null");
            _minValue = (int)_mathMinFunction.Apply(null, new object[] { 5, 6, 2, 3, 7 });
        }

        internal static Uri _blobURL;
        public static void SetBlobUrl(string blobUrl)
        {
            _blobURL = new Uri(blobUrl);
        }

        internal static Uri _blobURI;
        public static void SetBlobAsUri(Uri blobUri)
        {
            _blobURI = blobUri;
        }

        internal static uint _uintValue;
        private static void InvokeUInt(uint value)
        {
            _uintValue = value;
        }

        internal static TestEnum _enumValue;
        private static void SetEnumValue(TestEnum value)
        {
            _enumValue = value;
        }
        private static TestEnum GetEnumValue()
        {
            return _enumValue;
        }

        private static UInt64 GetUInt64()
        {
            return UInt64.MaxValue;
        }

        internal static int _functionResultValue;
        private static Func<int, int, int> CreateFunctionDelegate()
        {
            return (a, b) =>
            {
                _functionResultValue = a + b;
                return _functionResultValue;
            };
        }

        internal static int _functionActionResultValue;
        internal static int _functionActionResultValueOfAction;
        private static Func<int, int, Action<int,int>> CreateFunctionDelegateWithAction()
        {
            return (a, b) =>
            {
                _functionActionResultValue = a + b;
                return (i1, i2) =>
                {
                    _functionActionResultValueOfAction = i1 + i2;
                };
            };
        }

        internal static int _actionResultValue;
        private static Action<int,int> CreateActionDelegate()
        {
            return (a1, a2) =>
            {
                _actionResultValue = a1 + a2;
            };
        }

        private static bool AreEqual(int a, int b)
        {
            return a == b;
        }

        private static string TestString1(string a)
        {
            return "Received: " + a;
        }

        private static void SetTestString1(string a)
        {
            _delMethodStringResultValue = a;
        }

        // Create a method for a delegate.
        public static void DelegateMethod(string message)
        {
            _delMethodResultValue = message;
        }

        delegate void Del(string message);
        internal static string _delMethodResultValue;
        private static Del CreateDelegateMethod()
        {
            // Instantiate the delegate.
            Del handler = DelegateMethod;
            return handler;
        }

        delegate string Del2(string message);
        internal static string _delMethodStringResultValue;
        private static Del2 CreateDelegateMethodReturnString()
        {
            // Instantiate the delegate.
            Del2 handler = TestString1;
            return handler;
        }

        internal static string _delegateCallResult;
        private static Del CreateDelegateFromAnonymousMethod_VoidString()
        {
            // Instantiate the delegate.
            Del handler = delegate(string name) { _delegateCallResult = $"Notification received for: {name}"; };
            return handler;
        }

        private static Del CreateDelegateFromLambda_VoidString()
        {
            // Instantiate the delegate.
            Del handler = (string name) => { _delegateCallResult = $"Notification received for: {name}"; };
            return handler;
        }

        public static void DelegateMethod_VoidString(string name) => _delegateCallResult = $"Notification received for: {name}";

        private static Del CreateDelegateFromMethod_VoidString()
        {
            // Instantiate the delegate.
            Del handler = DelegateMethod_VoidString;
            return handler;
        }

        private static Action<string> CreateActionT_VoidString()
            => (string name) => _delegateCallResult = $"Notification received for: {name}";

        static void Hello(string s)
        {
            _delegateCallResult += $"  Hello, {s}!";
        }

        static void GoodMorning(string s)
        {
            _delegateCallResult += $"  GoodMorning, {s}!";
        }

        delegate void CustomDelStr(string s);
        private static CustomDelStr CreateCustomMultiCastDelegate_VoidString()
        {
            CustomDelStr hiDel, mornDel, multiDel;
            hiDel = Hello;
            mornDel = GoodMorning;
            multiDel = hiDel + mornDel;

            return multiDel;
        }

        private static Action<string> CreateMultiCastAction_VoidString()
        {
            Action<string> hiDel, mornDel, multiDel;
            hiDel = Hello;
            mornDel = GoodMorning;
            multiDel = hiDel + mornDel;

            return multiDel;
        }

        internal static JSObject _funcActionBufferObjectResultValue;
        internal static int _funcActionBufferResultLengthValue;

        private static Func<Uint8Array, Action<Uint8Array>> CreateFunctionAcceptingUint8Array()
        {
            return (buffer) =>
            {
                _funcActionBufferObjectResultValue = buffer;
                return (i1) =>
                {
                    _funcActionBufferResultLengthValue = i1.Length;
                };
            };
        }
        private static Func<Array, Action<Array>> CreateFunctionAcceptingArray()
        {
            return (buffer) =>
            {
                _funcActionBufferObjectResultValue = buffer;
                return (i1) =>
                {
                    _funcActionBufferResultLengthValue = i1.Length;
                };
            };
        }

        public static Task SynchronousTask() 
        {
            return Task.CompletedTask;
        }

        public static async Task AsynchronousTask() 
        {
            await Task.Yield();
        }

        public static Task<int> SynchronousTaskInt(int i) 
        {
            return Task.FromResult(i);
        }

        public static async Task<int> AsynchronousTaskInt(int i) 
        {
            await Task.Yield();
            return i;
        }

        public static Task FailedSynchronousTask() 
        {
            return Task.FromException(new Exception());
        }

        public static async Task FailedAsynchronousTask() 
        {
            await Task.Yield();
            throw new Exception();
        }

        public static async ValueTask AsynchronousValueTask() 
        {
            await Task.Yield();
        }

        public static ValueTask SynchronousValueTask() 
        {
            return ValueTask.CompletedTask;
        }

        public static ValueTask<int> SynchronousValueTaskInt(int i) 
        {
            return ValueTask.FromResult(i);
        }

        public static async ValueTask<int> AsynchronousValueTaskInt(int i) 
        {
            await Task.Yield();
            return i;
        }

        public static ValueTask FailedSynchronousValueTask() 
        {
            return ValueTask.FromException(new Exception());
        }

        public static async ValueTask FailedAsynchronousValueTask() 
        {
            await Task.Yield();
            throw new Exception();
        }
    }

    public enum TestEnum : uint {
        FirstValue = 1,
        Zero = 0,
        Five = 5,
        BigValue = 0xFFFFFFFEu
    }
}
