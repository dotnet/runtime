// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Reflection
{
    internal partial class ConstructorInvoker
    {
        private unsafe object? InterpretedInvoke(object? obj, IntPtr *args)
        {
            Exception exc;
            object? o = _method.InternalInvoke(obj, args, out exc);

            if (exc != null)
                throw exc;

            return obj == null ? o : null;
        }
    }
}
