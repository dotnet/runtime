// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Diagnostics
{
    internal sealed class LegacyPropagator : DistributedContextPropagator
    {
        internal static DistributedContextPropagator Instance { get; } = new LegacyPropagator();

        public override IReadOnlyCollection<string> Fields { get; } = new ReadOnlyCollection<string>(new[] { TraceParent, RequestId, TraceState, Baggage, CorrelationContext });

        public override void Inject(Activity? activity, object? carrier, PropagatorSetterCallback? setter)
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
                if (!string.IsNullOrEmpty(activity.TraceStateString))
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

        public override void ExtractTraceIdAndState(object? carrier, PropagatorGetterCallback? getter, out string? traceId, out string? traceState)
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

        public override IEnumerable<KeyValuePair<string, string?>>? ExtractBaggage(object? carrier, PropagatorGetterCallback? getter)
        {
            if (getter is null)
            {
                return null;
            }

            getter(carrier, Baggage, out string? theBaggage, out _);

            IEnumerable<KeyValuePair<string, string?>>? baggage = null;
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

        internal static bool TryExtractBaggage(string baggageString, out IEnumerable<KeyValuePair<string, string?>>? baggage)
        {
            baggage = null;
            List<KeyValuePair<string, string?>>? baggageList = null;

            if (string.IsNullOrEmpty(baggageString))
            {
                return true;
            }

            int currentIndex = 0;

            do
            {
                // Skip spaces
                while (currentIndex < baggageString.Length && (baggageString[currentIndex] == Space || baggageString[currentIndex] == Tab))
                {
                    currentIndex++;
                }

                if (currentIndex >= baggageString.Length)
                {
                    break; // No Key exist
                }

                int keyStart = currentIndex;

                // Search end of the key
                while (currentIndex < baggageString.Length && baggageString[currentIndex] != Space && baggageString[currentIndex] != Tab && baggageString[currentIndex] != '=')
                {
                    currentIndex++;
                }

                if (currentIndex >= baggageString.Length)
                {
                    break;
                }

                int keyEnd = currentIndex;

                if (baggageString[currentIndex] != '=')
                {
                    // Skip Spaces
                    while (currentIndex < baggageString.Length && (baggageString[currentIndex] == Space || baggageString[currentIndex] == Tab))
                    {
                        currentIndex++;
                    }

                    if (currentIndex >= baggageString.Length)
                    {
                        break; // Wrong key format
                    }

                    if (baggageString[currentIndex] != '=')
                    {
                        break; // wrong key format.
                    }
                }

                currentIndex++;

                // Skip spaces
                while (currentIndex < baggageString.Length && (baggageString[currentIndex] == Space || baggageString[currentIndex] == Tab))
                {
                    currentIndex++;
                }

                if (currentIndex >= baggageString.Length)
                {
                    break; // Wrong value format
                }

                int valueStart = currentIndex;

                // Search end of the value
                while (currentIndex < baggageString.Length && baggageString[currentIndex] != Space && baggageString[currentIndex] != Tab &&
                       baggageString[currentIndex] != Comma && baggageString[currentIndex] != Semicolon)
                {
                    currentIndex++;
                }

                if (keyStart < keyEnd && valueStart < currentIndex)
                {
                    baggageList ??= new List<KeyValuePair<string, string?>>();

                    // Insert in reverse order for asp.net compatibility.
                    baggageList.Insert(0, new KeyValuePair<string, string?>(
                                                WebUtility.UrlDecode(baggageString.Substring(keyStart, keyEnd - keyStart)).Trim(s_trimmingSpaceCharacters),
                                                WebUtility.UrlDecode(baggageString.Substring(valueStart, currentIndex - valueStart)).Trim(s_trimmingSpaceCharacters)));
                }

                // Skip to end of values
                while (currentIndex < baggageString.Length && baggageString[currentIndex] != Comma)
                {
                    currentIndex++;
                }

                currentIndex++; // Move to next key-value entry
            } while (currentIndex < baggageString.Length);

            baggage = baggageList;
            return baggageList != null;
        }
    }
}
