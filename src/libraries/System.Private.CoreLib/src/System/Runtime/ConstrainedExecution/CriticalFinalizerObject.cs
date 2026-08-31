// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.ConstrainedExecution
{
    /// <summary>
    /// Ensures that all finalization code in derived classes is marked as critical.
    /// </summary>
    public abstract class CriticalFinalizerObject
    {
        protected CriticalFinalizerObject()
        {
        }

        [SuppressMessage("Microsoft.Performance", "CA1821:RemoveEmptyFinalizers", Justification = "Base finalizer method on CriticalFinalizerObject")]
        ~CriticalFinalizerObject()
        {
        }
    }
}
