// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Net.Http.Metrics;

public class HttpMetricsEnrichmentContext
{
    private HttpRequestMessage? _request;
    private HttpResponseMessage? _response;
    private Exception? _exception;
    private TagCollection _tags = new TagCollection();

    public HttpRequestMessage Request
    {
        get
        {
            EnsureRunningCallback();
            return _request!;
        }
    }

    public HttpResponseMessage? Response
    {
        get
        {
            EnsureRunningCallback();
            return _response;
        }
    }

    public Exception? Exception
    {
        get
        {
            EnsureRunningCallback();
            return _exception;
        }
    }

    internal bool InProgress => _request != null;

    public ICollection<KeyValuePair<string, object?>> CustomTags => _tags;

    internal void ApplyEnrichment(HttpRequestMessage request, HttpResponseMessage? response, Exception? exception, ref TagList tags)
    {
        if (request._options?.TryGetMetricsEnrichmentCallbackCollection(out List<Action<HttpMetricsEnrichmentContext>>? callbackCollection) != true)
        {
            return;
        }

        _request = request;
        _response = response;
        _exception = exception;
        _tags._runningCallback = true;

        try
        {
            foreach (Action<HttpMetricsEnrichmentContext> callback in callbackCollection!)
            {
                callback(this);
            }

            foreach (KeyValuePair<string, object?> tag in _tags._tags)
            {
                tags.Add(tag);
            }
        }
        finally
        {
            _tags._tags.Clear();
            _tags._runningCallback = false;
            _request = null;
            _response = null;
            _exception = null;
        }
    }

    private void EnsureRunningCallback()
    {
        if (_request == null) throw new InvalidOperationException("Enrichment callback should not cache HttpMetricsEnrichmentContext");
    }

    private sealed class TagCollection : ICollection<KeyValuePair<string, object?>>
    {
        public bool _runningCallback;
        internal List<KeyValuePair<string, object?>> _tags = new();

        public int Count
        {
            get
            {
                EnsureRunningCallback();
                return _tags.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                EnsureRunningCallback();
                return false;
            }
        }

        public void Add(KeyValuePair<string, object?> item)
        {
            EnsureRunningCallback();
            _tags.Add(item);
        }

        public void Clear()
        {
            EnsureRunningCallback();
            _tags.Clear();
        }

        public bool Contains(KeyValuePair<string, object?> item)
        {
            EnsureRunningCallback();
            return _tags.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex)
        {
            EnsureRunningCallback();
            _tags.CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            EnsureRunningCallback();
            return _tags.GetEnumerator();
        }

        public bool Remove(KeyValuePair<string, object?> item)
        {
            EnsureRunningCallback();
            return _tags.Remove(item);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private void EnsureRunningCallback()
        {
            if (!_runningCallback) throw new InvalidOperationException("Enrichment callback should not cache HttpMetricsEnrichmentContext");
        }
    }
}
