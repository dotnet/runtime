// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// <copyright>
// </copyright>

using System.Collections.Generic;
using System.Xml;


namespace System.Runtime.Serialization.Json
{
    /// <summary>
    /// Dummy documentation
    /// </summary>
    public class DataContractJsonSerializerSettings
    {
        private int _maxItemsInObjectGraph = int.MaxValue;

        /// <summary>
        /// Gets or sets Dummy documentation
        /// </summary>
        public string RootName { get; set; }

        /// <summary>
        /// Gets or sets Dummy documentation
        /// </summary>
        public IEnumerable<Type> KnownTypes { get; set; }

        /// <summary>
        /// Gets or sets Dummy documentation
        /// </summary>
        public int MaxItemsInObjectGraph
        {
            get
            {
                return _maxItemsInObjectGraph;
            }

            set
            {
                _maxItemsInObjectGraph = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether Dummy documentation
        /// </summary>
        public bool IgnoreExtensionDataObject { get; set; }

        /// <summary>
        /// Gets or sets Dummy documentation
        /// </summary>
        public EmitTypeInformation EmitTypeInformation { get; set; }

        /// <summary>
        /// Gets or sets Dummy documentation
        /// </summary>
        public DateTimeFormat DateTimeFormat { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Dummy documentation
        /// </summary>
        public bool SerializeReadOnlyTypes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Dummy documentation
        /// </summary>
        public bool UseSimpleDictionaryFormat { get; set; }
    }
}
