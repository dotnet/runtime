// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace System.Diagnostics;

[Flags]
internal enum DsesActivityEvents
{
    None = 0x00,
    ActivityStart = 0x01,
    ActivityStop = 0x02,
    All = ActivityStart | ActivityStop,
}

/// <summary>
/// FilterAndTransform represents on transformation specification from a DiagnosticsSource
/// to EventSource's 'Event' method. (e.g. MySource/MyEvent:out=prop1.prop2.prop3).
/// Its main method is 'Morph' which takes a DiagnosticSource object and morphs it into
/// a list of string,string key value pairs.
///
/// This method also contains that static 'Create/Destroy FilterAndTransformList, which
/// simply parse a series of transformation specifications.
/// </summary>
internal sealed class DsesFilterAndTransform : IDisposable
{
    private const string c_ActivitySourcePrefix = "[AS]";
    private const string c_ParentRatioSamplerPrefix = "ParentRatioSampler(";

    /// <summary>
    /// Parses filterAndPayloadSpecs which is a list of lines each of which has the from
    ///
    ///    DiagnosticSourceName/EventName:PAYLOAD_SPEC
    ///
    /// where PAYLOADSPEC is a semicolon separated list of specifications of the form
    ///
    ///    OutputName=Prop1.Prop2.PropN
    ///
    /// Into linked list of FilterAndTransform that together forward events from the given
    /// DiagnosticSource's to 'eventSource'. Sets the 'specList' variable to this value
    /// (destroying anything that was there previously).
    ///
    /// By default any serializable properties of the payload object are also included
    /// in the output payload, however this feature and be tuned off by prefixing the
    /// PAYLOADSPEC with a '-'.
    /// </summary>
    public static IDisposable ParseFilterAndPayloadSpecs(string? filterAndPayloadSpecs)
    {
        filterAndPayloadSpecs ??= "";

        DsesFilterAndTransform? specList = null;
        DsesFilterAndTransform? activitySourceSpecList = null;

        // Points just beyond the last point in the string that has yet to be parsed. Thus we start with the whole string.
        int endIdx = filterAndPayloadSpecs.Length;
        while (true)
        {
            // Skip trailing whitespace.
            while (0 < endIdx && char.IsWhiteSpace(filterAndPayloadSpecs[endIdx - 1]))
                --endIdx;

            int newlineIdx = filterAndPayloadSpecs.LastIndexOf('\n', endIdx - 1, endIdx);
            int startIdx = 0;
            if (0 <= newlineIdx)
                startIdx = newlineIdx + 1;  // starts after the newline, or zero if we don't find one.

            // Skip leading whitespace
            while (startIdx < endIdx && char.IsWhiteSpace(filterAndPayloadSpecs[startIdx]))
                startIdx++;

            if (IsActivitySourceEntry(filterAndPayloadSpecs, startIdx, endIdx))
            {
                activitySourceSpecList = CreateActivitySourceTransform(filterAndPayloadSpecs, startIdx, endIdx, activitySourceSpecList);
            }
            else
            {
                specList = CreateTransform(filterAndPayloadSpecs, startIdx, endIdx, specList);
            }

            endIdx = newlineIdx;
            if (endIdx < 0)
                break;
        }

        DsesActivitySourceListener? activitySourceListener = activitySourceSpecList != null
            ? DsesActivitySourceListener.Create(activitySourceSpecList)
            : null;

        return new ParsedFilterAndPayloadSpecs(specList, activitySourceListener);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsActivitySourceEntry(string filterAndPayloadSpec, int startIdx, int endIdx) =>
        filterAndPayloadSpec.AsSpan(startIdx, endIdx - startIdx).StartsWith(c_ActivitySourcePrefix.AsSpan(), StringComparison.Ordinal);

    /// <summary>
    /// Creates one FilterAndTransform specification from filterAndPayloadSpec starting at 'startIdx' and ending just before 'endIdx'.
    /// This FilterAndTransform will subscribe to DiagnosticSources specified by the specification and forward them to 'eventSource.
    /// For convenience, the 'Next' field is set to the 'next' parameter, so you can easily form linked lists.
    /// </summary>
    private static DsesFilterAndTransform? CreateTransform(string filterAndPayloadSpec, int startIdx, int endIdx, DsesFilterAndTransform? next)
    {
        Debug.Assert(filterAndPayloadSpec != null && startIdx >= 0 && startIdx <= endIdx && endIdx <= filterAndPayloadSpec.Length);

        string? listenerNameFilter = null;       // Means WildCard.
        string? eventNameFilter = null;          // Means WildCard.
        string? activityName = null;
        bool noImplicitTransforms = false;
        TransformSpec? explicitTransforms = null;

        var startTransformIdx = startIdx;
        var endEventNameIdx = endIdx;
        var colonIdx = filterAndPayloadSpec.IndexOf(':', startIdx, endIdx - startIdx);
        if (0 <= colonIdx)
        {
            endEventNameIdx = colonIdx;
            startTransformIdx = colonIdx + 1;
        }

        // Parse the Source/Event name into listenerNameFilter and eventNameFilter
        var slashIdx = filterAndPayloadSpec.IndexOf('/', startIdx, endEventNameIdx - startIdx);
        if (0 <= slashIdx)
        {
            listenerNameFilter = filterAndPayloadSpec.Substring(startIdx, slashIdx - startIdx);

            var atIdx = filterAndPayloadSpec.IndexOf('@', slashIdx + 1, endEventNameIdx - slashIdx - 1);
            if (0 <= atIdx)
            {
                activityName = filterAndPayloadSpec.Substring(atIdx + 1, endEventNameIdx - atIdx - 1);
                eventNameFilter = filterAndPayloadSpec.Substring(slashIdx + 1, atIdx - slashIdx - 1);
            }
            else
            {
                eventNameFilter = filterAndPayloadSpec.Substring(slashIdx + 1, endEventNameIdx - slashIdx - 1);
            }
        }
        else if (startIdx < endEventNameIdx)
        {
            listenerNameFilter = filterAndPayloadSpec.Substring(startIdx, endEventNameIdx - startIdx);
        }

        DiagnosticSourceEventSource.Log.Message("DiagnosticSource: Enabling '" + (listenerNameFilter ?? "*") + "/" + (eventNameFilter ?? "*") + "'");

        // If the transform spec begins with a - it means you don't want implicit transforms.
        if (startTransformIdx < endIdx && filterAndPayloadSpec[startTransformIdx] == '-')
        {
            DiagnosticSourceEventSource.Log.Message("DiagnosticSource: suppressing implicit transforms.");
            noImplicitTransforms = true;
            startTransformIdx++;
        }

        // Parse all the explicit transforms, if present
        if (startTransformIdx < endIdx)
        {
            while (true)
            {
                int specStartIdx = startTransformIdx;
                int semiColonIdx = filterAndPayloadSpec.LastIndexOf(';', endIdx - 1, endIdx - startTransformIdx);
                if (0 <= semiColonIdx)
                    specStartIdx = semiColonIdx + 1;

                // Ignore empty specifications.
                if (specStartIdx < endIdx)
                {
                    if (DiagnosticSourceEventSource.Log.IsEnabled(EventLevel.Informational, DiagnosticSourceEventSource.Keywords.Messages))
                        DiagnosticSourceEventSource.Log.Message("DiagnosticSource: Parsing Explicit Transform '" + filterAndPayloadSpec.Substring(specStartIdx, endIdx - specStartIdx) + "'");

                    explicitTransforms = new TransformSpec(filterAndPayloadSpec, specStartIdx, endIdx, explicitTransforms);
                }
                if (startTransformIdx == specStartIdx)
                    break;
                endIdx = semiColonIdx;
            }
        }

        var transform = new DsesFilterAndTransform(
            next,
            noImplicitTransforms,
            explicitTransforms,
            sourceName: null,
            activityName: null,
            activityEvents: default,
            sampleFunc: null);

        transform.SetupDiagnosticListenerSubscription(listenerNameFilter, eventNameFilter, activityName);

        return transform;
    }

    private static DsesFilterAndTransform? CreateActivitySourceTransform(string filterAndPayloadSpec, int startIdx, int endIdx, DsesFilterAndTransform? next)
    {
        Debug.Assert(endIdx - startIdx >= 4);
        Debug.Assert(IsActivitySourceEntry(filterAndPayloadSpec, startIdx, endIdx));

        bool noImplicitTransforms = false;
        TransformSpec? explicitTransforms = null;
        ReadOnlySpan<char> eventName;
        ReadOnlySpan<char> activitySourceName;

        DsesActivityEvents supportedEvent = DsesActivityEvents.All; // Default events
        DsesSampleActivityFunc sampleFunc = static (bool hasActivityContext, ref ActivityCreationOptions<ActivityContext> options)
            => ActivitySamplingResult.AllDataAndRecorded; // Default sampler

        int colonIdx = filterAndPayloadSpec.IndexOf(':', startIdx + c_ActivitySourcePrefix.Length, endIdx - startIdx - c_ActivitySourcePrefix.Length);

        ReadOnlySpan<char> entry = filterAndPayloadSpec.AsSpan(
            startIdx + c_ActivitySourcePrefix.Length,
            (colonIdx >= 0 ? colonIdx : endIdx) - startIdx - c_ActivitySourcePrefix.Length)
            .Trim();

        int eventNameIndex = entry.IndexOf('/');
        if (eventNameIndex >= 0)
        {
            activitySourceName = entry.Slice(0, eventNameIndex).Trim();

            ReadOnlySpan<char> suffixPart = entry.Slice(eventNameIndex + 1).Trim();
            int samplingResultIndex = suffixPart.IndexOf('-');
            if (samplingResultIndex >= 0)
            {
                // We have the format "[AS]SourceName/[EventName]-[SamplingResult]
                eventName = suffixPart.Slice(0, samplingResultIndex).Trim();
                suffixPart = suffixPart.Slice(samplingResultIndex + 1).Trim();

                if (suffixPart.Length > 0)
                {
                    if (suffixPart.Equals("Propagate".AsSpan(), StringComparison.OrdinalIgnoreCase))
                    {
                        sampleFunc = static (bool hasActivityContext, ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.PropagationData;
                    }
                    else if (suffixPart.Equals("Record".AsSpan(), StringComparison.OrdinalIgnoreCase))
                    {
                        sampleFunc = static (bool hasActivityContext, ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData;
                    }
                    else if (suffixPart.StartsWith(c_ParentRatioSamplerPrefix.AsSpan(), StringComparison.OrdinalIgnoreCase))
                    {
                        int endingLocation = suffixPart.IndexOf(')');
                        if (endingLocation < 0
#if NETFRAMEWORK || NETSTANDARD
                            || !double.TryParse(suffixPart.Slice(c_ParentRatioSamplerPrefix.Length, endingLocation - c_ParentRatioSamplerPrefix.Length).ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double ratio))
#else
                            || !double.TryParse(suffixPart.Slice(c_ParentRatioSamplerPrefix.Length, endingLocation - c_ParentRatioSamplerPrefix.Length), NumberStyles.Float, CultureInfo.InvariantCulture, out double ratio))
#endif
                        {
                            if (DiagnosticSourceEventSource.Log.IsEnabled(EventLevel.Warning, DiagnosticSourceEventSource.Keywords.Messages))
                                DiagnosticSourceEventSource.Log.Message("DiagnosticSource: Ignoring filterAndPayloadSpec '[AS]" + entry.ToString() + "' because sampling ratio was invalid");
                            return next;
                        }

                        sampleFunc = DsesSamplerBuilder.CreateParentRatioSampler(ratio);
                    }
                    else
                    {
                        if (DiagnosticSourceEventSource.Log.IsEnabled(EventLevel.Warning, DiagnosticSourceEventSource.Keywords.Messages))
                            DiagnosticSourceEventSource.Log.Message("DiagnosticSource: Ignoring filterAndPayloadSpec '[AS]" + entry.ToString() + "' because sampling method was invalid");
                        return next;
                    }
                }
            }
            else
            {
                // We have the format "[AS]SourceName/[EventName]
                eventName = suffixPart;
            }

            if (eventName.Length > 0)
            {
                if (eventName.Equals("Start".AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    supportedEvent = DsesActivityEvents.ActivityStart;
                }
                else if (eventName.Equals("Stop".AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    supportedEvent = DsesActivityEvents.ActivityStop;
                }
                else
                {
                    if (DiagnosticSourceEventSource.Log.IsEnabled(EventLevel.Warning, DiagnosticSourceEventSource.Keywords.Messages))
                        DiagnosticSourceEventSource.Log.Message("DiagnosticSource: Ignoring filterAndPayloadSpec '[AS]" + entry.ToString() + "' because event name was invalid");
                    return next;
                }
            }
        }
        else
        {
            // We have the format "[AS]SourceName"
            activitySourceName = entry;
        }

        string? activityName = null;
        int plusSignIndex = activitySourceName.IndexOf('+');
        if (plusSignIndex >= 0)
        {
            activityName = activitySourceName.Slice(plusSignIndex + 1).Trim().ToString();
            activitySourceName = activitySourceName.Slice(0, plusSignIndex).Trim();

            if (activityName.Length > 0 && activitySourceName.Length == 1 && activitySourceName[0] == '*')
            {
                if (DiagnosticSourceEventSource.Log.IsEnabled(EventLevel.Warning, DiagnosticSourceEventSource.Keywords.Messages))
                    DiagnosticSourceEventSource.Log.Message("DiagnosticSource: Ignoring filterAndPayloadSpec '[AS]" + entry.ToString() + "' because activity name cannot be specified for wildcard activity sources");
                return next;
            }
        }

        if (colonIdx >= 0)
        {
            int startTransformIdx = colonIdx + 1;

            // If the transform spec begins with a - it means you don't want implicit transforms.
            if (startTransformIdx < endIdx && filterAndPayloadSpec[startTransformIdx] == '-')
            {
                DiagnosticSourceEventSource.Log.Message("DiagnosticSource: suppressing implicit transforms.");
                noImplicitTransforms = true;
                startTransformIdx++;
            }

            // Parse all the explicit transforms, if present
            if (startTransformIdx < endIdx)
            {
                while (true)
                {
                    int specStartIdx = startTransformIdx;
                    int semiColonIdx = filterAndPayloadSpec.LastIndexOf(';', endIdx - 1, endIdx - startTransformIdx);
                    if (0 <= semiColonIdx)
                        specStartIdx = semiColonIdx + 1;

                    // Ignore empty specifications.
                    if (specStartIdx < endIdx)
                    {
                        if (DiagnosticSourceEventSource.Log.IsEnabled(EventLevel.Informational, DiagnosticSourceEventSource.Keywords.Messages))
                            DiagnosticSourceEventSource.Log.Message("DiagnosticSource: Parsing Explicit Transform '" + filterAndPayloadSpec.Substring(specStartIdx, endIdx - specStartIdx) + "'");

                        explicitTransforms = new TransformSpec(filterAndPayloadSpec, specStartIdx, endIdx, explicitTransforms);
                    }
                    if (startTransformIdx == specStartIdx)
                        break;
                    endIdx = semiColonIdx;
                }
            }
        }

        return new DsesFilterAndTransform(
            next,
            noImplicitTransforms,
            explicitTransforms,
            activitySourceName.ToString(),
            activityName,
            supportedEvent,
            sampleFunc);
    }

    private DsesFilterAndTransform(
        DsesFilterAndTransform? next,
        bool noImplicitTransforms,
        TransformSpec? explicitTransforms,
        string? sourceName,
        string? activityName,
        DsesActivityEvents activityEvents,
        DsesSampleActivityFunc? sampleFunc)
    {
        _noImplicitTransforms = noImplicitTransforms;
        _explicitTransforms = explicitTransforms;

        Next = next;
        SourceName = sourceName;
        ActivityName = activityName;
        Events = activityEvents;
        SampleFunc = sampleFunc;
    }

    public void Dispose()
    {
        if (_diagnosticsListenersSubscription != null)
        {
            _diagnosticsListenersSubscription.Dispose();
            _diagnosticsListenersSubscription = null;
        }

        if (_liveSubscriptions != null)
        {
            Subscriptions? subscr = _liveSubscriptions;
            _liveSubscriptions = null;
            while (subscr != null)
            {
                subscr.Subscription.Dispose();
                subscr = subscr.Next;
            }
        }
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2112:ReflectionToRequiresUnreferencedCode",
        Justification = "In EventSource, EnsureDescriptorsInitialized's use of GetType preserves this method which " +
                        "requires unreferenced code, but EnsureDescriptorsInitialized does not access this member and is safe to call.")]
    [RequiresUnreferencedCode(DiagnosticSource.WriteRequiresUnreferencedCode)]
    public List<KeyValuePair<string, string?>> Morph(object? args)
    {
        // Transform the args into a bag of key-value strings.
        var outputArgs = new List<KeyValuePair<string, string?>>();
        if (args != null)
        {
            if (!_noImplicitTransforms)
            {
                // given the type, fetch the implicit transforms for that type and put it in the implicitTransforms variable.
                Type argType = args.GetType();
                TransformSpec? implicitTransforms;

                // First check the one-element cache _firstImplicitTransformsEntry
                ImplicitTransformEntry? cacheEntry = _firstImplicitTransformsEntry;
                if (cacheEntry != null && cacheEntry.Type == argType)
                {
                    implicitTransforms = cacheEntry.Transforms;     // Yeah we hit the cache.
                }
                else if (cacheEntry == null)
                {
                    // _firstImplicitTransformsEntry is empty, we should fill it.
                    // Note that it is OK that two threads may race and both call MakeImplicitTransforms on their own
                    // (that is we don't expect exactly once initialization of _firstImplicitTransformsEntry)
                    implicitTransforms = MakeImplicitTransforms(argType);
                    Interlocked.CompareExchange(ref _firstImplicitTransformsEntry,
                        new ImplicitTransformEntry() { Type = argType, Transforms = implicitTransforms }, null);
                }
                else
                {
                    // This should only happen when you are wildcarding your events (reasonably rare).
                    // In that case you will probably need many types
                    // Note currently we don't limit the cache size, but it is limited by the number of
                    // distinct types of objects passed to DiagnosticSource.Write.
                    if (_implicitTransformsTable == null)
                    {
                        Interlocked.CompareExchange(ref _implicitTransformsTable,
                            new ConcurrentDictionary<Type, TransformSpec?>(1, 8), null);
                    }
                    implicitTransforms = _implicitTransformsTable.GetOrAdd(argType, MakeImplicitTransforms);
                }

                // implicitTransformas now fetched from cache or constructed, use it to Fetch all the implicit fields.
                if (implicitTransforms != null)
                {
                    for (TransformSpec? serializableArg = implicitTransforms; serializableArg != null; serializableArg = serializableArg.Next)
                        outputArgs.Add(serializableArg.Morph(args));
                }
            }

            if (_explicitTransforms != null)
            {
                for (TransformSpec? explicitTransform = _explicitTransforms; explicitTransform != null; explicitTransform = explicitTransform.Next)
                {
                    var keyValue = explicitTransform.Morph(args);
                    if (keyValue.Value != null)
                        outputArgs.Add(keyValue);
                }
            }
        }
        return outputArgs;
    }

    private void SetupDiagnosticListenerSubscription(
        string? listenerNameFilter, // Means WildCard.
        string? eventNameFilter, // Means WildCard.
        string? activityName)
    {
        Action<string, string, IEnumerable<KeyValuePair<string, string?>>>? writeEvent = null;
        if (activityName != null && activityName.Contains("Activity"))
        {
            writeEvent = activityName switch
            {
                nameof(DiagnosticSourceEventSource.Activity1Start) => DiagnosticSourceEventSource.Log.Activity1Start,
                nameof(DiagnosticSourceEventSource.Activity1Stop) => DiagnosticSourceEventSource.Log.Activity1Stop,
                nameof(DiagnosticSourceEventSource.Activity2Start) => DiagnosticSourceEventSource.Log.Activity2Start,
                nameof(DiagnosticSourceEventSource.Activity2Stop) => DiagnosticSourceEventSource.Log.Activity2Stop,
                nameof(DiagnosticSourceEventSource.RecursiveActivity1Start) => DiagnosticSourceEventSource.Log.RecursiveActivity1Start,
                nameof(DiagnosticSourceEventSource.RecursiveActivity1Stop) => DiagnosticSourceEventSource.Log.RecursiveActivity1Stop,
                _ => null
            };

            if (writeEvent == null)
                DiagnosticSourceEventSource.Log.Message("DiagnosticSource: Could not find Event to log Activity " + activityName);
        }

        writeEvent ??= DiagnosticSourceEventSource.Log.Event;

        // Set up a subscription that watches for the given Diagnostic Sources and events which will call back
        // to the EventSource.
        _diagnosticsListenersSubscription = DiagnosticListener.AllListeners.Subscribe(new CallbackObserver<DiagnosticListener>(delegate (DiagnosticListener newListener)
        {
            if (listenerNameFilter == null || listenerNameFilter == newListener.Name)
            {
                DiagnosticSourceEventSource.Log.NewDiagnosticListener(newListener.Name);
                Predicate<string>? eventNameFilterPredicate = null;
                if (eventNameFilter != null)
                    eventNameFilterPredicate = (string eventName) => eventNameFilter == eventName;

                [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                    Justification = "DiagnosticSource.Write is marked with RequiresUnreferencedCode.")]
                [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2119",
                    Justification = "DAM on EventSource references this compiler-generated local function which calls a " +
                                    "method that requires unreferenced code. EventSource will not access this local function.")]
                void OnEventWritten(KeyValuePair<string, object?> evnt)
                {
                    // The filter given to the DiagnosticSource may not work if users don't is 'IsEnabled' as expected.
                    // Thus we look for any events that may have snuck through and filter them out before forwarding.
                    if (eventNameFilter != null && eventNameFilter != evnt.Key)
                        return;

                    var outputArgs = this.Morph(evnt.Value);
                    var eventName = evnt.Key;
                    writeEvent(newListener.Name, eventName, outputArgs);
                }

                var subscription = newListener.Subscribe(new CallbackObserver<KeyValuePair<string, object?>>(OnEventWritten), eventNameFilterPredicate);
                _liveSubscriptions = new Subscriptions(subscription, _liveSubscriptions);
            }
        }));
    }

    public DsesFilterAndTransform? Next { get; }

    // Specific ActivitySource Transforms information

    internal string? SourceName { get; }
    internal string? ActivityName { get; }
    internal DsesActivityEvents Events { get; }
    internal DsesSampleActivityFunc? SampleFunc { get; }

    private sealed class ParsedFilterAndPayloadSpecs : IDisposable
    {
        private DsesFilterAndTransform? _specList;
        private DsesActivitySourceListener? _activitySourceListener;

        public ParsedFilterAndPayloadSpecs(
            DsesFilterAndTransform? specList,
            DsesActivitySourceListener? activitySourceListener)
        {
            _specList = specList;
            _activitySourceListener = activitySourceListener;
        }

        /// <summary>
        /// This destroys (turns off) the FilterAndTransform stopping the forwarding started with CreateFilterAndTransformList
        /// </summary>
        public void Dispose()
        {
            _activitySourceListener?.Dispose();
            _activitySourceListener = null;

            var curSpec = _specList;
            _specList = null;            // Null out the list
            while (curSpec != null)     // Dispose everything in the list.
            {
                curSpec.Dispose();
                curSpec = curSpec.Next;
            }
        }
    }

    // This olds one the implicit transform for one type of object.
    // We remember this type-transform pair in the _firstImplicitTransformsEntry cache.
    private sealed class ImplicitTransformEntry
    {
        public Type? Type;
        public TransformSpec? Transforms;
    }

    /// <summary>
    /// Transform spec represents a string that describes how to extract a piece of data from
    /// the DiagnosticSource payload. An example string is OUTSTR=EVENT_VALUE.PROP1.PROP2.PROP3
    /// It has a Next field so they can be chained together in a linked list.
    /// </summary>
    private sealed class TransformSpec
    {
        /// <summary>
        /// parse the strings 'spec' from startIdx to endIdx (points just beyond the last considered char)
        /// The syntax is ID1=ID2.ID3.ID4 .... Where ID1= is optional.
        /// </summary>
        public TransformSpec(string transformSpec, int startIdx, int endIdx, TransformSpec? next = null)
        {
            Debug.Assert(transformSpec != null && startIdx >= 0 && startIdx < endIdx && endIdx <= transformSpec.Length);
            Next = next;

            // Pick off the Var=
            int equalsIdx = transformSpec.IndexOf('=', startIdx, endIdx - startIdx);
            if (0 <= equalsIdx)
            {
                _outputName = transformSpec.Substring(startIdx, equalsIdx - startIdx);
                startIdx = equalsIdx + 1;
            }

            // Working from back to front, create a PropertySpec for each .ID in the string.
            while (startIdx < endIdx)
            {
                int dotIdx = transformSpec.LastIndexOf('.', endIdx - 1, endIdx - startIdx);
                int idIdx = startIdx;
                if (0 <= dotIdx)
                    idIdx = dotIdx + 1;

                string propertyName = transformSpec.Substring(idIdx, endIdx - idIdx);
                _fetches = new PropertySpec(propertyName, _fetches);

                // If the user did not explicitly set a name, it is the last one (first to be processed from the end).
                _outputName ??= propertyName;

                endIdx = dotIdx;    // This works even when LastIndexOf return -1.
            }
        }

        /// <summary>
        /// Given the DiagnosticSourcePayload 'obj', compute a key-value pair from it. For example
        /// if the spec is OUTSTR=EVENT_VALUE.PROP1.PROP2.PROP3 and the ultimate value of PROP3 is
        /// 10 then the return key value pair is  KeyValuePair("OUTSTR","10")
        /// </summary>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2112:ReflectionToRequiresUnreferencedCode",
            Justification = "In EventSource, EnsureDescriptorsInitialized's use of GetType preserves this method which " +
                            "requires unreferenced code, but EnsureDescriptorsInitialized does not access this member and is safe to call.")]
        [RequiresUnreferencedCode(DiagnosticSource.WriteRequiresUnreferencedCode)]
        public KeyValuePair<string, string?> Morph(object? obj)
        {
            for (PropertySpec? cur = _fetches; cur != null; cur = cur.Next)
            {
                if (obj != null || cur.IsStatic)
                    obj = cur.Fetch(obj);
            }

            return new KeyValuePair<string, string?>(_outputName, obj?.ToString());
        }

