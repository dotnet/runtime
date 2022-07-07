// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Runtime.Serialization.Schema
{
    public sealed partial class ExportOptions
    {
        public System.Collections.ObjectModel.Collection<Type> KnownTypes { get { throw null; } }
        public ISerializationSurrogateProvider? SurrogateProvider { get { throw null; } set { throw null; } }
    }
    public sealed partial class ImportOptions
    {
        public System.CodeDom.Compiler.CodeDomProvider? CodeProvider { get { throw null; } set { throw null; } }
        public bool EnableDataBinding { get { throw null; } set { throw null; } }
        public ISerializationSurrogateProvider? SurrogateProvider { get { throw null; } set { throw null; } }
        public bool GenerateInternal { get { throw null; } set { throw null; } }
        public bool GenerateSerializable { get { throw null; } set { throw null; } }
        public bool ImportXmlType { get { throw null; } set { throw null; } }
        public System.Collections.Generic.IDictionary<string, string> Namespaces { get { throw null; } }
        public Func<System.CodeDom.CodeTypeDeclaration, System.CodeDom.CodeCompileUnit, System.CodeDom.CodeTypeDeclaration?>? ProcessImportedType;
        public System.Collections.Generic.ICollection<Type> ReferencedCollectionTypes { get { throw null; } }
        public System.Collections.Generic.ICollection<Type> ReferencedTypes { get { throw null; } }
    }
    public sealed class XsdDataContractExporter
    {
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public bool CanExport(System.Collections.Generic.ICollection<System.Reflection.Assembly> assemblies) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public bool CanExport(System.Collections.Generic.ICollection<Type> types) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public bool CanExport(Type type) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public void Export(System.Collections.Generic.ICollection<System.Reflection.Assembly> assemblies) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public void Export(System.Collections.Generic.ICollection<Type> types) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public void Export(Type type) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public System.Xml.XmlQualifiedName? GetRootElementName(Type type) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public System.Xml.Schema.XmlSchemaType? GetSchemaType(Type type) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public System.Xml.XmlQualifiedName GetSchemaTypeName(Type type) { throw null; }
        public ExportOptions? Options { get { throw null; } set { throw null; } }
        public System.Xml.Schema.XmlSchemaSet Schemas { get { throw null; } }
        public XsdDataContractExporter() { throw null; }
        public XsdDataContractExporter(System.Xml.Schema.XmlSchemaSet? schemas) { throw null; }
    }
    public sealed partial class XsdDataContractImporter
    {
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public bool CanImport(System.Xml.Schema.XmlSchemaSet schemas) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public bool CanImport(System.Xml.Schema.XmlSchemaSet schemas, System.Collections.Generic.ICollection<System.Xml.XmlQualifiedName> typeNames) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public bool CanImport(System.Xml.Schema.XmlSchemaSet schemas, System.Xml.XmlQualifiedName typeName) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public bool CanImport(System.Xml.Schema.XmlSchemaSet schemas, System.Xml.Schema.XmlSchemaElement element) { throw null; }
        public System.CodeDom.CodeCompileUnit CodeCompileUnit { get { throw null; } }
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
        public ImportOptions? Options { get { throw null; } set { throw null; } }
        public XsdDataContractImporter() { throw null; }
        public XsdDataContractImporter(System.CodeDom.CodeCompileUnit codeCompileUnit) { throw null; }
    }
}
