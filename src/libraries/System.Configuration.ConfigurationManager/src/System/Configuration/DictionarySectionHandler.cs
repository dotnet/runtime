// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Xml;

namespace System.Configuration
{
    /// <summary>
    /// Simple dictionary config factory
    /// config is a dictionary mapping key-&gt;value
    ///
    ///     &lt;add key="name" value="text"&gt;  sets key=text
    ///     &lt;remove key="name"&gt;            removes the definition of key
    ///     &lt;clear&gt;                        removes all definitions
    ///
    /// </summary>
    public class DictionarySectionHandler : IConfigurationSectionHandler
    {
        /// <summary>
        /// Given a partially composed config object (possibly null)
        /// and some input from the config system, return a
        /// further partially composed config object
        /// </summary>
        public virtual object Create(object parent, object context, XmlNode section)
        {
            Hashtable res;

            // start res off as a shallow clone of the parent

            if (parent == null)
                res = new Hashtable(StringComparer.OrdinalIgnoreCase);
            else
                res = (Hashtable)((Hashtable)parent).Clone();

            // process XML

            HandlerBase.CheckForUnrecognizedAttributes(section);

            foreach (XmlNode child in section.ChildNodes)
            {

                // skip whitespace and comments, throws if non-element
                if (HandlerBase.IsIgnorableAlsoCheckForNonElement(child))
                    continue;

                // handle <add>, <remove>, <clear> tags
                if (child.Name == "add")
                {
                    HandlerBase.CheckForChildNodes(child);
                    string key = HandlerBase.RemoveRequiredAttribute(child, KeyAttributeName);
                    string value = ValueRequired ?
                        HandlerBase.RemoveRequiredAttribute(child, ValueAttributeName) :
                        HandlerBase.RemoveAttribute(child, ValueAttributeName);
                    HandlerBase.CheckForUnrecognizedAttributes(child);

                    res[key] = value ?? "";
                }
                else if (child.Name == "remove")
                {
                    HandlerBase.CheckForChildNodes(child);
                    string key = HandlerBase.RemoveRequiredAttribute(child, KeyAttributeName);
                    HandlerBase.CheckForUnrecognizedAttributes(child);

                    res.Remove(key);
                }
                else if (child.Name.Equals("clear"))
                {
                    HandlerBase.CheckForChildNodes(child);
                    HandlerBase.CheckForUnrecognizedAttributes(child);
                    res.Clear();
                }
                else
                {
                    HandlerBase.ThrowUnrecognizedElement(child);
                }
            }

            return res;
        }

        /// <summary>
        /// Make the name of the key attribute configurable by derived classes.
        /// </summary>
        protected virtual string KeyAttributeName
        {
            get { return "key"; }
        }

        /// <summary>
        /// Make the name of the value attribute configurable by derived classes.
        /// </summary>
        protected virtual string ValueAttributeName
        {
            get { return "value"; }
        }

        internal virtual bool ValueRequired
        {
            get { return false; }
        }
    }

}
