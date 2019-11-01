// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Composition.Hosting.Core;
using System.Composition.TypedParts.ActivationFeatures;
using System.Composition.Hosting;

namespace System.Composition.TypedParts
{
    internal static class ContractHelpers
    {
        private const string ImportManyImportMetadataConstraintName = "IsImportMany";

        public static bool TryGetExplicitImportInfo(Type memberType, object[] attributes, object site, out ImportInfo importInfo)
        {
            if (attributes.Any(a => a is ImportAttribute || a is ImportManyAttribute))
            {
                importInfo = GetImportInfo(memberType, attributes, site);
                return true;
            }

            importInfo = null;
            return false;
        }

        public static ImportInfo GetImportInfo(Type memberType, object[] attributes, object site)
        {
            var importedContract = new CompositionContract(memberType);
            IDictionary<string, object> importMetadata = null;
            var allowDefault = false;
            var explicitImportsApplied = 0;

            foreach (var attr in attributes)
            {
                var ia = attr as ImportAttribute;
                if (ia != null)
                {
                    importedContract = new CompositionContract(memberType, ia.ContractName);
                    allowDefault = ia.AllowDefault;
                    explicitImportsApplied++;
                }
                else
                {
                    var ima = attr as ImportManyAttribute;
                    if (ima != null)
                    {
                        importMetadata = importMetadata ?? new Dictionary<string, object>();
                        importMetadata.Add(ImportManyImportMetadataConstraintName, true);
                        importedContract = new CompositionContract(memberType, ima.ContractName);
                        explicitImportsApplied++;
                    }
                    else
                    {
                        var imca = attr as ImportMetadataConstraintAttribute;
                        if (imca != null)
                        {
                            importMetadata = importMetadata ?? new Dictionary<string, object>();
                            importMetadata.Add(imca.Name, imca.Value);
                        }
                    }
                }

                var attrType = attr.GetType();
                // Note, we don't support ReflectionContext in this scenario
                if (attrType.GetTypeInfo().GetCustomAttribute<MetadataAttributeAttribute>(true) != null)
                {
                    // We don't coalesce to collections here the way export metadata does
                    foreach (var prop in attrType
                        .GetRuntimeProperties()
                        .Where(p => p.GetMethod.IsPublic && p.DeclaringType == attrType && p.CanRead))
                    {
                        importMetadata = importMetadata ?? new Dictionary<string, object>();
                        importMetadata.Add(prop.Name, prop.GetValue(attr, null));
                    }
                }
            }

            if (explicitImportsApplied > 1)
            {
                string message = SR.Format(SR.ContractHelpers_TooManyImports, site);
                throw new CompositionFailedException(message);
            }

            if (importMetadata != null)
            {
                importedContract = new CompositionContract(importedContract.ContractType, importedContract.ContractName, importMetadata);
            }

            return new ImportInfo(importedContract, allowDefault);
        }

        public static bool IsShared(IDictionary<string, object> partMetadata)
        {
            return partMetadata.ContainsKey(LifetimeFeature.SharingBoundaryPartMetadataName);
        }
    }
}
