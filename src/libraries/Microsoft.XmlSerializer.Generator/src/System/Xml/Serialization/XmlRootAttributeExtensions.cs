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
    internal static class XmlRootAttributeExtensions
    {
        private static PropertyInfo s_IsNullableSpecifiedPropertyInfo;
        private static PropertyInfo s_KeydPropertyInfo;

        internal static bool GetIsNullableSpecified(this XmlRootAttribute xmlRootAtt)
        {
            if(s_IsNullableSpecifiedPropertyInfo == null)
            {
                s_IsNullableSpecifiedPropertyInfo = typeof(XmlRootAttribute).GetProperty("IsNullableSpecified", BindingFlags.NonPublic | BindingFlags.Instance);
            }

            return (bool)s_IsNullableSpecifiedPropertyInfo.GetValue(xmlRootAtt);
        }

        internal static string GetKey(this XmlRootAttribute xmlRootAtt)
        {
            if (s_KeydPropertyInfo == null)
            {
                s_KeydPropertyInfo = typeof(XmlRootAttribute).GetProperty("Key", BindingFlags.NonPublic | BindingFlags.Instance);
            }

            return (string)s_KeydPropertyInfo.GetValue(xmlRootAtt);
        }
    }
}
