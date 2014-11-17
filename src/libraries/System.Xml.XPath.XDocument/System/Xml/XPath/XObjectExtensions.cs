// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
