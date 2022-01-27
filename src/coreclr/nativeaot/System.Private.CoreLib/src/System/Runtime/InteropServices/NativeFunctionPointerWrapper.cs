// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Base class for all 'wrapper' classes that wraps a native function pointer
    /// The forward delegates (that wraps native function pointers) points to derived Invoke method of this
    /// class, and the Invoke method would implement the marshalling and making the call
    /// </summary>
    public abstract class NativeFunctionPointerWrapper
    {
        public NativeFunctionPointerWrapper(IntPtr nativeFunctionPointer)
        {
            m_nativeFunctionPointer = nativeFunctionPointer;
        }

        IntPtr m_nativeFunctionPointer;

        public IntPtr NativeFunctionPointer
        {
            get { return m_nativeFunctionPointer; }
        }
    }
}
