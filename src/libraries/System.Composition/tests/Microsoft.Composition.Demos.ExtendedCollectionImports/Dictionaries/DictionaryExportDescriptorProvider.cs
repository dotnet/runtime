// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition.Hosting;
using System.Composition.Hosting.Core;
using System.Linq;
using System.Reflection;
using Microsoft.Composition.Demos.ExtendedCollectionImports.Util;

namespace Microsoft.Composition.Demos.ExtendedCollectionImports.Dictionaries
{
    public class DictionaryExportDescriptorProvider : ExportDescriptorProvider
    {
        /// <summary>
        /// Identifies the metadata used as key for a dictionary import.
        /// </summary>
        private const string KeyByMetadataImportMetadataConstraintName = "KeyMetadataName";

        private static readonly MethodInfo s_getDictionaryDefinitionsMethod = typeof(DictionaryExportDescriptorProvider).GetTypeInfo().GetDeclaredMethod("GetDictionaryDefinition");

        public override IEnumerable<ExportDescriptorPromise> GetExportDescriptors(CompositionContract contract, DependencyAccessor descriptorAccessor)
        {
            if (!(contract.ContractType.IsConstructedGenericType && contract.ContractType.GetGenericTypeDefinition() == typeof(IDictionary<,>)))
                return NoExportDescriptors;

            CompositionContract unwrapped;
            string keyByMetadataName;
            if (!contract.TryUnwrapMetadataConstraint(KeyByMetadataImportMetadataConstraintName, out keyByMetadataName, out unwrapped))
                return NoExportDescriptors;

            var args = contract.ContractType.GenericTypeArguments;
            var keyType = args[0];
            var valueType = args[1];

            var valueContract = unwrapped.ChangeType(valueType);

            var gdd = s_getDictionaryDefinitionsMethod.MakeGenericMethod(keyType, valueType);

            return new[] { (ExportDescriptorPromise)gdd.Invoke(null, new object[] { contract, valueContract, descriptorAccessor, keyByMetadataName }) };
        }

        private static ExportDescriptorPromise GetDictionaryDefinition<TKey, TValue>(CompositionContract dictionaryContract, CompositionContract valueContract, DependencyAccessor definitionAccessor, string keyByMetadataName)
        {
            return new ExportDescriptorPromise(
                dictionaryContract,
                typeof(IDictionary<TKey, TValue>).Name,
                false,
                () => definitionAccessor.ResolveDependencies("value", valueContract, true),
                deps =>
                {
                    var items = deps.Select(d => Tuple.Create(d.Target.Origin, d.Target.GetDescriptor())).ToArray();
                    var isValidated = false;
                    return ExportDescriptor.Create((c, o) =>
                    {
                        if (!isValidated)
                        {
                            Validate<TKey>(items, keyByMetadataName);
                            isValidated = true;
                        }

                        return items.ToDictionary(
                            item => (TKey)item.Item2.Metadata[keyByMetadataName],
                            item => (TValue)item.Item2.Activator(c, o));
                    },
                    NoMetadata);
                });
        }

        private static void Validate<TKey>(Tuple<string, ExportDescriptor>[] partsWithMatchedDescriptors, string keyByMetadataName)
        {
            var missing = partsWithMatchedDescriptors.Where(p => !p.Item2.Metadata.ContainsKey(keyByMetadataName)).ToArray();
            if (missing.Length != 0)
            {
                var problems = Formatters.ReadableQuotedList(missing.Select(p => p.Item1));
                var message = string.Format("The metadata '{0}' cannot be used as a dictionary import key because it is missing from exports on part(s) {1}.", keyByMetadataName, problems);
                throw new CompositionFailedException(message);
            }

            var wrongType = partsWithMatchedDescriptors.Where(p => !(p.Item2.Metadata[keyByMetadataName] is TKey)).ToArray();
            if (wrongType.Length != 0)
            {
                var problems = Formatters.ReadableQuotedList(wrongType.Select(p => p.Item1));
                var message = string.Format("The metadata '{0}' cannot be used as a dictionary import key of type '{1}' because the value(s) supplied by {2} are of the wrong type.", keyByMetadataName, typeof(TKey).Name, problems);
                throw new CompositionFailedException(message);
            }

            var firstDuplicated = partsWithMatchedDescriptors.GroupBy(p => (TKey)p.Item2.Metadata[keyByMetadataName]).Where(g => g.Count() > 1).FirstOrDefault();
            if (firstDuplicated != null)
            {
                var problems = Formatters.ReadableQuotedList(firstDuplicated.Select(p => p.Item1));
                var message = string.Format("The metadata '{0}' cannot be used as a dictionary import key because the value '{1}' is associated with exports from parts {2}.", keyByMetadataName, firstDuplicated.Key, problems);
                throw new CompositionFailedException(message);
            }
        }
    }
}
