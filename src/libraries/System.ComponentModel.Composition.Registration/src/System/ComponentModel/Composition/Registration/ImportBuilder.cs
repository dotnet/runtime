// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.ComponentModel.Composition.Registration
{
    public sealed class ImportBuilder
    {
        private static readonly Type s_stringType = typeof(string);
        private string _contractName;
        private Type _contractType;
        private bool _asMany;
        private bool _asManySpecified;
        private bool _allowDefault;
        private bool _allowRecomposition;
        private CreationPolicy _requiredCreationPolicy;
        private ImportSource _source;

        public ImportBuilder() { }

        public ImportBuilder AsContractType<T>()
        {
            return AsContractType(typeof(T));
        }

        public ImportBuilder AsContractType(Type type)
        {
            _contractType = type;
            return this;
        }

        public ImportBuilder AsContractName(string contractName)
        {
            _contractName = contractName;
            return this;
        }

        public ImportBuilder AsMany(bool isMany = true)
        {
            _asMany = isMany;
            _asManySpecified = true;
            return this;
        }

        public ImportBuilder AllowDefault()
        {
            _allowDefault = true;
            return this;
        }

        public ImportBuilder AllowRecomposition()
        {
            _allowRecomposition = true;
            return this;
        }

        public ImportBuilder RequiredCreationPolicy(CreationPolicy requiredCreationPolicy)
        {
            _requiredCreationPolicy = requiredCreationPolicy;
            return this;
        }

        public ImportBuilder Source(ImportSource source)
        {
            _source = source;
            return this;
        }

        internal void BuildAttributes(Type type, ref List<Attribute> attributes)
        {
            Attribute importAttribute;

            // Infer from Type when not explicitly set.
            bool asMany = (!_asManySpecified) ? type != s_stringType && typeof(IEnumerable).IsAssignableFrom(type) : _asMany;
            if (!asMany)
            {
                importAttribute = new ImportAttribute(_contractName, _contractType)
                {
                    AllowDefault = _allowDefault,
                    AllowRecomposition = _allowRecomposition,
                    RequiredCreationPolicy = _requiredCreationPolicy,
                    Source = _source
                };
            }
            else
            {
                importAttribute = new ImportManyAttribute(_contractName, _contractType)
                {
                    AllowRecomposition = _allowRecomposition,
                    RequiredCreationPolicy = _requiredCreationPolicy,
                    Source = _source
                };
            }

            attributes ??= new List<Attribute>();

            attributes.Add(importAttribute);
        }
    }
}
