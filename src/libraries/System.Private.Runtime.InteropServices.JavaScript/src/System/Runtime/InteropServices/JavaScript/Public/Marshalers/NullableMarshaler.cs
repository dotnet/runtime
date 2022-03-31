// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable


using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript.Private;

namespace System.Runtime.InteropServices.JavaScript
{
    public sealed class NullableMarshaler<T> : JavaScriptMarshalerBase<T?>
        where T : struct
    {
        protected override MarshalToManagedDelegate<T?> ToManaged => MarshalToManaged;
        protected override MarshalToJavaScriptDelegate<T?> ToJavaScript => MarshalToJs;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe T? MarshalToManaged(JavaScriptMarshalerArg arg)
        {
            if (arg.TypeHandle == IntPtr.Zero)
            {
                return null;
            }
            else if (arg.TypeHandle == typeof(DateTime?).TypeHandle.Value)
            {
                return (T)(object)DateTimeOffset.FromUnixTimeMilliseconds(arg.Int64Value).UtcDateTime;
            }
            else if (arg.TypeHandle == typeof(DateTimeOffset?).TypeHandle.Value)
            {
                return (T)(object)DateTimeOffset.FromUnixTimeMilliseconds(arg.Int64Value);
            }
            long v = arg.Int64Value;
            return Unsafe.As<long, T>(ref v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MarshalToJs(ref T? value, JavaScriptMarshalerArg arg)
        {
            if (!value.HasValue)
            {
                arg.TypeHandle = IntPtr.Zero;
            }
            else if (typeof(T) == typeof(DateTime))
            {
                arg.TypeHandle = JavaScriptMarshalImpl.dateTimeType;
                arg.Int64Value = new DateTimeOffset(((DateTime?)(object)value).Value).ToUnixTimeMilliseconds();
            }
            else if (typeof(T) == typeof(DateTimeOffset))
            {
                arg.TypeHandle = JavaScriptMarshalImpl.dateTimeOffsetType;
                arg.Int64Value = ((DateTimeOffset?)(object)value).Value.ToUnixTimeMilliseconds();
            }
            else
            {
                var v = value.Value;
                arg.Int64Value = Unsafe.As<T, long>(ref v);
                arg.TypeHandle = typeof(T?).TypeHandle.Value;
            }
        }

        protected override string JavaScriptCode
        {
            get
            {
                Type type = typeof(T);
                if (type == typeof(bool))
                {
                    return tmp.Replace("XXX", "b8");
                }
                if (type == typeof(byte))
                {
                    return tmp.Replace("XXX", "b32");
                }
                if (type == typeof(short))
                {
                    return tmp.Replace("XXX", "i16");
                }
                if (type == typeof(IntPtr) || type == typeof(int))
                {
                    return tmp.Replace("XXX", "i32");
                }
                if (type == typeof(long))
                {
                    return tmp.Replace("XXX", "i64");
                }
                if (type == typeof(float))
                {
                    return tmp.Replace("XXX", "f32");
                }
                if (type == typeof(double))
                {
                    return tmp.Replace("XXX", "f64");
                }
                if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
                {
                    return tmp.Replace("XXX", "date");
                }
                throw new NotImplementedException();
            }
        }

        private const string tmp = @"function createNullableMarshaler() {
        return {
            toManaged: (arg, value) => {
                if (value === null || value === undefined) {
                    set_arg_type(arg, 0);
                }
                else {
                    set_arg_type(arg, mono_type);
                    set_arg_XXX(arg, value);
                }
            },
            toJavaScript: (arg) => {
                const type = get_arg_type(arg);
                if (!type) {
                    return null;
                }
                return get_arg_XXX(arg);
            }
        }}";
    }
}
