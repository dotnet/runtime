// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.JavaScript.Private;
using System.Threading.Tasks;
using Xunit;

namespace System.Runtime.InteropServices.JavaScript.Tests
{

    // TODO js Arrays
    // TODO cs Arrays
    // TODO cs Span
    // TODO cs IntPtr
    // TODO cs void*
    // TODO cs char

    public class JSImportExportTest : IAsyncLifetime
    {
        [Fact]
        public unsafe void StructSize()
        {
            Assert.Equal(20, sizeof(JSMarshalerSig));
            Assert.Equal(16, sizeof(JSMarshalerArg));
            Assert.Equal(4, sizeof(JavaScriptMarshalerArg));
            Assert.Equal(4, sizeof(JavaScriptMarshalerArguments));
        }

        #region Boolean
        public static IEnumerable<object[]> MarshalBooleanCases()
        {
            yield return new object[] { true };
            yield return new object[] { false };
        }

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

        [Theory]
        [MemberData(nameof(MarshalBooleanCases))]
        public void JsExportBoolean(bool value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_Boolean,
                nameof(JavaScriptTestHelper.EchoBoolean),
                "boolean");
        }
        #endregion Boolean

        #region Byte
        public static IEnumerable<object[]> MarshalByteCases()
        {
            yield return new object[] { (byte)42 };
            yield return new object[] { (byte)1 };
            yield return new object[] { byte.MaxValue };
            yield return new object[] { byte.MinValue };
        }

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
        [MemberData(nameof(MarshalByteCases))]
        public void JsExportByte(byte value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_Byte,
                nameof(JavaScriptTestHelper.EchoByte),
                "number");
        }
        #endregion Byte

        #region Int16
        public static IEnumerable<object[]> MarshalInt16Cases()
        {
            yield return new object[] { 42 };
            yield return new object[] { 0 };
            yield return new object[] { 1 };
            yield return new object[] { -1 };
            yield return new object[] { short.MaxValue };
            yield return new object[] { short.MinValue };
        }

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

        [Theory]
        [MemberData(nameof(MarshalInt16Cases))]
        public void JsExportInt16(short value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_Int16,
                nameof(JavaScriptTestHelper.EchoInt16),
                "number");
        }
        #endregion Int16

        #region Int32
        public static IEnumerable<object[]> MarshalInt32Cases()
        {
            yield return new object[] { 42 };
            yield return new object[] { 0 };
            yield return new object[] { 1 };
            yield return new object[] { -1 };
            yield return new object[] { int.MaxValue };
            yield return new object[] { int.MinValue };
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
        [MemberData(nameof(MarshalInt32Cases))]
        public void JsExportInt32(int value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_Int32,
                nameof(JavaScriptTestHelper.EchoInt32),
                "number");
        }
        #endregion Int32

        #region Int64
        public static IEnumerable<object[]> MarshalInt64Cases()
        {
            yield return new object[] { -1 };
            yield return new object[] { 42 };
            yield return new object[] { 0 };
            yield return new object[] { 1 };
            yield return new object[] { 9007199254740991 };  // Number.MAX_SAFE_INTEGER
            yield return new object[] { -9007199254740991 }; // Number.MIN_SAFE_INTEGER
        }

        [Theory]
        [MemberData(nameof(MarshalInt64Cases))]
        public void JsImportInt64(long value)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_Int64,
                JavaScriptTestHelper.retrieve1_Int64,
                JavaScriptTestHelper.echo1_Int64,
                JavaScriptTestHelper.throw1_Int64,
                JavaScriptTestHelper.identity1_Int64,
                "number");
        }

        [Theory]
        [MemberData(nameof(MarshalInt64Cases))]
        public void JsExportInt64(long value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_Int64,
                nameof(JavaScriptTestHelper.EchoInt64),
                "number");
        }
        #endregion Int64

        #region Double
        public static IEnumerable<object[]> MarshalDoubleCases()
        {
            yield return new object[] { Math.PI };
            yield return new object[] { 0.0 };
            yield return new object[] { double.MaxValue };
            yield return new object[] { double.MinValue };
            yield return new object[] { double.NegativeInfinity };
            yield return new object[] { double.PositiveInfinity };
            yield return new object[] { double.NaN };
        }

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

        [Theory]
        [MemberData(nameof(MarshalDoubleCases))]
        public void JsExportDouble(double value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_Double,
                nameof(JavaScriptTestHelper.EchoDouble),
                "number");
        }
        #endregion Double

        #region Single
        public static IEnumerable<object[]> MarshalSingleCases()
        {
            yield return new object[] { (float)Math.PI };
            yield return new object[] { 0.0f };
            yield return new object[] { float.MaxValue };
            yield return new object[] { float.MinValue };
            yield return new object[] { float.NegativeInfinity };
            yield return new object[] { float.PositiveInfinity };
            yield return new object[] { float.NaN };
        }

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

        [Theory]
        [MemberData(nameof(MarshalSingleCases))]
        public void JsExportSingle(float value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_Single,
                nameof(JavaScriptTestHelper.EchoSingle),
                "number");
        }
        #endregion Single

        #region IntPtr
        public static IEnumerable<object[]> MarshalIntPtrCases()
        {
            yield return new object[] { (IntPtr)42 };
            yield return new object[] { IntPtr.Zero };
            yield return new object[] { (IntPtr)1 };
            yield return new object[] { (IntPtr)(-1) };
            yield return new object[] { IntPtr.MaxValue };
            yield return new object[] { IntPtr.MinValue };
        }

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

        [Theory]
        [MemberData(nameof(MarshalIntPtrCases))]
        public void JsExportIntPtr(IntPtr value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_IntPtr,
                nameof(JavaScriptTestHelper.EchoIntPtr),
                "number");
        }
        #endregion IntPtr

        #region Datetime
        public static IEnumerable<object[]> MarshalDateTimeCases()
        {
            yield return new object[] { new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
            yield return new object[] { TrimNano(DateTime.UtcNow) };
            yield return new object[] { TrimNano(DateTime.MaxValue) };
        }

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

        [Theory]
        [MemberData(nameof(MarshalDateTimeCases))]
        public void JsExportDateTime(DateTime value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_DateTime,
                nameof(JavaScriptTestHelper.EchoDateTime),
                "object", "Date");
        }
        #endregion Datetime

        #region DateTimeOffset
        public static IEnumerable<object[]> MarshalDateTimeOffsetCases()
        {
            yield return new object[] { DateTimeOffset.FromUnixTimeSeconds(0) };
            yield return new object[] { TrimNano(DateTimeOffset.UtcNow) };
            yield return new object[] { TrimNano(DateTimeOffset.MaxValue) };
        }

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

        [Theory]
        [MemberData(nameof(MarshalDateTimeOffsetCases))]
        public void JsExportDateTimeOffset(DateTimeOffset value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_DateTimeOffset,
                nameof(JavaScriptTestHelper.EchoDateTimeOffset),
                "object", "Date");
        }
        #endregion DateTimeOffset

        #region NullableBoolean
        public static IEnumerable<object[]> MarshalNullableBooleanCases()
        {
            yield return new object[] { null };
            yield return new object[] { true };
            yield return new object[] { false };
        }

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

        [Theory]
        [MemberData(nameof(MarshalNullableBooleanCases))]
        public void JsExportNullableBoolean(bool? value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_NullableBoolean,
                nameof(JavaScriptTestHelper.EchoNullableBoolean),
                "boolean");
        }
        #endregion NullableBoolean

        #region NullableInt32
        public static IEnumerable<object[]> MarshalNullableInt32Cases()
        {
            yield return new object[] { null };
            yield return new object[] { 42 };
            yield return new object[] { 0 };
            yield return new object[] { 1 };
            yield return new object[] { -1 };
            yield return new object[] { int.MaxValue };
            yield return new object[] { int.MinValue };
        }

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

        [Theory]
        [MemberData(nameof(MarshalNullableInt32Cases))]
        public void JsExportNullableInt32(int? value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_NullableInt32,
                nameof(JavaScriptTestHelper.EchoNullableInt32),
                "number");
        }
        #endregion NullableInt32

        #region NullableIntPtr
        public static IEnumerable<object[]> MarshalNullableIntPtrCases()
        {
            yield return new object[] { null };
            yield return new object[] { (IntPtr)42 };
            yield return new object[] { IntPtr.Zero };
            yield return new object[] { (IntPtr)1 };
            yield return new object[] { (IntPtr)(-1) };
            yield return new object[] { IntPtr.MaxValue };
            yield return new object[] { IntPtr.MinValue };
        }

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

        [Theory]
        [MemberData(nameof(MarshalNullableIntPtrCases))]
        public void JsExportNullableIntPtr(IntPtr? value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_NullableIntPtr,
                nameof(JavaScriptTestHelper.EchoNullableIntPtr),
                "number");
        }
        #endregion NullableIntPtr

        #region NullableDouble
        public static IEnumerable<object[]> MarshalNullableDoubleCases()
        {
            yield return new object[] { null };
            yield return new object[] { Math.PI };
            yield return new object[] { 0.0 };
            yield return new object[] { double.MaxValue };
            yield return new object[] { double.MinValue };
            yield return new object[] { double.NegativeInfinity };
            yield return new object[] { double.PositiveInfinity };
            yield return new object[] { double.NaN };
        }

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

        [Theory]
        [MemberData(nameof(MarshalNullableDoubleCases))]
        public void JsExportNullableDouble(double? value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_NullableDouble,
                nameof(JavaScriptTestHelper.EchoNullableDouble),
                "number");
        }
        #endregion NullableDouble

        #region NullableDateTime
        public static IEnumerable<object[]> MarshalNullableDateTimeCases()
        {
            yield return new object[] { null };
            yield return new object[] { new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
            yield return new object[] { TrimNano(DateTime.UtcNow) };
            yield return new object[] { TrimNano(DateTime.MaxValue) };
        }

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

        [Theory]
        [MemberData(nameof(MarshalNullableDateTimeCases))]
        public void JsExportNullableDateTime(DateTime? value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_NullableDateTime,
                nameof(JavaScriptTestHelper.EchoNullableDateTime),
                "object");
        }
        #endregion NullableDateTime

        #region String
        public static IEnumerable<object[]> MarshalStringCases()
        {
            yield return new object[] { "Hello-" + Random.Shared.Next() + "-JS" };
            //yield return new object[] { string.Intern("Hello JS Interned!") };
            //yield return new object[] { (string)null };
        }

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

        [Theory]
        [MemberData(nameof(MarshalStringCases))]
        public void JsExportString(string value)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_String,
                nameof(JavaScriptTestHelper.EchoString),
                "string");
        }
        #endregion String

        #region Object
        public static IEnumerable<object[]> MarshalObjectCases()
        {
            yield return new object[] { new object(), "ManagedObject" };
            yield return new object[] { null, null };
        }

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

        [Theory]
        [MemberData(nameof(MarshalObjectCases))]
        public void JsExportObject(object value, string clazz)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_Object,
                nameof(JavaScriptTestHelper.EchoObject),
                "object", clazz);
        }
        #endregion Object

        #region List
        public static IEnumerable<object[]> MarshalListCases()
        {
            yield return new object[] { new List<int>(), "ManagedObject" };
            yield return new object[] { null, null };
        }

        [Theory]
        [MemberData(nameof(MarshalListCases))]
        public void JSImportList(List<int> value, string clazz)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_ListOfInt,
                JavaScriptTestHelper.retrieve1_ListOfInt,
                JavaScriptTestHelper.echo1_ListOfInt,
                JavaScriptTestHelper.throw1_ListOfInt,
                JavaScriptTestHelper.identity1_ListOfInt,
                "object", clazz);
        }

        [Theory]
        [MemberData(nameof(MarshalListCases))]
        public void JsExportList(List<int> value, string clazz)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_ListOfInt,
                nameof(JavaScriptTestHelper.EchoListOfInt),
                "object", clazz);
        }
        #endregion List

        #region Exception
        public static IEnumerable<object[]> MarshalExceptionCases()
        {
            yield return new object[] { new Exception("Test"), "ManagedError" };
            yield return new object[] { null, "JSTestError" };
            yield return new object[] { null, null };
        }

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
        #endregion Exception

        #region JSException
        public static IEnumerable<object[]> MarshalJSExceptionCases()
        {
            yield return new object[] { new JSException("Test"), "ManagedError" };// marshaled as ManagedError because it doesn't have the js_handle
            yield return new object[] { null, "JSTestError" };
            yield return new object[] { null, null };
        }

        [Theory]
        [MemberData(nameof(MarshalJSExceptionCases))]
        public void JSImportJSException(JSException value, string clazz)
        {
            if (clazz == "JSTestError")
            {
                value = JavaScriptTestHelper.createException("!CreateEx!");
            }
            JsImportTest(value,
                JavaScriptTestHelper.store1_JSException,
                JavaScriptTestHelper.retrieve1_JSException,
                JavaScriptTestHelper.echo1_JSException,
                JavaScriptTestHelper.throw1_JSException,
                JavaScriptTestHelper.identity1_JSException,
                "object", clazz);
        }

        [Theory]
        [MemberData(nameof(MarshalJSExceptionCases))]
        public void JsExportJSException(JSException value, string clazz)
        {
            if (clazz == "JSTestError")
            {
                value = JavaScriptTestHelper.createException("!CreateEx!");
            }

            JsExportTest(value,
                JavaScriptTestHelper.invoke1_JSException,
                nameof(JavaScriptTestHelper.EchoJSException),
                "object", clazz);
        }
        #endregion JSException

        #region IOException
        public static IEnumerable<object[]> MarshalIOExceptionCases()
        {
            yield return new object[] { new IOException("Test"), "ManagedError" };
            yield return new object[] { null, null };
        }

        [Theory]
        [MemberData(nameof(MarshalIOExceptionCases))]
        public void JSImportIOException(IOException value, string clazz)
        {
            JsImportTest(value,
                JavaScriptTestHelper.store1_IOException,
                JavaScriptTestHelper.retrieve1_IOException,
                JavaScriptTestHelper.echo1_IOException,
                JavaScriptTestHelper.throw1_IOException,
                JavaScriptTestHelper.identity1_IOException,
                "object", clazz);
        }

        [Theory]
        [MemberData(nameof(MarshalIOExceptionCases))]
        public void JsExportIOException(IOException value, string clazz)
        {
            JsExportTest(value,
                JavaScriptTestHelper.invoke1_IOException,
                nameof(JavaScriptTestHelper.EchoIOException),
                "object", clazz);
        }
        #endregion IOException

        #region IJSObject
        public static IEnumerable<object[]> MarshalIJSObjectCases()
        {
            yield return new object[] { null, "JSData" };
            yield return new object[] { null, null };
        }

        [Theory]
        [MemberData(nameof(MarshalIJSObjectCases))]
        public void JSImportIJSObject(IJSObject value, string clazz)
        {
            if (clazz == "JSData")
            {
                value = JavaScriptTestHelper.createData("!CreateJS!");
            }

            JsImportTest(value,
                JavaScriptTestHelper.store1_IJSObject,
                JavaScriptTestHelper.retrieve1_IJSObject,
                JavaScriptTestHelper.echo1_IJSObject,
                JavaScriptTestHelper.throw1_IJSObject,
                JavaScriptTestHelper.identity1_IJSObject,
                "object", clazz);
        }

        [Theory]
        [MemberData(nameof(MarshalIJSObjectCases))]
        public void JsExportIJSObject(IJSObject value, string clazz)
        {
            if (clazz == "JSData")
            {
                value = JavaScriptTestHelper.createData("!CreateJS!");
            }

            JsExportTest(value,
                JavaScriptTestHelper.invoke1_IJSObject,
                nameof(JavaScriptTestHelper.EchoIJSObject),
                "object", clazz);
        }
        #endregion IJSObject

        #region JSObject
        [Theory]
        [MemberData(nameof(MarshalIJSObjectCases))]
        public void JSImportJSObject(JSObject value, string clazz)
        {
            if (clazz == "JSData")
            {
                value = (JSObject)JavaScriptTestHelper.createData("!CreateJS!");
            }

            JsImportTest(value,
                JavaScriptTestHelper.store1_JSObject,
                JavaScriptTestHelper.retrieve1_JSObject,
                JavaScriptTestHelper.echo1_JSObject,
                JavaScriptTestHelper.throw1_JSObject,
                JavaScriptTestHelper.identity1_JSObject,
                "object", clazz);
        }

        [Theory]
        [MemberData(nameof(MarshalIJSObjectCases))]
        public void JsExportJSObject(JSObject value, string clazz)
        {
            if (clazz == "JSData")
            {
                value = (JSObject)JavaScriptTestHelper.createData("!CreateJS!");
            }

            JsExportTest(value,
                JavaScriptTestHelper.invoke1_JSObject,
                nameof(JavaScriptTestHelper.EchoJSObject),
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
            Assert.Equal("test", ((Task<object>)task).Result);
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

        public static IEnumerable<object[]> TaskCases()
        {
            yield return new object[] { Math.PI };
            yield return new object[] { 0 };
            yield return new object[] { "test" };
            yield return new object[] { null };
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
        public async Task JsImportTaskAwait()
        {
            var task = JavaScriptTestHelper.awaitvoid(Task.CompletedTask);
            await Task.Delay(100);
            Assert.True(task.IsCompleted);
            await task;
        }

        /*[Theory]
        [MemberData(nameof(MarshalInt32Cases))]
        public async Task JsExportTaskOfInt(int value)
        {
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();

            var res = JavaScriptTestHelper.invoke1_TaskOfObject(tcs.Task, nameof(JavaScriptTestHelper.AwaitTaskOfObject));
            JavaScriptTestHelper.Log("JsExportTaskOfInt A");
            tcs.SetResult(value);
            JavaScriptTestHelper.Log("JsExportTaskOfInt B"+ res.IsCompleted);
            await Task.Yield();
            var rr = await res;
            await Task.Yield();
            JavaScriptTestHelper.Log("JsExportTaskOfInt C");
            Assert.Equal(value, rr);
        }*/

        #endregion

        private void JsExportTest<T>(T value
            , Func<T, string, T> invoke, string echoName, string jsType, string? jsClass = null)
        {
            T res;
            res = invoke(value, echoName);
            Assert.Equal(value, res);
        }

        private void JsImportTest<T>(T value
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
            Assert.Contains(" at throw0", exThrow0.StackTrace);

            var exThrow1 = Assert.Throws<JSException>(() => throw1(value));
            Assert.Contains("throw1-msg", exThrow1.Message);
            Assert.DoesNotContain(" at ", exThrow1.Message);
            Assert.Contains(" at throw1", exThrow1.StackTrace);

            // anything is a system.object, sometimes it would be JSObject wrapper
            if (typeof(T).IsPrimitive)
            {
                var resBoxed = JavaScriptTestHelper.echo1_Object(value);
                // js Number always boxes as double
                if(typeof(T) == typeof(IntPtr))
                {
                    Assert.Equal((IntPtr)(object)value, (IntPtr)(int)(double)resBoxed);
                }
                else if (typeof(T) == typeof(bool))
                {
                    Assert.Equal((bool)(object)value, (bool)resBoxed);
                }
                else
                {
                    Assert.Equal(Convert.ToDouble(value), resBoxed);
                }

                //TODO var task = JavaScriptTestHelper.await1(Task.FromResult((object)value));
            }
            else if (typeof(T)==typeof(DateTime))
            {
                var resBoxed = JavaScriptTestHelper.echo1_Object(value);
                Assert.Equal(value, resBoxed);
            }
            else if (typeof(T)==typeof(DateTimeOffset))
            {
                var resBoxed = JavaScriptTestHelper.echo1_Object(value);
                Assert.Equal(((DateTimeOffset)(object)value).UtcDateTime, resBoxed);
            }
            else if (Nullable.GetUnderlyingType(typeof(T)) != null)
            {
                var resBoxed = JavaScriptTestHelper.echo1_Object(value);
                if (resBoxed != null)
                {
                    var vt = Nullable.GetUnderlyingType(typeof(T));
                    if (vt == typeof(bool))
                    {
                        Assert.Equal(((bool?)(object)value).Value, (bool)resBoxed);
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
                        Assert.Equal((double)((IntPtr?)(object)value).Value, resBoxed);
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

        public async Task InitializeAsync()
        {
            await JavaScriptTestHelper.InitializeAsync();
        }

        public Task DisposeAsync() => Task.CompletedTask;

        // js Date doesn't have nanosecond precision
        public static DateTime TrimNano(DateTime date)
        {
            return new DateTime(date.Ticks - (date.Ticks % TimeSpan.TicksPerMillisecond), DateTimeKind.Utc);
        }

        public static DateTimeOffset TrimNano(DateTimeOffset date)
        {
            return new DateTime(date.Ticks - (date.Ticks % TimeSpan.TicksPerMillisecond), DateTimeKind.Utc);
        }
    }
}
