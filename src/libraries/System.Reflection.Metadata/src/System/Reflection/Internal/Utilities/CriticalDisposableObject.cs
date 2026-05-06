// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Runtime.ConstrainedExecution;

namespace System.Reflection.Internal
{
    internal abstract class CriticalDisposableObject : CriticalFinalizerObject, IDisposable
    {
        protected abstract void Release();

        public void Dispose()
        {
            Release();
            GC.SuppressFinalize(this);
        }

        ~CriticalDisposableObject()
        {
            Release();
        }
    }
}
