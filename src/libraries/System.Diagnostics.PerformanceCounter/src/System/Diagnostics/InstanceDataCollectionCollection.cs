// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Globalization;

namespace System.Diagnostics
{
    /// <summary>
    ///     The collection returned from  the <see cref='System.Diagnostics.PerformanceCounterCategory.ReadCategory'/> method.  
    ///     that contains all the counter and instance data.
    ///     The collection contains an InstanceDataCollection object for each counter.  Each InstanceDataCollection
    ///     object contains the performance data for all counters for that instance.  In other words the data is
    ///     indexed by counter name and then by instance name.
    /// </summary>    
    public class InstanceDataCollectionCollection : DictionaryBase
    {
        [Obsolete("This constructor has been deprecated.  Please use System.Diagnostics.PerformanceCounterCategory.ReadCategory() to get an instance of this collection instead.  https://go.microsoft.com/fwlink/?linkid=14202")]
        public InstanceDataCollectionCollection() : base() { }

        public InstanceDataCollection this[string counterName]
        {
            get
            {
                if (counterName == null)
                    throw new ArgumentNullException(nameof(counterName));

                object objectName = counterName.ToLower(CultureInfo.InvariantCulture);
                return (InstanceDataCollection)Dictionary[objectName];
            }
        }

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

        internal void Add(string counterName, InstanceDataCollection value)
        {
            object objectName = counterName.ToLower(CultureInfo.InvariantCulture);
            Dictionary.Add(objectName, value);
        }

        public bool Contains(string counterName)
        {
            if (counterName == null)
                throw new ArgumentNullException(nameof(counterName));

            object objectName = counterName.ToLower(CultureInfo.InvariantCulture);
            return Dictionary.Contains(objectName);
        }

        public void CopyTo(InstanceDataCollection[] counters, int index)
        {
            Dictionary.Values.CopyTo((Array)counters, index);
        }
    }
}
