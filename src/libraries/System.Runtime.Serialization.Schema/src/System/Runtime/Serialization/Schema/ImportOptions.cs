// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace System.Runtime.Serialization.Schema
{
    /// <summary>
    /// Represents the options that can be set on an <see cref="XsdDataContractImporter"/>.
    /// </summary>
    /// <remarks>
    /// The <see cref="XsdDataContractImporter"/> is used to generate code from XML schema using the .NET CodeDOM. To generate an XML schema from an assembly, use the <see cref="XsdDataContractExporter"/>.
    /// </remarks>
    public sealed class ImportOptions
    {
        private ICollection<Type>? _referencedTypes;
        private ICollection<Type>? _referencedCollectionTypes;
        private IDictionary<string, string>? _namespaces;

        /// <summary>
        /// Gets or sets a <see cref="CodeDomProvider"/> instance that provides the means to check whether particular options for a target language are supported.
        /// </summary>
        public CodeDomProvider? CodeProvider { get; set; }

        /// <summary>
        /// Gets or sets a value that specifies whether types in generated code should implement the <see cref="System.ComponentModel.INotifyPropertyChanged"/> interface.
        /// </summary>
        public bool EnableDataBinding { get; set; }

        /// <summary>
        /// Gets or sets a data contract surrogate provider that can be used to modify the code generated during an import operation.
        /// </summary>
        /// <remarks>
        /// The interface type for this option is ISerializationSurrogateProvider, but to take full advantage of the imported code modification
        /// abilities, using an ISerializationExtendedSurrogateProvider is recommended.
        /// </remarks>
        public ISerializationSurrogateProvider? SurrogateProvider { get; set; }

        /// <summary>
        /// Gets or sets a value that specifies whether generated code will be marked internal or public.
        /// </summary>
        public bool GenerateInternal { get; set; }

        /// <summary>
        /// Gets or sets a value that specifies whether generated data contract classes will be marked with the <see cref="SerializableAttribute"/> attribute in addition to the <see cref="DataContractAttribute"/> attribute.
        /// </summary>
        public bool GenerateSerializable { get; set; }

        /// <summary>
        /// Gets or sets a value that determines whether all XML schema types, even those that do not conform to a data contract schema, will be imported.
        /// </summary>
        public bool ImportXmlType { get; set; }

        /// <summary>
        /// Gets a dictionary that contains the mapping of data contract namespaces to the CLR namespaces that must be used to generate code during an import operation.
        /// </summary>
        public IDictionary<string, string> Namespaces => _namespaces ??= new Dictionary<string, string>();

        /// <summary>
        /// Gets a collection of types that represents data contract collections that should be referenced when generating code for collections, such as lists or dictionaries of items.
        /// </summary>
        public ICollection<Type> ReferencedCollectionTypes => _referencedCollectionTypes ??= new List<Type>();

        /// <summary>
        /// Gets a <see cref="IList{T}"/> containing types referenced in generated code.
        /// </summary>
        public ICollection<Type> ReferencedTypes => _referencedTypes ??= new List<Type>();

        /// <summary>
        /// A Func to processes the type that has been generated from the imported schema.
        /// </summary>
        public Func<CodeTypeDeclaration, CodeCompileUnit, CodeTypeDeclaration?>? ProcessImportedType;
    }
}
