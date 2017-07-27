// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Composition
{
    /// <summary>
    /// A handle allowing the graph of parts associated with an exported instance
    /// to be released.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class Export<T> : IDisposable
    {
        private readonly Action _disposeAction;

        /// <summary>
        /// Construct an Export.
        /// </summary>
        /// <param name="value">The value of the export.</param>
        /// <param name="disposeAction">An action that releases resources associated with the export.</param>
        public Export(T value, Action disposeAction)
        {
            Value = value;
            _disposeAction = disposeAction;
        }

        /// <summary>
        /// The exported value.
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// Release the parts associated with the exported value.
        /// </summary>
        public void Dispose() => _disposeAction?.Invoke();
    }
}

