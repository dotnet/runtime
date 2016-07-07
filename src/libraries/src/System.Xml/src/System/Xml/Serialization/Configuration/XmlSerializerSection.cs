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

    internal sealed class XmlSerializerSection : ConfigurationSection
    {
        public XmlSerializerSection()
        {
        }

        protected override ConfigurationPropertyCollection Properties
        {
            get
            {
                return _properties;
            }
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

        private ConfigurationPropertyCollection _properties = new ConfigurationPropertyCollection();
    }


    internal class RootedPathValidator : ConfigurationValidatorBase
    {
        public override bool CanValidate(Type type)
        {
            return (type == typeof(string));
        }

        public override void Validate(object value)
        {
            string tempDirectory = value as string;
            if (string.IsNullOrEmpty(tempDirectory))
                return;
            tempDirectory = tempDirectory.Trim();
            if (string.IsNullOrEmpty(tempDirectory))
                return;
            if (!Path.IsPathRooted(tempDirectory))
            {
                // Make sure the path is not relative (VSWhidbey 260075)
                throw new ConfigurationErrorsException();
            }
            char firstChar = tempDirectory[0];
            if (firstChar == Path.DirectorySeparatorChar || firstChar == Path.AltDirectorySeparatorChar)
            {
                // Make sure the path is explicitly rooted
                throw new ConfigurationErrorsException();
            }
        }
    }
}


