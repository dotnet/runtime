// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;

namespace System.Configuration
{
    /// <summary>
    /// Simple dictionary config factory
    /// </summary>
    public class NameValueSectionHandler : IConfigurationSectionHandler
    {
        private const string DefaultKeyAttribute = "key";
        private const string DefaultValueAttribute = "value";

        public object Create(object parent, object context, XmlNode section)
        {
            return CreateStatic(parent, section, KeyAttributeName, ValueAttributeName);
        }

        internal static object CreateStatic(object parent, XmlNode section)
        {
            return CreateStatic(parent, section, DefaultKeyAttribute, DefaultValueAttribute);
        }

        internal static object CreateStatic(object parent, XmlNode section, string keyAttriuteName, string valueAttributeName)
        {
            ReadOnlyNameValueCollection result;

            // start result off as a shallow clone of the parent

            if (parent == null)
                result = new ReadOnlyNameValueCollection(StringComparer.OrdinalIgnoreCase);
            else
            {
                ReadOnlyNameValueCollection parentCollection = (ReadOnlyNameValueCollection)parent;
                result = new ReadOnlyNameValueCollection(parentCollection);
            }

            // process XML

            HandlerBase.CheckForUnrecognizedAttributes(section);

            foreach (XmlNode child in section.ChildNodes)
            {

                // skip whitespace and comments
                if (HandlerBase.IsIgnorableAlsoCheckForNonElement(child))
                    continue;

                // handle <set>, <remove>, <clear> tags
                if (child.Name == "add")
                {
                    String key = HandlerBase.RemoveRequiredAttribute(child, keyAttriuteName);
                    String value = HandlerBase.RemoveRequiredAttribute(child, valueAttributeName, true/*allowEmptyString*/);
                    HandlerBase.CheckForUnrecognizedAttributes(child);

                    result[key] = value;
                }
                else if (child.Name == "remove")
                {
                    String key = HandlerBase.RemoveRequiredAttribute(child, keyAttriuteName);
                    HandlerBase.CheckForUnrecognizedAttributes(child);

                    result.Remove(key);
                }
                else if (child.Name.Equals("clear"))
                {
                    HandlerBase.CheckForUnrecognizedAttributes(child);
                    result.Clear();
                }
                else
                {
                    HandlerBase.ThrowUnrecognizedElement(child);
                }
            }

            result.SetReadOnly();

            return result;
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        protected virtual string KeyAttributeName
        {
            get { return DefaultKeyAttribute; }
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        protected virtual string ValueAttributeName
        {
            get { return DefaultValueAttribute; }
        }
    }

}
