// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Internal.Collections;

namespace System.ComponentModel.Composition
{
    internal struct CompositionResult<T>
    {
        private readonly IEnumerable<CompositionError> _errors;
        private readonly T _value;

        public CompositionResult(T value)
            : this(value, null)
        {
        }

        public CompositionResult(params CompositionError[] errors)
            : this(default, errors)
        {
        }

        public CompositionResult(IEnumerable<CompositionError> errors)
            : this(default, errors)
        {
        }

        internal CompositionResult(T value, IEnumerable<CompositionError> errors)
        {
            _errors = errors;
            _value = value;
        }

        public bool Succeeded
        {
            get { return _errors == null || !_errors.FastAny(); }
        }

        public IEnumerable<CompositionError> Errors
        {
            get { return _errors ?? Enumerable.Empty<CompositionError>(); }
        }

        /// <summary>
        ///     Gets the value from the result, throwing a CompositionException if there are any errors.
        /// </summary>
        public T Value
        {
            get
            {
                ThrowOnErrors();

                return _value;
            }
        }

        internal CompositionResult<TValue> ToResult<TValue>()
        {
            return new CompositionResult<TValue>(_errors);
        }

        internal CompositionResult ToResult()
        {
            return new CompositionResult(_errors);
        }

        private void ThrowOnErrors()
        {
            if (!Succeeded)
            {
                throw new CompositionException(_errors);
            }
        }
    }
}
