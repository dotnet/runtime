// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;

namespace System.Runtime.Serialization.Schema
{
    // NOTE TODO smolloy - ExportOptions, ImportOptions, XsdDataContractExporter, XsdDataContractImporter... they all have the same
    //  names in this library as they did in 4.8... and as the two 'Export' classes currently have in Core. The namespace is
    //  different here so there isn't a collision. But should we consider using different names for these classes?
    //  (The 'SurrogateProvider' property is named differently from previous versions. Should we align that property with whatever
    //  decision we make on the class names?)
    /// <summary>
    /// Represents the options that can be set for an <see cref="XsdDataContractExporter"/>.
    /// </summary>
    /// <remarks>
    /// The <see cref="XsdDataContractExporter"/> is used to generate XSD schemas from a type or assembly. You can also use the <see cref="XsdDataContractImporter"/> to generate .NET Framework code from a schema document.
    ///
    /// The <see cref="KnownTypes"/> property is used by the <see cref="DataContractSerializer"/> to include types that can be read in an object graph.
    /// </remarks>
    public sealed class ExportOptions
    {
        /// <summary>
        /// Gets or sets a serialization surrogate provider.
        /// </summary>
        public ISerializationSurrogateProvider? SurrogateProvider { get; set; }

        private Collection<Type>? _knownTypes;
        /// <summary>
        /// Gets the collection of types that may be encountered during serialization or deserialization.
        /// </summary>
        public Collection<Type> KnownTypes => _knownTypes ??= new Collection<Type>();
    }
}
