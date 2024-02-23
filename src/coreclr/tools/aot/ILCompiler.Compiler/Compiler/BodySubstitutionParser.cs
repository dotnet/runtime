// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using Internal.TypeSystem;
using System.Xml;
using System.Xml.XPath;
using System.Globalization;
using System.Linq;
using ILLink.Shared;

namespace ILCompiler
{
    public sealed class BodySubstitutionsParser : ProcessLinkerXmlBase
    {
        private readonly Dictionary<MethodDesc, BodySubstitution> _methodSubstitutions;
        private readonly Dictionary<FieldDesc, object> _fieldSubstitutions;


        private BodySubstitutionsParser(Logger logger, TypeSystemContext context, Stream documentStream, ManifestResource resource, ModuleDesc resourceAssembly, string xmlDocumentLocation, IReadOnlyDictionary<string, bool> featureSwitchValues)
                : base(logger, context, documentStream, resource, resourceAssembly, xmlDocumentLocation, featureSwitchValues)
        {
            _methodSubstitutions = new Dictionary<MethodDesc, BodySubstitution>();
            _fieldSubstitutions = new Dictionary<FieldDesc, object>();
        }

        private BodySubstitutionsParser(Logger logger, TypeSystemContext context, XmlReader document, string xmlDocumentLocation, IReadOnlyDictionary<string, bool> featureSwitchValues)
                : base(logger, context, document, xmlDocumentLocation, featureSwitchValues)
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
#if !READYTORUN
                LogWarning(methodNav, DiagnosticId.XmlCouldNotFindMethodOnType, signature, type.GetDisplayName());
#endif
                return;
            }

            string action = GetAttribute(methodNav, "body");
            switch (action)
            {
                case "remove":
                    _methodSubstitutions.Add(method, BodySubstitution.ThrowingBody);
                    break;
                case "stub":
                    BodySubstitution stubBody = null;
                    if (method.Signature.ReturnType.IsVoid)
                        stubBody = BodySubstitution.EmptyBody;
                    else
                    {
                        object substitution = TryCreateSubstitution(method.Signature.ReturnType, GetAttribute(methodNav, "value"));
                        if (substitution != null)
                            stubBody = BodySubstitution.Create(substitution);
                    }

                    if (stubBody != null)
                    {
                        _methodSubstitutions[method] = stubBody;
                    }
                    else
                    {
#if !READYTORUN
                        LogWarning(methodNav, DiagnosticId.XmlInvalidValueForStub, method.GetDisplayName());
#endif
                    }
                    break;
                default:
#if !READYTORUN
                    LogWarning(methodNav, DiagnosticId.XmlUnkownBodyModification, action, method.GetDisplayName());
#endif
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
#if !READYTORUN
                LogWarning(fieldNav, DiagnosticId.XmlCouldNotFindFieldOnType, name, type.GetDisplayName());
#endif
                return;
            }

            if (!field.IsStatic || field.IsLiteral)
            {
#if !READYTORUN
                LogWarning(fieldNav, DiagnosticId.XmlSubstitutedFieldNeedsToBeStatic, field.GetDisplayName());
#endif
                return;
            }

            string value = GetAttribute(fieldNav, "value");
            if (string.IsNullOrEmpty(value))
            {
#if !READYTORUN
                LogWarning(fieldNav, DiagnosticId.XmlMissingSubstitutionValueForField, field.GetDisplayName());
#endif
                return;
            }

            object substitution = TryCreateSubstitution(field.FieldType, value);
            if (substitution == null)
            {
#if !READYTORUN
                LogWarning(fieldNav, DiagnosticId.XmlInvalidSubstitutionValueForField, value, field.GetDisplayName());
#endif
                return;
            }

            if (string.Equals(GetAttribute(fieldNav, "initialize"), "true", StringComparison.InvariantCultureIgnoreCase))
            {
                // We would need to also mess with the cctor of the type to set the field to this value:
                //
                // * ILLink will remove all stsfld instructions referencing this field from the cctor
                // * It will place an explicit stsfld in front of the last "ret" instruction in the cctor
                //
                // This approach... has issues.
                throw new NotSupportedException();
            }

            _fieldSubstitutions[field] = substitution;
        }

        private static MethodDesc FindMethod(TypeDesc type, string signature)
        {
            foreach (MethodDesc meth in type.GetMethods())
                if (signature == GetMethodSignature(meth, includeGenericParameters: true))
                    return meth;
            return null;
        }

        private static object TryCreateSubstitution(TypeDesc type, string value)
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
                    if (string.IsNullOrEmpty(value))
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

        public static BodyAndFieldSubstitutions GetSubstitutions(Logger logger, TypeSystemContext context, Stream documentStream, ManifestResource resource, ModuleDesc resourceAssembly, string xmlDocumentLocation, IReadOnlyDictionary<string, bool> featureSwitchValues)
        {
            var rdr = new BodySubstitutionsParser(logger, context, documentStream, resource, resourceAssembly, xmlDocumentLocation, featureSwitchValues);
            rdr.ProcessXml(false);
            return new BodyAndFieldSubstitutions(rdr._methodSubstitutions, rdr._fieldSubstitutions);
        }

        public static BodyAndFieldSubstitutions GetSubstitutions(Logger logger, TypeSystemContext context, XmlReader reader, string xmlDocumentLocation, IReadOnlyDictionary<string, bool> featureSwitchValues)
        {
            var rdr = new BodySubstitutionsParser(logger, context, reader, xmlDocumentLocation, featureSwitchValues);
            rdr.ProcessXml(false);
            return new BodyAndFieldSubstitutions(rdr._methodSubstitutions, rdr._fieldSubstitutions);
        }
    }

    public struct BodyAndFieldSubstitutions
    {
        private Dictionary<MethodDesc, BodySubstitution> _bodySubstitutions;
        private Dictionary<FieldDesc, object> _fieldSubstitutions;

        public IReadOnlyDictionary<MethodDesc, BodySubstitution> BodySubstitutions => _bodySubstitutions;
        public IReadOnlyDictionary<FieldDesc, object> FieldSubstitutions => _fieldSubstitutions;

        public BodyAndFieldSubstitutions(Dictionary<MethodDesc, BodySubstitution> bodySubstitutions, Dictionary<FieldDesc, object> fieldSubstitutions)
            => (_bodySubstitutions, _fieldSubstitutions) = (bodySubstitutions, fieldSubstitutions);

        public void AppendFrom(BodyAndFieldSubstitutions other)
        {
            if (_bodySubstitutions == null)
            {
                _bodySubstitutions = other._bodySubstitutions;
                _fieldSubstitutions = other._fieldSubstitutions;
            }
            else if (other._bodySubstitutions == null)
            {
                // Nothing to do
            }
            else
            {
                foreach (var kvp in other._bodySubstitutions)
                    _bodySubstitutions[kvp.Key] = kvp.Value;

                foreach (var kvp in other._fieldSubstitutions)
                    _fieldSubstitutions[kvp.Key] = kvp.Value;
            }
        }
    }
}
