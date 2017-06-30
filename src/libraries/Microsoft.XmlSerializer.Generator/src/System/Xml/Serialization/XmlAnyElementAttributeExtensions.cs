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
    internal static class XmlAnyElementAttributeExtensions
    {
        private static PropertyInfo s_NamespaceSpecifiedPropertyInfo;
        internal static bool GetNamespaceSpecified(this XmlAnyElementAttribute xmlAnyElementAtt)
        {
            if(s_NamespaceSpecifiedPropertyInfo == null)
            {
                s_NamespaceSpecifiedPropertyInfo = typeof(XmlAnyElementAttribute).GetProperty("NamespaceSpecified", BindingFlags.NonPublic | BindingFlags.Instance);
            }

            return (bool)s_NamespaceSpecifiedPropertyInfo.GetValue(xmlAnyElementAtt);
        }
    }
}
