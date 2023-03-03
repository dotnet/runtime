// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;

using ILCompiler.Dataflow;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

#nullable enable

namespace ILCompiler
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
        private const string FullNameAttributeName = "fullname";
        private const string LinkerElementName = "linker";
        private const string TypeElementName = "type";
        private const string SignatureAttributeName = "signature";
        private const string NameAttributeName = "name";
        private const string FieldElementName = "field";
        private const string MethodElementName = "method";
        private const string EventElementName = "event";
        private const string PropertyElementName = "property";
        private const string AllAssembliesFullName = "*";
        protected const string XmlNamespace = "";

        protected readonly string _xmlDocumentLocation;
        private readonly XPathNavigator _document;
        protected readonly ModuleDesc? _owningModule;
        private readonly IReadOnlyDictionary<string, bool> _featureSwitchValues;
        protected readonly TypeSystemContext _context;

        protected ProcessLinkerXmlBase(TypeSystemContext context, Stream documentStream, string xmlDocumentLocation, IReadOnlyDictionary<string, bool> featureSwitchValues)
        {
            _context = context;
            using (documentStream)
            {
                _document = XDocument.Load(documentStream, LoadOptions.SetLineInfo).CreateNavigator();
            }
            _xmlDocumentLocation = xmlDocumentLocation;
            _featureSwitchValues = featureSwitchValues;
        }

        protected ProcessLinkerXmlBase(TypeSystemContext context, Stream documentStream, ManifestResource resource, ModuleDesc resourceAssembly, string xmlDocumentLocation, IReadOnlyDictionary<string, bool> featureSwitchValues)
            : this(context, documentStream, xmlDocumentLocation, featureSwitchValues)
        {
            _owningModule = resourceAssembly;
        }

        protected virtual bool ShouldProcessElement(XPathNavigator nav) => FeatureSettings.ShouldProcessElement(nav, _featureSwitchValues);

        protected virtual void ProcessXml(bool ignoreResource)
        {
            if (!AllowedAssemblySelector.HasFlag(AllowedAssemblies.AnyAssembly) && _owningModule == null)
                throw new InvalidOperationException("The containing assembly must be specified for XML which is restricted to modifying that assembly only.");

            try
            {
                XPathNavigator nav = _document.CreateNavigator();

                // Initial structure check - ignore XML document which don't look like linker XML format
                if (!nav.MoveToChild(LinkerElementName, XmlNamespace))
                    return;

                if (_owningModule != null)
                {
                    if (ignoreResource)
                        return;
                }

                if (!ShouldProcessElement(nav))
                    return;

                ProcessAssemblies(nav);

                // For embedded XML, allow not specifying the assembly explicitly in XML.
                if (_owningModule != null)
                    ProcessAssembly(_owningModule, nav, warnOnUnresolvedTypes: true);

            }
            catch (Exception ex)
            {
                // throw new LinkerFatalErrorException(MessageContainer.CreateErrorMessage(null, DiagnosticId.ErrorProcessingXmlLocation, _xmlDocumentLocation), ex);
                throw ex;
            }
        }

        protected virtual AllowedAssemblies AllowedAssemblySelector { get => _owningModule != null ? AllowedAssemblies.ContainingAssembly : AllowedAssemblies.AnyAssembly; }

        private bool ShouldProcessAllAssemblies(XPathNavigator nav, [NotNullWhen(false)] out AssemblyName? assemblyName)
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
                bool processAllAssemblies = ShouldProcessAllAssemblies(assemblyNav, out AssemblyName? name);
                if (processAllAssemblies && AllowedAssemblySelector != AllowedAssemblies.AllAssemblies)
                {
                    //LogWarning(assemblyNav, DiagnosticId.XmlUnsuportedWildcard);
                    continue;
                }

                ModuleDesc? assemblyToProcess = null;
                if (!AllowedAssemblySelector.HasFlag(AllowedAssemblies.AnyAssembly))
                {
                    Debug.Assert(!processAllAssemblies);
                    Debug.Assert(_owningModule != null);
                    if (_owningModule.Assembly.GetName().Name != name!.Name)
                    {
                        //LogWarning(assemblyNav, DiagnosticId.AssemblyWithEmbeddedXmlApplyToAnotherAssembly, _resource.Value.Assembly.Name.Name, name.ToString());
                        continue;
                    }
                    assemblyToProcess = _owningModule;
                }

                if (!ShouldProcessElement(assemblyNav))
                    continue;

                if (processAllAssemblies)
                {
                    throw new NotImplementedException();
                    // We could avoid loading all references in this case: https://github.com/dotnet/linker/issues/1708
                    //foreach (ModuleDesc assembly in GetReferencedAssemblies())
                    //    ProcessAssembly(assembly, assemblyNav, warnOnUnresolvedTypes: false);
                }
                else
                {
                    Debug.Assert(!processAllAssemblies);
                    ModuleDesc? assembly = assemblyToProcess ?? _context.ResolveAssembly(name!);

                    if (assembly == null)
                    {
                        //LogWarning(assemblyNav, DiagnosticId.XmlCouldNotResolveAssembly, name!.Name);
                        continue;
                    }

                    ProcessAssembly(assembly, assemblyNav, warnOnUnresolvedTypes: true);
                }
            }
        }


        protected abstract void ProcessAssembly(ModuleDesc assembly, XPathNavigator nav, bool warnOnUnresolvedTypes);

        protected virtual void ProcessTypes(ModuleDesc assembly, XPathNavigator nav, bool warnOnUnresolvedTypes)
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

                // TODO: Process exported types

                // TODO: Semantics differ and xml format is cecil specific, therefore they are discrepancies on things like nested types
                // for now just hack replacing / for + to support basic resolving of nested types
                // https://github.com/dotnet/runtime/issues/73083
                fullname = fullname.Replace("/", "+");
                TypeDesc type = CustomAttributeTypeNameParser.GetTypeByCustomAttributeTypeName(assembly, fullname, throwIfNotFound: false);

                if (type == null)
                {
                    //if (warnOnUnresolvedTypes)
                    //    LogWarning(typeNav, DiagnosticId.XmlCouldNotResolveType, fullname);
                    continue;
                }

                ProcessType(type, typeNav);
            }
        }

        private void MatchType(TypeDesc type, Regex regex, XPathNavigator nav)
        {
            StringBuilder sb = new StringBuilder();
            CecilTypeNameFormatter.Instance.AppendName(sb, type);
            if (regex.IsMatch(sb.ToString()))
                ProcessType(type, nav);
        }

        protected virtual bool ProcessTypePattern(string fullname, ModuleDesc assembly, XPathNavigator nav)
        {
            Regex regex = new Regex(fullname.Replace(".", @"\.").Replace("*", "(.*)"));

            foreach (TypeDesc type in assembly.GetAllTypes())
                MatchType(type, regex, nav);

            return true;
        }

        protected abstract void ProcessType(TypeDesc type, XPathNavigator nav);

        protected void ProcessTypeChildren(TypeDesc type, XPathNavigator nav, object? customData = null)
        {
            if (nav.HasChildren)
            {
                ProcessSelectedFields(nav, type);
                ProcessSelectedMethods(nav, type, customData);
                ProcessSelectedEvents(nav, type, customData);
                ProcessSelectedProperties(nav, type, customData);
            }
        }

        private void ProcessSelectedFields(XPathNavigator nav, TypeDesc type)
        {
            foreach (XPathNavigator fieldNav in nav.SelectChildren(FieldElementName, XmlNamespace))
            {
                if (!ShouldProcessElement(fieldNav))
                    continue;
                ProcessField(type, fieldNav);
            }
        }

        protected virtual void ProcessField(TypeDesc type, XPathNavigator nav)
        {
            string signature = GetSignature(nav);
            if (!string.IsNullOrEmpty(signature))
            {
                FieldDesc? field = GetField(type, signature);
                if (field == null)
                {
                    //LogWarning(nav, DiagnosticId.XmlCouldNotFindFieldOnType, signature, type.GetDisplayName());
                    return;
                }

                ProcessField(type, field, nav);
            }

            string name = GetName(nav);
            if (!string.IsNullOrEmpty(name))
            {
                bool foundMatch = false;
                foreach (FieldDesc field in type.GetFields())
                {
                    if (field.Name == name)
                    {
                        foundMatch = true;
                        ProcessField(type, field, nav);
                    }
                }


                if (!foundMatch)
                {
                    // LogWarning(nav, DiagnosticId.XmlCouldNotFindFieldOnType, name, type.GetDisplayName());
                }
            }
        }

        protected static FieldDesc? GetField(TypeDesc type, string signature)
        {
            StringBuilder sb = new StringBuilder();
            foreach (FieldDesc field in type.GetFields())
            {
                sb.Clear();
                CecilTypeNameFormatter.Instance.AppendName(sb, field.FieldType);
                if (signature == sb.ToString() + " " + field.Name)
                    return field;
            }

            return null;
        }

        protected virtual void ProcessField(TypeDesc type, FieldDesc field, XPathNavigator nav) { }

        private void ProcessSelectedMethods(XPathNavigator nav, TypeDesc type, object? customData)
        {
            foreach (XPathNavigator methodNav in nav.SelectChildren(MethodElementName, XmlNamespace))
            {
                if (!ShouldProcessElement(methodNav))
                    continue;
                ProcessMethod(type, methodNav, customData);
            }
        }

        protected virtual void ProcessMethod(TypeDesc type, XPathNavigator nav, object? customData)
        {
            string signature = GetSignature(nav);
            if (!string.IsNullOrEmpty(signature))
            {
                MethodDesc? method = GetMethod(type, signature);
                if (method == null)
                {
                    //LogWarning(nav, DiagnosticId.XmlCouldNotFindMethodOnType, signature, type.GetDisplayName());
                    return;
                }

                ProcessMethod(type, method, nav, customData);
            }

            string name = GetAttribute(nav, NameAttributeName);
            if (!string.IsNullOrEmpty(name))
            {
                bool foundMatch = false;
                foreach (MethodDesc method in type.GetAllMethods())
                {
                    if (name == method.Name)
                    {
                        foundMatch = true;
                        ProcessMethod(type, method, nav, customData);
                    }
                }
                if (!foundMatch)
                {
                    // LogWarning(nav, DiagnosticId.XmlCouldNotFindMethodOnType, name, type.GetDisplayName());
                }
            }
        }

        protected virtual MethodDesc? GetMethod(TypeDesc type, string signature) => null;

        protected virtual void ProcessMethod(TypeDesc type, MethodDesc method, XPathNavigator nav, object? customData) { }

        private void ProcessSelectedEvents(XPathNavigator nav, TypeDesc type, object? customData)
        {
            foreach (XPathNavigator eventNav in nav.SelectChildren(EventElementName, XmlNamespace))
            {
                if (!ShouldProcessElement(eventNav))
                    continue;
                ProcessEvent(type, eventNav, customData);
            }
        }

        protected virtual void ProcessEvent(TypeDesc type, XPathNavigator nav, object? customData)
        {
            string signature = GetSignature(nav);
            if (!string.IsNullOrEmpty(signature))
            {
                EventPseudoDesc? @event = GetEvent(type, signature);
                if (@event is null)
                {
                    // LogWarning(nav, DiagnosticId.XmlCouldNotFindEventOnType, signature, type.GetDisplayName());
                    return;
                }

                ProcessEvent(type, @event, nav, customData);
            }

            string name = GetAttribute(nav, NameAttributeName);
            if (!string.IsNullOrEmpty(name))
            {
                bool foundMatch = false;
                if (type.GetTypeDefinition() is not EcmaType ecmaType)
                {
                    return;
                }
                foreach (var @event in type.GetEventsOnTypeHierarchy(e => e.Name == name))
                {
                    foundMatch = true;
                    ProcessEvent(type, @event, nav, customData);
                }

                if (!foundMatch)
                {
                    // LogWarning(nav, DiagnosticId.XmlCouldNotFindEventOnType, name, type.GetDisplayName());
                }
            }
        }

        protected static EventPseudoDesc? GetEvent(TypeDesc type, string signature)
        {
            foreach (EventPseudoDesc @event in type.GetEventsOnTypeHierarchy(e => string.Concat(CecilTypeNameFormatter.Instance.FormatName(e.AddMethod.Signature[0]), " ", e.Name) == signature))
                return @event;

            return null;
        }

        protected virtual void ProcessEvent(TypeDesc type, EventPseudoDesc @event, XPathNavigator nav, object? customData) { }

        private void ProcessSelectedProperties(XPathNavigator nav, TypeDesc type, object? customData)
        {
            foreach (XPathNavigator propertyNav in nav.SelectChildren(PropertyElementName, XmlNamespace))
            {
                if (!ShouldProcessElement(propertyNav))
                    continue;
                ProcessProperty(type, propertyNav, customData);
            }
        }

        protected virtual void ProcessProperty(TypeDesc type, XPathNavigator nav, object? customData)
        {
            string signature = GetSignature(nav);
            if (!string.IsNullOrEmpty(signature))
            {
                PropertyPseudoDesc? property = GetProperty(type, signature);
                if (property is null)
                {
                    // LogWarning(nav, DiagnosticId.XmlCouldNotFindPropertyOnType, signature, type.GetDisplayName());
                    return;
                }

                ProcessProperty(type, property, nav, customData, true);
            }

            string name = GetAttribute(nav, NameAttributeName);
            if (!string.IsNullOrEmpty(name))
            {
                bool foundMatch = false;
                foreach (PropertyPseudoDesc property in type.GetPropertiesOnTypeHierarchy(p => p.Name == name))
                {
                   foundMatch = true;
                   ProcessProperty(type, property, nav, customData, false);
                }

                if (!foundMatch)
                {
                    //LogWarning(nav, DiagnosticId.XmlCouldNotFindPropertyOnType, name, type.GetDisplayName());
                }
            }
        }

        protected static PropertyPseudoDesc? GetProperty(TypeDesc type, string signature)
        {
            foreach (PropertyPseudoDesc property in type.GetPropertiesOnTypeHierarchy(p => string.Concat(CecilTypeNameFormatter.Instance.FormatName(p.Signature.ReturnType), " ", p.Name) == signature))
                return property;

            return null;
        }

        protected virtual void ProcessProperty(TypeDesc type, PropertyPseudoDesc property, XPathNavigator nav, object? customData, bool fromSignature) { }

        protected virtual AssemblyName GetAssemblyName(XPathNavigator nav)
        {
            return new AssemblyName(GetFullName(nav));
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

        public static string GetMethodSignature(MethodDesc meth, bool includeGenericParameters)
        {
            StringBuilder sb = new StringBuilder();
            CecilTypeNameFormatter.Instance.AppendName(sb, meth.Signature.ReturnType);
            sb.Append(' ');
            sb.Append(meth.Name);
            if (includeGenericParameters && meth.HasInstantiation)
            {
                sb.Append('`');
                sb.Append(meth.Instantiation.Length);
            }

            sb.Append('(');
            for (int i = 0; i < meth.Signature.Length; i++)
            {
                if (i > 0)
                    sb.Append(',');

                CecilTypeNameFormatter.Instance.AppendName(sb, meth.Signature[i]);
            }

            sb.Append(')');
            return sb.ToString();
        }

#if false
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
#endif

        private sealed class CecilTypeNameFormatter : TypeNameFormatter
        {
            public static readonly CecilTypeNameFormatter Instance = new CecilTypeNameFormatter();

            public override void AppendName(StringBuilder sb, ArrayType type)
            {
                AppendName(sb, type.ElementType);
                sb.Append('[');
                if (type.Rank > 1)
                    sb.Append(new string(',', type.Rank - 1));
                sb.Append(']');
            }
            public override void AppendName(StringBuilder sb, ByRefType type)
            {
                AppendName(sb, type.ParameterType);
                sb.Append('&');
            }

            public override void AppendName(StringBuilder sb, PointerType type)
            {
                AppendName(sb, type.ParameterType);
                sb.Append('*');
            }

            public override void AppendName(StringBuilder sb, FunctionPointerType type)
            {
                sb.Append(' ');
                AppendName(sb, type.Signature.ReturnType);
                sb.Append(" *");

                sb.Append('(');

                for (int i = 0; i < type.Signature.Length; i++)
                {
                    var parameter = type.Signature[i];
                    if (i > 0)
                        sb.Append(',');

                    AppendName(sb, parameter);
                }

                sb.Append(')');
            }

            public override void AppendName(StringBuilder sb, GenericParameterDesc type)
            {
                sb.Append(type.Name);
            }
            public override void AppendName(StringBuilder sb, SignatureMethodVariable type)
            {
            }
            public override void AppendName(StringBuilder sb, SignatureTypeVariable type)
            {
            }
            protected override void AppendNameForInstantiatedType(StringBuilder sb, DefType type)
            {
                AppendName(sb, type.GetTypeDefinition());

                sb.Append('<');

                for (int i = 0; i < type.Instantiation.Length; i++)
                {
                    if (i != 0)
                        sb.Append(',');

                    AppendName(sb, type.Instantiation[i]);
                }

                sb.Append('>');
            }
            protected override void AppendNameForNamespaceType(StringBuilder sb, DefType type)
            {
                if (!string.IsNullOrEmpty(type.Namespace))
                {
                    sb.Append(type.Namespace);
                    sb.Append('.');
                }

                sb.Append(type.Name);
            }

            protected override void AppendNameForNestedType(StringBuilder sb, DefType nestedType, DefType containingType)
            {
                AppendName(sb, containingType);
                sb.Append('/');
                sb.Append(nestedType.Name);
            }

#if false
            public bool TryConvertValue(string value, TypeDesc type, out object? result)
            {
                switch (type.UnderlyingType.Category)
                {
                    case TypeFlags.Boolean:
                        if ((bool.TryParse(value, out bool bvalue)))
                        {
                            result = bvalue ? 1 : 0;
                            return true;
                        }
                        else
                            goto case TypeFlags.Int32;

                    case TypeFlags.Byte:
                        if (!byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte byteresult))
                            break;

                        result = (int)byteresult;
                        return true;

                    case TypeFlags.SByte:
                        if (!sbyte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out sbyte sbyteresult))
                            break;

                        result = (int)sbyteresult;
                        return true;

                    case TypeFlags.Int16:
                        if (!short.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out short shortresult))
                            break;

                        result = (int)shortresult;
                        return true;

                    case TypeFlags.UInt16:
                        if (!ushort.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort ushortresult))
                            break;

                        result = (int)ushortresult;
                        return true;

                    case TypeFlags.Int32:
                        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int iresult))
                            break;

                        result = iresult;
                        return true;

                    case TypeFlags.UInt32:
                        if (!uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint uresult))
                            break;

                        result = (int)uresult;
                        return true;

                    case TypeFlags.Double:
                        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double dresult))
                            break;

                        result = dresult;
                        return true;

                    case TypeFlags.Single:
                        if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float fresult))
                            break;

                        result = fresult;
                        return true;

                    case TypeFlags.Int64:
                        if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long lresult))
                            break;

                        result = lresult;
                        return true;

                    case TypeFlags.UInt64:
                        if (!ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong ulresult))
                            break;

                        result = (long)ulresult;
                        return true;

                    case TypeFlags.Char:
                        if (!char.TryParse(value, out char chresult))
                            break;

                        result = (int)chresult;
                        return true;

                    default:
                        throw new NotSupportedException(type.ToString());
                }

                result = null;
                return false;
            }
#endif
        }
    }
}
