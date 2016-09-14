// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml.Linq;

namespace System.Xml.XPath
{
    internal static class XObjectExtensions
    {
        public static XContainer GetParent(this XObject obj)
        {
            XContainer ret = obj.Parent;
            if (ret == null)
            {
                ret = obj.Document;
            }
            if (ret == obj)
            {
                return null;
            }
            return ret;
        }
    }
}
