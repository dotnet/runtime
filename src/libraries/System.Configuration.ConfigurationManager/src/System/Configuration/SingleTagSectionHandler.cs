// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Xml;

namespace System.Configuration
{
    /**
      * Single-tag dictionary config factory
      *
      * Use for tags of the form: &lt;MySingleTag key1="value1" ... keyN="valueN"/&gt;
      */
    public class SingleTagSectionHandler : IConfigurationSectionHandler
    {
        /**
         * Create
         *
         * Given a partially composed config object (possibly null)
         * and some input from the config system, return a
         * further partially composed config object
         */
        public virtual object Create(object parent, object context, XmlNode section)
        {
            Hashtable result;

            // start result off as a shallow clone of the parent

            if (parent == null)
                result = new Hashtable();
            else
                result = new Hashtable((IDictionary)parent);

            // verify that there are no children

            HandlerBase.CheckForChildNodes(section);

            // iterate through each XML section in order and apply the directives

            foreach (XmlAttribute attribute in section.Attributes)
            {
                // handle name-value pairs
                result[attribute.Name] = attribute.Value;
            }

            return result;
        }
    }
}
