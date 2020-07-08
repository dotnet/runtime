// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    public abstract partial class MethodInfo : MethodBase
    {
#if CORERT
        public // Needs to be public so that Reflection.Core can see it.
#else
        internal
#endif
        virtual int GenericParameterCount => GetGenericArguments().Length;
    }
}
