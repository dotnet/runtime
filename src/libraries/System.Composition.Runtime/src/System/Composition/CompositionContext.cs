// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Composition.Hosting.Core;
using System.Composition.Hosting;

namespace System.Composition
{
    /// <summary>
    /// Provides retrieval of exports from the composition.
    /// </summary>
    public abstract class CompositionContext
    {
        private const string ImportManyImportMetadataConstraintName = "IsImportMany";

        /// <summary>
        /// Retrieve the single <paramref name="contract"/> instance from the
        /// <see cref="CompositionContext"/>.
        /// </summary>
        /// <param name="contract">The contract to retrieve.</param>
        /// <returns>An instance of the export.</returns>
        /// <param name="export">The export if available, otherwise, null.</param>
        /// <exception cref="CompositionFailedException" />
        public abstract bool TryGetExport(CompositionContract contract, out object export);

        /// <summary>
        /// Retrieve the single <typeparamref name="TExport"/> instance from the
        /// <see cref="CompositionContext"/>.
        /// </summary>
        /// <typeparam name="TExport">The type of the export to retrieve.</typeparam>
        /// <returns>An instance of the export.</returns>
        /// <exception cref="CompositionFailedException" />
        public TExport GetExport<TExport>()
        {
            return GetExport<TExport>((string)null);
        }

        /// <summary>
        /// Retrieve the single <typeparamref name="TExport"/> instance from the
        /// <see cref="CompositionContext"/>.
        /// </summary>
        /// <typeparam name="TExport">The type of the export to retrieve.</typeparam>
        /// <param name="contractName">Optionally, a discriminator that constrains the selection of the export.</param>
        /// <returns>An instance of the export.</returns>
        /// <exception cref="CompositionFailedException" />
        public TExport GetExport<TExport>(string contractName)
        {
            return (TExport)GetExport(typeof(TExport), contractName);
        }

        /// <summary>
        /// Retrieve the single <paramref name="exportType"/> instance from the
        /// <see cref="CompositionContext"/>.
        /// </summary>
        /// <param name="exportType">The type of the export to retrieve.</param>
        /// <param name="contractName">Optionally, a discriminator that constrains the selection of the export.</param>
        /// <returns>An instance of the export.</returns>
        /// <param name="export">The export if available, otherwise, null.</param>
        /// <exception cref="CompositionFailedException" />
        public bool TryGetExport(Type exportType, string contractName, out object export)
        {
            return TryGetExport(new CompositionContract(exportType, contractName), out export);
        }

        /// <summary>
        /// Retrieve the single <paramref name="exportType"/> instance from the
        /// <see cref="CompositionContext"/>.
        /// </summary>
        /// <param name="exportType">The type of the export to retrieve.</param>
        /// <returns>An instance of the export.</returns>
        /// <param name="export">The export if available, otherwise, null.</param>
        /// <exception cref="CompositionFailedException" />
        public bool TryGetExport(Type exportType, out object export)
        {
            return TryGetExport(exportType, null, out export);
        }

        /// <summary>
        /// Retrieve the single <typeparamref name="TExport"/> instance from the
        /// <see cref="CompositionContext"/>.
        /// </summary>
        /// <typeparam name="TExport">The type of the export to retrieve.</typeparam>
        /// <returns>An instance of the export.</returns>
        /// <param name="export">The export if available, otherwise, null.</param>
        /// <exception cref="CompositionFailedException" />
        public bool TryGetExport<TExport>(out TExport export)
        {
            return TryGetExport<TExport>(null, out export);
        }

        /// <summary>
        /// Retrieve the single <typeparamref name="TExport"/> instance from the
        /// <see cref="CompositionContext"/>.
        /// </summary>
        /// <typeparam name="TExport">The type of the export to retrieve.</typeparam>
        /// <param name="contractName">Optionally, a discriminator that constrains the selection of the export.</param>
        /// <returns>An instance of the export.</returns>
        /// <param name="export">The export if available, otherwise, null.</param>
        /// <exception cref="CompositionFailedException" />
        public bool TryGetExport<TExport>(string contractName, out TExport export)
        {
            object untypedExport;
            if (!TryGetExport(typeof(TExport), contractName, out untypedExport))
            {
                export = default(TExport);
                return false;
            }

            export = (TExport)untypedExport;
            return true;
        }

