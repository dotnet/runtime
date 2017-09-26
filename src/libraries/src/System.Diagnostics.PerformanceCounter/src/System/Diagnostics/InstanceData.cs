// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics {

    using System.Diagnostics;

    using System;
    using System.Collections;

    /// <summary>
    ///     A holder of instance data.
    /// </summary>    
    public class InstanceData {
        private string instanceName;
        private CounterSample sample;

        public InstanceData(string instanceName, CounterSample sample) {
            this.instanceName = instanceName;
            this.sample = sample;
        }

        public string InstanceName {
            get {
                return instanceName;
            }
        }

        public CounterSample Sample {
            get {
                return sample;
            }
        }

        public long RawValue {
            get {
                return sample.RawValue;
            }
        }
    }
}
