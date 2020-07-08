// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;

namespace System.Security.Cryptography.Xml
{
    public abstract class KeyInfoClause
    {
        //
        // protected constructors
        //

        protected KeyInfoClause() { }

        //
        // public methods
        //

        public abstract XmlElement GetXml();
        internal virtual XmlElement GetXml(XmlDocument xmlDocument)
        {
            XmlElement keyInfo = GetXml();
            return (XmlElement)xmlDocument.ImportNode(keyInfo, true);
        }

        public abstract void LoadXml(XmlElement element);
    }
}
