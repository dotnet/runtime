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

        /// <summary>
        /// Construct a new <see cref="ActivityCreationOptions{T}"/> object.
        /// </summary>
        /// <param name="source">The trace Activity source<see cref="ActivitySource"/> used to request creating the Activity object.</param>
        /// <param name="name">The operation name of the Activity.</param>
        /// <param name="parent">The requested parent to create the Activity object with. The parent either be a parent Id represented as string or it can be a parent context <see cref="ActivityContext"/>.</param>
        /// <param name="kind"><see cref="ActivityKind"/> to create the Activity object with.</param>
        /// <param name="tags">Key-value pairs list for the tags to create the Activity object with.<see cref="ActivityContext"/></param>
        /// <param name="links"><see cref="ActivityLink"/> list to create the Activity object with.</param>
        internal ActivityCreationOptions(ActivitySource source, string name, T parent, ActivityKind kind, IEnumerable<KeyValuePair<string, object?>>? tags, IEnumerable<ActivityLink>? links)
        {
            Source = source;
            Name = name;
            Kind = kind;
            Parent = parent;
            Tags = tags;
            Links = links;

            _samplerTags = null;

            if (parent is ActivityContext ac)
            {
                _context = ac;
            }
            else if (parent is string p && p != null)
            {
                // We don't care about the return value. we care if _context is initialized accordingly.
                ActivityContext.TryParse(p, null, out _context);
            }
            else
            {
                _context = default;
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
#if ALLOW_PARTIALLY_TRUSTED_CALLERS
            [System.Security.SecuritySafeCriticalAttribute]
#endif
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
#if ALLOW_PARTIALLY_TRUSTED_CALLERS
            [System.Security.SecuritySafeCriticalAttribute]
#endif
            get
            {
                if (Parent is ActivityContext && _context == default)
                {
                    // Because the struct is readonly, we cannot directly assign _context. We have to workaround it by calling Unsafe.AsRef
                    Unsafe.AsRef(in _context) = new ActivityContext(ActivityTraceId.CreateRandom(), default, ActivityTraceFlags.None);
                }

                return _context.TraceId;
            }
        }

        // Helper to access the sampling tags. The SamplingTags Getter can allocate when not necessary.
        internal ActivityTagsCollection? GetSamplingTags() => _samplerTags;

        internal ActivityContext GetContext() => _context;
    }
}
