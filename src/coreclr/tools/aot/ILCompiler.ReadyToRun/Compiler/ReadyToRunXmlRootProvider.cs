// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using System.Xml.XPath;
using Internal.JitInterface;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    public class ReadyToRunXmlRootProvider : ICompilationRootProvider
    {
        private readonly TypeSystemContext _context;
        private readonly Stream _documentStream;
        private readonly ManifestResource _resource;
        private readonly ModuleDesc _owningModule;
        private readonly string _xmlDocumentLocation;

        public ReadyToRunXmlRootProvider(Stream documentStream, ManifestResource resource, ModuleDesc owningModule, string xmlDocumentLocation)
        {
            _context = owningModule.Context;
            _documentStream = documentStream;
            _resource = resource;
            _owningModule = owningModule;
            _xmlDocumentLocation = xmlDocumentLocation;
        }

        public void AddCompilationRoots(IRootingServiceProvider rootProvider)
        {
            CompilationRootProvider root = new CompilationRootProvider(rootProvider, _context, _documentStream, _resource, _owningModule, _xmlDocumentLocation);
            root.ProcessXml();
        }

        public static bool TryCreateRootProviderFromEmbeddedDescriptorFile(EcmaModule module, out ReadyToRunXmlRootProvider provider)
        {
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
                if (resourceName == "ILLink.Descriptors.xml")
                {
                    BlobReader reader = resourceDirectory.GetReader((int)resource.Offset, resourceDirectory.Length - (int)resource.Offset);
                    int length = (int)reader.ReadUInt32();

                    UnmanagedMemoryStream ms;
                    unsafe
                    {
                        ms = new UnmanagedMemoryStream(reader.CurrentPointer, length);
                    }

                    provider = new ReadyToRunXmlRootProvider(ms, resource, module, "resource " + resourceName + " in " + module.ToString());
                    return true;
                }
            }
            provider = null;
            return false;
        }

        private class CompilationRootProvider : ProcessLinkerXmlBase
        {
            private const string NamespaceElementName = "namespace";
            private const string _preserve = "preserve";
            private readonly IRootingServiceProvider _rootingServiceProvider;
            private InstructionSetSupport _instructionSetSupport;

            public CompilationRootProvider(IRootingServiceProvider provider, TypeSystemContext context, Stream documentStream, ManifestResource resource, ModuleDesc owningModule, string xmlDocumentLocation)
                : base(null , context, documentStream, resource, owningModule, xmlDocumentLocation, ImmutableDictionary<string, bool>.Empty)
            {
                _rootingServiceProvider = provider;
                _instructionSetSupport = ((ReadyToRunCompilerContext)owningModule.Context).InstructionSetSupport;
            }

            public void ProcessXml() => ProcessXml(false);

            protected override void ProcessAssembly(ModuleDesc assembly, XPathNavigator nav, bool warnOnUnresolvedTypes)
            {
                if (GetTypePreserve(nav) == TypePreserve.All)
                {
                    foreach (var type in assembly.GetAllTypes())
                    {
                        PreserveMethodsOnType(type);
                    }
                }
                else
                {
                    ProcessTypes(assembly, nav, warnOnUnresolvedTypes);
                    ProcessNamespaces(assembly, nav);
                }
            }

            private void PreserveMethodsOnType(TypeDesc type)
            {
                MetadataType typeWithMethods = (MetadataType)type;
                if (type.HasInstantiation)
                {
                    typeWithMethods = ReadyToRunLibraryRootProvider.InstantiateIfPossible(typeWithMethods);
                    if (typeWithMethods == null)
                        return;
                }

                foreach (MethodDesc method in typeWithMethods.GetAllMethods())
                {
                    RootMethod(method);
                }
            }

            private void RootMethod(MethodDesc method)
            {
                // Skip methods with no IL
                if (method.IsAbstract)
                    return;

                if (method.IsInternalCall)
                    return;

                MethodDesc methodToRoot = method;
                if (method.HasInstantiation)
                {
                    methodToRoot = ReadyToRunLibraryRootProvider.InstantiateIfPossible(method);

                    if (methodToRoot == null)
                        return;
                }

                try
                {
                    if (!CorInfoImpl.ShouldSkipCompilation(_instructionSetSupport, method))
                    {
                        ReadyToRunLibraryRootProvider.CheckCanGenerateMethod(methodToRoot);
                        _rootingServiceProvider.AddCompilationRoot(methodToRoot, rootMinimalDependencies: false, reason: "Linker XML descriptor");
                    }
                }
                catch (TypeSystemException)
                {
                    // Individual methods can fail to load types referenced in their signatures.
                    // Skip them in library mode since they're not going to be callable.
                    return;
                }
            }

            private void ProcessNamespaces(ModuleDesc assembly, XPathNavigator nav)
            {
                foreach (XPathNavigator namespaceNav in nav.SelectChildren(NamespaceElementName, XmlNamespace))
                {
                    if (!ShouldProcessElement(namespaceNav))
                        continue;

                    string fullname = GetFullName(namespaceNav);
                    foreach (DefType type in assembly.GetAllTypes())
                    {
                        if (type.Namespace != fullname)
                            continue;

                        ProcessType(type, nav);
                    }
                }
            }

            protected override void ProcessType(TypeDesc type, XPathNavigator nav)
            {
                TypePreserve preserve = GetTypePreserve(nav);
                if (preserve == TypePreserve.All || preserve == TypePreserve.Methods)
                {
                    PreserveMethodsOnType(type);
                }
                else
                {
                    ProcessTypeChildren(type, nav);
                }
            }

            protected override void ProcessMethod(TypeDesc type, MethodDesc method, XPathNavigator nav, object customData)
            {
                MetadataType typeWithMethods = (MetadataType)type;
                if (type.HasInstantiation)
                {
                    InstantiatedType instantiated = ReadyToRunLibraryRootProvider.InstantiateIfPossible(typeWithMethods);
                    method = method.Context.GetMethodForInstantiatedType(method, instantiated);
                }

                RootMethod(method);
            }

            private static TypePreserve GetTypePreserve(XPathNavigator nav)
            {
                string attribute = GetAttribute(nav, _preserve);
                if (string.IsNullOrEmpty(attribute))
                    return nav.HasChildren ? TypePreserve.Nothing : TypePreserve.All;

                if (Enum.TryParse(attribute, true, out TypePreserve result))
                    return result;
                return TypePreserve.Nothing;
            }
        }
    }
}
