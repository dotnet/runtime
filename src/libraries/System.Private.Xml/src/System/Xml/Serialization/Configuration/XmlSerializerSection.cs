// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Serialization.Configuration
{
    using System;
    using System.IO;
    using System.Configuration;
    using System.ComponentModel;
    using System.Globalization;
    using System.Reflection;
    using System.Resources;

    internal sealed class XmlSerializerSection
    {
        public XmlSerializerSection()
        {
        }

        public bool CheckDeserializeAdvances
        {
            get { return false; }
            set { }
        }

        public string TempFilesLocation
        {
            get { return null; }
            set { }
        }

        public bool UseLegacySerializerGeneration
        {
            get { return false; }
            set { }
        }
    }
}


