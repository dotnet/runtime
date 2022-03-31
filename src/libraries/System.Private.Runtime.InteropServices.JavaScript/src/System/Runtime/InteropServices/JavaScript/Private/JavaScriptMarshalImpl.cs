// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript.Private
{
    internal static unsafe partial class JavaScriptMarshalImpl
    {
        internal static Function log = new Function("data", @"console.log(data)");
        private static Dictionary<Type, JavaScriptMarshalerBase> marshalers = new Dictionary<Type, JavaScriptMarshalerBase>();
        private static List<JavaScriptMarshalerBase> marshalersSequence = new List<JavaScriptMarshalerBase>();

        static JavaScriptMarshalImpl()
        {
            ExceptionMarshaler exception = new();
            JSObjectMarshaler jsObject = new();
            SystemObjectMarshaler sysObject = new SystemObjectMarshaler();
            StringMarshaler str = new();
            TaskMarshaler task = new();
            DateTimeMarshaler datetime = new();
            DateTimeOffsetMarshaler datetimeOffset = new();

            lock (marshalers)
            {
                marshalers.Add(typeof(string), str);
                marshalers.Add(typeof(DateTime), datetime);
                marshalers.Add(typeof(DateTimeOffset), datetimeOffset);

                marshalersSequence.Add(jsObject);
                marshalers.Add(typeof(JSObject), jsObject);

                marshalersSequence.Add(exception);
                marshalers.Add(typeof(JSException), exception);

                marshalersSequence.Add(task);
                marshalers.Add(typeof(Task), task);

                marshalersSequence.Add(sysObject);
                marshalers.Add(typeof(object), sysObject);
            }
        }

        private static void RegisterMarshaler(JavaScriptMarshalerBase marshaler)
        {
            lock (marshalers)
            {
                if (!marshaler.MarshaledType.IsSealed)
                {
                    marshalersSequence.Insert(0, marshaler);
                }
                marshalers.Add(marshaler.MarshaledType, marshaler);
            }
        }

        internal static void ThrowException(JavaScriptMarshalerArg arg)
        {
            var ex = JavaScriptMarshal.MarshalToManagedException(arg);
            if (ex != null)
            {
                throw ex;
            }
            throw new JSException("unknown exception");
        }
    }
}
