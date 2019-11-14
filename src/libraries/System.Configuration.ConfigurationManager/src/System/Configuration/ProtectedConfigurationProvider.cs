// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration.Provider;
using System.Xml;

namespace System.Configuration
{
    public abstract class ProtectedConfigurationProvider : ProviderBase
    {
        public abstract XmlNode Encrypt(XmlNode node);
        public abstract XmlNode Decrypt(XmlNode encryptedNode);
    }
}