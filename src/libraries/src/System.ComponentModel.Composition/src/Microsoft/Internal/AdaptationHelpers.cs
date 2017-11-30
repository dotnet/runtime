// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;

namespace Microsoft.Internal
{
    internal static class AdaptationHelpers
    {
        internal static CompositionResult TryInvoke(Action action)
        {
            try
            {
                action();
                return CompositionResult.SucceededResult;
            }
            catch (CompositionException ex)
            {
                return new CompositionResult(ex.Errors);
            }
        }

        internal static CompositionResult<T> TryInvoke<T>(Func<T> action)
        {
            try
            {
                T value = action();
                return new CompositionResult<T>(value);
            }
            catch (CompositionException ex)
            {
                return new CompositionResult<T>(ex.Errors);
            }
        }

        internal static TValue GetValueFromAtomicComposition<TValue>(AtomicComposition atomicComposition, object key, TValue defaultResult)
        {
            TValue result;
            if (atomicComposition != null && atomicComposition.TryGetValue(key, out result))
            {
                return result;
            }

            return defaultResult;
        }
    }
}
