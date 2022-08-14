// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Runtime.Serialization
{
    public partial interface ISerializationCodeDomSurrogateProvider
    {
        System.CodeDom.CodeTypeDeclaration ProcessImportedType(System.CodeDom.CodeTypeDeclaration typeDeclaration, System.CodeDom.CodeCompileUnit compileUnit);
    }
    public partial class ImportOptions
    {
        public System.CodeDom.Compiler.CodeDomProvider? CodeProvider { get { throw null; } set { throw null; } }
        public bool EnableDataBinding { get { throw null; } set { throw null; } }
        public ISerializationSurrogateProvider? DataContractSurrogate { get { throw null; } set { throw null; } }
        public bool GenerateInternal { get { throw null; } set { throw null; } }
        public bool GenerateSerializable { get { throw null; } set { throw null; } }
        public bool ImportXmlType { get { throw null; } set { throw null; } }
        public System.Collections.Generic.IDictionary<string, string> Namespaces { get { throw null; } }
        public System.Collections.Generic.ICollection<Type> ReferencedCollectionTypes { get { throw null; } }
        public System.Collections.Generic.ICollection<Type> ReferencedTypes { get { throw null; } }
    }
    public partial class XsdDataContractImporter
    {
        public XsdDataContractImporter() { throw null; }
        public XsdDataContractImporter(System.CodeDom.CodeCompileUnit codeCompileUnit) { throw null; }

        public System.CodeDom.CodeCompileUnit CodeCompileUnit { get { throw null; } }
        public ImportOptions? Options { get { throw null; } set { throw null; } }

        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public bool CanImport(System.Xml.Schema.XmlSchemaSet schemas) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public bool CanImport(System.Xml.Schema.XmlSchemaSet schemas, System.Collections.Generic.ICollection<System.Xml.XmlQualifiedName> typeNames) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public bool CanImport(System.Xml.Schema.XmlSchemaSet schemas, System.Xml.XmlQualifiedName typeName) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public bool CanImport(System.Xml.Schema.XmlSchemaSet schemas, System.Xml.Schema.XmlSchemaElement element) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public System.CodeDom.CodeTypeReference GetCodeTypeReference(System.Xml.XmlQualifiedName typeName) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public System.CodeDom.CodeTypeReference GetCodeTypeReference(System.Xml.XmlQualifiedName typeName, System.Xml.Schema.XmlSchemaElement element) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public System.Collections.Generic.ICollection<System.CodeDom.CodeTypeReference>? GetKnownTypeReferences(System.Xml.XmlQualifiedName typeName) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public void Import(System.Xml.Schema.XmlSchemaSet schemas) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public void Import(System.Xml.Schema.XmlSchemaSet schemas, System.Collections.Generic.ICollection<System.Xml.XmlQualifiedName> typeNames) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public void Import(System.Xml.Schema.XmlSchemaSet schemas, System.Xml.XmlQualifiedName typeName) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public System.Xml.XmlQualifiedName? Import(System.Xml.Schema.XmlSchemaSet schemas, System.Xml.Schema.XmlSchemaElement element) { throw null; }
    }
}