        /// <summary>
        /// Retrieve the single <paramref name="exportType"/> instance from the
        /// <see cref="CompositionContext"/>.
        /// </summary>
        /// <param name="exportType">The type of the export to retrieve.</param>
        /// <returns>An instance of the export.</returns>
        /// <exception cref="CompositionFailedException" />
        public object GetExport(Type exportType)
        {
            return GetExport(exportType, (string)null);
        }

        /// <summary>
        /// Retrieve the single <paramref name="exportType"/> instance from the
        /// <see cref="CompositionContext"/>.
        /// </summary>
        /// <param name="exportType">The type of the export to retrieve.</param>
        /// <param name="contractName">Optionally, a discriminator that constrains the selection of the export.</param>
        /// <returns>An instance of the export.</returns>
        /// <exception cref="CompositionFailedException" />
        public object GetExport(Type exportType, string contractName)
        {
            return GetExport(new CompositionContract(exportType, contractName));
        }

        /// <summary>
        /// Retrieve the single <paramref name="contract"/> instance from the
        /// <see cref="CompositionContext"/>.
        /// </summary>
        /// <param name="contract">The contract of the export to retrieve.</param>
        /// <returns>An instance of the export.</returns>
        /// <exception cref="CompositionFailedException" />
        public object GetExport(CompositionContract contract)
        {
            object export;
            if (!TryGetExport(contract, out export))
                throw new CompositionFailedException(
                    string.Format(SR.CompositionContext_NoExportFoundForContract, contract));

            return export;
        }

        /// <summary>
        /// Retrieve the single <paramref name="exportType"/> instance from the
        /// <see cref="CompositionContext"/>.
        /// </summary>
        /// <param name="exportType">The type of the export to retrieve.</param>
        /// <exception cref="CompositionFailedException" />
        public IEnumerable<object> GetExports(Type exportType)
        {
            return GetExports(exportType, (string)null);
        }

        /// <summary>
        /// Retrieve the single <paramref name="exportType"/> instance from the
        /// <see cref="CompositionContext"/>.
        /// </summary>
        /// <param name="exportType">The type of the export to retrieve.</param>
        /// <param name="contractName">The discriminator to apply when selecting the export.</param>
        /// <returns>An instance of the export.</returns>
        /// <exception cref="CompositionFailedException" />
        public IEnumerable<object> GetExports(Type exportType, string contractName)
        {
            var manyContract = new CompositionContract(
                exportType.MakeArrayType(),
                contractName,
                new Dictionary<string, object> { { ImportManyImportMetadataConstraintName, true } });

            return (IEnumerable<object>)GetExport(manyContract);
        }

        /// <summary>
        /// Retrieve the single <typeparamref name="TExport"/> instance from the
        /// <see cref="CompositionContext"/>.
        /// </summary>
        /// <typeparam name="TExport">The export type to retrieve.</typeparam>
        /// <returns>An instance of the export.</returns>
        /// <exception cref="CompositionFailedException" />
        public IEnumerable<TExport> GetExports<TExport>()
        {
            return GetExports<TExport>((string)null);
        }

        /// <summary>
        /// Retrieve the single <typeparamref name="TExport"/> instance from the
        /// <see cref="CompositionContext"/>.
        /// </summary>
        /// <typeparam name="TExport">The export type to retrieve.</typeparam>
        /// <returns>An instance of the export.</returns>
        /// <param name="contractName">The discriminator to apply when selecting the export.</param>
        /// <exception cref="CompositionFailedException" />
        public IEnumerable<TExport> GetExports<TExport>(string contractName)
        {
            return (IEnumerable<TExport>)GetExports(typeof(TExport), contractName);
        }
    }
}
