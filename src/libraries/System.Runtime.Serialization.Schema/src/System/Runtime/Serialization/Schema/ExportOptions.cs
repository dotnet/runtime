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
    public sealed class ExportOptions
    {
        public ISerializationSurrogateProvider? SurrogateProvider { get; set; }

        private Collection<Type>? _knownTypes;
        public Collection<Type> KnownTypes => _knownTypes ??= new Collection<Type>();
    }
}
