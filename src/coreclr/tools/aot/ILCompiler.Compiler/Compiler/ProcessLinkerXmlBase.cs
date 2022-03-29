// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml;

using Internal.TypeSystem;

namespace ILCompiler
{
    /// <summary>
    /// Base class for readers of IL Linker XML file format.
    /// </summary>
    internal abstract class ProcessLinkerXmlBase
    {
        private readonly XmlReader _reader;
        private readonly ModuleDesc _owningModule;
        private readonly IReadOnlyDictionary<string, bool> _featureSwitchValues;
        protected readonly TypeSystemContext _context;

        public ProcessLinkerXmlBase(TypeSystemContext context, XmlReader reader, ModuleDesc owningModule, IReadOnlyDictionary<string, bool> featureSwitchValues)
        {
            _reader = reader;
            _owningModule = owningModule;
            _featureSwitchValues = featureSwitchValues;
            _context = context;
        }

        public void ProcessXml()
        {
            if (_reader.IsStartElement() && _reader.Name == "linker")
            {
                if (!ShouldProcessElement())
                    return;

                _reader.Read();

                ProcessAssemblies();
            }
        }

        protected string GetAttribute(string attribute)
        {
            return _reader.GetAttribute(attribute);
        }

        protected bool IsEmpty()
        {
            return _reader.IsEmptyElement;
        }

        private void ProcessAssemblies()
        {
            while (_reader.IsStartElement())
            {
                if (_reader.Name == "assembly")
                {
                    string assemblyName = _reader.GetAttribute("fullname");

                    if (assemblyName == "*")
                    {
                        // https://github.com/dotnet/runtimelab/issues/1381
                        _reader.Skip();
                        continue;
                    }

                    // Errors for invalid assembly names should show up even if this element will be
                    // skipped due to feature conditions.
                    var name = new AssemblyName(assemblyName);

                    if (!ShouldProcessElement())
                    {
                        _reader.Skip();
                        continue;
                    }

                    ModuleDesc assembly = GetAssembly(name);

                    if (assembly == null)
                    {
                        //Context.LogWarning($"Could not resolve assembly '{name.Name}'", 2007, _xmlDocumentLocation);
                        _reader.Skip();
                        continue;
                    }

                    _reader.Read();

                    ProcessAssembly(assembly);
                }
                else if (_reader.Name == "type")
                {
                    ProcessType(_owningModule);
                }
                else if (_reader.Name == "resource")
                {
                    ProcessResource(_owningModule);
                }
                else
                {
                    _reader.Skip();
                }
            }
        }

        protected ModuleDesc GetAssembly(AssemblyName name)
        {
            return _context.ResolveAssembly(name);
        }

        private void ProcessAssembly(ModuleDesc assembly)
        {
            while (_reader.IsStartElement())
            {
                if (_reader.Name == "type")
                {
                    ProcessType(assembly);
                }
                else if (_reader.Name == "resource")
                {
                    ProcessResource(assembly);
                }

                _reader.Skip();
            }

            _reader.ReadEndElement();
        }

        private void ProcessType(ModuleDesc assembly)
        {
            if (ShouldProcessElement())
            {
                string typeName = _reader.GetAttribute("fullname");

                if (typeName.Contains('*'))
                    throw new NotSupportedException();

                TypeDesc type = CustomAttributeTypeNameParser.GetTypeByCustomAttributeTypeName(assembly, typeName, throwIfNotFound: false);
                if (type == null)
                {
                    //Context.LogWarning ($"Could not resolve type '{fullname}'", 2008, _xmlDocumentLocation);
                    _reader.Skip();
                    return;
                }

                _reader.Read();

                while (_reader.IsStartElement())
                {
                    if (_reader.Name == "method")
                    {
                        ProcessMethod(type);
                    }
                    else if (_reader.Name == "field")
                    {
                        ProcessField(type);
                    }
                    else if (_reader.Name == "attribute")
                    {
                        ProcessAttribute(type);
                    }

                    _reader.Skip();
                }
            }

            _reader.Skip();
        }

        private void ProcessMethod(TypeDesc type)
        {
            if (ShouldProcessElement())
            {
                string signature = _reader.GetAttribute("signature");
                if (!String.IsNullOrEmpty(signature))
                {
                    MethodDesc method = GetMethod(type, signature);
                    if (method == null)
                    {
                        //Context.LogWarning($"Could not find method '{signature}' on type '{type.GetDisplayName()}'", 2009, _xmlDocumentLocation);
                        return;
                    }

                    ProcessMethod(method);
                }

                string methodName = _reader.GetAttribute("name");
                if (!String.IsNullOrEmpty(methodName))
                {
                    bool foundMatch = false;
                    foreach (MethodDesc method in type.GetMethods())
                    {
                        if (method.Name == methodName)
                        {
                            foundMatch = true;
                            ProcessMethod(method);
                        }
                    }

                    if (!foundMatch)
                    {
                        //Context.LogWarning($"Could not find method '{name}' on type '{type.GetDisplayName()}'", 2009, _xmlDocumentLocation);
                    }
                }
            }
        }

        protected virtual void ProcessMethod(MethodDesc method)
        {
        }

        private void ProcessField(TypeDesc type)
        {
            if (ShouldProcessElement())
            {
                string fieldName = _reader.GetAttribute("name");
                if (!String.IsNullOrEmpty(fieldName))
                {
                    FieldDesc field = type.GetField(fieldName);

                    if (field == null)
                    {
                        //Context.LogWarning($"Could not find field '{name}' on type '{type.GetDisplayName()}'", 2012, _xmlDocumentLocation);
                    }
                    else
                    {
                        ProcessField(field);
                    }
                }
            }
        }

        protected virtual void ProcessField(FieldDesc field)
        {
        }

        protected virtual void ProcessAttribute(TypeDesc type)
        {
        }

        protected virtual void ProcessResource(ModuleDesc module)
        {
        }

        protected MethodDesc GetMethod(TypeDesc type, string signature)
        {
            foreach (MethodDesc meth in type.GetMethods())
                if (signature == GetMethodSignature(meth, false))
                    return meth;

            return null;
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

        private bool ShouldProcessElement()
        {
            string feature = _reader.GetAttribute("feature");
            if (string.IsNullOrEmpty(feature))
                return true;

            string value = _reader.GetAttribute("featurevalue");
            if (string.IsNullOrEmpty(value))
            {
                //context.LogError($"Failed to process '{documentLocation}'. Feature '{feature}' does not specify a 'featurevalue' attribute", 1001);
                return false;
            }

            if (!bool.TryParse(value, out bool bValue))
            {
                //context.LogError($"Failed to process '{documentLocation}'. Unsupported non-boolean feature definition '{feature}'", 1002);
                return false;
            }

            var isDefault = _reader.GetAttribute("featuredefault");
            bool bIsDefault = false;
            if (!string.IsNullOrEmpty(isDefault) && (!bool.TryParse(isDefault, out bIsDefault) || !bIsDefault))
            {
                //context.LogError($"Failed to process '{documentLocation}'. Unsupported value for featuredefault attribute", 1014);
                return false;
            }

            if (!_featureSwitchValues.TryGetValue(feature, out bool featureSetting))
                return bIsDefault;

            return bValue == featureSetting;
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
        }
    }
}
