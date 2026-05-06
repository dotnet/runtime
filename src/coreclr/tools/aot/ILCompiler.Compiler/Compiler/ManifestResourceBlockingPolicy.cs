// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Xml;
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

        public ManifestResourceBlockingPolicy(Logger logger, IReadOnlyDictionary<string, bool> switchValues, IReadOnlyDictionary<ModuleDesc, IReadOnlySet<string>> globalBlocks)
        {
            _hashtable = new FeatureSwitchHashtable(logger, switchValues, globalBlocks);
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

        public static IReadOnlyDictionary<ModuleDesc, IReadOnlySet<string>> UnionBlockings(IReadOnlyDictionary<ModuleDesc, IReadOnlySet<string>> left, IReadOnlyDictionary<ModuleDesc, IReadOnlySet<string>> right)
        {
            var result = new Dictionary<ModuleDesc, HashSet<string>>();
            if (left != null)
                AddAll(result, left);
            if (right != null)
                AddAll(result, right);

            return AsReadOnlyDictionary(result);

            static void AddAll(Dictionary<ModuleDesc, HashSet<string>> result, IReadOnlyDictionary<ModuleDesc, IReadOnlySet<string>> addend)
            {
                foreach (var item in addend)
                {
                    if (!result.TryGetValue(item.Key, out HashSet<string> set))
                        result.Add(item.Key, set = new HashSet<string>());
                    set.UnionWith(item.Value);
                }
            }
        }

        private static Dictionary<ModuleDesc, IReadOnlySet<string>> AsReadOnlyDictionary(Dictionary<ModuleDesc, HashSet<string>> original)
        {
            // IReadOnlyDictionary is not variant, so we need to:
            var copy = new Dictionary<ModuleDesc, IReadOnlySet<string>>();
            foreach (KeyValuePair<ModuleDesc, HashSet<string>> moduleSet in original)
                copy.Add(moduleSet.Key, moduleSet.Value);
            return copy;
        }

        private sealed class FeatureSwitchHashtable : LockFreeReaderHashtable<EcmaModule, AssemblyFeatureInfo>
        {
            private readonly IReadOnlyDictionary<string, bool> _switchValues;
            private readonly IReadOnlyDictionary<ModuleDesc, IReadOnlySet<string>> _globalBlocks;
            private readonly Logger _logger;

            public FeatureSwitchHashtable(Logger logger, IReadOnlyDictionary<string, bool> switchValues, IReadOnlyDictionary<ModuleDesc, IReadOnlySet<string>> globalBlocks)
            {
                _logger = logger;
                _switchValues = switchValues;
                _globalBlocks = globalBlocks;
            }

            protected override bool CompareKeyToValue(EcmaModule key, AssemblyFeatureInfo value) => key == value.Module;
            protected override bool CompareValueToValue(AssemblyFeatureInfo value1, AssemblyFeatureInfo value2) => value1.Module == value2.Module;
            protected override int GetKeyHashCode(EcmaModule key) => key.GetHashCode();
            protected override int GetValueHashCode(AssemblyFeatureInfo value) => value.Module.GetHashCode();

            protected override AssemblyFeatureInfo CreateValueFromKey(EcmaModule key)
            {
                return new AssemblyFeatureInfo(key, _logger, _switchValues, _globalBlocks);
            }
        }

        private sealed class AssemblyFeatureInfo
        {
            public EcmaModule Module { get; }

            public IReadOnlySet<string> BlockedResources { get; }

            public AssemblyFeatureInfo(EcmaModule module, Logger logger, IReadOnlyDictionary<string, bool> featureSwitchValues, IReadOnlyDictionary<ModuleDesc, IReadOnlySet<string>> globalBlocks)
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

                        BlockedResources = SubstitutionsReader.GetSubstitutions(logger, module.Context, ms, resource, module, "resource " + resourceName + " in " + module.ToString(), featureSwitchValues)[module];
                    }
                }

                if (globalBlocks != null && globalBlocks.TryGetValue(module, out IReadOnlySet<string> fromGlobal))
                {
                    var result = new HashSet<string>(fromGlobal);
                    result.UnionWith(BlockedResources);
                    BlockedResources = result;
                }
            }
        }

        public sealed class SubstitutionsReader : ProcessLinkerXmlBase
        {
            private readonly Dictionary<ModuleDesc, HashSet<string>> _substitutions = new();

            private SubstitutionsReader(Logger logger, TypeSystemContext context, Stream documentStream, ManifestResource resource, ModuleDesc resourceAssembly, string xmlDocumentLocation, IReadOnlyDictionary<string, bool> featureSwitchValues)
                : base(logger, context, documentStream, resource, resourceAssembly, xmlDocumentLocation, featureSwitchValues)
            {
                _substitutions.Add(resourceAssembly, new HashSet<string>());
            }

            private SubstitutionsReader(Logger logger, TypeSystemContext context, XmlReader document, string xmlDocumentLocation, IReadOnlyDictionary<string, bool> featureSwitchValues)
                : base(logger, context, document, xmlDocumentLocation, featureSwitchValues)
            {
            }

            public static IReadOnlyDictionary<ModuleDesc, IReadOnlySet<string>> GetSubstitutions(Logger logger, TypeSystemContext context, Stream documentStream, ManifestResource resource, ModuleDesc resourceAssembly, string xmlDocumentLocation, IReadOnlyDictionary<string, bool> featureSwitchValues)
            {
                var rdr = new SubstitutionsReader(logger, context, documentStream, resource, resourceAssembly, xmlDocumentLocation, featureSwitchValues);
                rdr.ProcessXml(false);
                return AsReadOnlyDictionary(rdr._substitutions);
            }

            public static IReadOnlyDictionary<ModuleDesc, IReadOnlySet<string>> GetSubstitutions(Logger logger, TypeSystemContext context, XmlReader document, string xmlDocumentLocation, IReadOnlyDictionary<string, bool> featureSwitchValues)
            {
                var rdr = new SubstitutionsReader(logger, context, document, xmlDocumentLocation, featureSwitchValues);
                rdr.ProcessXml(false);
                return AsReadOnlyDictionary(rdr._substitutions);
            }

            private void ProcessResources(ModuleDesc assembly, XPathNavigator nav)
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

                    if (!_substitutions.TryGetValue(assembly, out HashSet<string> removed))
                        _substitutions.Add(assembly, removed = new HashSet<string>());
                    removed.Add(name);
                }
            }

            protected override void ProcessAssembly(ModuleDesc assembly, XPathNavigator nav, bool warnOnUnresolvedTypes)
            {
                ProcessResources(assembly, nav);
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
