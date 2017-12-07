// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using Microsoft.Internal.Collections;

namespace System.ComponentModel.Composition
{
    internal struct CompositionResult
    {
        public static readonly CompositionResult SucceededResult = new CompositionResult();
        private readonly IEnumerable<CompositionError> _errors;

        public CompositionResult(params CompositionError[] errors)
            : this((IEnumerable<CompositionError>)errors)
        {            
        }

        public CompositionResult(IEnumerable<CompositionError> errors)
        {
            _errors = errors;
        }

        public bool Succeeded
        {
            get { return _errors == null || !_errors.FastAny(); }
        }

        public IEnumerable<CompositionError> Errors
        {
            get { return _errors ?? Enumerable.Empty<CompositionError>(); }
        }

        public CompositionResult MergeResult(CompositionResult result)
        {
            if (Succeeded)
            {
                return result;
            }
            if (result.Succeeded)
            {
                return this;
            }
            return MergeErrors(result._errors);
        }

        public CompositionResult MergeError(CompositionError error)
        {
            return MergeErrors(new CompositionError[] { error });
        }

        public CompositionResult MergeErrors(IEnumerable<CompositionError> errors)
        {
            return new CompositionResult(_errors.ConcatAllowingNull(errors));
        }

        public CompositionResult<T> ToResult<T>(T value)
        {
            return new CompositionResult<T>(value, _errors); 
        }

        public void ThrowOnErrors()
        {
            ThrowOnErrors(null);
        }

        public void ThrowOnErrors(AtomicComposition atomicComposition)
        {
            if (!Succeeded)
            {
                if (atomicComposition == null)
                {
                    throw new CompositionException(_errors);
                }
                else
                {
                    throw new ChangeRejectedException(_errors);
                }
            }
        }
    }
}