        /// <summary>
        /// A public field that can be used to form a linked list.
        /// </summary>
        public TransformSpec? Next;

        #region private
        /// <summary>
        /// A PropertySpec represents information needed to fetch a property from
        /// and efficiently. Thus it represents a '.PROP' in a TransformSpec
        /// (and a transformSpec has a list of these).
        /// </summary>
        private sealed class PropertySpec
        {
            private const string CurrentActivityPropertyName = "*Activity";
            private const string EnumeratePropertyName = "*Enumerate";

            /// <summary>
            /// Make a new PropertySpec for a property named 'propertyName'.
            /// For convenience you can set he 'next' field to form a linked
            /// list of PropertySpecs.
            /// </summary>
            public PropertySpec(string propertyName, PropertySpec? next)
            {
                Next = next;
                _propertyName = propertyName;

                // detect well-known names that are static functions
                if (_propertyName == CurrentActivityPropertyName)
                {
                    IsStatic = true;
                }
            }

            public bool IsStatic { get; }

            /// <summary>
            /// Given an object fetch the property that this PropertySpec represents.
            /// obj may be null when IsStatic is true, otherwise it must be non-null.
            /// </summary>
            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2112:ReflectionToRequiresUnreferencedCode",
                Justification = "In EventSource, EnsureDescriptorsInitialized's use of GetType preserves this method which " +
                                "requires unreferenced code, but EnsureDescriptorsInitialized does not access this member and is safe to call.")]
            [RequiresUnreferencedCode(DiagnosticSource.WriteRequiresUnreferencedCode)]
            public object? Fetch(object? obj)
            {
                PropertyFetch? fetch = _fetchForExpectedType;
                Debug.Assert(obj != null || IsStatic);
                Type? objType = obj?.GetType();
                if (fetch == null || fetch.Type != objType)
                {
                    _fetchForExpectedType = fetch = PropertyFetch.FetcherForProperty(objType, _propertyName);
                }
                object? ret = null;
                // Avoid the exception which can be thrown during accessing the object properties.
                try { ret = fetch!.Fetch(obj); } catch (Exception e) { DiagnosticSourceEventSource.Log.Message($"Property {objType}.{_propertyName} threw the exception {e}"); }
                return ret;
            }

            /// <summary>
            /// A public field that can be used to form a linked list.
            /// </summary>
            public PropertySpec? Next;

            #region private
            /// <summary>
            /// PropertyFetch is a helper class. It takes a PropertyInfo and then knows how
            /// to efficiently fetch that property from a .NET object (See Fetch method).
            /// It hides some slightly complex generic code.
            /// </summary>
            private class PropertyFetch
            {
                public PropertyFetch(Type? type)
                {
                    Type = type;
                }

                /// <summary>
                /// The type of the object that the property is fetched from. For well-known static methods that
                /// aren't actually property getters this will return null.
                /// </summary>
                internal Type? Type { get; }

                /// <summary>
                /// Create a property fetcher for a propertyName
                /// </summary>
                [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2112:ReflectionToRequiresUnreferencedCode",
                    Justification = "In EventSource, EnsureDescriptorsInitialized's use of GetType preserves this method which " +
                                    "requires unreferenced code, but EnsureDescriptorsInitialized does not access this member and is safe to call.")]
                [RequiresUnreferencedCode(DiagnosticSource.WriteRequiresUnreferencedCode)]
                public static PropertyFetch FetcherForProperty(Type? type, string propertyName)
                {
                    if (propertyName == null)
                        return new PropertyFetch(type);     // returns null on any fetch.
                    if (propertyName == CurrentActivityPropertyName)
                    {
                        return new CurrentActivityPropertyFetch();
                    }

                    Debug.Assert(type != null, "Type should only be null for the well-known static fetchers already checked");
                    TypeInfo typeInfo = type.GetTypeInfo();
                    if (propertyName == EnumeratePropertyName)
                    {
                        // If there are multiple implementations of IEnumerable<T>, this arbitrarily uses the first one
                        foreach (Type iFaceType in typeInfo.GetInterfaces())
                        {
                            TypeInfo iFaceTypeInfo = iFaceType.GetTypeInfo();
                            if (!iFaceTypeInfo.IsGenericType ||
                                iFaceTypeInfo.GetGenericTypeDefinition() != typeof(IEnumerable<>))
                            {
                                continue;
                            }

                            return CreateEnumeratePropertyFetch(type, iFaceTypeInfo);
                        }

                        // no implementation of IEnumerable<T> found, return a null fetcher
                        DiagnosticSourceEventSource.Log.Message($"*Enumerate applied to non-enumerable type {type}");
                        return new PropertyFetch(type);
                    }
                    else
                    {
                        PropertyInfo? propertyInfo = typeInfo.GetDeclaredProperty(propertyName);
                        if (propertyInfo == null)
                        {
                            foreach (PropertyInfo pi in typeInfo.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                            {
                                if (pi.Name == propertyName)
                                {
                                    propertyInfo = pi;
                                    break;
                                }
                            }
                        }

                        if (propertyInfo == null)
                        {
                            DiagnosticSourceEventSource.Log.Message($"Property {propertyName} not found on {type}. Ensure the name is spelled correctly. If you published the application with PublishTrimmed=true, ensure the property was not trimmed away.");
                            return new PropertyFetch(type);
                        }
                        // Delegate creation below is incompatible with static properties.
                        else if (propertyInfo.GetMethod?.IsStatic == true || propertyInfo.SetMethod?.IsStatic == true)
                        {
                            DiagnosticSourceEventSource.Log.Message($"Property {propertyName} is static.");
                            return new PropertyFetch(type);
                        }

                        return CreatePropertyFetch(typeInfo, propertyInfo);
                    }
                }

                [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode",
                    Justification = "MakeGenericType is only called when IsDynamicCodeSupported is true or only with ref types.")]
                private static PropertyFetch CreateEnumeratePropertyFetch(Type type, TypeInfo enumerableOfTType)
                {
                    Type elemType = enumerableOfTType.GetGenericArguments()[0];
#if NET
                    if (!RuntimeFeature.IsDynamicCodeSupported && elemType.IsValueType)
                    {
                        return new EnumeratePropertyFetch(type);
                    }
#endif
                    Type instantiatedTypedPropertyFetcher = typeof(EnumeratePropertyFetch<>)
                        .GetTypeInfo().MakeGenericType(elemType);
                    return (PropertyFetch)Activator.CreateInstance(instantiatedTypedPropertyFetcher, type)!;
                }

                [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode",
                    Justification = "MakeGenericType is only called when IsDynamicCodeSupported is true or only with ref types.")]
                private static PropertyFetch CreatePropertyFetch(Type type, PropertyInfo propertyInfo)
                {
#if NET
                    if (!RuntimeFeature.IsDynamicCodeSupported && (propertyInfo.DeclaringType!.IsValueType || propertyInfo.PropertyType.IsValueType))
                    {
                        return new ReflectionPropertyFetch(type, propertyInfo);
                    }
#endif
                    Type typedPropertyFetcher = type.IsValueType ?
                        typeof(ValueTypedFetchProperty<,>) : typeof(RefTypedFetchProperty<,>);
                    Type instantiatedTypedPropertyFetcher = typedPropertyFetcher.GetTypeInfo().MakeGenericType(
                        propertyInfo.DeclaringType!, propertyInfo.PropertyType);
                    return (PropertyFetch)Activator.CreateInstance(instantiatedTypedPropertyFetcher, type, propertyInfo)!;
                }

                /// <summary>
                /// Given an object, fetch the property that this propertyFech represents.
                /// </summary>
                public virtual object? Fetch(object? obj) { return null; }

                #region private

                private sealed class RefTypedFetchProperty<TObject, TProperty> : PropertyFetch
                {
                    public RefTypedFetchProperty(Type type, PropertyInfo property) : base(type)
                    {
                        Debug.Assert(typeof(TObject).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()));
                        _propertyFetch = (Func<TObject, TProperty>)property.GetMethod!.CreateDelegate(typeof(Func<TObject, TProperty>));
                    }
                    public override object? Fetch(object? obj)
                    {
                        Debug.Assert(obj is TObject);
                        return _propertyFetch((TObject)obj);
                    }
                    private readonly Func<TObject, TProperty> _propertyFetch;
                }

                private delegate TProperty StructFunc<TStruct, TProperty>(ref TStruct thisArg);

                // Value types methods require that the first argument is passed by reference. This requires a different delegate signature
                // from the reference type case.
                private sealed class ValueTypedFetchProperty<TStruct, TProperty> : PropertyFetch
                {
                    public ValueTypedFetchProperty(Type type, PropertyInfo property) : base(type)
                    {
                        Debug.Assert(typeof(TStruct) == type);
                        _propertyFetch = (StructFunc<TStruct, TProperty>)property.GetMethod!.CreateDelegate(typeof(StructFunc<TStruct, TProperty>));
                    }
                    public override object? Fetch(object? obj)
                    {
                        Debug.Assert(obj is TStruct);
                        // It is uncommon for property getters to mutate the struct, but if they do the change will be lost.
                        // We are calling the getter on an unboxed copy
                        TStruct structObj = (TStruct)obj;
                        return _propertyFetch(ref structObj);
                    }
                    private readonly StructFunc<TStruct, TProperty> _propertyFetch;
                }

