// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics
{
    /// <summary>
    ///     A holder of instance data.
    /// </summary>    
    public class InstanceData
    {
        private string _instanceName;
        private CounterSample _sample;

        public InstanceData(string instanceName, CounterSample sample)
        {
            _instanceName = instanceName;
            _sample = sample;
        }

        public string InstanceName
        {
            get
            {
                return _instanceName;
            }
        }

        public CounterSample Sample
        {
            get
            {
                return _sample;
            }
        }

        public long RawValue
        {
            get
            {
                return _sample.RawValue;
            }
        }
    }
}
