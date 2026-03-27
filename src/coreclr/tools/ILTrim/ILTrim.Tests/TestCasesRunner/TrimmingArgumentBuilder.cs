// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Linker.Tests.Extensions;

namespace Mono.Linker.Tests.TestCasesRunner
{
    public class TrimmingArgumentBuilder
    {
        private readonly TrimmerOptions _options = new();
        private readonly TestCaseMetadataProvider _metadataProvider;
        private readonly HashSet<string> _linkAssemblyNames = new();

        public TrimmingArgumentBuilder(TestCaseMetadataProvider metadataProvider)
        {
            _metadataProvider = metadataProvider;
        }

        public TrimmerOptions Build()
        {
            return _options;
        }

        public virtual void AddSearchDirectory(NPath directory)
        {
        }

        public virtual void AddReference(NPath path)
        {
            var pathStr = path.ToString();
            // Don't add to references if already in link assemblies (Trimmer.TrimAssembly combines both into a single dictionary)
            if (_options.AdditionalLinkAssemblies.Contains(pathStr))
                return;
            if (!_options.ReferencePaths.Contains(pathStr))
                _options.ReferencePaths.Add(pathStr);
        }

        public virtual void AddOutputDirectory(NPath directory)
        {
            _options.OutputDirectory = directory.ToString();
        }

        public virtual void AddLinkXmlFile(string file)
        {
        }

        public virtual void AddResponseFile(NPath path)
        {
        }

        public virtual void AddTrimMode(string value)
        {
        }

        public virtual void AddDefaultAction(string value)
        {
        }

        public virtual void AddLinkAssembly(string fileName)
        {
            if (!_options.AdditionalLinkAssemblies.Contains(fileName))
                _options.AdditionalLinkAssemblies.Add(fileName);
            // Remove from references to avoid duplicate key in Trimmer.TrimAssembly
            _options.ReferencePaths.Remove(fileName);
        }

        public virtual void LinkFromAssembly(string fileName)
        {
            _options.InputPath = fileName;
            _options.AdditionalLinkAssemblies.Remove(fileName);
        }

        public virtual void LinkFromPublicAndFamily(string fileName)
        {
        }

        public virtual void IgnoreDescriptors(bool value)
        {
        }

        public virtual void IgnoreSubstitutions(bool value)
        {
        }

        public virtual void IgnoreLinkAttributes(bool value)
        {
        }

        public virtual void AddIl8n(string value)
        {
        }

        public virtual void AddLinkSymbols(string value)
        {
        }

        public virtual void AddAssemblyAction(string action, string assembly)
        {
            if (action == "link")
            {
                _linkAssemblyNames.Add(assembly);
                var matchingRef = _options.ReferencePaths.FirstOrDefault(r => Path.GetFileNameWithoutExtension(r) == assembly);
                if (matchingRef is not null && !_options.AdditionalLinkAssemblies.Contains(matchingRef))
                {
                    _options.AdditionalLinkAssemblies.Add(matchingRef);
                    _options.ReferencePaths.Remove(matchingRef);
                }
            }
        }

        public virtual void AddSkipUnresolved(bool skipUnresolved)
        {
        }

        public virtual void AddStripDescriptors(bool stripDescriptors)
        {
        }

        public virtual void AddStripSubstitutions(bool stripSubstitutions)
        {
        }

        public virtual void AddStripLinkAttributes(bool stripLinkAttributes)
        {
        }

        public virtual void AddSubstitutions(string file)
        {
        }

        public virtual void AddLinkAttributes(string file)
        {
        }

        public virtual void AddAdditionalArgument(string flag, string[] values)
        {
            if (flag == "-a" && values.Contains("library"))
                _options.IsLibraryMode = true;
            else if (flag == "--feature")
            {
                _options.FeatureSwitches.Add(values[0], bool.Parse(values[1]));
            }
        }

        public virtual void ProcessTestInputAssembly(NPath inputAssemblyPath)
        {
            if (_metadataProvider.LinkPublicAndFamily())
                LinkFromPublicAndFamily(inputAssemblyPath.ToString());
            else
                LinkFromAssembly(inputAssemblyPath.ToString());
        }

        public virtual void ProcessOptions(TestCaseLinkerOptions options)
        {
            if (options.TrimMode != null)
                AddTrimMode(options.TrimMode);

            if (options.DefaultAssembliesAction != null)
                AddDefaultAction(options.DefaultAssembliesAction);

            if (options.AssembliesAction != null)
            {
                foreach (var entry in options.AssembliesAction)
                    AddAssemblyAction(entry.Key, entry.Value);
            }

            IgnoreDescriptors(options.IgnoreDescriptors);
            IgnoreSubstitutions(options.IgnoreSubstitutions);
            IgnoreLinkAttributes(options.IgnoreLinkAttributes);

#if !NETCOREAPP
            if (!string.IsNullOrEmpty(options.Il8n))
                AddIl8n(options.Il8n);
#endif

            if (!string.IsNullOrEmpty(options.LinkSymbols))
                AddLinkSymbols(options.LinkSymbols);

            AddSkipUnresolved(options.SkipUnresolved);
            AddStripDescriptors(options.StripDescriptors);
            AddStripSubstitutions(options.StripSubstitutions);
            AddStripLinkAttributes(options.StripLinkAttributes);

            foreach (var descriptor in options.Descriptors)
                AddLinkXmlFile(descriptor);

            foreach (var substitutions in options.Substitutions)
                AddSubstitutions(substitutions);

            foreach (var attributeDefinition in options.LinkAttributes)
                AddLinkAttributes(attributeDefinition);

            AddAdditionalArgument("--disable-opt", new[] { "ipconstprop" });

            foreach (var additionalArgument in options.AdditionalArguments)
                AddAdditionalArgument(additionalArgument.Key, additionalArgument.Value);
        }
    }
}
