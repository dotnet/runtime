// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;

namespace System.ComponentModel.Composition
{
    internal static class CompositionContainerExtensions
    {
        public static bool IsPresent<T>(this CompositionContainer container)
        {
            try
            {
                container.GetExportedValue<T>();
                return true;
            }
            catch (ImportCardinalityMismatchException)
            {
                return false;
            }
        }

        public static bool IsPresent(this CompositionContainer container, string contractName)
        {
            try
            {
                container.GetExportedValue<object>(contractName);
                return true;
            }
            catch (ImportCardinalityMismatchException)
            {
                return false;
            }
        }

        public static bool IsPresent<T>(this ExportProvider container)
        {
            try
            {
                container.GetExportedValue<T>();
                return true;
            }
            catch (ImportCardinalityMismatchException)
            {
                return false;
            }
        }

        public static bool IsPresent(this ExportProvider container, string contractName)
        {
            try
            {
                container.GetExportedValue<object>(contractName);
                return true;
            }
            catch (ImportCardinalityMismatchException)
            {
                return false;
            }
        }

        public static void AddAndComposeExportedValue<T>(this CompositionContainer container, T exportedValue)
        {
            var batch = new CompositionBatch();
            batch.AddExportedValue<T>(exportedValue);
            container.Compose(batch);
        }

        public static void AddAndComposeExportedValue<T>(this CompositionContainer container, string contractName, T exportedValue)
        {
            var batch = new CompositionBatch();
            batch.AddExportedValue<T>(contractName, exportedValue);
            container.Compose(batch);
        }

        public static void AddParts(this CompositionBatch batch, params object[] parts)
        {
            foreach (object instance in parts)
            {
                ComposablePart part = instance as ComposablePart;
                if (part != null)
                {
                    batch.AddPart(part);
                }
                else
                {
                    batch.AddPart((object)instance);
                }
            }
        }

        public static ComposablePart AddExportedValue(this CompositionBatch batch, string contractName, Type contractType, object exportedValue)
        {
            string typeIdentity = AttributedModelServices.GetTypeIdentity(contractType);

            IDictionary<string, object> metadata = null;

            if (typeIdentity != null)
            {
                metadata = new Dictionary<string, object>();
                metadata.Add(CompositionConstants.ExportTypeIdentityMetadataName, typeIdentity);
            }

            return batch.AddExport(new Export(contractName, metadata, () => exportedValue));
        }
    }
}
