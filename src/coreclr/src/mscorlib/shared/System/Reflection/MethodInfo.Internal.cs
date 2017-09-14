// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection
{
    public abstract partial class MethodInfo : MethodBase
    {
#if CORECLR
        internal
#else
        // Not an api but needs to be public so that Reflection.Core can access it.
        public
#endif
        virtual int GenericParameterCount => GetGenericArguments().Length;
    }
}
