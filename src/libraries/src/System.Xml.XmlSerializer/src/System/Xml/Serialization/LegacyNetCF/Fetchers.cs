// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//

using System;
using System.Xml;
using System.Text;
using System.Collections;
using System.Reflection;
using System.Diagnostics;

namespace System.Xml.Serialization.LegacyNetCF
{
    internal interface Fetcher
    {
        object fetch(object target);
    }

    /// <summary>
    /// This class retrieves the value from either a MemberInfo object or a FieldInfo. 
    /// </summary>
    internal class MemberFetcher : Fetcher
    {
        private MemberInfo _member;

        public MemberFetcher(MemberInfo memberInfo)
        {
            PropertyInfo property = memberInfo as PropertyInfo;
            if (property != null)
            {
                MethodInfo getMethod = property.GetMethod;
                MethodInfo setMethod = property.SetMethod;
                LogicalType.ValidateSecurity(getMethod);
                LogicalType.ValidateSecurity(setMethod);
            }
            else
                LogicalType.ValidateSecurity(memberInfo);
            _member = memberInfo;
        }

        object Fetcher.fetch(object target)
        {
            return SerializationHelper.GetValue(target, _member);
        }
    }
}
