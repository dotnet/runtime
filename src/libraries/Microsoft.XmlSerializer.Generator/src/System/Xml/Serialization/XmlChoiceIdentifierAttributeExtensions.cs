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
    internal static class XmlChoiceIdentifierAttributeExtensions
    {
        private static PropertyInfo s_MemberInfoPropertyInfo = typeof(XmlChoiceIdentifierAttribute).GetProperty("MemberInfo", BindingFlags.NonPublic | BindingFlags.Instance);

        internal static MemberInfo GetMemberInfo(this XmlChoiceIdentifierAttribute xmlChoiceIdentifierAtt)
        {
            return (MemberInfo)s_MemberInfoPropertyInfo.GetValue(xmlChoiceIdentifierAtt);
        }

        internal static void SetMemberInfo(this XmlChoiceIdentifierAttribute xmlChoiceIdentifierAtt, MemberInfo memberInfo)
        {
            s_MemberInfoPropertyInfo.SetValue(xmlChoiceIdentifierAtt, memberInfo);
        }
    }
}
