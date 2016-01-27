// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////
/*=============================================================================
**
**
**
** Purpose: Mark up the program to indicate various legacy or new opt-in behaviors.
**
**
=============================================================================*/

namespace System.Runtime.CompilerServices 
{

    using System;

[Serializable]
[AttributeUsage(AttributeTargets.Assembly, Inherited=false, AllowMultiple=false)]
    public sealed class RuntimeCompatibilityAttribute : Attribute 
    {
        // fields
        private bool m_wrapNonExceptionThrows;

        // constructors
        public RuntimeCompatibilityAttribute() {
            // legacy behavior is the default, and m_wrapNonExceptionThrows is implicitly
            // false thanks to the CLR's guarantee of zeroed memory.
        }

        // properties

        // If a non-CLSCompliant exception (i.e. one that doesn't derive from System.Exception) is
        // thrown, should it be wrapped up in a System.Runtime.CompilerServices.RuntimeWrappedException
        // instance when presented to catch handlers?
        public bool WrapNonExceptionThrows { 
            get { 
                return m_wrapNonExceptionThrows; 
            }
            set {
                m_wrapNonExceptionThrows = value;
            }
        }
    }
}
