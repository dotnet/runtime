// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//------------------------------------------------------------------------------
//------------------------------------------------------------------------------
namespace System.Runtime.CompilerServices 
{
    using System;

    [AttributeUsage(AttributeTargets.Field)]
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class AccessedThroughPropertyAttribute : Attribute
    {
        private readonly string propertyName;

        public AccessedThroughPropertyAttribute(string propertyName)
        {
            this.propertyName = propertyName;
        }

        public string PropertyName 
        {
            get 
            {
                return propertyName;
            }
        }
    }
}

