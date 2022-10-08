// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using DiagnosticsTraceSource = System.Diagnostics.TraceSource;

namespace Microsoft.Extensions.Logging.TraceSource
{
    /// <summary>
    /// Provides an ILoggerFactory based on System.Diagnostics.TraceSource.
    /// </summary>
    [ProviderAlias("TraceSource")]
    public class TraceSourceLoggerProvider : ILoggerProvider
    {
        private readonly SourceSwitch _rootSourceSwitch;
        private readonly TraceListener? _rootTraceListener;

        private readonly ConcurrentDictionary<string, DiagnosticsTraceSource> _sources = new ConcurrentDictionary<string, DiagnosticsTraceSource>(StringComparer.OrdinalIgnoreCase);

        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="TraceSourceLoggerProvider"/> class.
        /// </summary>
        /// <param name="rootSourceSwitch">The <see cref="SourceSwitch"/> to use.</param>
        public TraceSourceLoggerProvider(SourceSwitch rootSourceSwitch)
            : this(rootSourceSwitch, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TraceSourceLoggerProvider"/> class.
        /// </summary>
        /// <param name="rootSourceSwitch">The <see cref="SourceSwitch"/> to use.</param>
        /// <param name="rootTraceListener">The <see cref="TraceListener"/> to use.</param>
        public TraceSourceLoggerProvider(SourceSwitch rootSourceSwitch, TraceListener? rootTraceListener)
        {
            ThrowHelper.ThrowIfNull(rootSourceSwitch);

            _rootSourceSwitch = rootSourceSwitch;
            _rootTraceListener = rootTraceListener;
        }

        /// <summary>
        /// Creates a new <see cref="ILogger"/>  for the given component name.
        /// </summary>
        /// <param name="name">The name of the <see cref="TraceSource"/> to add.</param>
        /// <returns>The <see cref="TraceSourceLogger"/> that was created.</returns>
        public ILogger CreateLogger(string name)
        {
            return new TraceSourceLogger(GetOrAddTraceSource(name));
        }

        private DiagnosticsTraceSource GetOrAddTraceSource(string name) =>
            _sources.TryGetValue(name, out DiagnosticsTraceSource? source) ? source :
            _sources.GetOrAdd(name, InitializeTraceSource(name));

        private DiagnosticsTraceSource InitializeTraceSource(string traceSourceName)
        {
            var traceSource = new DiagnosticsTraceSource(traceSourceName);
            string? parentSourceName = ParentSourceName(traceSourceName);

            if (string.IsNullOrEmpty(parentSourceName))
            {
                if (HasDefaultSwitch(traceSource))
                {
                    traceSource.Switch = _rootSourceSwitch;
                }

                if (_rootTraceListener != null)
                {
                    traceSource.Listeners.Add(_rootTraceListener);
                }
            }
            else
            {
                if (HasDefaultListeners(traceSource))
                {
                    DiagnosticsTraceSource parentTraceSource = GetOrAddTraceSource(parentSourceName);
                    traceSource.Listeners.Clear();
                    traceSource.Listeners.AddRange(parentTraceSource.Listeners);
                }

                if (HasDefaultSwitch(traceSource))
                {
                    DiagnosticsTraceSource parentTraceSource = GetOrAddTraceSource(parentSourceName);
                    traceSource.Switch = parentTraceSource.Switch;
                }
            }

            return traceSource;
        }

        private static string? ParentSourceName(string traceSourceName)
        {
            int indexOfLastDot = traceSourceName.LastIndexOf('.');
            return indexOfLastDot == -1 ? null : traceSourceName.Substring(0, indexOfLastDot);
        }

        private static bool HasDefaultListeners(DiagnosticsTraceSource traceSource)
        {
            return traceSource.Listeners.Count == 1 && traceSource.Listeners[0] is DefaultTraceListener;
        }

        private static bool HasDefaultSwitch(DiagnosticsTraceSource traceSource)
        {
            return string.IsNullOrEmpty(traceSource.Switch.DisplayName) == string.IsNullOrEmpty(traceSource.Name) &&
                traceSource.Switch.Level == SourceLevels.Off;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                if (_rootTraceListener != null)
                {
                    _rootTraceListener.Flush();
                    _rootTraceListener.Dispose();
                }
            }
        }
    }
}
