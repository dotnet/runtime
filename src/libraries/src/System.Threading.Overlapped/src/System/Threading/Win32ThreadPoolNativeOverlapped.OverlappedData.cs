// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Threading
{
    partial struct Win32ThreadPoolNativeOverlapped
    {
        internal class OverlappedData
        {
            internal GCHandle[] _pinnedData;
            internal IOCompletionCallback _callback;
            internal object _state;
            internal ExecutionContext _executionContext;
            internal ThreadPoolBoundHandle _boundHandle;
            internal PreAllocatedOverlapped _preAllocated;
            internal bool _completed;

            internal void Reset()
            {
                Debug.Assert(_boundHandle == null); //not in use

                if (_pinnedData != null)
                {
                    for (int i = 0; i < _pinnedData.Length; i++)
                    {
                        if (_pinnedData[i].IsAllocated && _pinnedData[i].Target != null)
                            _pinnedData[i].Target = null;
                    }
                }

                _callback = null;
                _state = null;
                _executionContext = null;
                _completed = false;
                _preAllocated = null;
            }
        }
    }
}
