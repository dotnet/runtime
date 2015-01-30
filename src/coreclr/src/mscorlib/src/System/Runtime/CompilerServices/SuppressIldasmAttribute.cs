// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//------------------------------------------------------------------------------
//------------------------------------------------------------------------------
namespace System.Runtime.CompilerServices 
{
    using System;

    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module)]
    public sealed class SuppressIldasmAttribute : Attribute
    {
        public SuppressIldasmAttribute()
        {
        }
    }
}

