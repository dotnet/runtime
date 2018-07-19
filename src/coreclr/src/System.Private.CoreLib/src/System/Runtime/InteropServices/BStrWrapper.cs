// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Wrapper that is converted to a variant with VT_BSTR.
    /// </summary>
    public sealed class BStrWrapper
    {
        public BStrWrapper(string value)
        {
            m_WrappedObject = value;
        }

        public BStrWrapper(object value)
        {
            m_WrappedObject = (string)value;
        }

        public string WrappedObject => m_WrappedObject;

        private string m_WrappedObject;
    }
}
