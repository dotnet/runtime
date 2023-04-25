// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Reflection
{
    internal partial class MethodInvoker
    {
        public MethodInvoker(MethodBase method)
        {
            _method = method;

            if (LocalAppContextSwitches.ForceInterpretedInvoke && !LocalAppContextSwitches.ForceEmitInvoke)
            {
                // Always use the native invoke; useful for testing.
                _strategyDetermined = true;
            }
            else if (LocalAppContextSwitches.ForceEmitInvoke && !LocalAppContextSwitches.ForceInterpretedInvoke)
            {
                // Always use emit invoke (if IsDynamicCodeSupported == true); useful for testing.
                _invoked = true;
            }
        }

        private unsafe object? InterpretedInvoke(object? obj, IntPtr *args)
        {
            Exception? exc;

            object? o = ((RuntimeMethodInfo)_method).InternalInvoke(obj, args, out exc);

            if (exc != null)
                throw exc;

            return o;
        }
    }
}
