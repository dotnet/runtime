// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Reflection
{
    public abstract partial class DispatchProxy
    {
        protected DispatchProxy() { }
        public static T Create<T, TProxy>() where TProxy : System.Reflection.DispatchProxy { return default(T); }
        protected abstract object Invoke(System.Reflection.MethodInfo targetMethod, object[] args);
    }
}
