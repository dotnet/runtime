// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;

namespace System.Diagnostics
{
    /// <summary>
    /// An implementation of DistributedContextPropagator determines if and how distributed context information is encoded and decoded as it traverses the network.
    /// The encoding can be transported over any network protocol that supports string key-value pairs. For example when using HTTP, each key value pair is an HTTP header.
    /// DistributedContextPropagator inject values into and extracts values from carriers as string key/value pairs.
    /// </summary>
    public abstract class DistributedContextPropagator
    {
        private static DistributedContextPropagator s_current = CreateDefaultPropagator();

        /// <summary>
        /// The callback that is used in propagators' extract methods. The callback is invoked to lookup the value of a named field.
        /// </summary>
        /// <param name="carrier">Carrier is the medium used by Propagators to read values from.</param>
        /// <param name="fieldName">The propagation field name.</param>
        /// <param name="fieldValue">An output string to receive the value corresponds to the input fieldName. This should return non null value if there is only one value for the input field name.</param>
        /// <param name="fieldValues">An output collection of strings to receive the values corresponds to the input fieldName. This should return non null value if there are more than one value for the input field name.</param>
        public delegate void PropagatorGetterCallback(object? carrier, string fieldName, out string? fieldValue, out IEnumerable<string>? fieldValues);

        /// <summary>
        /// The callback that is used in propagators' inject methods. This callback is invoked to set the value of a named field.
        /// Propagators may invoke it multiple times in order to set multiple fields.
        /// </summary>
        /// <param name="carrier">Carrier is the medium used by Propagators to write values to.</param>
        /// <param name="fieldName">The propagation field name.</param>
        /// <param name="fieldValue">The value corresponds to the input fieldName. </param>
        public delegate void PropagatorSetterCallback(object? carrier, string fieldName, string fieldValue);

        /// <summary>
        /// The set of field names this propagator is likely to read or write.
        /// </summary>
        /// <returns>Returns list of fields that will be used by the DistributedContextPropagator.</returns>
        public abstract IReadOnlyCollection<string> Fields { get; }

        /// <summary>
        /// Injects the trace values stroed in the <see cref="Activity"/> object into a carrier. For example, into the headers of an HTTP request.
        /// </summary>
        /// <param name="activity">The Activity object has the distributed context to inject to the carrier.</param>
        /// <param name="carrier">Carrier is the medium in which the distributed context will be stored.</param>
        /// <param name="setter">The callback will be invoked to set a named key/value pair on the carrier.</param>
        public abstract void Inject(Activity? activity, object? carrier, PropagatorSetterCallback? setter);

        /// <summary>
        /// Extracts trace Id and trace state from an incoming request represented by the carrier. For example, from the headers of an HTTP request.
        /// </summary>
        /// <param name="carrier">Carrier is the medium from which values will be read.</param>
        /// <param name="getter">The callback will be invoked to get the propagation trace Id and trace state from carrier.</param>
        /// <param name="traceId">The extracted trace Id from the carrier.</param>
        /// <param name="traceState">The extracted trace state from the carrier.</param>
        public abstract void ExtractTraceIdAndState(object? carrier, PropagatorGetterCallback? getter, out string? traceId, out string? traceState);

        /// <summary>
        /// Extracts the baggage key-value pair list from an incoming request represented by the carrier. For example, from the headers of an HTTP request.
        /// </summary>
        /// <param name="carrier">Carrier is the medium from which values will be read.</param>
        /// <param name="getter">The callback will be invoked to get the propagation baggage list from carrier.</param>
        /// <returns>Returns the extracted key-value pair list from the carrier.</returns>
        public abstract IEnumerable<KeyValuePair<string, string?>>? ExtractBaggage(object? carrier, PropagatorGetterCallback? getter);

        /// <summary>
        /// Get or set the process wide propagator object which used as the current selected propagator.
        /// </summary>
        public static DistributedContextPropagator Current
        {
            get
            {
                Debug.Assert(s_current is not null);
                return s_current;
            }

            set
            {
                s_current = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        /// <summary>
        /// returns the default propagator object which Current property will be initialized with.
        /// </summary>
        /// <remarks>
        /// CreateDefaultPropagator will create a propagator instance that can inject and extract the headers with field names "tracestate",
        /// "traceparent" of the identifiers which are formatted as W3C trace parent, "Request-Id" of the identifiers which are formatted as a hierarchical identifier.
        /// The returned propagator can inject the baggage key-value pair list with header name "Correlation-Context" and it can extract the baggage values mapped to header names "Correlation-Context" and "baggage".
        /// </remarks>
        public static DistributedContextPropagator CreateDefaultPropagator() => LegacyPropagator.Instance;

        /// <summary>
        /// Returns a propagator which attempts to act transparently, emitting the same data on outbound network requests that was received on the in-bound request.
        /// When encoding the outbound message, this propagator uses information from the request's root Activity, ignoring any intermediate Activities that may have been created while processing the request.
        /// </summary>
        public static DistributedContextPropagator CreatePassThroughPropagator() => PassThroughPropagator.Instance;

        /// <summary>
        /// Returns a propagator which does not transmit any distributed context information in outbound network messages.
        /// </summary>
        public static DistributedContextPropagator CreateNoOutputPropagator() => NoOutputPropagator.Instance;

        // internal stuff

        internal static void InjectBaggage(object? carrier, IEnumerable<KeyValuePair<string, string?>> baggage, PropagatorSetterCallback setter)
        {
            using (IEnumerator<KeyValuePair<string, string?>> e = baggage.GetEnumerator())
            {
                if (e.MoveNext())
                {
                    StringBuilder baggageList = new StringBuilder();

                    do
                    {
                        KeyValuePair<string, string?> item = e.Current;
                        baggageList.Append(WebUtility.UrlEncode(item.Key)).Append('=').Append(WebUtility.UrlEncode(item.Value)).Append(CommaWithSpace);
                    } while (e.MoveNext());

                    setter(carrier, CorrelationContext, baggageList.ToString(0, baggageList.Length - 2));
                }
            }
        }

        internal const string TraceParent        = "traceparent";
        internal const string RequestId          = "Request-Id";
        internal const string TraceState         = "tracestate";
        internal const string Baggage            = "baggage";
        internal const string CorrelationContext = "Correlation-Context";
        internal const char   Space              = ' ';
        internal const char   Tab                = (char)9;
        internal const char   Comma              = ',';
        internal const char   Semicolon          = ';';
        internal const string CommaWithSpace     = ", ";

        internal static readonly char [] s_trimmingSpaceCharacters = new char[] { Space, Tab };
    }
}
