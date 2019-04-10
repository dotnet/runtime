// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
#if ES_BUILD_PCL
    using System.Threading.Tasks;
#endif

#if ES_BUILD_STANDALONE
namespace Microsoft.Diagnostics.Tracing
#else
namespace System.Diagnostics.Tracing
#endif
{
    [EventData]
    internal class CounterPayload : IEnumerable<KeyValuePair<string, object>>
    {
        public string Name { get; set; }

        public string DisplayName { get; set; }

        public double Mean { get; set; }

        public double StandardDeviation { get; set; }

        public int Count { get; set; }

        public double Min { get; set; }

        public double Max { get; set; }

        public float IntervalSec { get; internal set; }

        public string Metadata { get; set; }

        #region Implementation of the IEnumerable interface

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return ForEnumeration.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ForEnumeration.GetEnumerator();
        }

        private IEnumerable<KeyValuePair<string, object>> ForEnumeration
        {
            get
            {
                yield return new KeyValuePair<string, object>("Name", Name);
                yield return new KeyValuePair<string, object>("DisplayName", DisplayName);
                yield return new KeyValuePair<string, object>("Mean", Mean);
                yield return new KeyValuePair<string, object>("StandardDeviation", StandardDeviation);
                yield return new KeyValuePair<string, object>("Count", Count);
                yield return new KeyValuePair<string, object>("Min", Min);
                yield return new KeyValuePair<string, object>("Max", Max);
                yield return new KeyValuePair<string, object>("IntervalSec", IntervalSec);
                yield return new KeyValuePair<string, object>("Series", $"Interval={IntervalSec}");
                yield return new KeyValuePair<string, object>("CounterType", "Mean");
                yield return new KeyValuePair<string, object>("Metadata", Metadata);
            }
        }

        #endregion // Implementation of the IEnumerable interface
    }

    [EventData]
    internal class IncrementingCounterPayload : IEnumerable<KeyValuePair<string, object>>
    {
        public string Name { get; set; }

        public string DisplayName { get; set; }

        public string DisplayRateTimeScale { get; set; }

        public double Increment { get; set; }

        public float IntervalSec { get; internal set; }

        public string Metadata { get; set; }

        #region Implementation of the IEnumerable interface

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return ForEnumeration.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ForEnumeration.GetEnumerator();
        }

        private IEnumerable<KeyValuePair<string, object>> ForEnumeration
        {
            get
            {
                yield return new KeyValuePair<string, object>("Name", Name);
                yield return new KeyValuePair<string, object>("DisplayName", DisplayName);
                yield return new KeyValuePair<string, object>("DisplayRateTimeScale", DisplayRateTimeScale);
                yield return new KeyValuePair<string, object>("Increment", Increment);
                yield return new KeyValuePair<string, object>("IntervalSec", IntervalSec);
                yield return new KeyValuePair<string, object>("Series", $"Interval={IntervalSec}");
                yield return new KeyValuePair<string, object>("CounterType", "Sum");
                yield return new KeyValuePair<string, object>("Metadata", Metadata);
            }
        }

        #endregion // Implementation of the IEnumerable interface
    }
}
