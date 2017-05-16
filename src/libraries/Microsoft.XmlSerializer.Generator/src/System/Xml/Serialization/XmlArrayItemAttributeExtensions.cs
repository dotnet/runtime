// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Microsoft.XmlSerializer.Generator
{
    internal static class XmlArrayItemAttributeExtensions
    {
        private static PropertyInfo s_IsNullableSpecifiedPropertyInfo;
        internal static bool GetIsNullableSpecified(this XmlArrayItemAttribute xmlArrayItemAtt)
        {
            if(s_IsNullableSpecifiedPropertyInfo == null)
            {
                s_IsNullableSpecifiedPropertyInfo= typeof(XmlArrayItemAttribute).GetProperty("IsNullableSpecified", BindingFlags.NonPublic | BindingFlags.Instance);
            }

            return (bool)s_IsNullableSpecifiedPropertyInfo.GetValue(xmlArrayItemAtt);
        }
    }
}
