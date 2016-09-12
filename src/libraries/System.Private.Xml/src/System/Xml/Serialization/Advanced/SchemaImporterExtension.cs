// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Serialization.Advanced
{
    using System.Xml.Schema;
    using System.Xml;
    using System.Collections;
    using System.Collections.Specialized;
    using System.CodeDom;
    using System.CodeDom.Compiler;
    using System.Xml.Serialization;
    using System.Reflection;

    /// <include file='doc\SchemaImporterExtension.uex' path='docs/doc[@for="SchemaImporterExtension"]/*' />
    ///<internalonly/>
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public abstract class SchemaImporterExtension
    {
        /// <include file='doc\SchemaImporterExtension.uex' path='docs/doc[@for="SchemaImporterExtension.ImportSchemaType"]/*' />
        internal virtual string ImportSchemaType(string name, string ns, XmlSchemaObject context, XmlSchemas schemas, XmlSchemaImporter importer,
            CodeCompileUnit compileUnit, CodeNamespace mainNamespace, CodeGenerationOptions options, CodeDomProvider codeProvider)
        {
            return null;
        }

        /// <include file='doc\SchemaImporterExtension.uex' path='docs/doc[@for="SchemaImporterExtension.ImportSchemaType1"]/*' />
        internal virtual string ImportSchemaType(XmlSchemaType type, XmlSchemaObject context, XmlSchemas schemas, XmlSchemaImporter importer,
            CodeCompileUnit compileUnit, CodeNamespace mainNamespace, CodeGenerationOptions options, CodeDomProvider codeProvider)
        {
            return null;
        }

        /// <include file='doc\SchemaImporterExtension.uex' path='docs/doc[@for="SchemaImporterExtension.ImportSchemaType1"]/*' />
        internal virtual string ImportAnyElement(XmlSchemaAny any, bool mixed, XmlSchemas schemas, XmlSchemaImporter importer,
            CodeCompileUnit compileUnit, CodeNamespace mainNamespace, CodeGenerationOptions options, CodeDomProvider codeProvider)
        {
            return null;
        }

        /// <include file='doc\SchemaImporterExtension.uex' path='docs/doc[@for="SchemaImporterExtension.ImportDefaultValue"]/*' />
        internal virtual CodeExpression ImportDefaultValue(string value, string type)
        {
            return null;
        }
    }

    public class SchemaImporterExtensionCollection : CollectionBase
    {
        private Hashtable _exNames;

        internal Hashtable Names
        {
            get
            {
                if (_exNames == null)
                    _exNames = new Hashtable();
                return _exNames;
            }
        }

        public int Add(SchemaImporterExtension extension)
        {
            return Add(extension.GetType().FullName, extension);
        }

        public int Add(string name, Type type)
        {
            if (type.GetTypeInfo().IsSubclassOf(typeof(SchemaImporterExtension)))
            {
                return Add(name, (SchemaImporterExtension)Activator.CreateInstance(type));
            }
            else
            {
                throw new ArgumentException(SR.Format(SR.XmlInvalidSchemaExtension, type));
            }
        }

        public void Remove(string name)
        {
            if (Names[name] != null)
            {
                List.Remove(Names[name]);
                Names[name] = null;
            }
        }

        public new void Clear()
        {
            Names.Clear();
            List.Clear();
        }

        internal SchemaImporterExtensionCollection Clone()
        {
            SchemaImporterExtensionCollection clone = new SchemaImporterExtensionCollection();
            clone._exNames = (Hashtable)this.Names.Clone();
            foreach (object o in this.List)
            {
                clone.List.Add(o);
            }
            return clone;
        }

        public SchemaImporterExtension this[int index]
        {
            get { return (SchemaImporterExtension)List[index]; }
            set { List[index] = value; }
        }

        internal int Add(string name, SchemaImporterExtension extension)
        {
            if (Names[name] != null)
            {
                if (Names[name].GetType() != extension.GetType())
                {
                    throw new InvalidOperationException(SR.Format(SR.XmlConfigurationDuplicateExtension, name));
                }
                return -1;
            }
            Names[name] = extension;
            return List.Add(extension);
        }

        public void Insert(int index, SchemaImporterExtension extension)
        {
            List.Insert(index, extension);
        }

        public int IndexOf(SchemaImporterExtension extension)
        {
            return List.IndexOf(extension);
        }

        public bool Contains(SchemaImporterExtension extension)
        {
            return List.Contains(extension);
        }

        public void Remove(SchemaImporterExtension extension)
        {
            List.Remove(extension);
        }

        public void CopyTo(SchemaImporterExtension[] array, int index)
        {
            List.CopyTo(array, index);
        }
    }

    internal class MappedTypeDesc
    {
        private string _name;
        private string _ns;
        private XmlSchemaType _xsdType;
        private XmlSchemaObject _context;
        private string _clrType;
        private SchemaImporterExtension _extension;
        private CodeNamespace _code;
        private bool _exported = false;
        private StringCollection _references;

        internal MappedTypeDesc(string clrType, string name, string ns, XmlSchemaType xsdType, XmlSchemaObject context, SchemaImporterExtension extension, CodeNamespace code, StringCollection references)
        {
            _clrType = clrType.Replace('+', '.');
            _name = name;
            _ns = ns;
            _xsdType = xsdType;
            _context = context;
            _code = code;
            _references = references;
            _extension = extension;
        }

        internal SchemaImporterExtension Extension { get { return _extension; } }
        internal string Name { get { return _clrType; } }

        internal StringCollection ReferencedAssemblies
        {
            get
            {
                if (_references == null)
                    _references = new StringCollection();
                return _references;
            }
        }

        internal CodeTypeDeclaration ExportTypeDefinition(CodeNamespace codeNamespace, CodeCompileUnit codeCompileUnit)
        {
            if (_exported)
                return null;
            _exported = true;

            foreach (CodeNamespaceImport import in _code.Imports)
            {
                codeNamespace.Imports.Add(import);
            }
            CodeTypeDeclaration codeClass = null;
            string comment = SR.Format(SR.XmlExtensionComment, _extension.GetType().FullName);
            foreach (CodeTypeDeclaration type in _code.Types)
            {
                if (_clrType == type.Name)
                {
                    if (codeClass != null)
                        throw new InvalidOperationException(SR.Format(SR.XmlExtensionDuplicateDefinition, _extension.GetType().FullName, _clrType));
                    codeClass = type;
                }
                type.Comments.Add(new CodeCommentStatement(comment, false));
                codeNamespace.Types.Add(type);
            }
            if (codeCompileUnit != null)
            {
                foreach (string reference in ReferencedAssemblies)
                {
                    if (codeCompileUnit.ReferencedAssemblies.Contains(reference))
                        continue;
                    codeCompileUnit.ReferencedAssemblies.Add(reference);
                }
            }
            return codeClass;
        }
    }
}
