// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Reflection;

namespace System.Composition.Convention
{
    /// <summary>
    /// Configures an export associated with a part.
    /// </summary>
    public sealed class ExportConventionBuilder
    {
        private string _contractName;
        private Type _contractType;
        private List<Tuple<string, object>> _metadataItems;
        private List<Tuple<string, Func<Type, object>>> _metadataItemFuncs;
        private Func<Type, string> _getContractNameFromPartType;

        internal ExportConventionBuilder() { }

        /// <summary>
        /// Specify the contract type for the export.
        /// </summary>
        /// <typeparam name="T">The contract type.</typeparam>
        /// <returns>An export builder allowing further configuration.</returns>
        public ExportConventionBuilder AsContractType<T>()
        {
            return AsContractType(typeof(T));
        }

        /// <summary>
        /// Specify the contract type for the export.
        /// </summary>
        /// <param name="type">The contract type.</param>
        /// <returns>An export builder allowing further configuration.</returns>
        public ExportConventionBuilder AsContractType(Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            _contractType = type;
            return this;
        }

        /// <summary>
        /// Specify the contract name for the export.
        /// </summary>
        /// <param name="contractName">The contract name.</param>
        /// <returns>An export builder allowing further configuration.</returns>
        public ExportConventionBuilder AsContractName(string contractName)
        {
            if (contractName is null)
            {
                throw new ArgumentNullException(nameof(contractName));
            }

            if (contractName.Length == 0)
            {
                throw new ArgumentException(SR.Format(SR.ArgumentException_EmptyString, nameof(contractName)), nameof(contractName));
            }
            _contractName = contractName;
            return this;
        }

        /// <summary>
        /// Specify the contract name for the export.
        /// </summary>
        /// <param name="getContractNameFromPartType">A Func to retrieve the contract name from the part typeThe contract name.</param>
        /// <returns>An export builder allowing further configuration.</returns>
        public ExportConventionBuilder AsContractName(Func<Type, string> getContractNameFromPartType)
        {
            if (getContractNameFromPartType is null)
            {
                throw new ArgumentNullException(nameof(getContractNameFromPartType));
            }

            _getContractNameFromPartType = getContractNameFromPartType;
            return this;
        }

        /// <summary>
        /// Add export metadata to the export.
        /// </summary>
        /// <param name="name">The name of the metadata item.</param>
        /// <param name="value">The value of the metadata item.</param>
        /// <returns>An export builder allowing further configuration.</returns>
        public ExportConventionBuilder AddMetadata(string name, object value)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (name.Length == 0)
            {
                throw new ArgumentException(SR.Format(SR.ArgumentException_EmptyString, nameof(name)), nameof(name));
            }

            _metadataItems ??= new List<Tuple<string, object>>();
            _metadataItems.Add(Tuple.Create(name, value));
            return this;
        }

        /// <summary>
        /// Add export metadata to the export.
        /// </summary>
        /// <param name="name">The name of the metadata item.</param>
        /// <param name="getValueFromPartType">A function that calculates the metadata value based on the type.</param>
        /// <returns>An export builder allowing further configuration.</returns>
        public ExportConventionBuilder AddMetadata(string name, Func<Type, object> getValueFromPartType)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (getValueFromPartType is null)
            {
                throw new ArgumentNullException(nameof(getValueFromPartType));
            }

            if (name.Length == 0)
            {
                throw new ArgumentException(SR.Format(SR.ArgumentException_EmptyString, nameof(name)), nameof(name));
            }

            _metadataItemFuncs ??= new List<Tuple<string, Func<Type, object>>>();
            _metadataItemFuncs.Add(Tuple.Create(name, getValueFromPartType));
            return this;
        }

        internal void BuildAttributes(Type type, ref List<Attribute> attributes)
        {
            attributes ??= new List<Attribute>();

            var contractName = (_getContractNameFromPartType != null) ? _getContractNameFromPartType(type) : _contractName;
            attributes.Add(new ExportAttribute(contractName, _contractType));

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
                    var name = item.Item1;
                    var value = (item.Item2 != null) ? item.Item2(type) : null;
                    attributes.Add(new ExportMetadataAttribute(name, value));
                }
            }
        }
    }
}
