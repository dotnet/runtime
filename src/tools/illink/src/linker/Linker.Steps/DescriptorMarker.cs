// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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

        protected readonly HashSet<object> _preservedMembers;
        IMemberDefinition? _conditionalSource;

        DependencyInfo DescriptorReason => new DependencyInfo(DependencyKind.XmlDescriptor,
            _conditionalSource is null ? _xmlDocumentLocation : (object)_conditionalSource);

        public DescriptorMarker(LinkContext context, Stream documentStream, string xmlDocumentLocation)
            : base(context, documentStream, xmlDocumentLocation)
        {
            _preservedMembers = new();
        }

        public DescriptorMarker(LinkContext context, Stream documentStream, EmbeddedResource resource, AssemblyDefinition resourceAssembly, string xmlDocumentLocation = "<unspecified>")
            : base(context, documentStream, resource, resourceAssembly, xmlDocumentLocation)
        {
            _preservedMembers = new();
        }

        protected void LogDuplicatePreserve(string memberName, XPathNavigator duplicatePosition)
        {
            var origin = GetMessageOriginForPosition(duplicatePosition);
            _context.LogMessage(MessageContainer.CreateInfoMessage(origin, $"Duplicate preserve of '{memberName}'"));
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
                ProcessConditionalDependencies(assembly, nav, warnOnUnresolvedTypes);
            }
        }

        void ProcessConditionalDependencies(AssemblyDefinition assembly, XPathNavigator nav, bool warnOnUnresolvedTypes)
        {
            foreach (XPathNavigator conditional in nav.SelectChildren("conditional", XmlNamespace))
            {
                if (!ShouldProcessElement(conditional))
                    continue;

                string typeName = GetAttribute(conditional, "type");
                string methodName = GetAttribute(conditional, "method");
                string fieldName = GetAttribute(conditional, "field");
                if (methodName.Length != 0 && fieldName.Length != 0)
                    throw new InvalidOperationException("A conditional descriptor cannot specify both a method and a field trigger.");

                TypeDefinition? type = assembly.MainModule.GetType(typeName);
                if (type is null)
                {
                    if (warnOnUnresolvedTypes)
                        LogWarning(conditional, DiagnosticId.XmlCouldNotResolveType, typeName);
                    continue;
                }

                var triggers = new List<IMemberDefinition>();
                if (methodName.Length != 0)
                {
                    foreach (MethodDefinition method in type.Methods)
                        if (method.Name == methodName)
                            triggers.Add(method);
                    if (triggers.Count == 0)
                        LogWarning(conditional, DiagnosticId.XmlCouldNotFindMethodOnType, methodName, type.GetDisplayName());
                }
                else if (fieldName.Length != 0)
                {
                    foreach (FieldDefinition field in type.Fields)
                        if (field.Name == fieldName)
                            triggers.Add(field);
                    if (triggers.Count == 0)
                        LogWarning(conditional, DiagnosticId.XmlCouldNotFindFieldOnType, fieldName, type.GetDisplayName());
                }
                else
                {
                    triggers.Add(type);
                }

                bool activated = false;
                XPathNavigator block = conditional.Clone();
                foreach (IMemberDefinition trigger in triggers)
                {
                    _context.Annotations.AddConditionalRoot(trigger, () => {
                        if (activated)
                            return;
                        activated = true;
                        IMemberDefinition? previousSource = _conditionalSource;
                        _conditionalSource = trigger;
                        try
                        {
                            ProcessTypes(assembly, block, warnOnUnresolvedTypes);
                        }
                        catch (Exception ex) when (ex is not LinkerFatalErrorException)
                        {
                            throw new LinkerFatalErrorException(MessageContainer.CreateErrorMessage(null,
                                DiagnosticId.ErrorProcessingXmlLocation, _xmlDocumentLocation), ex);
                        }
                        finally
                        {
                            _conditionalSource = previousSource;
                        }
                    });
                }
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
            _context.Annotations.Mark(type, DescriptorReason, GetMessageOriginForPosition(nav));
            _context.Annotations.SetPreserve(type, TypePreserve.All);

            if (!type.HasNestedTypes)
                return;

            foreach (TypeDefinition nested in type.NestedTypes)
                MarkAndPreserveAll(nested, nav);
        }

        protected override TypeDefinition? ProcessExportedType(ExportedType exported, AssemblyDefinition assembly, XPathNavigator nav)
        {
            _context.MarkingHelpers.MarkExportedType(exported, assembly.MainModule, DescriptorReason, GetMessageOriginForPosition(nav));

            // If a nested exported type is marked, then the declaring type must also be marked otherwise cecil will write out an invalid exported type table
            // and anything that tries to read the assembly with cecil will crash
            if (exported.DeclaringType != null)
            {
                var currentType = exported.DeclaringType;
                while (currentType != null)
                {
                    var parent = currentType.DeclaringType;
                    _context.MarkingHelpers.MarkExportedType(currentType, assembly.MainModule, new DependencyInfo(DependencyKind.DeclaringType, currentType), GetMessageOriginForPosition(nav));
                    currentType = parent;
                }
            }

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

            // For types with preserve="all"/"methods"/"fields", the type becomes
            // reflection-visible through its members via ApplyPreserveInfo. For explicit
            // <method>/<field> children, ProcessMethod/ProcessField handle it. For
            // preserve="nothing" (no members preserved), we schedule the type itself
            // for reflection-visible treatment.
            if (preserve is TypePreserve.Nothing)
                _context.Annotations.MarkPendingReflectionVisibleType(type, GetMessageOriginForPosition(nav));

            _context.Annotations.Mark(type, DescriptorReason, GetMessageOriginForPosition(nav));

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

        protected static TypePreserve GetTypePreserve(XPathNavigator nav)
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
            if (!_preservedMembers.Add(field))
                LogDuplicatePreserve(field.FullName, nav);

            var reason = DescriptorReason;
            var origin = GetMessageOriginForPosition(nav);
            _context.Annotations.MarkPendingReflectionVisibleField(field, origin);
            _context.Annotations.Mark(field, reason, origin);
        }

        protected override void ProcessMethod(TypeDefinition type, MethodDefinition method, XPathNavigator nav, object? customData)
        {
            if (!_preservedMembers.Add(method))
                LogDuplicatePreserve(method.GetDisplayName(), nav);

            _context.Annotations.MarkIndirectlyCalledMethod(method);
            _context.Annotations.SetAction(method, MethodAction.Parse);

            if (customData is bool required && !required)
            {
                _context.Annotations.AddPreservedMethod(type, method);
            }
            else
            {
                var reason = DescriptorReason;
                var origin = GetMessageOriginForPosition(nav);
                _context.Annotations.MarkPendingReflectionVisibleMethod(method, origin);
                _context.Annotations.Mark(method, reason, origin);
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
            sb.Append(' ');
            sb.Append(meth.Name);
            if (includeGenericParameters && meth.HasGenericParameters)
            {
                sb.Append('`');
                sb.Append(meth.GenericParameters.Count);
            }

            sb.Append('(');
            if (meth.HasMetadataParameters())
            {
                int i = 0;
                foreach (var p in meth.GetMetadataParameters())
                {
                    if (i++ > 0)
                        sb.Append(',');
                    sb.Append(p.ParameterType.FullName);
                }
            }
            sb.Append(')');
            return sb.ToString();
        }

        protected override void ProcessEvent(TypeDefinition type, EventDefinition @event, XPathNavigator nav, object? customData)
        {
            if (!_preservedMembers.Add(@event))
                LogDuplicatePreserve(@event.FullName, nav);

            ProcessMethod(type, @event.AddMethod, nav, customData);
            ProcessMethod(type, @event.RemoveMethod, nav, customData);
            ProcessMethodIfNotNull(type, @event.InvokeMethod, nav, customData);
        }

        protected override void ProcessProperty(TypeDefinition type, PropertyDefinition property, XPathNavigator nav, object? customData, bool fromSignature)
        {
            string[] accessors = fromSignature ? GetAccessors(nav) : _accessorsAll;

            if (!_preservedMembers.Add(property))
                LogDuplicatePreserve(property.FullName, nav);

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
                        accessors[i] = accessors[i].ToLowerInvariant();

                    return accessors;
                }
            }
            return _accessorsAll;
        }
    }
}
