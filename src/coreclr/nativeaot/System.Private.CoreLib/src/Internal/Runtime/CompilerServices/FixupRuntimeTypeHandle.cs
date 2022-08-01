// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Internal.Runtime;

namespace Internal.Runtime.CompilerServices
{
    public unsafe struct FixupRuntimeTypeHandle
    {
        private IntPtr _value;

        public FixupRuntimeTypeHandle(RuntimeTypeHandle runtimeTypeHandle)
        {
            _value = *(IntPtr*)&runtimeTypeHandle;
        }

        public RuntimeTypeHandle RuntimeTypeHandle
        {
            get
            {
                // Managed debugger uses this logic to figure out the interface's type
                // Update managed debugger too whenever this is changed.
                // See CordbObjectValue::WalkPtrAndTypeData in debug\dbi\values.cpp

                if (((_value.ToInt64()) & IndirectionConstants.IndirectionCellPointer) != 0)
                {
                    return *(RuntimeTypeHandle*)(_value.ToInt64() - IndirectionConstants.IndirectionCellPointer);
                }
                else
                {
                    RuntimeTypeHandle returnValue = default(RuntimeTypeHandle);
                    *(IntPtr*)&returnValue = _value;
                    return returnValue;
                }
            }
        }
    }
}
