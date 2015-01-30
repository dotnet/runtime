// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace System.Runtime.CompilerServices 
{
[Serializable]
[AttributeUsage (AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface, 
                     AllowMultiple=true, Inherited=false)]
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class RequiredAttributeAttribute : Attribute 
    {
        private Type requiredContract;

        public RequiredAttributeAttribute (Type requiredContract) 
        {
            this.requiredContract= requiredContract;
        }
        public Type RequiredContract 
        {
            get { return this.requiredContract; }
        }
    }
}
