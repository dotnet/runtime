// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace System.Composition.Convention
{
    /// <summary>
    /// Configures an import associated with a part.
    /// </summary>
    public sealed class ImportConventionBuilder
    {
        private static readonly Type[] s_supportedImportManyTypes = new[] { typeof(IList<>), typeof(ICollection<>), typeof(IEnumerable<>) };

        private string _contractName;
        private bool? _asMany;
        private bool _allowDefault;
        private Func<Type, string> _getContractNameFromPartType;
        private List<Tuple<string, object>> _metadataConstraintItems;
        private List<Tuple<string, Func<Type, object>>> _metadataConstraintItemFuncs;

        internal ImportConventionBuilder() { }

        /// <summary>
        /// Specify the contract name for the import.
        /// </summary>
        /// <param name="contractName"></param>
        /// <returns>An import builder allowing further configuration.</returns>
        public ImportConventionBuilder AsContractName(string contractName!!)
        {
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
        public ImportConventionBuilder AsContractName(Func<Type, string> getContractNameFromPartType!!)
        {
            _getContractNameFromPartType = getContractNameFromPartType;
            return this;
        }

        /// <summary>
        /// Configure the import to receive all exports of the contract.
        /// </summary>
        /// <returns>An import builder allowing further configuration.</returns>
        public ImportConventionBuilder AsMany()
        {
            return AsMany(true);
        }

        /// <summary>
        /// Configure the import to receive all exports of the contract.
        /// </summary>
        /// <param name="isMany">True if the import receives all values; otherwise false.</param>
        /// <returns>An import builder allowing further configuration.</returns>
        public ImportConventionBuilder AsMany(bool isMany)
        {
            _asMany = isMany;
            return this;
        }

        /// <summary>
        /// Allow the import to receive the default value for its type if
        /// the contract cannot be supplied by another part.
        /// </summary>
        /// <returns>An import builder allowing further configuration.</returns>
        public ImportConventionBuilder AllowDefault()
        {
            _allowDefault = true;
            return this;
        }

        /// <summary>
        /// Add an import constraint
        /// </summary>
        /// <param name="name">The name of the constraint item.</param>
        /// <param name="value">The value to match.</param>
        /// <returns>An import builder allowing further configuration.</returns>
        public ImportConventionBuilder AddMetadataConstraint(string name!!, object value)
        {
            if (name.Length == 0)
            {
                throw new ArgumentException(SR.Format(SR.ArgumentException_EmptyString, nameof(name)), nameof(name));
            }
            if (_metadataConstraintItems == null)
            {
                _metadataConstraintItems = new List<Tuple<string, object>>();
            }
            _metadataConstraintItems.Add(Tuple.Create(name, value));
            return this;
        }

        /// <summary>
        /// Add an import constraint
        /// </summary>
        /// <param name="name">The name of the constraint item.</param>
        /// <param name="getConstraintValueFromPartType">A function that calculates the value to match.</param>
        /// <returns>An export builder allowing further configuration.</returns>
        public ImportConventionBuilder AddMetadataConstraint(string name!!, Func<Type, object> getConstraintValueFromPartType!!)
        {
            if (name.Length == 0)
            {
                throw new ArgumentException(SR.Format(SR.ArgumentException_EmptyString, nameof(name)), nameof(name));
            }

            if (_metadataConstraintItemFuncs == null)
            {
                _metadataConstraintItemFuncs = new List<Tuple<string, Func<Type, object>>>();
            }
            _metadataConstraintItemFuncs.Add(Tuple.Create(name, getConstraintValueFromPartType));
            return this;
        }

        internal void BuildAttributes(Type type, ref List<Attribute> attributes)
        {
            Attribute importAttribute;

            var contractName = (_getContractNameFromPartType != null) ? _getContractNameFromPartType(type) : _contractName;

            // Infer from Type when not explicitly set.
            var asMany = _asMany ?? IsSupportedImportManyType(type.GetTypeInfo());
            if (!asMany)
            {
                importAttribute = new ImportAttribute(contractName)
                {
                    AllowDefault = _allowDefault
                };
            }
            else
            {
                importAttribute = new ImportManyAttribute(contractName);
            }
            if (attributes == null)
            {
                attributes = new List<Attribute>();
            }
            attributes.Add(importAttribute);


            //Add metadata attributes from direct specification
            if (_metadataConstraintItems != null)
            {
                foreach (Tuple<string, object> item in _metadataConstraintItems)
                {
                    attributes.Add(new ImportMetadataConstraintAttribute(item.Item1, item.Item2));
                }
            }

            //Add metadata attributes from func specification
            if (_metadataConstraintItemFuncs != null)
            {
                foreach (Tuple<string, Func<Type, object>> item in _metadataConstraintItemFuncs)
                {
                    var name = item.Item1;
                    var value = (item.Item2 != null) ? item.Item2(type) : null;
                    attributes.Add(new ImportMetadataConstraintAttribute(name, value));
                }
            }
            return;
        }

        private static bool IsSupportedImportManyType(TypeInfo typeInfo)
        {
            return typeInfo.IsArray ||
                (typeInfo.IsGenericTypeDefinition && s_supportedImportManyTypes.Contains(typeInfo.AsType())) ||
                (typeInfo.AsType().IsConstructedGenericType && s_supportedImportManyTypes.Contains(typeInfo.GetGenericTypeDefinition()));
        }
    }
}
