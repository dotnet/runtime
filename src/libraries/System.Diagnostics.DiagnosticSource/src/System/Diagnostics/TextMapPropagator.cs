// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;

namespace System.Diagnostics
{
    /// <summary>
    /// Propagator defines the restrictions imposed by a specific transport and is bound to a data type, in order to propagate in-band context data across process boundaries.
    /// TextMapPropagator inject values into and extracts values from carriers as string key/value pairs.
    /// </summary>
    public abstract class TextMapPropagator
    {
        private static TextMapPropagator s_current = CreateDefaultPropagator();

        /// <summary>
        /// Define the callback that can be used with the propagators extract methods. This callback will be invoked for each propagation key to get.
        /// </summary>
        /// <param name="carrier">Carrier is the medium used by Propagators to read values from.</param>
        /// <param name="fieldName">The propagation field name.</param>
        /// <param name="fieldValue">An output string to receive the value corresponds to the input fieldName. This should return non null value if there is only one value for the input field name.</param>
        /// <param name="fieldValues">An output collection of strings to receive the values corresponds to the input fieldName. This should return non null value if there are more than one value for the input field name.</param>
        public delegate void PropagatorGetterCallback(object? carrier, string fieldName, out string? fieldValue, out IEnumerable<string>? fieldValues);

        /// <summary>
        /// Define the callback that can be used with the propagators inject methods. This callback will be invoked to set a propagation key/value pair.
        /// Propagators may invoke it multiple times in order to set multiple pairs.
        /// </summary>
        /// <param name="carrier">Carrier is the medium used by Propagators to write values to.</param>
        /// <param name="fieldName">The propagation field name.</param>
        /// <param name="fieldValue">The value corresponds to the input fieldName. </param>
        public delegate void PropagatorSetterCallback(object? carrier, string fieldName, string fieldValue);

        /// <summary>
        /// The predefined propagation fields
        /// </summary>
        /// <returns>Returns list of fields that will be used by the TextMapPropagator.</returns>
        public abstract IReadOnlyCollection<string> Fields { get; }

        /// <summary>
        /// Injects the trace values stroed in the <see cref="Activity"/> object into a carrier. For example, into the headers of an HTTP request.
        /// </summary>
        /// <param name="activity">The Activity object has the trace context to inject to the carrier.</param>
        /// <param name="carrier">Carrier is the medium used by the propagators to write values to.</param>
        /// <param name="setter">The callback will be invoked to set a propagation key/value pair to the carrier.</param>
        public abstract void Inject(Activity? activity, object? carrier, PropagatorSetterCallback? setter);

        /// <summary>
        /// Extracts the value from an incoming request represented by the carrier. For example, from the headers of an HTTP request.
        /// </summary>
        /// <param name="carrier">Carrier is the medium used by the propagators to read values from.</param>
        /// <param name="getter">The callback will be invoked to get the propagation trace Id and trace state from carrier.</param>
        /// <param name="traceId">The extracted trace Id from the carrier.</param>
        /// <param name="traceState">The extracted trace state from the carrier.</param>
        public abstract void ExtractTraceIdAndState(object? carrier, PropagatorGetterCallback? getter, out string? traceId, out string? traceState);

        /// <summary>
        /// Extracts the baggage key-value pairs list from an incoming request represented by the carrier. For example, from the headers of an HTTP request.
        /// </summary>
        /// <param name="carrier">Carrier is the medium used by the propagators to read values from.</param>
        /// <param name="getter">The callback will be invoked to get the propagation baggage list from carrier.</param>
        /// <returns>Returns the extracted key-value pair list from teh carrier.</returns>
        public abstract IEnumerable<KeyValuePair<string, string?>>? ExtractBaggage(object? carrier, PropagatorGetterCallback? getter);

        /// <summary>
        /// Get or set the process wide propagator object which used as the current selected propagator.
        /// </summary>
        public static TextMapPropagator Current
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
        public static TextMapPropagator CreateDefaultPropagator() => LegacyPropagator.Instance;

        /// <summary>
        /// returns the propagator which can propagate the trace context data using the root Activity and ignore any created child activities.
        /// </summary>
        public static TextMapPropagator CreatePassThroughPropagator() => PassThroughPropagator.Instance;

        /// <summary>
        /// returns the propagator which suppress injecting any data to teh carriers.
        /// </summary>
        public static TextMapPropagator CreateNoOutputPropagator() => NoOutputPropagator.Instance;

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
