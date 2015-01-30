// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
