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
        public InstanceData(string instanceName, CounterSample sample)
        {
            InstanceName = instanceName;
            Sample = sample;
        }

        public string InstanceName { get; }

        public CounterSample Sample { get; }

        public long RawValue
        {
            get
            {
                return Sample.RawValue;
            }
        }
    }
}
