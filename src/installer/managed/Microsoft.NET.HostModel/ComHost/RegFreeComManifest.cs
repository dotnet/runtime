// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace Microsoft.NET.HostModel.ComHost
{
    public class RegFreeComManifest
    {
        /// <summary>
        /// Generates a side-by-side application manifest to enable reg-free COM.
        /// </summary>
        /// <param name="assemblyName">The name of the assembly.</param>
        /// <param name="comHostName">The name of the comhost library.</param>
        /// <param name="assemblyVersion">The version of the assembly.</param>
        /// <param name="clsidMapPath">The path to the clsidmap file.</param>
        /// <param name="comManifestPath">The path to which to write the manifest.</param>
        /// <param name="typeLibraries">The type libraries to include in the manifest.</param>
        public static void CreateManifestFromClsidmap(string assemblyName, string comHostName, string assemblyVersion, string clsidMapPath, string comManifestPath, IReadOnlyDictionary<int, string> typeLibraries = null)
        {
            XNamespace ns = "urn:schemas-microsoft-com:asm.v1";

            XElement manifest = new XElement(ns + "assembly", new XAttribute("manifestVersion", "1.0"));
            manifest.Add(new XElement(ns + "assemblyIdentity",
                new XAttribute("type", "win32"),
                new XAttribute("name", $"{assemblyName}.X"),
                new XAttribute("version", assemblyVersion)));

            var fileElement = CreateComHostFileElement(clsidMapPath, comHostName, ns);
            if (typeLibraries is not null)
            {
                AddTypeLibElementsToFileElement(typeLibraries, ns, fileElement);
            }
            manifest.Add(fileElement);

            XDocument manifestDocument = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), manifest);
            XmlWriterSettings settings = new XmlWriterSettings()
            {
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
            };
            using (XmlWriter manifestWriter = XmlWriter.Create(comManifestPath, settings))
            {
                manifestDocument.WriteTo(manifestWriter);
            }
        }

        private static XElement CreateComHostFileElement(string clsidMapPath, string comHostName, XNamespace ns)
        {
            XElement fileElement = new XElement(ns + "file", new XAttribute("name", comHostName));
            JsonElement clsidMap;
            using (FileStream clsidMapStream = File.OpenRead(clsidMapPath))
            {
                clsidMap = JsonDocument.Parse(clsidMapStream).RootElement;
            }

            foreach (JsonProperty property in clsidMap.EnumerateObject())
            {
                string guidMaybe = property.Name;
                Guid guid = Guid.Parse(guidMaybe);
                XElement comClassElement = new XElement(ns + "comClass", new XAttribute("clsid", guid.ToString("B")), new XAttribute("threadingModel", "Both"));
                if (property.Value.TryGetProperty("progid", out JsonElement progIdValue))
                {
                    comClassElement.Add(new XAttribute("progid", progIdValue.GetString()));
                }

                fileElement.Add(comClassElement);
            }
            return fileElement;
        }

        private static void AddTypeLibElementsToFileElement(IReadOnlyDictionary<int, string> typeLibraries, XNamespace ns, XElement fileElement)
        {
            foreach (var typeLibrary in typeLibraries)
            {
                try
                {
                    byte[] tlbFileBytes = File.ReadAllBytes(typeLibrary.Value);
                    TypeLibReader reader = new TypeLibReader(tlbFileBytes);
                    if (!reader.TryReadTypeLibGuidAndVersion(out Guid name, out Version version))
                    {
                        throw new InvalidTypeLibraryException(typeLibrary.Value);
                    }
                    XElement typeLibElement = new XElement(ns + "typelib",
                        new XAttribute("tlbid", name.ToString("B")),
                        new XAttribute("resourceid", typeLibrary.Key),
                        new XAttribute("version", version),
                        new XAttribute("helpdir", ""));
                    fileElement.Add(typeLibElement);
                }
                catch (FileNotFoundException ex)
                {
                    throw new TypeLibraryDoesNotExistException(typeLibrary.Value, ex);
                }
            }
        }
    }
}
