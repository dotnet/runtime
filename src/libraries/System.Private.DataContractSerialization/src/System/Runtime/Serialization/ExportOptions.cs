// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;

namespace System.Runtime.Serialization
{
    /// <summary>
    /// Represents the options that can be set for an <see cref="XsdDataContractExporter"/>.
    /// </summary>
    /// <remarks>
    /// The <see cref="XsdDataContractExporter"/> is used to generate XSD schemas from a type or assembly. You can also use the XsdDataContractImporter to generate .NET Framework code from a schema document.
    ///
    /// The <see cref="KnownTypes"/> property is used by the <see cref="DataContractSerializer"/> to include types that can be read in an object graph.
    /// </remarks>
    public class ExportOptions
    {
        private Collection<Type>? _knownTypes;

        /// <summary>
        /// Gets or sets a serialization surrogate provider.
        /// </summary>
        public ISerializationSurrogateProvider? DataContractSurrogate { get; set; }

        /// <summary>
        /// Gets the collection of types that may be encountered during serialization or deserialization.
        /// </summary>
        public Collection<Type> KnownTypes => _knownTypes ??= new Collection<Type>();
    }
}
