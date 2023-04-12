// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Xml.XPath;

using Internal.TypeSystem;

using EcmaModule = Internal.TypeSystem.Ecma.EcmaModule;

namespace ILCompiler
{
    /// <summary>
    /// Represents a manifest resource blocking policy. The policy dictates whether manifest resources should
    /// be generated into the executable.
    /// </summary>
    public class ManifestResourceBlockingPolicy
    {
        private readonly FeatureSwitchHashtable _hashtable;

        protected ManifestResourceBlockingPolicy() { }

        public ManifestResourceBlockingPolicy(Logger logger, IEnumerable<KeyValuePair<string, bool>> switchValues)
        {
            _hashtable = new FeatureSwitchHashtable(logger, new Dictionary<string, bool>(switchValues));
        }

        /// <summary>
        /// Returns true if manifest resource with name '<paramref name="resourceName"/>' in module '<paramref name="module"/>'
        /// is reflection blocked.
        /// </summary>
        public virtual bool IsManifestResourceBlocked(ModuleDesc module, string resourceName)
        {
            return module is EcmaModule ecmaModule &&
                (_hashtable.GetOrCreateValue(ecmaModule).BlockedResources.Contains(resourceName)
                || (resourceName.StartsWith("ILLink.") && resourceName.EndsWith(".xml")));
        }

        private sealed class FeatureSwitchHashtable : LockFreeReaderHashtable<EcmaModule, AssemblyFeatureInfo>
        {
            private readonly Dictionary<string, bool> _switchValues;
            private readonly Logger _logger;

            public FeatureSwitchHashtable(Logger logger, Dictionary<string, bool> switchValues)
            {
                _logger = logger;
                _switchValues = switchValues;
            }

            protected override bool CompareKeyToValue(EcmaModule key, AssemblyFeatureInfo value) => key == value.Module;
            protected override bool CompareValueToValue(AssemblyFeatureInfo value1, AssemblyFeatureInfo value2) => value1.Module == value2.Module;
            protected override int GetKeyHashCode(EcmaModule key) => key.GetHashCode();
            protected override int GetValueHashCode(AssemblyFeatureInfo value) => value.Module.GetHashCode();

            protected override AssemblyFeatureInfo CreateValueFromKey(EcmaModule key)
            {
                return new AssemblyFeatureInfo(key, _logger, _switchValues);
            }
        }

        private sealed class AssemblyFeatureInfo
        {
            public EcmaModule Module { get; }

            public HashSet<string> BlockedResources { get; }

            public AssemblyFeatureInfo(EcmaModule module, Logger logger, IReadOnlyDictionary<string, bool> featureSwitchValues)
            {
                Module = module;
                BlockedResources = new HashSet<string>();

                PEMemoryBlock resourceDirectory = module.PEReader.GetSectionData(module.PEReader.PEHeaders.CorHeader.ResourcesDirectory.RelativeVirtualAddress);

                foreach (var resourceHandle in module.MetadataReader.ManifestResources)
                {
                    ManifestResource resource = module.MetadataReader.GetManifestResource(resourceHandle);

                    // Don't try to process linked resources or resources in other assemblies
                    if (!resource.Implementation.IsNil)
                    {
                        continue;
                    }

                    string resourceName = module.MetadataReader.GetString(resource.Name);
                    if (resourceName == "ILLink.Substitutions.xml")
                    {
                        BlobReader reader = resourceDirectory.GetReader((int)resource.Offset, resourceDirectory.Length - (int)resource.Offset);
                        int length = (int)reader.ReadUInt32();

                        UnmanagedMemoryStream ms;
                        unsafe
                        {
                            ms = new UnmanagedMemoryStream(reader.CurrentPointer, length);
                        }

                        BlockedResources = SubstitutionsReader.GetSubstitutions(logger, module.Context, ms, resource, module, "resource " + resourceName + " in " + module.ToString(), featureSwitchValues);
                    }
                }
            }
        }

        private sealed class SubstitutionsReader : ProcessLinkerXmlBase
        {
            private readonly HashSet<string> _substitutions = new();

            private SubstitutionsReader(Logger logger, TypeSystemContext context, Stream documentStream, ManifestResource resource, ModuleDesc resourceAssembly, string xmlDocumentLocation, IReadOnlyDictionary<string, bool> featureSwitchValues)
                : base(logger, context, documentStream, resource, resourceAssembly, xmlDocumentLocation, featureSwitchValues)
            {
            }

            public static HashSet<string> GetSubstitutions(Logger logger, TypeSystemContext context, Stream documentStream, ManifestResource resource, ModuleDesc resourceAssembly, string xmlDocumentLocation, IReadOnlyDictionary<string, bool> featureSwitchValues)
            {
                var rdr = new SubstitutionsReader(logger, context, documentStream, resource, resourceAssembly, xmlDocumentLocation, featureSwitchValues);
                rdr.ProcessXml(false);
                return rdr._substitutions;
            }

            private void ProcessResources(XPathNavigator nav)
            {
                foreach (XPathNavigator resourceNav in nav.SelectChildren("resource", ""))
                {
                    if (!ShouldProcessElement(resourceNav))
                        continue;

                    string name = GetAttribute(resourceNav, "name");
                    if (string.IsNullOrEmpty(name))
                    {
                        //LogWarning(resourceNav, DiagnosticId.XmlMissingNameAttributeInResource);
                        continue;
                    }

                    string action = GetAttribute(resourceNav, "action");
                    if (action != "remove")
                    {
                        //LogWarning(resourceNav, DiagnosticId.XmlInvalidValueForAttributeActionForResource, action, name);
                        continue;
                    }

                    _substitutions.Add(name);
                }
            }

            protected override void ProcessAssembly(ModuleDesc assembly, XPathNavigator nav, bool warnOnUnresolvedTypes)
            {
                ProcessResources(nav);
            }

            // Should not be resolving types. That's useless work.
            protected override void ProcessType(TypeDesc type, XPathNavigator nav) => throw new System.NotImplementedException();
        }
    }

    public class FullyBlockedManifestResourceBlockingPolicy : ManifestResourceBlockingPolicy
    {
        public override bool IsManifestResourceBlocked(ModuleDesc module, string resourceName)
        {
            return true;
        }
    }
}
