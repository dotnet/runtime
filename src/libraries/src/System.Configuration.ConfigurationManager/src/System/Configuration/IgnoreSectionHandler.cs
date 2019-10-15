// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;

namespace System.Configuration
{
    public class IgnoreSectionHandler : IConfigurationSectionHandler
    {
        /// <summary>
        ///     Given a partially composed config object (possibly null) and some input from the config system,
        ///     return a further partially composed config object.
        /// </summary>
        public virtual object Create(object parent, object configContext, XmlNode section)
        {
            return null;
        }
    }
}