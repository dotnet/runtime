// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Xml.Linq;

using Microsoft.DotNet.CoreSetup.Test;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.NET.HostModel.ComHost.Tests
{
    public class RegFreeComManifestTests
    {
        private static XNamespace regFreeComManifestNamespace = "urn:schemas-microsoft-com:asm.v1";

        [Fact]
        public void RegFreeComManifestCorrectlyIncludesComHostFile()
        {
            using TestArtifact directory = TestArtifact.Create(nameof(RegFreeComManifestCorrectlyIncludesComHostFile));
            JObject clsidMap = new JObject
            {
            };

            string clsidmapPath = Path.Combine(directory.Location, "test.clsidmap");
            string json = JsonConvert.SerializeObject(clsidMap);

            string comHostName = "comhost.dll";

            File.WriteAllText(clsidmapPath, json);

            string regFreeComManifestPath = Path.Combine(directory.Location, "test.manifest");

            RegFreeComManifest.CreateManifestFromClsidmap("assemblyName", comHostName, "1.0.0.0", clsidmapPath, regFreeComManifestPath);

            using FileStream manifestStream = File.OpenRead(regFreeComManifestPath);

            XElement manifest = XElement.Load(manifestStream);

            XElement fileElement = manifest.Element(regFreeComManifestNamespace + "file");

            Assert.NotNull(fileElement);
            Assert.Equal(comHostName, fileElement.Attribute("name").Value);
        }

        [Fact]
        public void EntryInClsidMapAddedToRegFreeComManifest()
        {
            using TestArtifact directory = TestArtifact.Create(nameof(EntryInClsidMapAddedToRegFreeComManifest));
            string guid = "{190f1974-fa98-4922-8ed4-cf748630abbe}";
            string assemblyName = "ComLibrary";
            string typeName = "ComLibrary.Server";
            string assemblyVersion = "1.0.0.0";
            JObject clsidMap = new JObject
            {
                {
                    guid,
                    new JObject() { {"assembly", assemblyName }, {"type", typeName } }
                }
            };

            string clsidmapPath = Path.Combine(directory.Location, "test.clsidmap");
            string json = JsonConvert.SerializeObject(clsidMap);
            string comHostName = "comhost.dll";

            File.WriteAllText(clsidmapPath, json);

            string regFreeComManifestPath = Path.Combine(directory.Location, "test.manifest");

            RegFreeComManifest.CreateManifestFromClsidmap(assemblyName, comHostName, assemblyVersion, clsidmapPath, regFreeComManifestPath);

            using FileStream manifestStream = File.OpenRead(regFreeComManifestPath);

            XElement manifest = XElement.Load(manifestStream);

            XElement fileElement = manifest.Element(regFreeComManifestNamespace + "file");

            Assert.Single(fileElement.Elements(regFreeComManifestNamespace + "comClass").Where(cls => cls.Attribute("clsid").Value == guid));
        }

        [Fact]
        public void EntryInClsidMapAddedToRegFreeComManifestIncludesProgId()
        {
            using TestArtifact directory = TestArtifact.Create(nameof(EntryInClsidMapAddedToRegFreeComManifestIncludesProgId));
            string guid = "{190f1974-fa98-4922-8ed4-cf748630abbe}";
            string assemblyName = "ComLibrary";
            string typeName = "ComLibrary.Server";
            string progId = "CustomProgId";
            string assemblyVersion = "1.0.0.0";
            JObject clsidMap = new JObject
            {
                {
                    guid,
                    new JObject() { {"assembly", assemblyName }, {"type", typeName }, { "progid", progId } }
                }
            };

            string clsidmapPath = Path.Combine(directory.Location, "test.clsidmap");
            string json = JsonConvert.SerializeObject(clsidMap);
            string comHostName = "comhost.dll";

            File.WriteAllText(clsidmapPath, json);

            string regFreeComManifestPath = Path.Combine(directory.Location, "test.manifest");

            RegFreeComManifest.CreateManifestFromClsidmap(assemblyName, comHostName, assemblyVersion, clsidmapPath, regFreeComManifestPath);

            using FileStream manifestStream = File.OpenRead(regFreeComManifestPath);

            XElement manifest = XElement.Load(manifestStream);

            XElement fileElement = manifest.Element(regFreeComManifestNamespace + "file");

            Assert.Single(fileElement.Elements(regFreeComManifestNamespace + "comClass").Where(cls => cls.Attribute("clsid").Value == guid && cls.Attribute("progid").Value == progId));
        }
    }
}
