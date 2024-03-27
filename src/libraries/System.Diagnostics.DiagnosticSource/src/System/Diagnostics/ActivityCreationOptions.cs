// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Diagnostics
{
    /// <summary>
    /// ActivityCreationOptions is encapsulating all needed information which will be sent to the ActivityListener to decide about creating the Activity object and with what state.
    /// The possible generic type parameters is <see cref="ActivityContext"/> or <see cref="string"/>
    /// </summary>
    public readonly struct ActivityCreationOptions<T>
    {
        private readonly ActivityTagsCollection? _samplerTags;
        private readonly ActivityContext _context;
        private readonly string? _traceState;

        /// <summary>
        /// Construct a new <see cref="ActivityCreationOptions{T}"/> object.
        /// </summary>
        /// <param name="source">The trace Activity source<see cref="ActivitySource"/> used to request creating the Activity object.</param>
        /// <param name="name">The operation name of the Activity.</param>
        /// <param name="parent">The requested parent to create the Activity object with. The parent either be a parent Id represented as string or it can be a parent context <see cref="ActivityContext"/>.</param>
        /// <param name="kind"><see cref="ActivityKind"/> to create the Activity object with.</param>
        /// <param name="tags">Key-value pairs list for the tags to create the Activity object with.<see cref="ActivityContext"/></param>
        /// <param name="links"><see cref="ActivityLink"/> list to create the Activity object with.</param>
        /// <param name="idFormat">The default Id format to use.</param>
        internal ActivityCreationOptions(ActivitySource source, string name, T parent, ActivityKind kind, IEnumerable<KeyValuePair<string, object?>>? tags, IEnumerable<ActivityLink>? links, ActivityIdFormat idFormat)
        {
            Source = source;
            Name = name;
            Kind = kind;
            Parent = parent;
            Tags = tags;
            Links = links;
            IdFormat = idFormat;

            if (IdFormat == ActivityIdFormat.Unknown && Activity.ForceDefaultIdFormat)
            {
                IdFormat = Activity.DefaultIdFormat;
            }

            _samplerTags = null;
            _traceState = null;

            if (parent is ActivityContext ac && ac != default)
            {
                _context = ac;
                if (IdFormat == ActivityIdFormat.Unknown)
                {
                    IdFormat = ActivityIdFormat.W3C;
                }

                _traceState = ac.TraceState;
            }
            else if (parent is string p)
            {
                if (IdFormat != ActivityIdFormat.Hierarchical)
                {
                    if (ActivityContext.TryParse(p, null, out _context))
                    {
                        IdFormat = ActivityIdFormat.W3C;
                    }

                    if (IdFormat == ActivityIdFormat.Unknown)
                    {
                        IdFormat = ActivityIdFormat.Hierarchical;
                    }
                }
                else
                {
                    _context = default;
                }
            }
            else
            {
                _context = default;
                if (IdFormat == ActivityIdFormat.Unknown)
                {
                    IdFormat = Activity.Current != null ? Activity.Current.IdFormat : Activity.DefaultIdFormat;
                }
            }
        }

        /// <summary>
        /// Retrieve the <see cref="ActivitySource"/> object.
        /// </summary>
        public ActivitySource Source { get; }

        /// <summary>
        /// Retrieve the name which requested to create the Activity object with.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Retrieve the <see cref="ActivityKind"/> which requested to create the Activity object with.
        /// </summary>
        public ActivityKind Kind { get; }

        /// <summary>
        /// Retrieve the parent which requested to create the Activity object with. Parent will be either in form of string or <see cref="ActivityContext"/>.
        /// </summary>
        public T Parent { get; }

        /// <summary>
        /// Retrieve the tags which requested to create the Activity object with.
        /// </summary>
        public IEnumerable<KeyValuePair<string, object?>>? Tags { get; }

        /// <summary>
        /// Retrieve the list of <see cref="ActivityLink"/> which requested to create the Activity object with.
        /// </summary>
        public IEnumerable<ActivityLink>? Links { get; }

        public ActivityTagsCollection SamplingTags
        {
            get
            {
                if (_samplerTags == null)
                {
                    // Because the struct is readonly, we cannot directly assign _samplerTags. We have to workaround it by calling Unsafe.AsRef
                    Unsafe.AsRef(in _samplerTags) = new ActivityTagsCollection();
                }

                return _samplerTags!;
            }
        }

        public ActivityTraceId TraceId
        {
            get
            {
                if (Parent is ActivityContext && IdFormat == ActivityIdFormat.W3C && _context == default)
                {
                    Func<ActivityTraceId>? traceIdGenerator = Activity.TraceIdGenerator;
                    ActivityTraceId id = traceIdGenerator == null ? ActivityTraceId.CreateRandom() : traceIdGenerator();

                    // Because the struct is readonly, we cannot directly assign _context. We have to workaround it by calling Unsafe.AsRef
                    Unsafe.AsRef(in _context) = new ActivityContext(id, default, ActivityTraceFlags.None);
                }

                return _context.TraceId;
            }
        }

        /// <summary>
        /// Retrieve or initialize the trace state to use for the Activity we may create.
        /// </summary>
        public string? TraceState
        {
            get => _traceState;
            init
            {
                _traceState = value;
            }
        }

        // SetTraceState is to set the _traceState without the need of copying the whole structure.
        internal void SetTraceState(string? traceState) => Unsafe.AsRef(in _traceState) = traceState;

        /// <summary>
        /// Retrieve Id format of to use for the Activity we may create.
        /// </summary>
        internal ActivityIdFormat IdFormat { get; }

        // Helper to access the sampling tags. The SamplingTags Getter can allocate when not necessary.
        internal ActivityTagsCollection? GetSamplingTags() => _samplerTags;

        internal ActivityContext GetContext() => _context;
    }
}
