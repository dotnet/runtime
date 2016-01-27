// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
