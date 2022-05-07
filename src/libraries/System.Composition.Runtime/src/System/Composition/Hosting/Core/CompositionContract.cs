// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Composition.Runtime.Util;
using System.Linq;

namespace System.Composition.Hosting.Core
{
    /// <summary>
    /// The link between exports and imports.
    /// </summary>
    public sealed class CompositionContract
    {
        private readonly Type _contractType;
        private readonly string _contractName;
        private readonly IDictionary<string, object> _metadataConstraints;

        /// <summary>
        /// Construct a <see cref="CompositionContract"/>.
        /// </summary>
        /// <param name="contractType">The type shared between the exporter and importer.</param>
        public CompositionContract(Type contractType)
            : this(contractType, null)
        {
        }

        /// <summary>
        /// Construct a <see cref="CompositionContract"/>.
        /// </summary>
        /// <param name="contractType">The type shared between the exporter and importer.</param>
        /// <param name="contractName">Optionally, a name that discriminates this contract from others with the same type.</param>
        public CompositionContract(Type contractType, string contractName)
            : this(contractType, contractName, null)
        {
        }

        /// <summary>
        /// Construct a <see cref="CompositionContract"/>.
        /// </summary>
        /// <param name="contractType">The type shared between the exporter and importer.</param>
        /// <param name="contractName">Optionally, a name that discriminates this contract from others with the same type.</param>
        /// <param name="metadataConstraints">Optionally, a non-empty collection of named constraints that apply to the contract.</param>
        public CompositionContract(Type contractType, string contractName, IDictionary<string, object> metadataConstraints)
        {
            if (contractType == null) throw new ArgumentNullException(nameof(contractType));
            if (metadataConstraints?.Count == 0) throw new ArgumentOutOfRangeException(nameof(metadataConstraints));

            _contractType = contractType;
            _contractName = contractName;
            _metadataConstraints = metadataConstraints;
        }

        /// <summary>
        /// The type shared between the exporter and importer.
        /// </summary>
        public Type ContractType => _contractType;

        /// <summary>
        /// A name that discriminates this contract from others with the same type.
        /// </summary>
        public string ContractName => _contractName;

        /// <summary>
        /// Constraints applied to the contract. Instead of using this collection
        /// directly it is advisable to use the <see cref="TryUnwrapMetadataConstraint"/> method.
        /// </summary>
        public IEnumerable<KeyValuePair<string, object>> MetadataConstraints => _metadataConstraints;

        /// <summary>
        /// Determines equality between two contracts.
        /// </summary>
        /// <param name="obj">The contract to test.</param>
        /// <returns>True if the contracts are equivalent; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            var contract = obj as CompositionContract;
            return contract != null &&
                contract._contractType.Equals(_contractType) &&
                (_contractName == null ? contract._contractName == null : _contractName.Equals(contract._contractName)) &&
                ConstraintEqual(_metadataConstraints, contract._metadataConstraints);
        }

        /// <summary>
        /// Gets a hash code for the contract.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            var hc = _contractType.GetHashCode();
            if (_contractName != null)
                hc = hc ^ _contractName.GetHashCode();
            if (_metadataConstraints != null)
                hc = hc ^ ConstraintHashCode(_metadataConstraints);
            return hc;
        }

        /// <summary>
        /// Creates a string representation of the contract.
        /// </summary>
        /// <returns>A string representation of the contract.</returns>
        public override string ToString()
        {
            var result = Formatters.Format(_contractType);

            if (_contractName != null)
                result += " " + Formatters.Format(_contractName);

            if (_metadataConstraints != null)
                result += string.Format(" {{ {0} }}",
                    string.Join(SR.Formatter_ListSeparatorWithSpace,
                        _metadataConstraints.Select(kv => $"{kv.Key} = {Formatters.Format(kv.Value)}")));

            return result;
        }

        /// <summary>
        /// Transform the contract into a matching contract with a
        /// new contract type (with the same contract name and constraints).
        /// </summary>
        /// <param name="newContractType">The contract type for the new contract.</param>
        /// <returns>A matching contract with a
        /// new contract type.</returns>
        public CompositionContract ChangeType(Type newContractType)
        {
            if (newContractType == null) throw new ArgumentNullException(nameof(newContractType));
            return new CompositionContract(newContractType, _contractName, _metadataConstraints);
        }

        /// <summary>
        /// Check the contract for a constraint with a particular name  and value, and, if it exists,
        /// retrieve both the value and the remainder of the contract with the constraint
        /// removed.
        /// </summary>
        /// <typeparam name="T">The type of the constraint value.</typeparam>
        /// <param name="constraintName">The name of the constraint.</param>
        /// <param name="constraintValue">The value if it is present and of the correct type, otherwise null.</param>
        /// <param name="remainingContract">The contract with the constraint removed if present, otherwise null.</param>
        /// <returns>True if the constraint is present and of the correct type, otherwise false.</returns>
        public bool TryUnwrapMetadataConstraint<T>(string constraintName, out T constraintValue, out CompositionContract remainingContract)
        {
            if (constraintName is null)
            {
                throw new ArgumentNullException(nameof(constraintName));
            }

            constraintValue = default(T);
            remainingContract = null;

            if (_metadataConstraints == null)
                return false;

            if (!_metadataConstraints.TryGetValue(constraintName, out object value))
                return false;

            if (!(value is T))
                return false;

            constraintValue = (T)value;
            if (_metadataConstraints.Count == 1)
            {
                remainingContract = new CompositionContract(_contractType, _contractName);
            }
            else
            {
                var remainingConstraints = new Dictionary<string, object>(_metadataConstraints);
                remainingConstraints.Remove(constraintName);
                remainingContract = new CompositionContract(_contractType, _contractName, remainingConstraints);
            }

            return true;
        }

        internal static bool ConstraintEqual(IDictionary<string, object> first, IDictionary<string, object> second)
        {
            if (first == second)
                return true;

            if (first == null || second == null)
                return false;

            if (first.Count != second.Count)
                return false;

            foreach (KeyValuePair<string, object> firstItem in first)
            {
                if (!second.TryGetValue(firstItem.Key, out object secondValue))
                    return false;

                if (firstItem.Value == null)
                {
                    return secondValue == null;
                }
                else if (secondValue == null)
                {
                    return false;
                }
                else
                {
                    if (firstItem.Value is IEnumerable firstEnumerable && !(firstEnumerable is string))
                    {
                        var secondEnumerable = secondValue as IEnumerable;
                        if (secondEnumerable == null || !Enumerable.SequenceEqual(firstEnumerable.Cast<object>(), secondEnumerable.Cast<object>()))
                            return false;
                    }
                    else if (!firstItem.Value.Equals(secondValue))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static int ConstraintHashCode(IDictionary<string, object> metadata)
        {
            var result = -1;
            foreach (KeyValuePair<string, object> kv in metadata)
            {
                result ^= kv.Key.GetHashCode();
                if (kv.Value != null)
                {
                    if (kv.Value is string sval)
                    {
                        result ^= sval.GetHashCode();
                    }
                    else
                    {
                        if (kv.Value is IEnumerable enumerableValue)
                        {
                            foreach (var ev in enumerableValue)
                                if (ev != null)
                                    result ^= ev.GetHashCode();
                        }
                        else
                        {
                            result ^= kv.Value.GetHashCode();
                        }
                    }
                }
            }

            return result;
        }
    }
}
