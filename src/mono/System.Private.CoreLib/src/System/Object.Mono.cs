// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace System
{
    public partial class Object
    {
        [Intrinsic]
        public Type GetType() => GetType();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        protected extern object MemberwiseClone();

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = nameof(Finalize))]
        private static extern void CallFinalize(object o);
        internal void GuardedFinalize()
        {
            try
            {
                CallFinalize(this);
            }
            catch (Exception ex) when (ExceptionHandling.IsHandledByGlobalHandler(ex))
            {
                // the handler returned "true" means the exception is now "handled" and we should continue.
            }
        }
    }
}
