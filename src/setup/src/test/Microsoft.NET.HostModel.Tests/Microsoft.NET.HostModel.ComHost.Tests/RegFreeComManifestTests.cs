﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Xunit;

namespace Microsoft.NET.HostModel.ComHost.Tests
{
    public class RegFreeComManifestTests
    {
        private static XNamespace regFreeComManifestNamespace = "urn:schemas-microsoft-com:asm.v1";

        [Fact]
        public void RegFreeComManifestCorrectlyIncludesComHostFile()
        {
            using TestDirectory directory = TestDirectory.Create();
            JObject clsidMap = new JObject
            {
            };

            string clsidmapPath = Path.Combine(directory.Path, "test.clsidmap");
            string json = JsonConvert.SerializeObject(clsidMap);

            string comHostName = "comhost.dll";

            File.WriteAllText(clsidmapPath, json);

            string regFreeComManifestPath = Path.Combine(directory.Path, "test.manifest");

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
            using TestDirectory directory = TestDirectory.Create();
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

            string clsidmapPath = Path.Combine(directory.Path, "test.clsidmap");
            string json = JsonConvert.SerializeObject(clsidMap);
            string comHostName = "comhost.dll";

            File.WriteAllText(clsidmapPath, json);

            string regFreeComManifestPath = Path.Combine(directory.Path, "test.manifest");

            RegFreeComManifest.CreateManifestFromClsidmap(assemblyName, comHostName, assemblyVersion, clsidmapPath, regFreeComManifestPath);

            using FileStream manifestStream = File.OpenRead(regFreeComManifestPath);

            XElement manifest = XElement.Load(manifestStream);

            XElement fileElement = manifest.Element(regFreeComManifestNamespace + "file");

            Assert.Single(fileElement.Elements(regFreeComManifestNamespace + "comClass").Where(cls => cls.Attribute("clsid").Value == guid));
        }

        [Fact]
        public void EntryInClsidMapAddedToRegFreeComManifestIncludesProgId()
        {
            using TestDirectory directory = TestDirectory.Create();
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

            string clsidmapPath = Path.Combine(directory.Path, "test.clsidmap");
            string json = JsonConvert.SerializeObject(clsidMap);
            string comHostName = "comhost.dll";

            File.WriteAllText(clsidmapPath, json);

            string regFreeComManifestPath = Path.Combine(directory.Path, "test.manifest");

            RegFreeComManifest.CreateManifestFromClsidmap(assemblyName, comHostName, assemblyVersion, clsidmapPath, regFreeComManifestPath);

            using FileStream manifestStream = File.OpenRead(regFreeComManifestPath);

            XElement manifest = XElement.Load(manifestStream);

            XElement fileElement = manifest.Element(regFreeComManifestNamespace + "file");

            Assert.Single(fileElement.Elements(regFreeComManifestNamespace + "comClass").Where(cls => cls.Attribute("clsid").Value == guid && cls.Attribute("progid").Value == progId));
        }
    }
}
