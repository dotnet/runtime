// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable


using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.JavaScript.Private
{
    internal sealed class DateTimeMarshaler : JavaScriptMarshalerBase<DateTime>
    {
        protected override string JavaScriptCode => null;
        protected override MarshalToManagedDelegate<DateTime> ToManaged => JavaScriptMarshal.MarshalToManagedDateTime;
        protected override MarshalToJavaScriptDelegate<DateTime> ToJavaScript => JavaScriptMarshal.MarshalDateTimeToJs;
    }

    internal sealed class DateTimeOffsetMarshaler : JavaScriptMarshalerBase<DateTimeOffset>
    {
        protected override string JavaScriptCode => null;
        protected override MarshalToManagedDelegate<DateTimeOffset> ToManaged => JavaScriptMarshal.MarshalToManagedDateTimeOffset;
        protected override MarshalToJavaScriptDelegate<DateTimeOffset> ToJavaScript => JavaScriptMarshal.MarshalDateTimeOffsetToJs;
    }
}

namespace System.Runtime.InteropServices.JavaScript
{
    public partial class JavaScriptMarshal
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe DateTime MarshalToManagedDateTime(JavaScriptMarshalerArg arg)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(arg.Int64Value).UtcDateTime;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MarshalDateTimeToJs(ref DateTime value, JavaScriptMarshalerArg arg)
        {
            // ToUnixTimeMilliseconds is always UTC
            arg.Int64Value = new DateTimeOffset(value).ToUnixTimeMilliseconds();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe DateTimeOffset MarshalToManagedDateTimeOffset(JavaScriptMarshalerArg arg)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(arg.Int64Value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MarshalDateTimeOffsetToJs(ref DateTimeOffset value, JavaScriptMarshalerArg arg)
        {
            // ToUnixTimeMilliseconds is always UTC
            arg.Int64Value = value.ToUnixTimeMilliseconds();
        }
    }
}
