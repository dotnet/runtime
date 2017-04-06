// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if XMLSERIALIZERGENERATOR
namespace Microsoft.XmlSerializer.Generator
#else
namespace System.Xml.Serialization
#endif
{
    using System;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.Diagnostics.CodeAnalysis;

    internal static class AppSettings
    {
        private const string UseLegacySerializerGenerationAppSettingsString = "System:Xml:Serialization:UseLegacySerializerGeneration";
        private static object s_appSettingsLock = new object();

        internal static bool? UseLegacySerializerGeneration
        {
            get
            {
                return null;
            }
        }
    }
}
