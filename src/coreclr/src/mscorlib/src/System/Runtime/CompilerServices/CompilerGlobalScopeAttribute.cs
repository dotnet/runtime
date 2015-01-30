// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
**
**
**
** Purpose: Attribute used to communicate to the VS7 debugger
**          that a class should be treated as if it has
**          global scope.
**
** 
===========================================================*/
    

namespace System.Runtime.CompilerServices
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Class)]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class CompilerGlobalScopeAttribute : Attribute
    {
       public CompilerGlobalScopeAttribute () {}
    }
}

