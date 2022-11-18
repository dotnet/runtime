// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;

namespace System.Xml.XPath
{
    internal static class XObjectExtensions
    {
        public static XContainer? GetParent(this XObject obj)
        {
            XContainer? ret = (XContainer?)obj.Parent ?? obj.Document;
            if (ret == obj)
            {
                return null;
            }
            return ret;
        }
    }
}