#if NET
                /// <summary>
                /// A fetcher that can be used when MakeGenericType isn't available.
                /// </summary>
                private sealed class ReflectionPropertyFetch : PropertyFetch
                {
                    private readonly MethodInvoker _getterInvoker;
                    public ReflectionPropertyFetch(Type type, PropertyInfo property) : base(type)
                    {
                        _getterInvoker = MethodInvoker.Create(property.GetMethod!);
                    }

                    public override object? Fetch(object? obj) => _getterInvoker.Invoke(obj);
                }

                /// <summary>
                /// A fetcher that enumerates and formats an IEnumerable when MakeGenericType isn't available.
                /// </summary>
                private sealed class EnumeratePropertyFetch : PropertyFetch
                {
                    public EnumeratePropertyFetch(Type type) : base(type) { }

                    public override object? Fetch(object? obj)
                    {
                        IEnumerable? enumerable = obj as IEnumerable;
                        Debug.Assert(enumerable is not null);

                        // string.Join for a non-generic IEnumerable
                        IEnumerator en = enumerable.GetEnumerator();
                        using (IDisposable? disposable = en as IDisposable)
                        {
                            if (!en.MoveNext())
                            {
                                return string.Empty;
                            }

                            object? currentValue = en.Current;
                            string? firstString = currentValue?.ToString();

                            // If there's only 1 item, simply return the ToString of that
                            if (!en.MoveNext())
                            {
                                // Only one value available
                                return firstString ?? string.Empty;
                            }

                            var result = new ValueStringBuilder(stackalloc char[256]);

                            result.Append(firstString);

                            do
                            {
                                currentValue = en.Current;

                                result.Append(",");
                                if (currentValue != null)
                                {
                                    result.Append(currentValue.ToString());
                                }
                            }
                            while (en.MoveNext());

                            return result.ToString();
                        }
                    }
                }
