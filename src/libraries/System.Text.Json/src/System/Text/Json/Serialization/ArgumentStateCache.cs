// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Text.Json
{
    internal class ArgumentStateCache
    {
        private List<ArgumentState>? _stateCache;

        public (int, ArgumentState) GetState(int index)
        {
            if (index == 0)
            {
                if (_stateCache == null)
                {
                    _stateCache = new List<ArgumentState>();
                }

                var newState = new ArgumentState();
                _stateCache.Add(newState);

                // Return index + 1 so a zero index on ReadStackFrame is reserved for new frames.
                return (_stateCache.Count, newState);
            }

            return (index, _stateCache![index - 1]);
        }
    }
}
