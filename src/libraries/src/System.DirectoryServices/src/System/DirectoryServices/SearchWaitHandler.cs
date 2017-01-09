// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using INTPTR_INTCAST = System.Int32;
using INTPTR_INTPTRCAST = System.IntPtr;

namespace System.DirectoryServices
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Xml;
    using System.Configuration;
    using System.Reflection;
    using System.Security.Permissions;
    using System.Globalization;

    internal class SearchWaitHandler : IConfigurationSectionHandler
    {
        public virtual object Create(object parent, object configContext, XmlNode section)
        {
            bool foundWaitStatus = false;
            bool waitForSearchResult = false;

            foreach (XmlNode child in section.ChildNodes)
            {
                switch (child.Name)
                {
                    case "DirectorySearcher":
                        if (foundWaitStatus)
                            throw new ConfigurationErrorsException(Res.GetString(Res.ConfigSectionsUnique, "DirectorySearcher"));
                        HandlerBase.RemoveBooleanAttribute(child, "waitForPagedSearchData", ref waitForSearchResult);
                        foundWaitStatus = true;
                        break;

                    default:
                        break;
                } // switch(child.Name)
            }

            object o = waitForSearchResult;
            return o;
        }
    }

    internal class HandlerBase
    {
        private HandlerBase()
        {
        }

        static internal void RemoveBooleanAttribute(XmlNode node, string name, ref bool value)
        {
            value = false;
            XmlNode attribute = node.Attributes.RemoveNamedItem(name);
            if (null != attribute)
            {
                try
                {
                    value = bool.Parse(attribute.Value);
                }
                catch (FormatException)
                {
                    throw new ConfigurationErrorsException(Res.GetString(Res.Invalid_boolean_attribute, name));
                }
            }
        }
    }
}
