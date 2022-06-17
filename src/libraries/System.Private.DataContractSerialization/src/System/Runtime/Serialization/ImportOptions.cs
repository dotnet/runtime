// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if smolloy_add_schema_import

namespace System.Runtime.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.CodeDom.Compiler;

    public class ImportOptions
    {
        private bool _generateSerializable;
        private bool _generateInternal;
        private bool _enableDataBinding;
        private CodeDomProvider? _codeProvider;
        private ICollection<Type>? _referencedTypes;
        private ICollection<Type>? _referencedCollectionTypes;
        private IDictionary<string, string>? _namespaces;
        private bool _importXmlType;
        private ISerializationExtendedSurrogateProvider? _serializationExtendedSurrogateProvider;

        public bool GenerateSerializable
        {
            get { return _generateSerializable; }
            set { _generateSerializable = value; }
        }

        public bool GenerateInternal
        {
            get { return _generateInternal; }
            set { _generateInternal = value; }
        }

        public bool EnableDataBinding
        {
            get { return _enableDataBinding; }
            set { _enableDataBinding = value; }
        }

        public CodeDomProvider? CodeProvider
        {
            get { return _codeProvider; }
            set { _codeProvider = value; }
        }

        public ICollection<Type> ReferencedTypes
        {
            get
            {
                if (_referencedTypes == null)
                {
                    _referencedTypes = new List<Type>();
                }
                return _referencedTypes;
            }
        }

        public ICollection<Type> ReferencedCollectionTypes
        {
            get
            {
                if (_referencedCollectionTypes == null)
                {
                    _referencedCollectionTypes = new List<Type>();
                }
                return _referencedCollectionTypes;
            }
        }

        public IDictionary<string, string> Namespaces
        {
            get
            {
                if (_namespaces == null)
                {
                    _namespaces = new Dictionary<string, string>();
                }
                return _namespaces;
            }
        }

        public bool ImportXmlType
        {
            get { return _importXmlType; }
            set { _importXmlType = value; }
        }

        public ISerializationExtendedSurrogateProvider? SerializationExtendedSurrogateProvider
        {
            get { return _serializationExtendedSurrogateProvider; }
            set { _serializationExtendedSurrogateProvider = value; }
        }
    }
}


#endif
