// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.JavaScript;
using System.Collections.Generic;
using Xunit;

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    public static class HelperMarshal
    {
        internal const string INTEROP_CLASS = "[System.Runtime.InteropServices.JavaScript.Tests]System.Runtime.InteropServices.JavaScript.Tests.HelperMarshal:";
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
        internal static int[] _intBuffer;
        private static void MarshalArrayBufferToInt32Array(ArrayBuffer buffer)
        {
            using (var ints = new Int32Array(buffer))
                _intBuffer = ints.ToArray();
        }

        internal static string _stringResource;
        private static void InvokeString(string s)
        {
            _stringResource = s;
        }
        internal static string _marshalledString;
        private static string InvokeMarshalString()
        {
            _marshalledString = "Hic Sunt Dracones";
            return _marshalledString;
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

        internal static object _marshalledObject;
        private static object InvokeMarshalObj()
        {
            _marshalledObject = new object();
            return _marshalledObject;
        }

        internal static int _valOne, _valTwo;
        private static void ManipulateObject(JSObject obj)
        {
            _valOne = (int)obj.Invoke("inc"); ;
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

        internal static int _functionResultValue;
        private static Func<int, int, int> CreateFunctionDelegate()
        {
            return (a, b) =>
            {
                _functionResultValue = a + b;
                return _functionResultValue;
            };
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
    }
}
