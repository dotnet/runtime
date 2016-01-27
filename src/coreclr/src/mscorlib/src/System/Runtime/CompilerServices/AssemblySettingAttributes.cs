// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

namespace System.Runtime.CompilerServices 
{

    using System;
    using System.Runtime.InteropServices;

    /*
    NGenHint is not supported in Whidbey     

    [Serializable]
    public enum NGenHint
    {    
        Default             = 0x0000, // No preference specified
        
        Eager               = 0x0001, // NGen at install time
        Lazy                = 0x0002, // NGen after install time
        Never               = 0x0003, // Assembly should not be ngened      
    }
    */

    [Serializable]
    public enum LoadHint
    {
        Default             = 0x0000, // No preference specified
        
        Always              = 0x0001, // Dependency is always loaded
        Sometimes           = 0x0002, // Dependency is sometimes loaded
        //Never               = 0x0003, // Dependency is never loaded
    }

    [Serializable]
    [AttributeUsage(AttributeTargets.Assembly)]  
    public sealed class DefaultDependencyAttribute : Attribute 
    {
        private LoadHint loadHint;
    
        public DefaultDependencyAttribute (
            LoadHint loadHintArgument
            )
        {
            loadHint = loadHintArgument;
        }  
    
        public LoadHint LoadHint
        {
            get
            {
                return loadHint;
            }
        }       
    } 


[Serializable]
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]  
    public sealed class DependencyAttribute : Attribute 
    {
        private String                dependentAssembly;
        private LoadHint              loadHint;

        public DependencyAttribute (
            String   dependentAssemblyArgument,
            LoadHint loadHintArgument
            )
        {
            dependentAssembly     = dependentAssemblyArgument;
            loadHint              = loadHintArgument;
        }
        
        public String DependentAssembly
        {
            get
            {
                return dependentAssembly;
            }
        }       

        public LoadHint LoadHint
        {
            get
            {
                return loadHint;
            }
        }       
    }
}

