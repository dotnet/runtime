// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using ILLink.Shared;
using Internal.TypeSystem;

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

        readonly XPathNavigator _document;
        protected readonly (ManifestResource Resource, ModuleDesc Module)? _resource;
        private readonly IReadOnlyDictionary<string, bool> _featureSwitchValues;
        protected readonly TypeSystemContext _context;

        protected ProcessLinkerXmlBase(TypeSystemContext context, UnmanagedMemoryStream documentStream, IReadOnlyDictionary<string, bool> featureSwitchValues)
        {
            _context = context;
            using (documentStream)
            {
                _document = XDocument.Load(documentStream, LoadOptions.SetLineInfo).CreateNavigator();
            }
            _featureSwitchValues = featureSwitchValues;
        }

        protected ProcessLinkerXmlBase(TypeSystemContext context, UnmanagedMemoryStream documentStream, ManifestResource resource, ModuleDesc resourceAssembly, IReadOnlyDictionary<string, bool> featureSwitchValues)
            : this(context, documentStream, featureSwitchValues)
        {
            _resource = (
                resource.Equals(default(ManifestResource)) ? throw new ArgumentNullException(nameof(resource)) : resource,
                resourceAssembly ?? throw new ArgumentNullException(nameof(resourceAssembly))
            );
        }

        protected virtual bool ShouldProcessElement(XPathNavigator nav) => FeatureSettings.ShouldProcessElement(nav, _featureSwitchValues);

        protected virtual void ProcessXml(bool ignoreResource)
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
                    if (ignoreResource)
                        return;
                }

                if (!ShouldProcessElement(nav))
                    return;

                ProcessAssemblies(nav);

                // For embedded XML, allow not specifying the assembly explicitly in XML.
                if (_resource != null)
                    ProcessAssembly(_resource.Value.Module, nav, warnOnUnresolvedTypes: true);

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        protected virtual AllowedAssemblies AllowedAssemblySelector { get => _resource != null ? AllowedAssemblies.ContainingAssembly : AllowedAssemblies.AnyAssembly; }

        bool ShouldProcessAllAssemblies(XPathNavigator nav, [NotNullWhen(false)] out AssemblyName? assemblyName)
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
                    Debug.Assert(_resource != null);
                    if (_resource.Value.Module.Assembly.GetName().Name != name!.Name)
                    {
                        //LogWarning(assemblyNav, DiagnosticId.AssemblyWithEmbeddedXmlApplyToAnotherAssembly, _resource.Value.Assembly.Name.Name, name.ToString());
                        continue;
                    }
                    assemblyToProcess = _resource.Value.Module;
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

        void MatchType(TypeDesc type, Regex regex, XPathNavigator nav)
        {
            if (regex.Match(type.GetDisplayName()).Success)
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
            }
        }

        void ProcessSelectedFields(XPathNavigator nav, TypeDesc type)
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
            if (!String.IsNullOrEmpty(signature))
            {
                FieldDesc field = type.GetField(signature);
                if (field == null)
                {
                    //LogWarning(nav, DiagnosticId.XmlCouldNotFindFieldOnType, signature, type.GetDisplayName());
                    return;
                }

                ProcessField(type, field, nav);
            }

            string name = GetName(nav);
            if (!String.IsNullOrEmpty(name))
            {
                foreach (FieldDesc field in type.GetFields())
                {
                    if (field.Name == name)
                    {
                        ProcessField(type, field, nav);
                    }
                }
            }
        }

        protected virtual void ProcessField(TypeDesc type, FieldDesc field, XPathNavigator nav) { }

        void ProcessSelectedMethods(XPathNavigator nav, TypeDesc type, object? customData)
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
            if (!String.IsNullOrEmpty(signature))
            {
                foreach (MethodDesc meth in type.GetMethods())
                    if (signature == GetMethodSignature(meth, false))
                        ProcessMethod(type, meth, nav, customData);
            }

            string name = GetAttribute(nav, NameAttributeName);
            if (!String.IsNullOrEmpty(name))
            {
                foreach (MethodDesc method in type.GetAllMethods())
                {
                    if (name == method.Name)
                    {
                        ProcessMethod(type, method, nav, customData);
                    }
                }
            }
        }

        protected virtual MethodDesc? GetMethod(TypeDesc type, string signature) => null;

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

        protected virtual void ProcessMethod(TypeDesc type, MethodDesc method, XPathNavigator nav, object? customData) { }

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

        class CecilTypeNameFormatter : TypeNameFormatter
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
                sb.Append(" ");
                AppendName(sb, type.Signature.ReturnType);
                sb.Append(" *");

                sb.Append("(");

                for (int i = 0; i < type.Signature.Length; i++)
                {
                    var parameter = type.Signature[i];
                    if (i > 0)
                        sb.Append(",");

                    AppendName(sb, parameter);
                }

                sb.Append(")");
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
                if (!String.IsNullOrEmpty(type.Namespace))
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
