// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using ILLink.Shared;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
    [Flags]
    public enum AllowedAssemblies
    {
        ContainingAssembly = 0x1,
        AnyAssembly = 0x2 | ContainingAssembly,
        AllAssemblies = 0x4 | AnyAssembly
    }

    public abstract class ProcessLinkerXmlBase
    {
        const string FullNameAttributeName = "fullname";
        const string LinkerElementName = "linker";
        const string TypeElementName = "type";
        const string SignatureAttributeName = "signature";
        const string NameAttributeName = "name";
        const string FieldElementName = "field";
        const string MethodElementName = "method";
        const string EventElementName = "event";
        const string PropertyElementName = "property";
        const string AllAssembliesFullName = "*";
        protected const string XmlNamespace = "";

        protected readonly string _xmlDocumentLocation;
        readonly XPathNavigator _document;
        protected readonly (EmbeddedResource Resource, AssemblyDefinition Assembly)? _resource;
        protected readonly LinkContext _context;

        protected ProcessLinkerXmlBase(LinkContext context, Stream documentStream, string xmlDocumentLocation)
        {
            _context = context;
            using (documentStream)
            {
                _document = XDocument.Load(documentStream, LoadOptions.SetLineInfo).CreateNavigator();
            }
            _xmlDocumentLocation = xmlDocumentLocation;
        }

        protected ProcessLinkerXmlBase(LinkContext context, Stream documentStream, EmbeddedResource resource, AssemblyDefinition resourceAssembly, string xmlDocumentLocation)
            : this(context, documentStream, xmlDocumentLocation)
        {
            _resource = (
                resource ?? throw new ArgumentNullException(nameof(resource)),
                resourceAssembly ?? throw new ArgumentNullException(nameof(resourceAssembly))
            );
        }

        protected virtual bool ShouldProcessElement(XPathNavigator nav) => FeatureSettings.ShouldProcessElement(nav, _context, _xmlDocumentLocation);

        protected virtual void ProcessXml(bool stripResource, bool ignoreResource)
        {
            if (!AllowedAssemblySelector.HasFlag(AllowedAssemblies.AnyAssembly) && _resource == null)
                throw new InvalidOperationException("The containing assembly must be specified for XML which is restricted to modifying that assembly only.");

            try
            {
                XPathNavigator nav = _document.CreateNavigator();

                // Initial structure check - ignore XML document which don't look like linker XML format
                if (!nav.MoveToChild(LinkerElementName, XmlNamespace))
                    return;

                if (_resource != null)
                {
                    if (stripResource)
                        _context.Annotations.AddResourceToRemove(_resource.Value.Assembly, _resource.Value.Resource);
                    if (ignoreResource)
                        return;
                }

                if (!ShouldProcessElement(nav))
                    return;

                ProcessAssemblies(nav);

                // For embedded XML, allow not specifying the assembly explicitly in XML.
                if (_resource != null)
                    ProcessAssembly(_resource.Value.Assembly, nav, warnOnUnresolvedTypes: true);

            }
            catch (Exception ex) when (!(ex is LinkerFatalErrorException))
            {
                throw new LinkerFatalErrorException(MessageContainer.CreateErrorMessage(null, DiagnosticId.ErrorProcessingXmlLocation, _xmlDocumentLocation), ex);
            }
        }

        protected virtual AllowedAssemblies AllowedAssemblySelector { get => _resource != null ? AllowedAssemblies.ContainingAssembly : AllowedAssemblies.AnyAssembly; }

        bool ShouldProcessAllAssemblies(XPathNavigator nav, [NotNullWhen(false)] out AssemblyNameReference? assemblyName)
        {
            assemblyName = null;
            if (GetFullName(nav) == AllAssembliesFullName)
                return true;

            assemblyName = GetAssemblyName(nav);
            return false;
        }

        protected virtual void ProcessAssemblies(XPathNavigator nav)
        {
            foreach (XPathNavigator assemblyNav in nav.SelectChildren("assembly", ""))
            {
                // Errors for invalid assembly names should show up even if this element will be
                // skipped due to feature conditions.
                bool processAllAssemblies = ShouldProcessAllAssemblies(assemblyNav, out AssemblyNameReference? name);
                if (processAllAssemblies && AllowedAssemblySelector != AllowedAssemblies.AllAssemblies)
                {
                    LogWarning(assemblyNav, DiagnosticId.XmlUnsuportedWildcard);
                    continue;
                }

                AssemblyDefinition? assemblyToProcess = null;
                if (!AllowedAssemblySelector.HasFlag(AllowedAssemblies.AnyAssembly))
                {
                    Debug.Assert(!processAllAssemblies);
                    Debug.Assert(_resource != null);
                    if (_resource.Value.Assembly.Name.Name != name!.Name)
                    {
                        LogWarning(assemblyNav, DiagnosticId.AssemblyWithEmbeddedXmlApplyToAnotherAssembly, _resource.Value.Assembly.Name.Name, name.ToString());
                        continue;
                    }
                    assemblyToProcess = _resource.Value.Assembly;
                }

                if (!ShouldProcessElement(assemblyNav))
                    continue;

                if (processAllAssemblies)
                {
                    // We could avoid loading all references in this case: https://github.com/dotnet/linker/issues/1708
                    foreach (AssemblyDefinition assembly in _context.GetReferencedAssemblies())
                        ProcessAssembly(assembly, assemblyNav, warnOnUnresolvedTypes: false);
                }
                else
                {
                    Debug.Assert(!processAllAssemblies);
                    AssemblyDefinition? assembly = assemblyToProcess ?? _context.TryResolve(name!);

                    if (assembly == null)
                    {
                        LogWarning(assemblyNav, DiagnosticId.XmlCouldNotResolveAssembly, name!.Name);
                        continue;
                    }

                    ProcessAssembly(assembly, assemblyNav, warnOnUnresolvedTypes: true);
                }
            }
        }

        protected abstract void ProcessAssembly(AssemblyDefinition assembly, XPathNavigator nav, bool warnOnUnresolvedTypes);

        protected virtual void ProcessTypes(AssemblyDefinition assembly, XPathNavigator nav, bool warnOnUnresolvedTypes)
        {
            foreach (XPathNavigator typeNav in nav.SelectChildren(TypeElementName, XmlNamespace))
            {

                if (!ShouldProcessElement(typeNav))
                    continue;

                string fullname = GetFullName(typeNav);

                if (fullname.IndexOf("*") != -1)
                {
                    if (ProcessTypePattern(fullname, assembly, typeNav))
                        continue;
                }

                TypeDefinition type = assembly.MainModule.GetType(fullname);

                if (type == null && assembly.MainModule.HasExportedTypes)
                {
                    foreach (var exported in assembly.MainModule.ExportedTypes)
                    {
                        if (fullname == exported.FullName)
                        {
                            var resolvedExternal = ProcessExportedType(exported, assembly, typeNav);
                            if (resolvedExternal != null)
                            {
                                type = resolvedExternal;
                                break;
                            }
                        }
                    }
                }

                if (type == null)
                {
                    if (warnOnUnresolvedTypes)
                        LogWarning(typeNav, DiagnosticId.XmlCouldNotResolveType, fullname);
                    continue;
                }

                ProcessType(type, typeNav);
            }
        }

        protected virtual TypeDefinition? ProcessExportedType(ExportedType exported, AssemblyDefinition assembly, XPathNavigator nav) => exported.Resolve();

        void MatchType(TypeDefinition type, Regex regex, XPathNavigator nav)
        {
            if (regex.IsMatch(type.FullName))
                ProcessType(type, nav);

            if (!type.HasNestedTypes)
                return;

            foreach (var nt in type.NestedTypes)
                MatchType(nt, regex, nav);
        }

        protected virtual bool ProcessTypePattern(string fullname, AssemblyDefinition assembly, XPathNavigator nav)
        {
            Regex regex = new Regex(fullname.Replace(".", @"\.").Replace("*", "(.*)"));

            foreach (TypeDefinition type in assembly.MainModule.Types)
            {
                MatchType(type, regex, nav);
            }

            if (assembly.MainModule.HasExportedTypes)
            {
                foreach (var exported in assembly.MainModule.ExportedTypes)
                {
                    if (regex.IsMatch(exported.FullName))
                    {
                        var type = ProcessExportedType(exported, assembly, nav);
                        if (type != null)
                        {
                            ProcessType(type, nav);
                        }
                    }
                }
            }

            return true;
        }

        protected abstract void ProcessType(TypeDefinition type, XPathNavigator nav);

        protected void ProcessTypeChildren(TypeDefinition type, XPathNavigator nav, object? customData = null)
        {
            if (nav.HasChildren)
            {
                ProcessSelectedFields(nav, type);
                ProcessSelectedMethods(nav, type, customData);
                ProcessSelectedEvents(nav, type, customData);
                ProcessSelectedProperties(nav, type, customData);
            }
        }

        void ProcessSelectedFields(XPathNavigator nav, TypeDefinition type)
        {
            foreach (XPathNavigator fieldNav in nav.SelectChildren(FieldElementName, XmlNamespace))
            {
                if (!ShouldProcessElement(fieldNav))
                    continue;
                ProcessField(type, fieldNav);
            }
        }

        protected virtual void ProcessField(TypeDefinition type, XPathNavigator nav)
        {
            string signature = GetSignature(nav);
            if (!String.IsNullOrEmpty(signature))
            {
                FieldDefinition? field = GetField(type, signature);
                if (field == null)
                {
                    LogWarning(nav, DiagnosticId.XmlCouldNotFindFieldOnType, signature, type.GetDisplayName());
                    return;
                }

                ProcessField(type, field, nav);
            }

            string name = GetName(nav);
            if (!String.IsNullOrEmpty(name))
            {
                bool foundMatch = false;
                if (type.HasFields)
                {
                    foreach (FieldDefinition field in type.Fields)
                    {
                        if (field.Name == name)
                        {
                            foundMatch = true;
                            ProcessField(type, field, nav);
                        }
                    }
                }

                if (!foundMatch)
                {
                    LogWarning(nav, DiagnosticId.XmlCouldNotFindFieldOnType, name, type.GetDisplayName());
                }
            }
        }

        protected static FieldDefinition? GetField(TypeDefinition type, string signature)
        {
            if (!type.HasFields)
                return null;

            foreach (FieldDefinition field in type.Fields)
                if (signature == field.FieldType.FullName + " " + field.Name)
                    return field;

            return null;
        }

        protected virtual void ProcessField(TypeDefinition type, FieldDefinition field, XPathNavigator nav) { }

        void ProcessSelectedMethods(XPathNavigator nav, TypeDefinition type, object? customData)
        {
            foreach (XPathNavigator methodNav in nav.SelectChildren(MethodElementName, XmlNamespace))
            {
                if (!ShouldProcessElement(methodNav))
                    continue;
                ProcessMethod(type, methodNav, customData);
            }
        }

        protected virtual void ProcessMethod(TypeDefinition type, XPathNavigator nav, object? customData)
        {
            string signature = GetSignature(nav);
            if (!String.IsNullOrEmpty(signature))
            {
                MethodDefinition? method = GetMethod(type, signature);
                if (method == null)
                {
                    LogWarning(nav, DiagnosticId.XmlCouldNotFindMethodOnType, signature, type.GetDisplayName());
                    return;
                }

                ProcessMethod(type, method, nav, customData);
            }

            string name = GetAttribute(nav, NameAttributeName);
            if (!String.IsNullOrEmpty(name))
            {
                bool foundMatch = false;
                if (type.HasMethods)
                {
                    foreach (MethodDefinition method in type.Methods)
                    {
                        if (name == method.Name)
                        {
                            foundMatch = true;
                            ProcessMethod(type, method, nav, customData);
                        }
                    }
                }

                if (!foundMatch)
                {
                    LogWarning(nav, DiagnosticId.XmlCouldNotFindMethodOnType, name, type.GetDisplayName());
                }
            }
        }

        protected virtual MethodDefinition? GetMethod(TypeDefinition type, string signature) => null;

        protected virtual void ProcessMethod(TypeDefinition type, MethodDefinition method, XPathNavigator nav, object? customData) { }

        void ProcessSelectedEvents(XPathNavigator nav, TypeDefinition type, object? customData)
        {
            foreach (XPathNavigator eventNav in nav.SelectChildren(EventElementName, XmlNamespace))
            {
                if (!ShouldProcessElement(eventNav))
                    continue;
                ProcessEvent(type, eventNav, customData);
            }
        }

        protected virtual void ProcessEvent(TypeDefinition type, XPathNavigator nav, object? customData)
        {
            string signature = GetSignature(nav);
            if (!String.IsNullOrEmpty(signature))
            {
                EventDefinition? @event = GetEvent(type, signature);
                if (@event == null)
                {
                    LogWarning(nav, DiagnosticId.XmlCouldNotFindEventOnType, signature, type.GetDisplayName());
                    return;
                }

                ProcessEvent(type, @event, nav, customData);
            }

            string name = GetAttribute(nav, NameAttributeName);
            if (!String.IsNullOrEmpty(name))
            {
                bool foundMatch = false;
                foreach (EventDefinition @event in type.Events)
                {
                    if (@event.Name == name)
                    {
                        foundMatch = true;
                        ProcessEvent(type, @event, nav, customData);
                    }
                }

                if (!foundMatch)
                {
                    LogWarning(nav, DiagnosticId.XmlCouldNotFindEventOnType, name, type.GetDisplayName());
                }
            }
        }

        protected static EventDefinition? GetEvent(TypeDefinition type, string signature)
        {
            if (!type.HasEvents)
                return null;

            foreach (EventDefinition @event in type.Events)
                if (signature == @event.EventType.FullName + " " + @event.Name)
                    return @event;

            return null;
        }

        protected virtual void ProcessEvent(TypeDefinition type, EventDefinition @event, XPathNavigator nav, object? customData) { }

        void ProcessSelectedProperties(XPathNavigator nav, TypeDefinition type, object? customData)
        {
            foreach (XPathNavigator propertyNav in nav.SelectChildren(PropertyElementName, XmlNamespace))
            {
                if (!ShouldProcessElement(propertyNav))
                    continue;
                ProcessProperty(type, propertyNav, customData);
            }
        }

        protected virtual void ProcessProperty(TypeDefinition type, XPathNavigator nav, object? customData)
        {
            string signature = GetSignature(nav);
            if (!String.IsNullOrEmpty(signature))
            {
                PropertyDefinition? property = GetProperty(type, signature);
                if (property == null)
                {
                    LogWarning(nav, DiagnosticId.XmlCouldNotFindPropertyOnType, signature, type.GetDisplayName());
                    return;
                }

                ProcessProperty(type, property, nav, customData, true);
            }

            string name = GetAttribute(nav, NameAttributeName);
            if (!String.IsNullOrEmpty(name))
            {
                bool foundMatch = false;
                foreach (PropertyDefinition property in type.Properties)
                {
                    if (property.Name == name)
                    {
                        foundMatch = true;
                        ProcessProperty(type, property, nav, customData, false);
                    }
                }

                if (!foundMatch)
                {
                    LogWarning(nav, DiagnosticId.XmlCouldNotFindPropertyOnType, name, type.GetDisplayName());
                }
            }
        }

        protected static PropertyDefinition? GetProperty(TypeDefinition type, string signature)
        {
            if (!type.HasProperties)
                return null;

            foreach (PropertyDefinition property in type.Properties)
                if (signature == property.PropertyType.FullName + " " + property.Name)
                    return property;

            return null;
        }

        protected virtual void ProcessProperty(TypeDefinition type, PropertyDefinition property, XPathNavigator nav, object? customData, bool fromSignature) { }

        protected virtual AssemblyNameReference GetAssemblyName(XPathNavigator nav)
        {
            return AssemblyNameReference.Parse(GetFullName(nav));
        }

        protected static string GetFullName(XPathNavigator nav)
        {
            return GetAttribute(nav, FullNameAttributeName);
        }

        protected static string GetName(XPathNavigator nav)
        {
            return GetAttribute(nav, NameAttributeName);
        }

        protected static string GetSignature(XPathNavigator nav)
        {
            return GetAttribute(nav, SignatureAttributeName);
        }

        protected static string GetAttribute(XPathNavigator nav, string attribute)
        {
            return nav.GetAttribute(attribute, XmlNamespace);
        }

        protected MessageOrigin GetMessageOriginForPosition(XPathNavigator position)
        {
            return (position is IXmlLineInfo lineInfo)
                    ? new MessageOrigin(_xmlDocumentLocation, lineInfo.LineNumber, lineInfo.LinePosition, _resource?.Assembly)
                    : new MessageOrigin(_xmlDocumentLocation, 0, 0, _resource?.Assembly);
        }
        protected void LogWarning(string message, int warningCode, XPathNavigator position)
        {
            _context.LogWarning(message, warningCode, GetMessageOriginForPosition(position));
        }

        protected void LogWarning(XPathNavigator position, DiagnosticId id, params string[] args)
        {
            _context.LogWarning(GetMessageOriginForPosition(position), id, args);
        }

        public override string ToString() => GetType().Name + ": " + _xmlDocumentLocation;

        public bool TryConvertValue(string value, TypeReference target, out object? result)
        {
            switch (target.MetadataType)
            {
                case MetadataType.Boolean:
                    if (bool.TryParse(value, out bool bvalue))
                    {
                        result = bvalue ? 1 : 0;
                        return true;
                    }

                    goto case MetadataType.Int32;

                case MetadataType.Byte:
                    if (!byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte byteresult))
                        break;

                    result = (int)byteresult;
                    return true;

                case MetadataType.SByte:
                    if (!sbyte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out sbyte sbyteresult))
                        break;

                    result = (int)sbyteresult;
                    return true;

                case MetadataType.Int16:
                    if (!short.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out short shortresult))
                        break;

                    result = (int)shortresult;
                    return true;

                case MetadataType.UInt16:
                    if (!ushort.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort ushortresult))
                        break;

                    result = (int)ushortresult;
                    return true;

                case MetadataType.Int32:
                    if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int iresult))
                        break;

                    result = iresult;
                    return true;

                case MetadataType.UInt32:
                    if (!uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint uresult))
                        break;

                    result = (int)uresult;
                    return true;

                case MetadataType.Double:
                    if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double dresult))
                        break;

                    result = dresult;
                    return true;

                case MetadataType.Single:
                    if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float fresult))
                        break;

                    result = fresult;
                    return true;

                case MetadataType.Int64:
                    if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long lresult))
                        break;

                    result = lresult;
                    return true;

                case MetadataType.UInt64:
                    if (!ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong ulresult))
                        break;

                    result = (long)ulresult;
                    return true;

                case MetadataType.Char:
                    if (!char.TryParse(value, out char chresult))
                        break;

                    result = (int)chresult;
                    return true;

                case MetadataType.String:
                    if (value is string || value == null)
                    {
                        result = value;
                        return true;
                    }

                    break;

                case MetadataType.ValueType:
                    if (value is string &&
                        _context.TryResolve(target) is TypeDefinition typeDefinition &&
                        typeDefinition.IsEnum)
                    {
                        var enumField = typeDefinition.Fields.Where(f => f.IsStatic && f.Name == value).FirstOrDefault();
                        if (enumField != null)
                        {
                            result = Convert.ToInt32(enumField.Constant);
                            return true;
                        }
                    }

                    break;
            }

            result = null;
            return false;
        }
    }
}
