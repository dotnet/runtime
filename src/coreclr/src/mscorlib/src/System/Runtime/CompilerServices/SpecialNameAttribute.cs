// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices 
{
    [AttributeUsage(AttributeTargets.Class | 
                    AttributeTargets.Method |
                    AttributeTargets.Property |
                    AttributeTargets.Field |
                    AttributeTargets.Event |
                    AttributeTargets.Struct)]

   
    public sealed class SpecialNameAttribute : Attribute
    {
        public SpecialNameAttribute() { }
    }
}



