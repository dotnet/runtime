// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    public partial class JavaScriptTestHelper
    {
        [JSImport("console.log")]
        public static partial void Log(string message);

        [JSImport("javaScriptTestHelper.getType1")]
        internal static partial string getType1();

        [JSImport("javaScriptTestHelper.getClass1")]
        internal static partial string getClass1();

        [JSImport("javaScriptTestHelper.throw0")]
        internal static partial void throw0();

        [JSImport("javaScriptTestHelper.echo1")]
        internal static partial Task echo1_Task(Task arg1);

        [JSImport("javaScriptTestHelper.createException")]
        internal static partial JSException createException(string name);

        [JSImport("javaScriptTestHelper.createData")]
        internal static partial IJSObject createData(string name);

        #region Task
        [JSImport("javaScriptTestHelper.awaitvoid")]
        internal static partial Task awaitvoid(Task arg1);
        [JSImport("javaScriptTestHelper.await1")]
        internal static partial Task<object> await1(Task<object> arg1);
        [JSImport("javaScriptTestHelper.invoke1")]
        internal static partial Task<object> invoke1_TaskOfObject(Task<object> value, string name);
        [JSExport("JavaScriptTestHelper.AwaitTaskOfObject")]
        public static async Task<object> AwaitTaskOfObject(Task<object> arg1)
        {
            var res = await arg1;
            return res;
        }
        #endregion

        #region Boolean
        [JSImport("javaScriptTestHelper.echo1")]
        internal static partial bool echo1_Boolean(bool value);
        [JSImport("javaScriptTestHelper.store1")]
        internal static partial void store1_Boolean(bool value);
        [JSImport("javaScriptTestHelper.retrieve1")]
        internal static partial bool retrieve1_Boolean();
        [JSImport("javaScriptTestHelper.identity1")]
        internal static partial bool identity1_Boolean(bool value);
        [JSImport("javaScriptTestHelper.throw1")]
        internal static partial bool throw1_Boolean(bool value);
        [JSImport("javaScriptTestHelper.invoke1")]
        internal static partial bool invoke1_Boolean(bool value, string name);
        [JSExport("JavaScriptTestHelper.EchoBoolean")]
        public static bool EchoBoolean(bool arg1)
        {
            return arg1;
        }
        #endregion Boolean

        #region Byte
        [JSImport("javaScriptTestHelper.echo1")]
        internal static partial byte echo1_Byte(byte value);
        [JSImport("javaScriptTestHelper.store1")]
        internal static partial void store1_Byte(byte value);
        [JSImport("javaScriptTestHelper.retrieve1")]
        internal static partial byte retrieve1_Byte();
        [JSImport("javaScriptTestHelper.identity1")]
        internal static partial bool identity1_Byte(byte value);
        [JSImport("javaScriptTestHelper.throw1")]
        internal static partial byte throw1_Byte(byte value);
        [JSImport("javaScriptTestHelper.invoke1")]
        internal static partial byte invoke1_Byte(byte value, string name);
        [JSExport("JavaScriptTestHelper.EchoByte")]
        public static byte EchoByte(byte arg1)
        {
            return arg1;
        }
        #endregion Byte

        #region Int16
        [JSImport("javaScriptTestHelper.echo1")]
        internal static partial short echo1_Int16(short value);
        [JSImport("javaScriptTestHelper.store1")]
        internal static partial void store1_Int16(short value);
        [JSImport("javaScriptTestHelper.retrieve1")]
        internal static partial short retrieve1_Int16();
        [JSImport("javaScriptTestHelper.identity1")]
        internal static partial bool identity1_Int16(short value);
        [JSImport("javaScriptTestHelper.throw1")]
        internal static partial short throw1_Int16(short value);
        [JSImport("javaScriptTestHelper.invoke1")]
        internal static partial short invoke1_Int16(short value, string name);
        [JSExport("JavaScriptTestHelper.EchoInt16")]
        public static short EchoInt16(short arg1)
        {
            return arg1;
        }
        #endregion Int16

        #region Int32
        [JSImport("javaScriptTestHelper.echo1")]
        internal static partial int echo1_Int32(int value);
        [JSImport("javaScriptTestHelper.store1")]
        internal static partial void store1_Int32(int value);
        [JSImport("javaScriptTestHelper.retrieve1")]
        internal static partial int retrieve1_Int32();
        [JSImport("javaScriptTestHelper.identity1")]
        internal static partial bool identity1_Int32(int value);
        [JSImport("javaScriptTestHelper.throw1")]
        internal static partial int throw1_Int32(int value);
        [JSImport("javaScriptTestHelper.invoke1")]
        internal static partial int invoke1_Int32(int value, string name);
        [JSExport("JavaScriptTestHelper.EchoInt32")]
        public static int EchoInt32(int arg1)
        {
            return arg1;
        }
        #endregion Int32

        #region Int64
        [JSImport("javaScriptTestHelper.echo1")]
        internal static partial long echo1_Int64(long value);
        [JSImport("javaScriptTestHelper.store1")]
        internal static partial void store1_Int64(long value);
        [JSImport("javaScriptTestHelper.retrieve1")]
        internal static partial long retrieve1_Int64();
        [JSImport("javaScriptTestHelper.identity1")]
        internal static partial bool identity1_Int64(long value);
        [JSImport("javaScriptTestHelper.throw1")]
        internal static partial long throw1_Int64(long value);
        [JSImport("javaScriptTestHelper.invoke1")]
        internal static partial long invoke1_Int64(long value, string name);
        [JSExport("JavaScriptTestHelper.EchoInt64")]
        public static long EchoInt64(long arg1)
        {
            return arg1;
        }
        #endregion Int64

        #region Double
        [JSImport("javaScriptTestHelper.echo1")]
        internal static partial double echo1_Double(double value);
        [JSImport("javaScriptTestHelper.store1")]
        internal static partial void store1_Double(double value);
        [JSImport("javaScriptTestHelper.retrieve1")]
        internal static partial double retrieve1_Double();
        [JSImport("javaScriptTestHelper.identity1")]
        internal static partial bool identity1_Double(double value);
        [JSImport("javaScriptTestHelper.throw1")]
        internal static partial double throw1_Double(double value);
        [JSImport("javaScriptTestHelper.invoke1")]
        internal static partial double invoke1_Double(double value, string name);
        [JSExport("JavaScriptTestHelper.EchoDouble")]
        public static double EchoDouble(double arg1)
        {
            return arg1;
        }
        #endregion Double

        #region Single
        [JSImport("javaScriptTestHelper.echo1")]
        internal static partial float echo1_Single(float value);
        [JSImport("javaScriptTestHelper.store1")]
        internal static partial void store1_Single(float value);
        [JSImport("javaScriptTestHelper.retrieve1")]
        internal static partial float retrieve1_Single();
        [JSImport("javaScriptTestHelper.identity1")]
        internal static partial bool identity1_Single(float value);
        [JSImport("javaScriptTestHelper.throw1")]
        internal static partial float throw1_Single(float value);
        [JSImport("javaScriptTestHelper.invoke1")]
        internal static partial float invoke1_Single(float value, string name);
        [JSExport("JavaScriptTestHelper.EchoSingle")]
        public static float EchoSingle(float arg1)
        {
            return arg1;
        }
        #endregion Single

        #region IntPtr
        [JSImport("javaScriptTestHelper.echo1")]
        internal static partial IntPtr echo1_IntPtr(IntPtr value);
        [JSImport("javaScriptTestHelper.store1")]
        internal static partial void store1_IntPtr(IntPtr value);
        [JSImport("javaScriptTestHelper.retrieve1")]
        internal static partial IntPtr retrieve1_IntPtr();
        [JSImport("javaScriptTestHelper.identity1")]
        internal static partial bool identity1_IntPtr(IntPtr value);
        [JSImport("javaScriptTestHelper.throw1")]
        internal static partial IntPtr throw1_IntPtr(IntPtr value);
        [JSImport("javaScriptTestHelper.invoke1")]
        internal static partial IntPtr invoke1_IntPtr(IntPtr value, string name);
        [JSExport("JavaScriptTestHelper.EchoIntPtr")]
        public static IntPtr EchoIntPtr(IntPtr arg1)
        {
            return arg1;
        }
        #endregion IntPtr

        #region DateTime
        [JSImport("javaScriptTestHelper.echo1")]
        internal static partial DateTime echo1_DateTime(DateTime value);
        [JSImport("javaScriptTestHelper.store1")]
        internal static partial void store1_DateTime(DateTime value);
        [JSImport("javaScriptTestHelper.retrieve1")]
        internal static partial DateTime retrieve1_DateTime();
        [JSImport("javaScriptTestHelper.identity1")]
        internal static partial bool identity1_DateTime(DateTime value);
        [JSImport("javaScriptTestHelper.throw1")]
        internal static partial DateTime throw1_DateTime(DateTime value);
        [JSImport("javaScriptTestHelper.invoke1")]
        internal static partial DateTime invoke1_DateTime(DateTime value, string name);
        [JSExport("JavaScriptTestHelper.EchoDateTime")]
        public static DateTime EchoDateTime(DateTime arg1)
        {
            return arg1;
        }
        #endregion DateTime

        #region DateTimeOffset
        [JSImport("javaScriptTestHelper.echo1")]
        internal static partial DateTimeOffset echo1_DateTimeOffset(DateTimeOffset value);
        [JSImport("javaScriptTestHelper.store1")]
        internal static partial void store1_DateTimeOffset(DateTimeOffset value);
        [JSImport("javaScriptTestHelper.retrieve1")]
        internal static partial DateTimeOffset retrieve1_DateTimeOffset();
        [JSImport("javaScriptTestHelper.identity1")]
        internal static partial bool identity1_DateTimeOffset(DateTimeOffset value);
        [JSImport("javaScriptTestHelper.throw1")]
        internal static partial DateTimeOffset throw1_DateTimeOffset(DateTimeOffset value);
        [JSImport("javaScriptTestHelper.invoke1")]
        internal static partial DateTimeOffset invoke1_DateTimeOffset(DateTimeOffset value, string name);
        [JSExport("JavaScriptTestHelper.EchoDateTimeOffset")]
        public static DateTimeOffset EchoDateTimeOffset(DateTimeOffset arg1)
        {
            return arg1;
        }
        #endregion DateTimeOffset

        #region NullableBoolean

        [JSImport("javaScriptTestHelper.echo1")]
        internal static partial bool? echo1_NullableBoolean(bool? value);
        [JSImport("javaScriptTestHelper.store1")]
        internal static partial void store1_NullableBoolean(bool? value);
        [JSImport("javaScriptTestHelper.retrieve1")]
        internal static partial bool? retrieve1_NullableBoolean();
        [JSImport("javaScriptTestHelper.identity1")]
        internal static partial bool identity1_NullableBoolean(bool? value);
        [JSImport("javaScriptTestHelper.throw1")]
        internal static partial bool? throw1_NullableBoolean(bool? value);
        [JSImport("javaScriptTestHelper.invoke1")]
        internal static partial bool? invoke1_NullableBoolean(bool? value, string name);
        [JSExport("JavaScriptTestHelper.EchoNullableBoolean")]
        public static bool? EchoNullableBoolean(bool? arg1)
        {
            return arg1;
        }
        #endregion NullableBoolean

        #region NullableInt32

        [JSImport("javaScriptTestHelper.echo1")]
        internal static partial int? echo1_NullableInt32(int? value);
        [JSImport("javaScriptTestHelper.store1")]
        internal static partial void store1_NullableInt32(int? value);
        [JSImport("javaScriptTestHelper.retrieve1")]
        internal static partial int? retrieve1_NullableInt32();
        [JSImport("javaScriptTestHelper.identity1")]
        internal static partial bool identity1_NullableInt32(int? value);
        [JSImport("javaScriptTestHelper.throw1")]
        internal static partial int? throw1_NullableInt32(int? value);
        [JSImport("javaScriptTestHelper.invoke1")]
        internal static partial int? invoke1_NullableInt32(int? value, string name);
        [JSExport("JavaScriptTestHelper.EchoNullableInt32")]
        public static int? EchoNullableInt32(int? arg1)
        {
            return arg1;
        }
        #endregion NullableInt32

        #region NullableIntPtr

        [JSImport("javaScriptTestHelper.echo1")]
        internal static partial IntPtr? echo1_NullableIntPtr(IntPtr? value);
        [JSImport("javaScriptTestHelper.store1")]
        internal static partial void store1_NullableIntPtr(IntPtr? value);
        [JSImport("javaScriptTestHelper.retrieve1")]
        internal static partial IntPtr? retrieve1_NullableIntPtr();
        [JSImport("javaScriptTestHelper.identity1")]
        internal static partial bool identity1_NullableIntPtr(IntPtr? value);
        [JSImport("javaScriptTestHelper.throw1")]
        internal static partial IntPtr? throw1_NullableIntPtr(IntPtr? value);
        [JSImport("javaScriptTestHelper.invoke1")]
        internal static partial IntPtr? invoke1_NullableIntPtr(IntPtr? value, string name);
        [JSExport("JavaScriptTestHelper.EchoNullableIntPtr")]
        public static IntPtr? EchoNullableIntPtr(IntPtr? arg1)
        {
            return arg1;
        }
        #endregion NullableIntPtr

        #region NullableDouble

        [JSImport("javaScriptTestHelper.echo1")]
        internal static partial double? echo1_NullableDouble(double? value);
        [JSImport("javaScriptTestHelper.store1")]
        internal static partial void store1_NullableDouble(double? value);
        [JSImport("javaScriptTestHelper.retrieve1")]
        internal static partial double? retrieve1_NullableDouble();
        [JSImport("javaScriptTestHelper.identity1")]
        internal static partial bool identity1_NullableDouble(double? value);
        [JSImport("javaScriptTestHelper.throw1")]
        internal static partial double? throw1_NullableDouble(double? value);
        [JSImport("javaScriptTestHelper.invoke1")]
        internal static partial double? invoke1_NullableDouble(double? value, string name);
        [JSExport("JavaScriptTestHelper.EchoNullableDouble")]
        public static double? EchoNullableDouble(double? arg1)
        {
            return arg1;
        }
        #endregion NullableDouble

        #region NullableDateTime

        [JSImport("javaScriptTestHelper.echo1")]
        internal static partial DateTime? echo1_NullableDateTime(DateTime? value);
        [JSImport("javaScriptTestHelper.store1")]
        internal static partial void store1_NullableDateTime(DateTime? value);
        [JSImport("javaScriptTestHelper.retrieve1")]
        internal static partial DateTime? retrieve1_NullableDateTime();
        [JSImport("javaScriptTestHelper.identity1")]
        internal static partial bool identity1_NullableDateTime(DateTime? value);
        [JSImport("javaScriptTestHelper.throw1")]
        internal static partial DateTime? throw1_NullableDateTime(DateTime? value);
        [JSImport("javaScriptTestHelper.invoke1")]
        internal static partial DateTime? invoke1_NullableDateTime(DateTime? value, string name);
        [JSExport("JavaScriptTestHelper.EchoNullableDateTime")]
        public static DateTime? EchoNullableDateTime(DateTime? arg1)
        {
            return arg1;
        }
        #endregion NullableDateTime

        #region String
        [JSImport("javaScriptTestHelper.echo1")]
        internal static partial string echo1_String(string value);
        [JSImport("javaScriptTestHelper.store1")]
        internal static partial void store1_String(string value);
        [JSImport("javaScriptTestHelper.retrieve1")]
        internal static partial string retrieve1_String();
        [JSImport("javaScriptTestHelper.identity1")]
        internal static partial bool identity1_String(string value);
        [JSImport("javaScriptTestHelper.throw1")]
        internal static partial string throw1_String(string value);
        [JSImport("javaScriptTestHelper.invoke1")]
        internal static partial string invoke1_String(string value, string name);
        [JSExport("JavaScriptTestHelper.EchoString")]
        public static string EchoString(string arg1)
        {
            return arg1;
        }
        #endregion String

        #region Object
        [JSImport("javaScriptTestHelper.echo1")]
        internal static partial object echo1_Object(object value);
        [JSImport("javaScriptTestHelper.store1")]
        internal static partial void store1_Object(object value);
        [JSImport("javaScriptTestHelper.retrieve1")]
        internal static partial object retrieve1_Object();
        [JSImport("javaScriptTestHelper.identity1")]
        internal static partial bool identity1_Object(object value);
        [JSImport("javaScriptTestHelper.throw1")]
        internal static partial object throw1_Object(object value);
        [JSImport("javaScriptTestHelper.invoke1")]
        internal static partial object invoke1_Object(object value, string name);
        [JSExport("JavaScriptTestHelper.EchoObject")]
        public static object EchoObject(object arg1)
        {
            return arg1;
        }
        #endregion Object

        #region Exception
        [JSImport("javaScriptTestHelper.echo1")]
        internal static partial Exception echo1_Exception(Exception value);
        [JSImport("javaScriptTestHelper.store1")]
        internal static partial void store1_Exception(Exception value);
        [JSImport("javaScriptTestHelper.retrieve1")]
        internal static partial Exception retrieve1_Exception();
        [JSImport("javaScriptTestHelper.identity1")]
        internal static partial bool identity1_Exception(Exception value);
        [JSImport("javaScriptTestHelper.throw1")]
        internal static partial Exception throw1_Exception(Exception value);
        [JSImport("javaScriptTestHelper.invoke1")]
        internal static partial Exception invoke1_Exception(Exception value, string name);
        [JSExport("JavaScriptTestHelper.EchoException")]
        public static Exception EchoException(Exception arg1)
        {
            return arg1;
        }
        #endregion Exception

        #region IOException
        [JSImport("javaScriptTestHelper.echo1")]
        internal static partial IOException echo1_IOException(IOException value);
        [JSImport("javaScriptTestHelper.store1")]
        internal static partial void store1_IOException(IOException value);
        [JSImport("javaScriptTestHelper.retrieve1")]
        internal static partial IOException retrieve1_IOException();
        [JSImport("javaScriptTestHelper.identity1")]
        internal static partial bool identity1_IOException(IOException value);
        [JSImport("javaScriptTestHelper.throw1")]
        internal static partial IOException throw1_IOException(IOException value);
        [JSImport("javaScriptTestHelper.invoke1")]
        internal static partial IOException invoke1_IOException(IOException value, string name);
        [JSExport("JavaScriptTestHelper.EchoIOException")]
        public static IOException EchoIOException(IOException arg1)
        {
            return arg1;
        }
        #endregion IOException

        #region JSException
        [JSImport("javaScriptTestHelper.echo1")]
        internal static partial JSException echo1_JSException(JSException value);
        [JSImport("javaScriptTestHelper.store1")]
        internal static partial void store1_JSException(JSException value);
        [JSImport("javaScriptTestHelper.retrieve1")]
        internal static partial JSException retrieve1_JSException();
        [JSImport("javaScriptTestHelper.identity1")]
        internal static partial bool identity1_JSException(JSException value);
        [JSImport("javaScriptTestHelper.throw1")]
        internal static partial JSException throw1_JSException(JSException value);
        [JSImport("javaScriptTestHelper.invoke1")]
        internal static partial JSException invoke1_JSException(JSException value, string name);
        [JSExport("JavaScriptTestHelper.EchoJSException")]
        public static JSException EchoJSException(JSException arg1)
        {
            return arg1;
        }
        #endregion JSException

        #region JSObject
        [JSImport("javaScriptTestHelper.echo1")]
        internal static partial JSObject echo1_JSObject(JSObject value);
        [JSImport("javaScriptTestHelper.store1")]
        internal static partial void store1_JSObject(JSObject value);
        [JSImport("javaScriptTestHelper.retrieve1")]
        internal static partial JSObject retrieve1_JSObject();
        [JSImport("javaScriptTestHelper.identity1")]
        internal static partial bool identity1_JSObject(JSObject value);
        [JSImport("javaScriptTestHelper.throw1")]
        internal static partial JSObject throw1_JSObject(JSObject value);
        [JSImport("javaScriptTestHelper.invoke1")]
        internal static partial JSObject invoke1_JSObject(JSObject value, string name);
        [JSExport("JavaScriptTestHelper.EchoJSObject")]
        public static JSObject EchoJSObject(JSObject arg1)
        {
            return arg1;
        }
        #endregion JSObject

        #region IJSObject
        [JSImport("javaScriptTestHelper.echo1")]
        internal static partial IJSObject echo1_IJSObject(IJSObject value);
        [JSImport("javaScriptTestHelper.store1")]
        internal static partial void store1_IJSObject(IJSObject value);
        [JSImport("javaScriptTestHelper.retrieve1")]
        internal static partial IJSObject retrieve1_IJSObject();
        [JSImport("javaScriptTestHelper.identity1")]
        internal static partial bool identity1_IJSObject(IJSObject value);
        [JSImport("javaScriptTestHelper.throw1")]
        internal static partial IJSObject throw1_IJSObject(IJSObject value);
        [JSImport("javaScriptTestHelper.invoke1")]
        internal static partial IJSObject invoke1_IJSObject(IJSObject value, string name);
        [JSExport("JavaScriptTestHelper.EchoIJSObject")]
        public static IJSObject EchoIJSObject(IJSObject arg1)
        {
            return arg1;
        }
        #endregion IJSObject

        #region ListOfInt
        [JSImport("javaScriptTestHelper.echo1")]
        internal static partial List<int> echo1_ListOfInt(List<int> value);
        [JSImport("javaScriptTestHelper.store1")]
        internal static partial void store1_ListOfInt(List<int> value);
        [JSImport("javaScriptTestHelper.retrieve1")]
        internal static partial List<int> retrieve1_ListOfInt();
        [JSImport("javaScriptTestHelper.identity1")]
        internal static partial bool identity1_ListOfInt(List<int> value);
        [JSImport("javaScriptTestHelper.throw1")]
        internal static partial List<int> throw1_ListOfInt(List<int> value);
        [JSImport("javaScriptTestHelper.invoke1")]
        internal static partial List<int> invoke1_ListOfInt(List<int> value, string name);
        [JSExport("JavaScriptTestHelper.EchoListOfInt")]
        public static List<int> EchoListOfInt(List<int> arg1)
        {
            return arg1;
        }
        #endregion ListOfInt

        static JSObject _invokeJsTester;
        public static async Task InitializeAsync()
        {
            if (_invokeJsTester == null)
            {
                Function helper = new Function(@"
                    const loadJs = async () => {
                        try{
                            await import('./JavaScriptTestHelper.js');
                            console.log('LOADED JavaScriptTestHelper.js');
                        }
                        catch(ex){
                            console.log('FAILED loading JavaScriptTestHelper.js ' + ex);
                        }
                    };
                    return loadJs();
                ");
                await (Task)helper.Call();
                _invokeJsTester = (JSObject)Runtime.GetGlobalObject("javaScriptTestHelper");
            }
        }
    }
}
