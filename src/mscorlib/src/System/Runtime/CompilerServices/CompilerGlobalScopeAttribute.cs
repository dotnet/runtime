// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

