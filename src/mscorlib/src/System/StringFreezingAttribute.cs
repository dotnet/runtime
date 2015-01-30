// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
**
**
**
** Purpose:     Custom attribute to indicate that strings should be frozen
**
**
===========================================================*/

namespace System.Runtime.CompilerServices
{
    
[Serializable]
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    public sealed class StringFreezingAttribute : Attribute
    {
        public StringFreezingAttribute()
        {
        }
    }
}
