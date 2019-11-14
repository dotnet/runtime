// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.ComponentModel.Composition.Registration
{
    public sealed class ExportBuilder
    {
        private bool _isInherited;
        private string _contractName;
        private Type _contractType;
        private List<Tuple<string, object>> _metadataItems;
        private List<Tuple<string, Func<Type, object>>> _metadataItemFuncs;

        public ExportBuilder() { }

        public ExportBuilder AsContractType<T>()
        {
            return AsContractType(typeof(T));
        }

        public ExportBuilder AsContractType(Type type)
        {
            _contractType = type;
            return this;
        }

        public ExportBuilder AsContractName(string contractName)
        {
            _contractName = contractName;
            return this;
        }

        public ExportBuilder Inherited()
        {
            _isInherited = true;
            return this;
        }

        public ExportBuilder AddMetadata(string name, object value)
        {
            if (_metadataItems == null)
            {
                _metadataItems = new List<Tuple<string, object>>();
            }
            _metadataItems.Add(Tuple.Create(name, value));

            return this;
        }

        public ExportBuilder AddMetadata(string name, Func<Type, object> itemFunc)
        {
            if (_metadataItemFuncs == null)
            {
                _metadataItemFuncs = new List<Tuple<string, Func<Type, object>>>();
            }
            _metadataItemFuncs.Add(Tuple.Create(name, itemFunc));

            return this;
        }

        internal void BuildAttributes(Type type, ref List<Attribute> attributes)
        {
            if (attributes == null)
            {
                attributes = new List<Attribute>();
            }

            if (_isInherited)
            {
                // Default export
                attributes.Add(new InheritedExportAttribute(_contractName, _contractType));
            }
            else
            {
                // Default export
                attributes.Add(new ExportAttribute(_contractName, _contractType));
            }

            //Add metadata attributes from direct specification
            if (_metadataItems != null)
            {
                foreach (Tuple<string, object> item in _metadataItems)
                {
                    attributes.Add(new ExportMetadataAttribute(item.Item1, item.Item2));
                }
            }

            //Add metadata attributes from func specification
            if (_metadataItemFuncs != null)
            {
                foreach (Tuple<string, Func<Type, object>> item in _metadataItemFuncs)
                {
                    string name = item.Item1;
                    object value = (item.Item2 != null) ? item.Item2(type.UnderlyingSystemType) : null;
                    attributes.Add(new ExportMetadataAttribute(name, value));
                }
            }
        }
    }
}