#endif

                /// <summary>
                /// A fetcher that returns the result of Activity.Current
                /// </summary>
                private sealed class CurrentActivityPropertyFetch : PropertyFetch
                {
                    public CurrentActivityPropertyFetch() : base(null) { }
                    public override object? Fetch(object? obj)
                    {
                        return Activity.Current;
                    }
                }

                /// <summary>
                /// A fetcher that enumerates and formats an IEnumerable
                /// </summary>
                private sealed class EnumeratePropertyFetch<ElementType> : PropertyFetch
                {
                    public EnumeratePropertyFetch(Type type) : base(type) { }
                    public override object? Fetch(object? obj)
                    {
                        Debug.Assert(obj is IEnumerable<ElementType>);
                        return string.Join(",", (IEnumerable<ElementType>)obj);
                    }
                }
                #endregion
            }

            private readonly string _propertyName;
            private volatile PropertyFetch? _fetchForExpectedType;
            #endregion
        }

        private readonly string _outputName = null!;
        private readonly PropertySpec? _fetches;
        #endregion
    }

    /// <summary>
    /// CallbackObserver is an adapter class that creates an observer (which you can pass
    /// to IObservable.Subscribe), and calls the given callback every time the 'next'
    /// operation on the IObserver happens.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    private sealed class CallbackObserver<T> : IObserver<T>
    {
        public CallbackObserver(Action<T> callback) { _callback = callback; }

        #region private
        public void OnCompleted() { }
        public void OnError(Exception error) { }
        public void OnNext(T value) { _callback(value); }

        private readonly Action<T> _callback;
        #endregion
    }

    // A linked list of IObservable subscriptions (which are IDisposable).
    // We use this to keep track of the DiagnosticSource subscriptions.
    // We use this linked list for thread atomicity
    private sealed class Subscriptions
    {
        public Subscriptions(IDisposable subscription, Subscriptions? next)
        {
            Subscription = subscription;
            Next = next;
        }
        public IDisposable Subscription;
        public Subscriptions? Next;
    }

    // Given a type generate all the implicit transforms for type (that is for every field
    // generate the spec that fetches it).
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2112:ReflectionToRequiresUnreferencedCode",
        Justification = "In EventSource, EnsureDescriptorsInitialized's use of GetType preserves this method which " +
                        "requires unreferenced code, but EnsureDescriptorsInitialized does not access this member and is safe to call.")]
    [RequiresUnreferencedCode(DiagnosticSource.WriteRequiresUnreferencedCode)]
    private static TransformSpec? MakeImplicitTransforms(Type type)
    {
        TransformSpec? newSerializableArgs = null;
        TypeInfo curTypeInfo = type.GetTypeInfo();
        foreach (PropertyInfo property in curTypeInfo.GetProperties(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            // prevent TransformSpec from attempting to implicitly transform index properties
            if (property.GetMethod == null || property.GetMethod!.GetParameters().Length > 0)
                continue;
            newSerializableArgs = new TransformSpec(property.Name, 0, property.Name.Length, newSerializableArgs);
        }
        return Reverse(newSerializableArgs);
    }

    // Reverses a linked list (of TransformSpecs) in place.
    private static TransformSpec? Reverse(TransformSpec? list)
    {
        TransformSpec? ret = null;
        while (list != null)
        {
            var next = list.Next;
            list.Next = ret;
            ret = list;
            list = next;
        }
        return ret;
    }

    private IDisposable? _diagnosticsListenersSubscription; // This is our subscription that listens for new Diagnostic source to appear.
    private Subscriptions? _liveSubscriptions;              // These are the subscriptions that we are currently forwarding to the EventSource.
    private readonly bool _noImplicitTransforms;                    // Listener can say they don't want implicit transforms.
    private ImplicitTransformEntry? _firstImplicitTransformsEntry; // The transform for _firstImplicitFieldsType
    private ConcurrentDictionary<Type, TransformSpec?>? _implicitTransformsTable; // If there is more than one object type for an implicit transform, they go here.
    private readonly TransformSpec? _explicitTransforms;             // payload to include because the user explicitly indicated how to fetch the field.
}
