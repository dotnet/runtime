// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.CompilerServices {
    
    using System;
    using System.Reflection;    
    
    // This Enum matchs the miImpl flags defined in corhdr.h. It is used to specify 
    // certain method properties.
    
    [Serializable]
    [Flags]
[System.Runtime.InteropServices.ComVisible(true)]
    public enum MethodImplOptions
    {
        Unmanaged          =   System.Reflection.MethodImplAttributes.Unmanaged,
        ForwardRef         =   System.Reflection.MethodImplAttributes.ForwardRef,
        PreserveSig        =   System.Reflection.MethodImplAttributes.PreserveSig,
        InternalCall       =   System.Reflection.MethodImplAttributes.InternalCall,
        Synchronized       =   System.Reflection.MethodImplAttributes.Synchronized,
        NoInlining         =   System.Reflection.MethodImplAttributes.NoInlining,
        [System.Runtime.InteropServices.ComVisible(false)]
        AggressiveInlining =   System.Reflection.MethodImplAttributes.AggressiveInlining,
        NoOptimization     =   System.Reflection.MethodImplAttributes.NoOptimization,
        // **** If you add something, update internal MethodImplAttribute(MethodImplAttributes methodImplAttributes)! ****
    }

    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public enum MethodCodeType
    {
        IL              =   System.Reflection.MethodImplAttributes.IL,
        Native          =   System.Reflection.MethodImplAttributes.Native,
        /// <internalonly/>
        OPTIL           =   System.Reflection.MethodImplAttributes.OPTIL,
        Runtime         =   System.Reflection.MethodImplAttributes.Runtime  
    }

    // Custom attribute to specify additional method properties.
[Serializable]
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)] 
[System.Runtime.InteropServices.ComVisible(true)]
    sealed public class MethodImplAttribute : Attribute  
    {    
        internal MethodImplOptions  _val;
        public   MethodCodeType     MethodCodeType;

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
        
        public MethodImplOptions Value { get {return _val;} }   
    }

}
