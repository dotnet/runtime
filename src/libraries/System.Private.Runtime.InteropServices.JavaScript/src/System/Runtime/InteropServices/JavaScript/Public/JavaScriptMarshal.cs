// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript.Private;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript
{
    [CLSCompliant(false)]
    public unsafe partial class JavaScriptMarshal
    {
        public static JavaScriptMarshalerSignature BindCSFunction(string fullyQualifiedName, int signatureHash, string exportAsName, JavaScriptMarshalerBase[] customMarshalers, params Type[] types)
        {
            JavaScriptMarshalImpl.RegisterCustomMarshalers(customMarshalers);
            var signature = JavaScriptMarshalImpl.GetMethodSignature(customMarshalers, types);

            var exceptionMessage = JavaScriptMarshalImpl._BindCSFunction(fullyQualifiedName, signatureHash, exportAsName, signature.Header, out int isException);
            if (isException != 0)
            {
                throw new JSException(exceptionMessage);
            }
            return signature;
        }

        public static JavaScriptMarshalerSignature BindJSFunction(string functionName, JavaScriptMarshalerBase[] customMarshalers, params Type[] types)
        {
            JavaScriptMarshalImpl.RegisterCustomMarshalers(customMarshalers);
            var signature = JavaScriptMarshalImpl.GetMethodSignature(customMarshalers, types);

            // TODO wrap with JSObject
            var exceptionMessage = JavaScriptMarshalImpl._BindJSFunction(functionName, signature.Header, out IntPtr jsFunctionHandle, out int isException);
            if (isException != 0)
            {
                throw new JSException(exceptionMessage);
            }
            signature.JSHandle = jsFunctionHandle;

            return signature;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InvokeBoundJSFunction(JavaScriptMarshalerSignature signature, JavaScriptMarshalerArguments data)
        {
            JavaScriptMarshalImpl._InvokeBoundJSFunction(signature.JSHandle, data.Buffer);
            if (data.Exception.TypeHandle != IntPtr.Zero)
            {
                JavaScriptMarshalImpl.ThrowException(data.Exception);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe JavaScriptMarshalerArguments CreateArguments(void* buffer)
        {
            return new JavaScriptMarshalerArguments
            {
                Buffer = buffer
            };
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void InitResult(ref string messageRoot, ref object root, JavaScriptMarshalerArguments args, JavaScriptMarshalerSignature signature)
        {
#if DEBUG
            // check alignment
            Debug.Assert((int)args.Buffer % 8 == 0);
#endif
            var exc = args.Exception;
            exc.TypeHandle = IntPtr.Zero;
            exc.RootRef = (IntPtr)Unsafe.AsPointer(ref messageRoot);

            var res = args.Result;
            var sig = signature.Result;

            res.TypeHandle = sig.TypeHandle;
            if (sig.BufferOffset != -1)
            {
                var buf = (IntPtr)args.Buffer;
                res.ExtraBufferPtr = buf + signature.ArgumentsBufferLength + sig.BufferOffset;
            }
            if (sig.UseRoot != 0)
            {
                res.RootRef = (IntPtr)Unsafe.AsPointer(ref root);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void InitVoid(ref string exceptionMessageRoot, JavaScriptMarshalerArguments args)
        {
#if DEBUG
            // check alignment
            Debug.Assert((int)args.Buffer % 8 == 0);
#endif
            var exc = args.Exception;
            exc.TypeHandle = IntPtr.Zero;
            exc.RootRef = (IntPtr)Unsafe.AsPointer(ref exceptionMessageRoot);

            var res = args.Result;
            res.TypeHandle = IntPtr.Zero;
            res.RootRef = IntPtr.Zero;
            res.ExtraBufferPtr = IntPtr.Zero;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void InitArgument<T>(int position, ref T value, JavaScriptMarshalerArguments args, JavaScriptMarshalerSignature signature)
        {
#if DEBUG
            // check alignment
            Debug.Assert((int)args.Buffer % 8 == 0);
#endif
            var arg = args[position];
            var sig = signature[position];

            arg.TypeHandle = sig.TypeHandle;
            if (sig.BufferOffset != -1)
            {
                var buf = (IntPtr)args.Buffer;
                arg.ExtraBufferPtr = buf + signature.ArgumentsBufferLength + sig.BufferOffset;
            }
            if (sig.UseRoot != 0)
            {
                arg.RootRef = (IntPtr)Unsafe.AsPointer(ref value);
            }
        }

        public static void SetRootRef(ref string value, JavaScriptMarshalerArg arg)
        {
            var valuePtrPtr = (IntPtr*)Unsafe.AsPointer(ref value);
            var rootPtr = (IntPtr*)arg.RootRef.ToPointer();
            rootPtr[0] = valuePtrPtr[0];
        }
    }
}
