// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Xml.XPath;
using ILCompiler.Dataflow;
using ILCompiler.DependencyAnalysis;

using ILLink.Shared;
using Internal.TypeSystem;
using DependencyList = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.DependencyList;

#nullable enable

namespace ILCompiler
{
    internal sealed class DescriptorMarker : ProcessLinkerXmlBase
    {
        private const string NamespaceElementName = "namespace";
        private const string _required = "required";
        private const string _preserve = "preserve";
        private const string _accessors = "accessors";
        private static readonly string[] _accessorsAll = new string[] { "all" };
        private static readonly char[] _accessorsSep = new char[] { ';' };

        private NodeFactory _factory;

        private DependencyList _dependencies = new DependencyList();
        public DependencyList Dependencies { get => _dependencies; }

        public DescriptorMarker(Logger logger, NodeFactory factory, Stream documentStream, string xmlDocumentLocation, IReadOnlyDictionary<string, bool> featureSwitchValues)
            : base(logger, factory.TypeSystemContext, documentStream, xmlDocumentLocation, featureSwitchValues)
        {
            _dependencies = new DependencyList();
            _factory = factory;
        }

        public DescriptorMarker(Logger logger, NodeFactory factory, Stream documentStream, ManifestResource resource, ModuleDesc resourceAssembly, string xmlDocumentLocation, IReadOnlyDictionary<string, bool> featureSwitchValues)
            : base(logger, factory.TypeSystemContext, documentStream, resource, resourceAssembly, xmlDocumentLocation, featureSwitchValues)
        {
            _factory = factory;
        }

        protected override AllowedAssemblies AllowedAssemblySelector { get => AllowedAssemblies.AnyAssembly; }

        protected override void ProcessAssembly(ModuleDesc assembly, XPathNavigator nav, bool warnOnUnresolvedTypes)
        {
            if (GetTypePreserve(nav) == TypePreserve.All)
            {
                foreach (MetadataType type in assembly.GetAllTypes())
                {
                    if (type.ContainingType is null)
                        MarkAndPreserveAll(type);
                }

                //foreach (var exportedType in assembly.MainModule.ExportedTypes)
                //    _context.MarkingHelpers.MarkExportedType(exportedType, assembly.MainModule, new DependencyInfo(DependencyKind.XmlDescriptor, assembly.MainModule), GetMessageOriginForPosition(nav));
            }
            else
            {
                ProcessTypes(assembly, nav, warnOnUnresolvedTypes);
                ProcessNamespaces(assembly, nav);
            }
        }

        private void ProcessNamespaces(ModuleDesc assembly, XPathNavigator nav)
        {
            foreach (XPathNavigator namespaceNav in nav.SelectChildren(NamespaceElementName, XmlNamespace))
            {
                if (!ShouldProcessElement(namespaceNav))
                    continue;

                string fullname = GetFullName(namespaceNav);
                bool foundMatch = false;
                foreach (MetadataType type in assembly.GetAllTypes())
                {
                    if (type.ContainingType is not null || !type.Namespace.StringEquals(fullname))
                        continue;

                    foundMatch = true;
                    MarkAndPreserveAll(type);
                }

                if (!foundMatch)
                {
#if !READYTORUN
                    LogWarning(namespaceNav, DiagnosticId.XmlCouldNotFindAnyTypeInNamespace, fullname);
#endif
                }
            }
        }

        private void MarkAndPreserveAll(MetadataType type)
        {
            RootingHelpers.TryAddDependenciesForReflectedType(_dependencies, _factory, type, "type kept due to descriptor");
            MarkAndPreserve(type, TypePreserve.All);

            foreach (MetadataType nested in type.GetNestedTypes())
                MarkAndPreserveAll(nested);
        }

        private void MarkAndPreserve(TypeDesc type, TypePreserve preserve)
        {
            const string Reason = "member was kept by a descriptor file";

            if (preserve is TypePreserve.All or TypePreserve.Methods)
            {
                foreach (MethodDesc method in type.GetMethods())
                    RootingHelpers.TryAddDependenciesForReflectedMethod(_dependencies, _factory, method, Reason);
            }

            if (preserve is TypePreserve.All or TypePreserve.Fields)
            {
                foreach (FieldDesc field in type.GetFields())
                    RootingHelpers.TryAddDependenciesForReflectedField(_dependencies, _factory, field, Reason);
            }

            if (preserve is TypePreserve.All)
            {
                foreach (DefType interfaceType in type.GetAllInterfaceImplementations(declaredOnly: true))
                    RootingHelpers.TryAddDependenciesForReflectedType(_dependencies, _factory, interfaceType, Reason);
            }
        }

#if false
        protected override TypeDesc? ProcessExportedType(ExportedType exported, ModuleDesc assembly, XPathNavigator nav)
        {
            _context.MarkingHelpers.MarkExportedType(exported, assembly.MainModule, new DependencyInfo(DependencyKind.XmlDescriptor, _xmlDocumentLocation), GetMessageOriginForPosition(nav));
            return base.ProcessExportedType(exported, assembly, nav);
        }
#endif

        protected override MetadataType? ProcessExportedType(MetadataType exported, ModuleDesc assembly, XPathNavigator nav)
        {
            // Rooting module metadata roots type forwarder metadata for all types in the module that are reflection visible.
            // (We don't track individual type forwarders right now.)
            // TODO-ILTRIM: add handling of type forwards
#if !ILTRIM
            _dependencies.Add(_factory.ModuleMetadata(assembly), "Type used through forwarder");
#endif
            return base.ProcessExportedType(exported, assembly, nav);
        }

