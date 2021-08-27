// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.FileProviders
{
    /// <summary>
    /// An empty change token that doesn't raise any change callbacks.
    /// </summary>
    public class NullChangeToken : IChangeToken
    {
        /// <summary>
        /// A singleton instance of <see cref="NullChangeToken"/>
        /// </summary>
        public static NullChangeToken Singleton { get; } = new NullChangeToken();

        private NullChangeToken()
        {
        }

        /// <summary>
        /// Always false.
        /// </summary>
        public bool HasChanged => false;

        /// <summary>
        /// Always false.
        /// </summary>
        public bool ActiveChangeCallbacks => false;

        /// <summary>
        /// Always returns an empty disposable object. Callbacks will never be called.
        /// </summary>
        /// <param name="callback">This parameter is ignored</param>
        /// <param name="state">This parameter is ignored</param>
        /// <returns>A disposable object that noops on dispose.</returns>
        public IDisposable RegisterChangeCallback(Action<object?> callback, object? state)
        {
            return EmptyDisposable.Instance;
        }
    }
}
