// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.Protocols
{
    using System;
    using System.Net;
    using System.Security.Cryptography.X509Certificates;
    using System.Xml;
    using System.Security.Permissions;

    public abstract class DsmlSoapConnection : DirectoryConnection
    {
        internal XmlNode soapHeaders = null;

        protected DsmlSoapConnection() { }

        public abstract string SessionId
        {
            get;
        }

        public XmlNode SoapRequestHeader
        {
            get
            {
                return soapHeaders;
            }
            set
            {
                soapHeaders = value;
            }
        }

        [
           DirectoryServicesPermission(SecurityAction.LinkDemand, Unrestricted = true)
        ]
        public abstract void BeginSession();
        [
           DirectoryServicesPermission(SecurityAction.LinkDemand, Unrestricted = true)
        ]
        public abstract void EndSession();
    }
}
