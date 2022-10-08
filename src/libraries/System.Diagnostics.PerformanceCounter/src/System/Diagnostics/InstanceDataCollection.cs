// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Globalization;

namespace System.Diagnostics
{
    /// <summary>
    ///     A collection containing all the instance data for a counter.  This collection is contained in the
    ///     <see cref='System.Diagnostics.InstanceDataCollectionCollection'/> when using the
    ///     <see cref='System.Diagnostics.PerformanceCounterCategory.ReadCategory'/> method.
    /// </summary>
    public class InstanceDataCollection : DictionaryBase
    {
        [Obsolete("This constructor has been deprecated. Use System.Diagnostics.InstanceDataCollectionCollection.get_Item to get an instance of this collection instead.")]
        public InstanceDataCollection(string counterName)
        {
            ArgumentNullException.ThrowIfNull(counterName);

            CounterName = counterName;
        }

        public string CounterName { get; }

        public ICollection Keys
        {
            get { return Dictionary.Keys; }
        }

        public ICollection Values
        {
            get
            {
                return Dictionary.Values;
            }
        }

        public InstanceData this[string instanceName]
        {
            get
            {
                if (instanceName == null)
                    throw new ArgumentNullException(nameof(instanceName));

                if (instanceName.Length == 0)
                    instanceName = PerformanceCounterLib.SingleInstanceName;

                string objectName = instanceName.ToLowerInvariant();
                return (InstanceData)Dictionary[objectName];
            }
        }

        internal void Add(string instanceName, InstanceData value)
        {
            string objectName = instanceName.ToLowerInvariant();
            Dictionary.Add(objectName, value);
        }

        public bool Contains(string instanceName)
        {
            ArgumentNullException.ThrowIfNull(instanceName);

            string objectName = instanceName.ToLowerInvariant();
            return Dictionary.Contains(objectName);
        }

        public void CopyTo(InstanceData[] instances, int index)
        {
            Dictionary.Values.CopyTo(instances, index);
        }
    }
}
