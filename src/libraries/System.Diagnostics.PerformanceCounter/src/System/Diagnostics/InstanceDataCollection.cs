// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        [Obsolete("This constructor has been deprecated.  Please use System.Diagnostics.InstanceDataCollectionCollection.get_Item to get an instance of this collection instead.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public InstanceDataCollection(string counterName)
        {
            if (counterName == null)
                throw new ArgumentNullException(nameof(counterName));
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

                string objectName = instanceName.ToLower(CultureInfo.InvariantCulture);
                return (InstanceData)Dictionary[objectName];
            }
        }

        internal void Add(string instanceName, InstanceData value)
        {
            string objectName = instanceName.ToLower(CultureInfo.InvariantCulture);
            Dictionary.Add(objectName, value);
        }

        public bool Contains(string instanceName)
        {
            if (instanceName == null)
                throw new ArgumentNullException(nameof(instanceName));

            string objectName = instanceName.ToLower(CultureInfo.InvariantCulture);
            return Dictionary.Contains(objectName);
        }

        public void CopyTo(InstanceData[] instances, int index)
        {
            Dictionary.Values.CopyTo(instances, index);
        }
    }
}


