// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;

namespace System.ComponentModel.Composition.Factories
{
    // This class deliberately does not create instances of ExportDefinition,
    // so as to test other derived classes from ImportDefinition.
    internal static partial class ExportDefinitionFactory
    {
        public static ExportDefinition Create()
        {
            return Create((string)null, (IDictionary<string, object>)null);
        }

        public static ExportDefinition Create(string contractName)
        {
            return Create(contractName, (IDictionary<string, object>)null);
        }

        public static ExportDefinition Create(string contractName, IDictionary<string, object> metadata)
        {
            return new DerivedExportDefinition(contractName, metadata);
        }
    }
}
