using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.XPath;
using ILLink.Shared;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
    public class BodySubstitutionParser : ProcessLinkerXmlBase
    {
        SubstitutionInfo? _substitutionInfo;

        public BodySubstitutionParser(LinkContext context, Stream documentStream, string xmlDocumentLocation)
            : base(context, documentStream, xmlDocumentLocation)
        {
        }

        public BodySubstitutionParser(LinkContext context, Stream documentStream, EmbeddedResource resource, AssemblyDefinition resourceAssembly, string xmlDocumentLocation = "")
            : base(context, documentStream, resource, resourceAssembly, xmlDocumentLocation)
        {
        }

        public void Parse(SubstitutionInfo xmlInfo)
        {
            _substitutionInfo = xmlInfo;
            bool stripSubstitutions = _context.IsOptimizationEnabled(CodeOptimizations.RemoveSubstitutions, _resource?.Assembly);
            ProcessXml(stripSubstitutions, _context.IgnoreSubstitutions);
        }

        protected override void ProcessAssembly(AssemblyDefinition assembly, XPathNavigator nav, bool warnOnUnresolvedTypes)
        {
            ProcessTypes(assembly, nav, warnOnUnresolvedTypes);
            ProcessResources(assembly, nav);
        }

        protected override TypeDefinition? ProcessExportedType(ExportedType exported, AssemblyDefinition assembly, XPathNavigator nav) => null;

        protected override bool ProcessTypePattern(string fullname, AssemblyDefinition assembly, XPathNavigator nav) => false;

        protected override void ProcessType(TypeDefinition type, XPathNavigator nav)
        {
            Debug.Assert(ShouldProcessElement(nav));
            ProcessTypeChildren(type, nav);
        }

        protected override void ProcessMethod(TypeDefinition type, XPathNavigator methodNav, object? _customData)
        {
            Debug.Assert(_substitutionInfo != null);
            string signature = GetSignature(methodNav);
            if (string.IsNullOrEmpty(signature))
                return;

            MethodDefinition? method = FindMethod(type, signature);
            if (method == null)
            {
                LogWarning(methodNav, DiagnosticId.XmlCouldNotFindMethodOnType, signature, type.GetDisplayName());
                return;
            }

            string action = GetAttribute(methodNav, "body");
            switch (action)
            {
                case "remove":
                    _substitutionInfo.SetMethodAction(method, MethodAction.ConvertToThrow);
                    return;
                case "stub":
                    string value = GetAttribute(methodNav, "value");
                    if (!string.IsNullOrEmpty(value))
                    {
                        if (!TryConvertValue(value, method.ReturnType, out object? res))
                        {
                            LogWarning(methodNav, DiagnosticId.XmlInvalidValueForStub, method.GetDisplayName());
                            return;
                        }

                        _substitutionInfo.SetMethodStubValue(method, res);
                    }

                    _substitutionInfo.SetMethodAction(method, MethodAction.ConvertToStub);
                    return;
                default:
                    LogWarning(methodNav, DiagnosticId.XmlUnkownBodyModification, action, method.GetDisplayName());
                    return;
            }
        }

        protected override void ProcessField(TypeDefinition type, XPathNavigator fieldNav)
        {
            Debug.Assert(_substitutionInfo != null);
            string name = GetAttribute(fieldNav, "name");
            if (string.IsNullOrEmpty(name))
                return;

            var field = type.Fields.FirstOrDefault(f => f.Name == name);
            if (field == null)
            {
                LogWarning(fieldNav, DiagnosticId.XmlCouldNotFindFieldOnType, name, type.GetDisplayName());
                return;
            }

            if (!field.IsStatic || field.IsLiteral)
            {
                LogWarning(fieldNav, DiagnosticId.XmlSubstitutedFieldNeedsToBeStatic, field.GetDisplayName());
                return;
            }

            string value = GetAttribute(fieldNav, "value");
            if (string.IsNullOrEmpty(value))
            {
                LogWarning(fieldNav, DiagnosticId.XmlMissingSubstitutionValueForField, field.GetDisplayName());
                return;
            }
            if (!TryConvertValue(value, field.FieldType, out object? res))
            {
                LogWarning(fieldNav, DiagnosticId.XmlInvalidSubstitutionValueForField, value, field.GetDisplayName());
                return;
            }

            _substitutionInfo.SetFieldValue(field, res);

            string init = GetAttribute(fieldNav, "initialize");
            if (init?.ToLowerInvariant() == "true")
            {
                _substitutionInfo.SetFieldInit(field);
            }
        }

        void ProcessResources(AssemblyDefinition assembly, XPathNavigator nav)
        {
            foreach (XPathNavigator resourceNav in nav.SelectChildren("resource", ""))
            {
                if (!ShouldProcessElement(resourceNav))
                    continue;

                string name = GetAttribute(resourceNav, "name");
                if (String.IsNullOrEmpty(name))
                {
                    LogWarning(resourceNav, DiagnosticId.XmlMissingNameAttributeInResource);
                    continue;
                }

                string action = GetAttribute(resourceNav, "action");
                if (action != "remove")
                {
                    LogWarning(resourceNav, DiagnosticId.XmlInvalidValueForAttributeActionForResource, action, name);
                    continue;
                }

                EmbeddedResource? resource = assembly.FindEmbeddedResource(name);
                if (resource == null)
                {
                    LogWarning(resourceNav, DiagnosticId.XmlCouldNotFindResourceToRemoveInAssembly, name, assembly.Name.Name);
                    continue;
                }

                _context.Annotations.AddResourceToRemove(assembly, resource);
            }
        }

        static MethodDefinition? FindMethod(TypeDefinition type, string signature)
        {
            if (!type.HasMethods)
                return null;

            foreach (MethodDefinition meth in type.Methods)
                if (signature == DescriptorMarker.GetMethodSignature(meth, includeGenericParameters: true))
                    return meth;

            return null;
        }
    }
}
