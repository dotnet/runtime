// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml.XPath;
using ILLink.Shared;

using Mono.Cecil;

namespace Mono.Linker.Steps
{
    public class DescriptorMarker : ProcessLinkerXmlBase
    {
        const string NamespaceElementName = "namespace";

        const string _required = "required";
        const string _preserve = "preserve";
        const string _accessors = "accessors";

        static readonly string[] _accessorsAll = new string[] { "all" };
        static readonly char[] _accessorsSep = new char[] { ';' };

        public DescriptorMarker(LinkContext context, Stream documentStream, string xmlDocumentLocation)
            : base(context, documentStream, xmlDocumentLocation)
        {
        }

        public DescriptorMarker(LinkContext context, Stream documentStream, EmbeddedResource resource, AssemblyDefinition resourceAssembly, string xmlDocumentLocation = "<unspecified>")
            : base(context, documentStream, resource, resourceAssembly, xmlDocumentLocation)
        {
        }

        public void Mark()
        {
            bool stripDescriptors = _context.IsOptimizationEnabled(CodeOptimizations.RemoveDescriptors, _resource?.Assembly);
            ProcessXml(stripDescriptors, _context.IgnoreDescriptors);
        }

        protected override AllowedAssemblies AllowedAssemblySelector { get => AllowedAssemblies.AnyAssembly; }

        protected override void ProcessAssembly(AssemblyDefinition assembly, XPathNavigator nav, bool warnOnUnresolvedTypes)
        {
            if (GetTypePreserve(nav) == TypePreserve.All)
            {
                foreach (var type in assembly.MainModule.Types)
                    MarkAndPreserveAll(type, nav);

                foreach (var exportedType in assembly.MainModule.ExportedTypes)
                    _context.MarkingHelpers.MarkExportedType(exportedType, assembly.MainModule, new DependencyInfo(DependencyKind.XmlDescriptor, assembly.MainModule), GetMessageOriginForPosition(nav));
            }
            else
            {
                ProcessTypes(assembly, nav, warnOnUnresolvedTypes);
                ProcessNamespaces(assembly, nav);
            }
        }

        void ProcessNamespaces(AssemblyDefinition assembly, XPathNavigator nav)
        {
            foreach (XPathNavigator namespaceNav in nav.SelectChildren(NamespaceElementName, XmlNamespace))
            {
                if (!ShouldProcessElement(namespaceNav))
                    continue;

                string fullname = GetFullName(namespaceNav);
                bool foundMatch = false;
                foreach (TypeDefinition type in assembly.MainModule.Types)
                {
                    if (type.Namespace != fullname)
                        continue;

                    foundMatch = true;
                    MarkAndPreserveAll(type, nav);
                }

                if (!foundMatch)
                {
                    LogWarning(namespaceNav, DiagnosticId.XmlCouldNotFindAnyTypeInNamespace, fullname);
                }
            }
        }

        void MarkAndPreserveAll(TypeDefinition type, XPathNavigator nav)
        {
            _context.Annotations.Mark(type, new DependencyInfo(DependencyKind.XmlDescriptor, _xmlDocumentLocation), GetMessageOriginForPosition(nav));
            _context.Annotations.SetPreserve(type, TypePreserve.All);

            if (!type.HasNestedTypes)
                return;

            foreach (TypeDefinition nested in type.NestedTypes)
                MarkAndPreserveAll(nested, nav);
        }

        protected override TypeDefinition? ProcessExportedType(ExportedType exported, AssemblyDefinition assembly, XPathNavigator nav)
        {
            _context.MarkingHelpers.MarkExportedType(exported, assembly.MainModule, new DependencyInfo(DependencyKind.XmlDescriptor, _xmlDocumentLocation), GetMessageOriginForPosition(nav));
            return base.ProcessExportedType(exported, assembly, nav);
        }

        protected override void ProcessType(TypeDefinition type, XPathNavigator nav)
        {
            Debug.Assert(ShouldProcessElement(nav));

            TypePreserve preserve = GetTypePreserve(nav);
            switch (preserve)
            {
                case TypePreserve.Fields when !type.HasFields:
                    LogWarning(nav, DiagnosticId.TypeHasNoFieldsToPreserve, type.GetDisplayName());
                    break;

                case TypePreserve.Methods when !type.HasMethods:
                    LogWarning(nav, DiagnosticId.TypeHasNoMethodsToPreserve, type.GetDisplayName());
                    break;

                case TypePreserve.Fields:
                case TypePreserve.Methods:
                case TypePreserve.All:
                    _context.Annotations.SetPreserve(type, preserve);
                    break;
            }

            bool required = IsRequired(nav);
            ProcessTypeChildren(type, nav, required);

            if (!required)
                return;

            _context.Annotations.Mark(type, new DependencyInfo(DependencyKind.XmlDescriptor, _xmlDocumentLocation), GetMessageOriginForPosition(nav));

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
        }

        static TypePreserve GetTypePreserve(XPathNavigator nav)
        {
            string attribute = GetAttribute(nav, _preserve);
            if (string.IsNullOrEmpty(attribute))
                return nav.HasChildren ? TypePreserve.Nothing : TypePreserve.All;

            if (Enum.TryParse(attribute, true, out TypePreserve result))
                return result;
            return TypePreserve.Nothing;
        }

