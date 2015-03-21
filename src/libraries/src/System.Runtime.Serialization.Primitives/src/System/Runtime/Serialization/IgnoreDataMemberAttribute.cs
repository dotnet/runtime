// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------

//------------------------------------------------------------

namespace System.Runtime.Serialization
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class IgnoreDataMemberAttribute : Attribute
    {
        public IgnoreDataMemberAttribute()
        {
        }
    }
}