// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: Attribute for properties/members that the Xml Serializer should
**          ignore.
**
**
=============================================================================*/

namespace System
{
    [AttributeUsage(AttributeTargets.Property|AttributeTargets.Field)]
    internal sealed class XmlIgnoreMemberAttribute : Attribute
    {
    }
}
