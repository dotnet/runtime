// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace System.Runtime.Serialization.Schema
{
    public sealed class ImportOptions
    {
        private ICollection<Type>? _referencedTypes;
        private ICollection<Type>? _referencedCollectionTypes;
        private IDictionary<string, string>? _namespaces;

        public CodeDomProvider? CodeProvider { get; set; }

        public bool EnableDataBinding { get; set; }

        public ISerializationSurrogateProvider? SurrogateProvider { get; set; }

        public bool GenerateInternal { get; set; }

        public bool GenerateSerializable { get; set; }

        public bool ImportXmlType { get; set; }

        public IDictionary<string, string> Namespaces => _namespaces ??= new Dictionary<string, string>();

        public ICollection<Type> ReferencedCollectionTypes => _referencedCollectionTypes ??= new List<Type>();

        public ICollection<Type> ReferencedTypes => _referencedTypes ??= new List<Type>();

        public Func<CodeTypeDeclaration, CodeCompileUnit, CodeTypeDeclaration?>? ProcessImportedType;
    }
}
