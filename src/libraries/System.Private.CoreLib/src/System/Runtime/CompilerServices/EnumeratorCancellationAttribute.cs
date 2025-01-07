// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    /// <summary>Allows users of async-enumerable methods to mark the parameter that should receive the cancellation token value from <see cref="M:System.Collections.Generic.IAsyncEnumerable`1.GetAsyncEnumerator(System.Threading.CancellationToken)" />.</summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    public sealed class EnumeratorCancellationAttribute : Attribute
    {
        /// <summary>Initializes a new instance of the <see cref="T:System.Runtime.CompilerServices.EnumeratorCancellationAttribute" /> class.</summary>
        public EnumeratorCancellationAttribute()
        {
        }
    }
}
