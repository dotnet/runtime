// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
    public sealed class DisableRuntimeMarshallingAttribute : Attribute
    {
    }
}
