// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Xml.Serialization
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    public class SoapIgnoreAttribute : System.Attribute
    {
        public SoapIgnoreAttribute()
        {
        }
    }
}
