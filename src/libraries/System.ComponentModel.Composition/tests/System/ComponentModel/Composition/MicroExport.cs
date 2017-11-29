// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;

namespace System.ComponentModel.Composition
{
    internal class MicroExport
    {
        public MicroExport(Type contractType, params object[] exportedValues)
            : this(AttributedModelServices.GetContractName(contractType), contractType, (IDictionary<string, object>)null, exportedValues)
        {
        }

        public MicroExport(string contractName, params object[] exportedValues)
            : this(contractName, exportedValues[0].GetType(), (IDictionary<string, object>)null, exportedValues)
        {
        }

        public MicroExport(Type contractType, IDictionary<string, object> metadata, params object[] exportedValues)
            : this(AttributedModelServices.GetContractName(contractType), exportedValues[0].GetType(), metadata, exportedValues)
        {
        }

        public MicroExport(string contractName, Type contractType, params object[] exportedValues)
            : this(contractName, contractType, (IDictionary<string, object>)null, exportedValues)
        {
        }

        public MicroExport(string contractName, IDictionary<string, object> metadata, params object[] exportedValues)
            : this(contractName, exportedValues[0].GetType(), metadata, exportedValues)
        {
        }

        public MicroExport(string contractName, Type contractType, IDictionary<string, object> metadata, params object[] exportedValues)
        {
            this.ContractName = contractName;
            this.ExportedValues = exportedValues;

            if (contractType != null)
            {
                string typeIdentity = AttributedModelServices.GetTypeIdentity(contractType);

                if (metadata == null)
                {
                    metadata = new Dictionary<string, object>();
                }

                object val;
                if (!metadata.TryGetValue(CompositionConstants.ExportTypeIdentityMetadataName, out val))
                {
                    metadata.Add(CompositionConstants.ExportTypeIdentityMetadataName, AttributedModelServices.GetTypeIdentity(contractType));
                }
            }
            this.Metadata = metadata;
        }

        public string ContractName
        {
            get;
            private set;
        }

        public object[] ExportedValues
        {
            get;
            private set;
        }

        public IDictionary<string, object> Metadata
        {
            get;
            private set;
        }
    }
}
