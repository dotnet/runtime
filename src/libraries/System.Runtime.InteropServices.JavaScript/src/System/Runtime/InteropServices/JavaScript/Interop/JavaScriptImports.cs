// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Runtime.InteropServices.JavaScript
{
    internal static unsafe partial class JavaScriptImports
    {
#if FEATURE_WASM_THREADS
        private const int messageSize = 4 * 16;

        public static void ResolveOrRejectPromise(Span<JSMarshalerArgument> arguments, JSSynchronizationContext synchronizationContext)
        {
            // TODO: optimize to zero copy for the sync dispatch on the same thread ? (only on JSWebWorker)
            var message = Marshal.AllocHGlobal(messageSize);
            fixed (JSMarshalerArgument* ptr = arguments)
            {
                Unsafe.CopyBlock((void*)message, ptr, messageSize);
            }

            if (synchronizationContext._IsMainThread)
            {
                Interop.Runtime.ResolveOrRejectPromiseUI(message);
            }
            else
            {
                synchronizationContext.Post(static (message) =>
                {
                    IntPtr m = (IntPtr)message!;
                    Interop.Runtime.ResolveOrRejectPromiseCurrent(m);
                }, message);
            }
        }
#else
        public static void ResolveOrRejectPromise(Span<JSMarshalerArgument> arguments)
        {
            fixed (JSMarshalerArgument* ptr = arguments)
            {
                Interop.Runtime.ResolveOrRejectPromise(ptr);
                ref JSMarshalerArgument exceptionArg = ref arguments[0];
                if (exceptionArg.slot.Type != MarshalerType.None)
                {
                    JSHostImplementation.ThrowException(ref exceptionArg);
                }
            }
        }
#endif

#if !DISABLE_LEGACY_JS_INTEROP
        #region legacy

        public static object GetGlobalObject(string? str = null)
        {
            int exception;
            Interop.Runtime.GetGlobalObjectRef(str, out exception, out object jsObj);

            if (exception != 0)
                throw new JSException(SR.Format(SR.ErrorResolvingFromGlobalThis, str));

            LegacyHostImplementation.ReleaseInFlight(jsObj);
            return jsObj;
        }

        public static IntPtr CreateCSOwnedObject(string typeName, object[] parms)
        {
            Interop.Runtime.CreateCSOwnedObjectRef(typeName, parms, out int exception, out object res);
            if (exception != 0)
                throw new JSException((string)res);

            return (IntPtr)(int)res;
        }

        #endregion
#endif
    }
}