        protected override void ProcessType(TypeDesc type, XPathNavigator nav)
        {
            Debug.Assert(ShouldProcessElement(nav));

            TypePreserve preserve = GetTypePreserve(nav);
            switch (preserve)
            {
                case TypePreserve.Fields when !type.GetFields().Any():
#if !READYTORUN
                    LogWarning(nav, DiagnosticId.TypeHasNoFieldsToPreserve, type.GetDisplayName());
#endif
                    break;

                case TypePreserve.Methods when !type.GetMethods().Any():
#if !READYTORUN
                    LogWarning(nav, DiagnosticId.TypeHasNoMethodsToPreserve, type.GetDisplayName());
#endif
                    break;

                case TypePreserve.Fields:
                case TypePreserve.Methods:
                case TypePreserve.All:
                    MarkAndPreserve(type, preserve);
                    break;
            }

            bool required = IsRequired(nav);
            ProcessTypeChildren(type, nav, required);

            if (!required)
                return;

            RootingHelpers.TryAddDependenciesForReflectedType(_dependencies, _factory, type, "member marked via descriptor");

#if false
            // Getting the dependencies of a nested type should mark the rest, this code is not needed
            if (type.IsNested)
            {
                var currentType = type;
                while (currentType.IsNested)
                {
                    var parent = currentType.DeclaringType;
                    _context.Annotations.Mark(parent, new DependencyInfo(DependencyKind.DeclaringType, currentType), GetMessageOriginForPosition(nav));
                    currentType = parent;
                }
            }
#endif
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

        protected override void ProcessField(TypeDesc type, FieldDesc field, XPathNavigator nav)
        {
            _dependencies.Add(_factory.ReflectedField(field), "field kept due to descriptor");
        }

        protected override void ProcessMethod(TypeDesc type, MethodDesc method, XPathNavigator nav, object? customData)
        {
            if (customData is bool required && !required)
            {
                //TODO: Add a conditional dependency if the type is used also mark the method
                _dependencies.Add(_factory.ReflectedMethod(method), "method kept due to descriptor");
            }
            else
            {
                _dependencies.Add(_factory.ReflectedMethod(method), "method kept due to descriptor");
            }
        }

        private void ProcessMethodIfNotNull(TypeDesc type, MethodDesc method, XPathNavigator nav, object? customData)
        {
            if (method == null)
                return;

            ProcessMethod(type, method, nav, customData);
        }

        protected override MethodDesc? GetMethod(TypeDesc type, string signature)
        {
            foreach (MethodDesc meth in type.GetAllMethods())
            {
                if (signature == GetMethodSignature(meth, false))
                    return meth;
            }
            return null;
        }

        protected override void ProcessEvent(TypeDesc type, EventPseudoDesc @event, XPathNavigator nav, object? customData)
        {
            ProcessMethod(type, @event.AddMethod, nav, customData);
            ProcessMethod(type, @event.RemoveMethod, nav, customData);
            ProcessMethodIfNotNull(type, @event.RaiseMethod, nav, customData);
        }

        protected override void ProcessProperty(TypeDesc type, PropertyPseudoDesc property, XPathNavigator nav, object? customData, bool fromSignature)
        {
            string[] accessors = fromSignature ? GetAccessors(nav) : _accessorsAll;

            if (Array.IndexOf(accessors, "all") >= 0)
            {
                ProcessMethodIfNotNull(type, property.GetMethod, nav, customData);
                ProcessMethodIfNotNull(type, property.SetMethod, nav, customData);
                return;
            }

            if (property.GetMethod != null && Array.IndexOf(accessors, "get") >= 0)
                ProcessMethod(type, property.GetMethod, nav, customData);
            else if (property.GetMethod == null)
            {
#if !READYTORUN
                LogWarning(nav, DiagnosticId.XmlCouldNotFindGetAccesorOfPropertyOnType, property.Name, type.GetDisplayName());
#endif
            }

            if (property.SetMethod != null && Array.IndexOf(accessors, "set") >= 0)
                ProcessMethod(type, property.SetMethod, nav, customData);
            else if (property.SetMethod == null)
            {
#if !READYTORUN
                LogWarning(nav, DiagnosticId.XmlCouldNotFindSetAccesorOfPropertyOnType, property.Name, type.GetDisplayName());
#endif
            }
        }

        private static bool IsRequired(XPathNavigator nav)
        {
            string attribute = GetAttribute(nav, _required);
            if (attribute == null || attribute.Length == 0)
                return true;

            return bool.TryParse(attribute, out bool result) && result;
        }

        private static string[] GetAccessors(XPathNavigator nav)
        {
            string accessorsValue = GetAttribute(nav, _accessors);

            if (accessorsValue != null)
            {
                string[] accessors = accessorsValue.Split(
                    _accessorsSep, StringSplitOptions.RemoveEmptyEntries);

                if (accessors.Length > 0)
                {
                    for (int i = 0; i < accessors.Length; ++i)
                        accessors[i] = accessors[i].ToLowerInvariant();

                    return accessors;
                }
            }
            return _accessorsAll;
        }

        public static DependencyList GetDependencies(Logger logger, NodeFactory factory, Stream documentStream, ManifestResource resource, ModuleDesc resourceAssembly, string xmlDocumentLocation, IReadOnlyDictionary<string, bool> featureSwitchValues)
        {
            var descriptor = new DescriptorMarker(logger, factory, documentStream, resource, resourceAssembly, xmlDocumentLocation, featureSwitchValues);
            descriptor.ProcessXml(false);
            return descriptor.Dependencies;
        }
    }
}
