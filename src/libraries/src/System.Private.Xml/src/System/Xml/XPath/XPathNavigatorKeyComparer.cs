// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MS.Internal.Xml.Cache;
using System.Collections;

namespace System.Xml.XPath
{
    internal class XPathNavigatorKeyComparer : IEqualityComparer
    {
        bool IEqualityComparer.Equals(Object obj1, Object obj2)
        {
            XPathNavigator nav1 = obj1 as XPathNavigator;
            XPathNavigator nav2 = obj2 as XPathNavigator;
            if ((nav1 != null) && (nav2 != null))
            {
                if (nav1.IsSamePosition(nav2))
                    return true;
            }
            return false;
        }

        int IEqualityComparer.GetHashCode(Object obj)
        {
            int hashCode;
            XPathNavigator nav;
            XPathDocumentNavigator xpdocNav;

            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            else if (null != (xpdocNav = obj as XPathDocumentNavigator))
            {
                hashCode = xpdocNav.GetPositionHashCode();
            }
            else if (null != (nav = obj as XPathNavigator))
            {
                Object underlyingObject = nav.UnderlyingObject;
                if (underlyingObject != null)
                {
                    hashCode = underlyingObject.GetHashCode();
                }
                else
                {
                    hashCode = (int)nav.NodeType;
                    hashCode ^= nav.LocalName.GetHashCode();
                    hashCode ^= nav.Prefix.GetHashCode();
                    hashCode ^= nav.NamespaceURI.GetHashCode();
                }
            }
            else
            {
                hashCode = obj.GetHashCode();
            }
            return hashCode;
        }
    }
}
