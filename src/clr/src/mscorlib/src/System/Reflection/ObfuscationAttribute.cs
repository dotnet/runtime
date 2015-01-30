// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// 

using System;
using System.Reflection;

namespace System.Reflection
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Enum | AttributeTargets.Delegate,
        AllowMultiple = true, Inherited = false)]
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class ObfuscationAttribute: Attribute
    {
        private bool m_strip = true;
        private bool m_exclude = true;
        private bool m_applyToMembers = true;
        private string m_feature = "all";

        public ObfuscationAttribute()
        {
        }

        public bool StripAfterObfuscation
        {
            get
            {
                return m_strip;
            }
            set
            {
                m_strip = value;
            }
        }

        public bool Exclude
        {
            get
            {
                return m_exclude;
            }
            set
            {
                m_exclude = value;
            }
        }

        public bool ApplyToMembers
        {
            get
            {
                return m_applyToMembers;
            }
            set
            {
                m_applyToMembers = value;
            }
        }

        public string Feature
        {
            get
            {
                return m_feature;
            }
            set
            {
                m_feature = value;
            }
        }
    }
}

