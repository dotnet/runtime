// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;

namespace System.ComponentModel.Composition.Factories
{
    // This class deliberately does not create instances of Lazy<T, TMetadataView>,
    // so as to test other derived classes from Lazy<T, TMetadataView>.
    internal static partial class ExportFactory
    {
        public static IEnumerable<Export> Create(string contractName, int count)
        {
            Export[] exports = new Export[count];

            for (int i = 0; i < count; i++)
            {
                exports[i] = Create(contractName, (object)null);
            }

            return exports;
        }

        public static Lazy<T> Create<T>(T value)
        {
            return new Lazy<T>(() => value, false);
        }

        public static Lazy<T, IDictionary<string, object>> Create<T>(T value, IDictionary<string, object> metadata)
        {
            return Create<T, IDictionary<string, object>>(() => value, metadata);
        }

        public static Export Create(string contractName, Func<object> exportedValueGetter)
        {
            return Create(contractName,(IDictionary<string, object>)null, exportedValueGetter);
        }

        public static Export Create(string contractName)
        {
            return Create(contractName, null, (IDictionary<string, object>)null);
        }

        public static Export Create(string contractName, object value)
        {
            return Create(contractName, value, (IDictionary<string, object>)null);
        }

        public static Export Create(string contractName, object value, IDictionary<string, object> metadata)
        {
            return Create(contractName, metadata, () => value);
        }

        public static Export Create(string contractName, IDictionary<string, object> metadata, Func<object> exportedValueGetter)
        {
            return new Export(ExportDefinitionFactory.Create(contractName, metadata), exportedValueGetter);
        }

        private static Lazy<T, TMetadataView> Create<T, TMetadataView>(Func<T> exportedValueGetter, IDictionary<string, object> metadata)
        {
            return new Lazy<T, TMetadataView>(exportedValueGetter, AttributedModelServices.GetMetadataView<TMetadataView>(metadata), false);
        }
    }
}
