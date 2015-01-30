// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace System.Diagnostics {
    [Serializable]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple=true)]
    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class ConditionalAttribute : Attribute
    {
        public ConditionalAttribute(String conditionString)
        {
            m_conditionString = conditionString;
        }

        public String ConditionString {
            get {
                return m_conditionString;
            }
        }

        private String m_conditionString;
    }
}