        protected override void ProcessField(TypeDefinition type, FieldDefinition field, XPathNavigator nav)
        {
            if (_context.Annotations.IsMarked(field))
                LogWarning(nav, DiagnosticId.XmlDuplicatePreserveMember, field.FullName);

            _context.Annotations.Mark(field, new DependencyInfo(DependencyKind.XmlDescriptor, _xmlDocumentLocation), GetMessageOriginForPosition(nav));
        }

        protected override void ProcessMethod(TypeDefinition type, MethodDefinition method, XPathNavigator nav, object? customData)
        {
            if (_context.Annotations.IsMarked(method))
                LogWarning(nav, DiagnosticId.XmlDuplicatePreserveMember, method.GetDisplayName());

            _context.Annotations.MarkIndirectlyCalledMethod(method);
            _context.Annotations.SetAction(method, MethodAction.Parse);

            if (customData is bool required && !required)
            {
                _context.Annotations.AddPreservedMethod(type, method);
            }
            else
            {
                _context.Annotations.Mark(method, new DependencyInfo(DependencyKind.XmlDescriptor, _xmlDocumentLocation), GetMessageOriginForPosition(nav));
            }
        }

        void ProcessMethodIfNotNull(TypeDefinition type, MethodDefinition method, XPathNavigator nav, object? customData)
        {
            if (method == null)
                return;

            ProcessMethod(type, method, nav, customData);
        }

        protected override MethodDefinition? GetMethod(TypeDefinition type, string signature)
        {
            if (type.HasMethods)
                foreach (MethodDefinition meth in type.Methods)
                    if (signature == GetMethodSignature(meth, false))
                        return meth;

            return null;
        }

        public static string GetMethodSignature(MethodDefinition meth, bool includeGenericParameters)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(meth.ReturnType.FullName);
            sb.Append(" ");
            sb.Append(meth.Name);
            if (includeGenericParameters && meth.HasGenericParameters)
            {
                sb.Append("`");
                sb.Append(meth.GenericParameters.Count);
            }

            sb.Append("(");
            if (meth.HasParameters)
            {
                for (int i = 0; i < meth.Parameters.Count; i++)
                {
                    if (i > 0)
                        sb.Append(",");

                    sb.Append(meth.Parameters[i].ParameterType.FullName);
                }
            }
            sb.Append(")");
            return sb.ToString();
        }

        protected override void ProcessEvent(TypeDefinition type, EventDefinition @event, XPathNavigator nav, object? customData)
        {
            if (_context.Annotations.IsMarked(@event))
                LogWarning(nav, DiagnosticId.XmlDuplicatePreserveMember, @event.FullName);

            ProcessMethod(type, @event.AddMethod, nav, customData);
            ProcessMethod(type, @event.RemoveMethod, nav, customData);
            ProcessMethodIfNotNull(type, @event.InvokeMethod, nav, customData);
        }

        protected override void ProcessProperty(TypeDefinition type, PropertyDefinition property, XPathNavigator nav, object? customData, bool fromSignature)
        {
            string[] accessors = fromSignature ? GetAccessors(nav) : _accessorsAll;

            if (_context.Annotations.IsMarked(property))
                LogWarning(nav, DiagnosticId.XmlDuplicatePreserveMember, property.FullName);

            if (Array.IndexOf(accessors, "all") >= 0)
            {
                ProcessMethodIfNotNull(type, property.GetMethod, nav, customData);
                ProcessMethodIfNotNull(type, property.SetMethod, nav, customData);
                return;
            }

            if (property.GetMethod != null && Array.IndexOf(accessors, "get") >= 0)
                ProcessMethod(type, property.GetMethod, nav, customData);
            else if (property.GetMethod == null)
                LogWarning(nav, DiagnosticId.XmlCouldNotFindGetAccesorOfPropertyOnType, property.Name, type.FullName);

            if (property.SetMethod != null && Array.IndexOf(accessors, "set") >= 0)
                ProcessMethod(type, property.SetMethod, nav, customData);
            else if (property.SetMethod == null)
                LogWarning(nav, DiagnosticId.XmlCouldNotFindSetAccesorOfPropertyOnType, property.Name, type.FullName);
        }

        static bool IsRequired(XPathNavigator nav)
        {
            string attribute = GetAttribute(nav, _required);
            if (attribute == null || attribute.Length == 0)
                return true;

            return bool.TryParse(attribute, out bool result) && result;
        }

        protected static string[] GetAccessors(XPathNavigator nav)
        {
            string accessorsValue = GetAttribute(nav, _accessors);

            if (accessorsValue != null)
            {
                string[] accessors = accessorsValue.Split(
                    _accessorsSep, StringSplitOptions.RemoveEmptyEntries);

                if (accessors.Length > 0)
                {
                    for (int i = 0; i < accessors.Length; ++i)
                        accessors[i] = accessors[i].ToLower();

                    return accessors;
                }
            }
            return _accessorsAll;
        }
    }
}
