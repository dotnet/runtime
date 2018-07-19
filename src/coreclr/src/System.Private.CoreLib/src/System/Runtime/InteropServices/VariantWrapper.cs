// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Wrapper that is converted to a variant with VT_BYREF | VT_VARIANT.
    /// </summary>
    public sealed class VariantWrapper
    {
        public VariantWrapper(object obj)
        {
            m_WrappedObject = obj;
        }

        public object WrappedObject => m_WrappedObject;

        private object m_WrappedObject;
    }
}
