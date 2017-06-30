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
    internal static class SoapAttributesExtensions
    {
        private static PropertyInfo s_soapFlagsPropertyInfo;
        internal static SoapAttributeFlags GetSoapFlags(this SoapAttributes soapAtt)
        {
            if (s_soapFlagsPropertyInfo == null)
            {
                s_soapFlagsPropertyInfo = typeof(SoapAttributes).GetProperty("SoapFlags", BindingFlags.NonPublic | BindingFlags.Instance);
            }

            return (SoapAttributeFlags)s_soapFlagsPropertyInfo.GetValue(soapAtt);
        }
    }

    internal enum SoapAttributeFlags
    {
        Enum = 0x1,
        Type = 0x2,
        Element = 0x4,
        Attribute = 0x8,
    }
}
