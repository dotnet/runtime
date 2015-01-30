// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

