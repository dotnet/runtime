// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

namespace System.Runtime.CompilerServices 
{

    using System;

    /// IMPORTANT: Keep this in sync with corhdr.h
[Serializable]
[Flags]
[System.Runtime.InteropServices.ComVisible(true)]
    public enum CompilationRelaxations : int
    { 
        NoStringInterning       = 0x0008, // Start in 0x0008, we had other non public flags in this enum before,
                                          // so we'll start here just in case somebody used them. This flag is only
                                          // valid when set for Assemblies.
    };
        
[Serializable]
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Method)]  
[System.Runtime.InteropServices.ComVisible(true)]
    public class CompilationRelaxationsAttribute : Attribute 
    {
        private int m_relaxations;      // The relaxations.
        
        public CompilationRelaxationsAttribute (
            int relaxations) 
        { 
            m_relaxations = relaxations; 
        }
        
        public CompilationRelaxationsAttribute (
            CompilationRelaxations relaxations) 
        { 
            m_relaxations = (int) relaxations; 
        }
        
        public int CompilationRelaxations
        { 
            get 
            { 
                return m_relaxations; 
            } 
        }
    }
    
}
