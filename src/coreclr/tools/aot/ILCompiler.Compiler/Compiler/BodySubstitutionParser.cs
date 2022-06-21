// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using Internal.TypeSystem;
using System.Xml.XPath;
using System.Globalization;
using System.Linq;

namespace ILCompiler
{
    internal sealed class BodySubstitutionsParser : ProcessLinkerXmlBase
    {
        private readonly Dictionary<MethodDesc, BodySubstitution> _methodSubstitutions;
        private readonly Dictionary<FieldDesc, object> _fieldSubstitutions;

        private BodySubstitutionsParser(TypeSystemContext context, Stream documentStream, ManifestResource resource, ModuleDesc resourceAssembly, string xmlDocumentLocation, IReadOnlyDictionary<string, bool> featureSwitchValues)
                : base(context, documentStream, resource, resourceAssembly, xmlDocumentLocation, featureSwitchValues)
        {
            _methodSubstitutions = new Dictionary<MethodDesc, BodySubstitution>();
            _fieldSubstitutions = new Dictionary<FieldDesc, object>();
        }

        protected override void ProcessAssembly(ModuleDesc assembly, XPathNavigator nav, bool warnOnUnresolvedTypes)
        {
            ProcessTypes(assembly, nav, warnOnUnresolvedTypes);
        }

        // protected override TypeDesc? ProcessExportedType(ExportedType exported, ModuleDesc assembly, XPathNavigator nav) => null;

        protected override bool ProcessTypePattern(string fullname, ModuleDesc assembly, XPathNavigator nav) => false;

        protected override void ProcessType(TypeDesc type, XPathNavigator nav)
        {
            Debug.Assert(ShouldProcessElement(nav));
            ProcessTypeChildren(type, nav);
        }

        protected override void ProcessMethod(TypeDesc type, XPathNavigator methodNav, object customData)
        {
            string signature = GetSignature(methodNav);
            if (string.IsNullOrEmpty(signature))
                return;

            MethodDesc method = FindMethod(type, signature);
            if (method == null)
            {
                // LogWarning(methodNav, DiagnosticId.XmlCouldNotFindMethodOnType, signature, type.GetDisplayName());
                return;
            }

            string action = GetAttribute(methodNav, "body");
            switch (action)
            {
                case "remove":
                    _methodSubstitutions.Add(method, BodySubstitution.ThrowingBody);
                    break;
                case "stub":
                    BodySubstitution stubBody;
                    if (method.Signature.ReturnType.IsVoid)
                        stubBody = BodySubstitution.EmptyBody;
                    else
                        stubBody = BodySubstitution.Create(TryCreateSubstitution(method.Signature.ReturnType, GetAttribute(methodNav, "value")));

                    if (stubBody != null)
                    {
                        _methodSubstitutions[method] = stubBody;
                    }
                    else
                    {
                        // Context.LogWarning ($"Invalid value for '{method.GetDisplayName ()}' stub", 2010, _xmlDocumentLocation);
                    }
                    break;
                default:
                    //Context.LogWarning($"Unknown body modification '{action}' for '{method.GetDisplayName()}'", 2011, _xmlDocumentLocation);
                    break;
            }
        }

        protected override void ProcessField(TypeDesc type, XPathNavigator fieldNav)
        {
            string name = GetAttribute(fieldNav, "name");
            if (string.IsNullOrEmpty(name))
                return;

            var field = type.GetFields().FirstOrDefault(f => f.Name == name);
            if (field == null)
            {
                // LogWarning(fieldNav, DiagnosticId.XmlCouldNotFindFieldOnType, name, type.GetDisplayName());
                return;
            }

            if (!field.IsStatic || field.IsLiteral)
            {
                // LogWarning(fieldNav, DiagnosticId.XmlSubstitutedFieldNeedsToBeStatic, field.GetDisplayName());
                return;
            }

            string value = GetAttribute(fieldNav, "value");
            if (string.IsNullOrEmpty(value))
            {
                //Context.LogWarning($"Missing 'value' attribute for field '{field.GetDisplayName()}'.", 2014, _xmlDocumentLocation);
                return;
            }

            object substitution = TryCreateSubstitution(field.FieldType, value);
            if (substitution == null)
            {
                //Context.LogWarning($"Invalid value '{value}' for '{field.GetDisplayName()}'.", 2015, _xmlDocumentLocation);
                return;
            }

            if (String.Equals(GetAttribute(fieldNav, "initialize"), "true", StringComparison.InvariantCultureIgnoreCase))
            {
                // We would need to also mess with the cctor of the type to set the field to this value:
                //
                // * Linker will remove all stsfld instructions referencing this field from the cctor
                // * It will place an explicit stsfld in front of the last "ret" instruction in the cctor
                //
                // This approach... has issues.
                throw new NotSupportedException();
            }

            _fieldSubstitutions[field] = substitution;
        }

        static MethodDesc FindMethod(TypeDesc type, string signature)
        {
            foreach (MethodDesc meth in type.GetMethods())
                if (signature == GetMethodSignature(meth, includeGenericParameters: true))
                    return meth;
            return null;
        }

        private object TryCreateSubstitution(TypeDesc type, string value)
        {
            switch (type.UnderlyingType.Category)
            {
                case TypeFlags.Int32:
                    if (string.IsNullOrEmpty(value))
                        return 0;
                    else if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int iresult))
                        return iresult;
                    break;

                case TypeFlags.Boolean:
                    if (String.IsNullOrEmpty(value))
                        return 0;
                    else if (bool.TryParse(value, out bool bvalue))
                        return bvalue ? 1 : 0;
                    else
                        goto case TypeFlags.Int32;

                default:
                    throw new NotSupportedException(type.ToString());
            }

            return null;
        }

        public static (Dictionary<MethodDesc, BodySubstitution>, Dictionary<FieldDesc, object>) GetSubstitutions(TypeSystemContext context, UnmanagedMemoryStream documentStream, ManifestResource resource, ModuleDesc resourceAssembly, string xmlDocumentLocation, IReadOnlyDictionary<string, bool> featureSwitchValues)
        {
            var rdr = new BodySubstitutionsParser(context, documentStream, resource, resourceAssembly, xmlDocumentLocation, featureSwitchValues);
            rdr.ProcessXml(false);
            return (rdr._methodSubstitutions, rdr._fieldSubstitutions);
        }
    }
}
