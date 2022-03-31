// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable


using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript.Private;

namespace System.Runtime.InteropServices.JavaScript.Private
{
    internal sealed class SystemObjectMarshaler : JavaScriptMarshalerBase<object>
    {
        protected override string JavaScriptCode => null;
        protected override bool UseRoot => true;// the actual marshaled type could be Exception or MonoString, which needs root
        protected override MarshalToManagedDelegate<object> ToManaged => JavaScriptMarshal.MarshalToManagedObject;
        protected override MarshalToJavaScriptDelegate<object> ToJavaScript => JavaScriptMarshal.MarshalObjectToJs;
    }
}

namespace System.Runtime.InteropServices.JavaScript
{
    public partial class JavaScriptMarshal
    {
        // TODO does this need to handle custom marshalers ?
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe object MarshalToManagedObject(JavaScriptMarshalerArg arg)
        {
            if (arg.TypeHandle == IntPtr.Zero)
            {
                return null;
            }
            if (arg.TypeHandle == JavaScriptMarshalImpl.objectType)
            {
                return ((GCHandle)arg.GCHandle).Target;
            }
            if (arg.TypeHandle == JavaScriptMarshalImpl.int64Type)
            {
                return MarshalToManagedInt64(arg);
            }
            if (arg.TypeHandle == JavaScriptMarshalImpl.int32Type)
            {
                return MarshalToManagedInt32(arg);
            }
            if (arg.TypeHandle == JavaScriptMarshalImpl.int16Type)
            {
                return MarshalToManagedInt16(arg);
            }
            if (arg.TypeHandle == JavaScriptMarshalImpl.byteType)
            {
                return MarshalToManagedByte(arg);
            }
            if (arg.TypeHandle == JavaScriptMarshalImpl.boolType)
            {
                return MarshalToManagedBoolean(arg);
            }
            if (arg.TypeHandle == JavaScriptMarshalImpl.doubleType)
            {
                return MarshalToManagedDouble(arg);
            }
            if (arg.TypeHandle == JavaScriptMarshalImpl.floatType)
            {
                return MarshalToManagedSingle(arg);
            }
            if (arg.TypeHandle == JavaScriptMarshalImpl.intptrType)
            {
                return MarshalToManagedIntPtr(arg);
            }
            if (arg.TypeHandle == JavaScriptMarshalImpl.ijsObjectType)
            {
                return MarshalToManagedIJSObject(arg);
            }
            if (arg.TypeHandle == JavaScriptMarshalImpl.stringType)
            {
                return MarshalToManagedString(arg);
            }
            if (arg.TypeHandle == JavaScriptMarshalImpl.exceptionType)
            {
                return MarshalToManagedException(arg);
            }
            if (arg.TypeHandle == JavaScriptMarshalImpl.exceptionType)
            {
                return MarshalToManagedException(arg);
            }
            if (arg.TypeHandle == JavaScriptMarshalImpl.dateTimeType)
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(arg.Int64Value).UtcDateTime;
            }
            if (arg.TypeHandle == JavaScriptMarshalImpl.dateTimeOffsetType)
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(arg.Int64Value);
            }

            throw new NotImplementedException("MarshalToManagedObject: " + arg.TypeHandle);
        }

        // TODO does this need to handle custom marshalers ?
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void MarshalObjectToJs(ref object value, JavaScriptMarshalerArg arg)
        {
            if (value == null)
            {
                arg.TypeHandle = IntPtr.Zero;
                return;
            }

            Type type = value.GetType();
            if (type.IsPrimitive)
            {
                arg.TypeHandle = type.TypeHandle.Value;
                if (typeof(long) == type)
                {
                    arg.Int64Value = (long)value;
                }
                else if (typeof(int) == type)
                {
                    arg.Int32Value = (int)value;
                }
                else if (typeof(short) == type)
                {
                    arg.Int16Value = (short)value;
                }
                else if (typeof(byte) == type)
                {
                    arg.ByteValue = (byte)value;
                }
                else if (typeof(bool) == type)
                {
                    arg.BooleanValue = (bool)value;
                }
                else if (typeof(double) == type)
                {
                    arg.DoubleValue = (double)value;
                }
                else if (typeof(float) == type)
                {
                    arg.SingleValue = (float)value;
                }
                else if (typeof(IntPtr) == type)
                {
                    arg.IntPtrValue = (IntPtr)value;
                }
                else
                {
                    throw new NotImplementedException(type.FullName);
                }
            }
            else if (typeof(DateTimeOffset) == type)
            {
                arg.TypeHandle = JavaScriptMarshalImpl.dateTimeOffsetType;
                arg.Int64Value = ((DateTimeOffset)value).ToUnixTimeMilliseconds();
            }
            else if (typeof(DateTime) == type)
            {
                arg.TypeHandle = JavaScriptMarshalImpl.dateTimeType;
                arg.Int64Value = new DateTimeOffset((DateTime)value).ToUnixTimeMilliseconds();
            }
            else if (typeof(JSObject).IsAssignableFrom(type))
            {
                arg.TypeHandle = JavaScriptMarshalImpl.ijsObjectType;
                arg.JSHandle = (IntPtr)((JSObject)value).JSHandle;
            }
            else if (typeof(Exception).IsAssignableFrom(type))
            {
                var ex = (Exception)value;
                MarshalExceptionToJs(ref ex, arg);
            }
            else if (!type.IsValueType)
            {
                arg.TypeHandle = JavaScriptMarshalImpl.objectType;
                arg.GCHandle = (IntPtr)Runtime.GetJSOwnedObjectGCHandle(value);
            }
            else if (Nullable.GetUnderlyingType(type) != null)
            {
                // TODO auto custom marshaler ?
                throw new NotImplementedException(type.FullName);
            }
            else
            {
                //TODO P/Invoke like marshaling of struct
                throw new NotImplementedException(type.FullName);
            }
        }
    }
}
