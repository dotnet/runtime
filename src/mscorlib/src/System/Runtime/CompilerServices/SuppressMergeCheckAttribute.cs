// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices 
{
    [AttributeUsage(AttributeTargets.Class | 
                    AttributeTargets.Constructor | 
                    AttributeTargets.Method |
                    AttributeTargets.Field |
                    AttributeTargets.Event |
                    AttributeTargets.Property)]

    internal sealed class SuppressMergeCheckAttribute : Attribute
    {
        public SuppressMergeCheckAttribute() 
        {}  
    }
}

