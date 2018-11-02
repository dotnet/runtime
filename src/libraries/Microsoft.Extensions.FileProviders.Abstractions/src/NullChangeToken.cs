// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
        public IDisposable RegisterChangeCallback(Action<object> callback, object state)
        {
            return EmptyDisposable.Instance;
        }
    }
}