// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

namespace System.Composition
{
    /// <summary>
    /// A handle allowing the graph of parts associated with an exported instance
    /// to be released.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class Export<T> : IDisposable
    {
        private readonly T _value;
        private readonly Action _disposeAction;

        /// <summary>
        /// Construct an ExportLifetimContext.
        /// </summary>
        /// <param name="value">The value of the export.</param>
        /// <param name="disposeAction">An action that releases resources associated with the export.</param>
        public Export(T value, Action disposeAction)
        {
            _value = value;
            _disposeAction = disposeAction;
        }

        /// <summary>
        /// The exported value.
        /// </summary>
        public T Value
        {
            get
            {
                return _value;
            }
        }

        /// <summary>
        /// Release the parts associated with the exported value.
        /// </summary>
        public void Dispose()
        {
            if (_disposeAction != null)
            {
                _disposeAction.Invoke();
            }
        }
    }
}

