// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Serialization.Configuration
{
    using System.Configuration;
    using System.Collections;
    using System.Globalization;
    using System.Reflection;
    using System.Threading;
    using System.Xml.Serialization.Advanced;

    internal sealed class SchemaImporterExtensionsSection
    {
        public SchemaImporterExtensionsSection()
        {
        }

        internal SchemaImporterExtensionCollection SchemaImporterExtensionsInternal
        {
            get
            {
                SchemaImporterExtensionCollection extensions = new SchemaImporterExtensionCollection();
                /*foreach(SchemaImporterExtensionElement elem in this.SchemaImporterExtensions) {
                    extensions.Add(elem.Name, elem.Type);
                }*/

                return extensions;
            }
        }
    }
}
