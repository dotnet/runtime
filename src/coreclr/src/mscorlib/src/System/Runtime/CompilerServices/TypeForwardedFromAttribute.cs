// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Delegate, Inherited = false, AllowMultiple = false)]
    public sealed class TypeForwardedFromAttribute : Attribute
    {
        string assemblyFullName;

        private TypeForwardedFromAttribute()
        {
            // Disallow default constructor
        }


        public TypeForwardedFromAttribute(string assemblyFullName)
        {
            if (String.IsNullOrEmpty(assemblyFullName))
            {
                throw new ArgumentNullException("assemblyFullName");
            }
            this.assemblyFullName = assemblyFullName;    
        }

        public string AssemblyFullName
        {
            get { 
                return assemblyFullName; 
            }
        }
    }
}