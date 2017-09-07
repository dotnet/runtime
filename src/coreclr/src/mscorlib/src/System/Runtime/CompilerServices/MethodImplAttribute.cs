// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Reflection;

namespace System.Runtime.CompilerServices
{
    // This Enum matchs the miImpl flags defined in corhdr.h. It is used to specify 
    // certain method properties.

    // Custom attribute to specify additional method properties.
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
    sealed public class MethodImplAttribute : Attribute
    {
        internal MethodImplOptions _val;
        public MethodCodeType MethodCodeType;

        internal MethodImplAttribute(MethodImplAttributes methodImplAttributes)
        {
            MethodImplOptions all =
                MethodImplOptions.Unmanaged | MethodImplOptions.ForwardRef | MethodImplOptions.PreserveSig |
                MethodImplOptions.InternalCall | MethodImplOptions.Synchronized |
                MethodImplOptions.NoInlining | MethodImplOptions.AggressiveInlining |
                MethodImplOptions.NoOptimization;
            _val = ((MethodImplOptions)methodImplAttributes) & all;
        }

        public MethodImplAttribute(MethodImplOptions methodImplOptions)
        {
            _val = methodImplOptions;
        }

        public MethodImplAttribute(short value)
        {
            _val = (MethodImplOptions)value;
        }

        public MethodImplAttribute()
        {
        }

        public MethodImplOptions Value { get { return _val; } }
    }
}
