// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Collections.Generic;

namespace System.Diagnostics
{
    internal class LegacyPropagator : TextMapPropagator
    {
        internal static TextMapPropagator Instance { get; } = new LegacyPropagator();

        public override IReadOnlyCollection<string> Fields { get; } = new HashSet<string>() { TraceParent, RequestId, TraceState, Baggage, CorrelationContext };

        public override void Inject(Activity activity, object carrier, PropagatorSetterCallback setter)
        {
            if (activity is null || setter is null)
            {
                return;
            }

            string? id = activity.Id;
            if (id is null)
            {
                return;
            }

            if (activity.IdFormat == ActivityIdFormat.W3C)
            {
                setter(carrier, TraceParent, id);
                if (activity.TraceStateString is not null)
                {
                    setter(carrier, TraceState, activity.TraceStateString);
                }
            }
            else
            {
                setter(carrier, RequestId, id);
            }

            InjectBaggage(carrier, activity.Baggage, setter);
        }

        public override void ExtractTraceIdAndState(object carrier, PropagatorGetterCallback getter, out string? traceId, out string? traceState)
        {
            if (getter is null)
            {
                traceId = null;
                traceState = null;
                return;
            }

            getter(carrier, TraceParent, out traceId, out _);
            if (traceId is null)
            {
                getter(carrier, RequestId, out traceId, out _);
            }

            getter(carrier, TraceState, out traceState, out _);
        }

        public override IEnumerable<KeyValuePair<string, string?>>? ExtractBaggage(object carrier, PropagatorGetterCallback getter)
        {
            IEnumerable<KeyValuePair<string, string?>>? baggage = null;
            if (getter is null)
            {
                return null;
            }

            getter(carrier, Baggage, out string? theBaggage, out _);
            if (theBaggage is null || !TryExtractBaggage(theBaggage, out baggage))
            {
                getter(carrier, CorrelationContext, out theBaggage, out _);
                if (theBaggage is not null)
                {
                    TryExtractBaggage(theBaggage, out baggage);
                }
            }

            return baggage;
        }

        internal static bool TryExtractBaggage(string baggagestring, out IEnumerable<KeyValuePair<string, string?>>? baggage)
        {
            baggage = null;
            List<KeyValuePair<string, string?>>? baggageList = null;

            if (string.IsNullOrEmpty(baggagestring))
            {
                return true;
            }

            int currentIndex = 0;

            do
            {
                // Skip spaces
                while (currentIndex < baggagestring.Length && (baggagestring[currentIndex] == Space || baggagestring[currentIndex] == Tab))
                {
                    currentIndex++;
                }

                if (currentIndex >= baggagestring.Length)
                {
                    break; // No Key exist
                }

                int keyStart = currentIndex;

                // Search end of the key
                while (currentIndex < baggagestring.Length && baggagestring[currentIndex] != Space && baggagestring[currentIndex] != Tab && baggagestring[currentIndex] != '=')
                {
                    currentIndex++;
                }

                if (currentIndex >= baggagestring.Length)
                {
                    break;
                }

                int keyEnd = currentIndex;

                if (baggagestring[currentIndex] != '=')
                {
                    // Skip Spaces
                    while (currentIndex < baggagestring.Length && (baggagestring[currentIndex] == Space || baggagestring[currentIndex] == Tab))
                    {
                        currentIndex++;
                    }

                    if (currentIndex >= baggagestring.Length)
                    {
                        break; // Wrong key format
                    }

                    if (baggagestring[currentIndex] != '=')
                    {
                        break; // wrong key format.
                    }
                }

                currentIndex++;

                // Skip spaces
                while (currentIndex < baggagestring.Length && (baggagestring[currentIndex] == Space || baggagestring[currentIndex] == Tab))
                {
                    currentIndex++;
                }

                if (currentIndex >= baggagestring.Length)
                {
                    break; // Wrong value format
                }

                int valueStart = currentIndex;

                // Search end of the value
                while (currentIndex < baggagestring.Length && baggagestring[currentIndex] != Space && baggagestring[currentIndex] != Tab &&
                       baggagestring[currentIndex] != Comma && baggagestring[currentIndex] != Semicolon)
                {
                    currentIndex++;
                }

                if (keyStart < keyEnd && valueStart < currentIndex)
                {
                    if (baggageList is null)
                    {
                        baggageList = new();
                    }

                    // Insert in reverse order for asp.net compatability.
                    baggageList.Insert(0, new KeyValuePair<string, string?>(
                                                WebUtility.UrlDecode(baggagestring.Substring(keyStart, keyEnd - keyStart)).Trim(s_trimmingSpaceCharacters),
                                                WebUtility.UrlDecode(baggagestring.Substring(valueStart, currentIndex - valueStart)).Trim(s_trimmingSpaceCharacters)));
                }

                // Skip to end of values
                while (currentIndex < baggagestring.Length && baggagestring[currentIndex] != Comma)
                {
                    currentIndex++;
                }

                currentIndex++; // Move to next key-value entry
            } while (currentIndex < baggagestring.Length);

            baggage = baggageList;
            return baggageList != null;
        }
    }
}
