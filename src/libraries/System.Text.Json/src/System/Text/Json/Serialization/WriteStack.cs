// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace System.Text.Json
{
    [DebuggerDisplay("Path:{PropertyPath()} Current: ConverterStrategy.{ConverterStrategy.JsonTypeInfo.PropertyInfoForTypeInfo.ConverterStrategy}, {Current.JsonTypeInfo.Type.Name}")]
    internal struct WriteStack
    {
        /// <summary>
        /// The number of stack frames when the continuation started.
        /// </summary>
        private int _continuationCount;

        /// <summary>
        /// The number of stack frames including Current. _previous will contain _count-1 higher frames.
        /// </summary>
        private int _count;

        /// <summary>
        /// Cancellation token used by converters performing async serialization (e.g. IAsyncEnumerable)
        /// </summary>
        public CancellationToken CancellationToken;

        /// <summary>
        /// Stores a pending task that a resumable converter depends on to continue work.
        /// It must be awaited by the root context before serialization is resumed.
        /// </summary>
        public Task? PendingTask;

        /// <summary>
        /// List of IAsyncDisposables that have been scheduled for disposal by converters.
        /// </summary>
        public List<IAsyncDisposable>? PendingAsyncDisposables;

        private List<WriteStackFrame> _previous;

        // A field is used instead of a property to avoid value semantics.
        public WriteStackFrame Current;

        /// <summary>
        /// The amount of bytes to write before the underlying Stream should be flushed and the
        /// current buffer adjusted to remove the processed bytes.
        /// </summary>
        public int FlushThreshold;

        public bool IsContinuation => _continuationCount != 0;

        // The bag of preservable references.
        public ReferenceResolver ReferenceResolver;

        /// <summary>
        /// Internal flag to let us know that we need to read ahead in the inner read loop.
        /// </summary>
        public bool SupportContinuation;

        private void AddCurrent()
        {
            if (_previous == null)
            {
                _previous = new List<WriteStackFrame>();
            }

            if (_count > _previous.Count)
            {
                // Need to allocate a new array element.
                _previous.Add(Current);
            }
            else
            {
                // Use a previously allocated slot.
                _previous[_count - 1] = Current;
            }

            _count++;
        }

        /// <summary>
        /// Initialize the state without delayed initialization of the JsonTypeInfo.
        /// </summary>
        public JsonConverter Initialize(Type type, JsonSerializerOptions options, bool supportContinuation)
        {
            JsonTypeInfo jsonTypeInfo = options.GetOrAddClassForRootType(type);
            Debug.Assert(options == jsonTypeInfo.Options);
            return Initialize(jsonTypeInfo, supportContinuation);
        }

        internal JsonConverter Initialize(JsonTypeInfo jsonTypeInfo, bool supportContinuation)
        {
            Current.JsonTypeInfo = jsonTypeInfo;
            Current.DeclaredJsonPropertyInfo = jsonTypeInfo.PropertyInfoForTypeInfo;
            Current.NumberHandling = Current.DeclaredJsonPropertyInfo.NumberHandling;

            JsonSerializerOptions options = jsonTypeInfo.Options;
            if (options.ReferenceHandlingStrategy != ReferenceHandlingStrategy.None)
            {
                Debug.Assert(options.ReferenceHandler != null);
                ReferenceResolver = options.ReferenceHandler.CreateResolver(writing: true);
            }

            SupportContinuation = supportContinuation;

            return jsonTypeInfo.PropertyInfoForTypeInfo.ConverterBase;
        }

        public void Push()
        {
            if (_continuationCount == 0)
            {
                if (_count == 0)
                {
                    // The first stack frame is held in Current.
                    _count = 1;
                }
                else
                {
                    JsonTypeInfo jsonTypeInfo = Current.GetPolymorphicJsonPropertyInfo().RuntimeTypeInfo;
                    JsonNumberHandling? numberHandling = Current.NumberHandling;

                    AddCurrent();
                    Current.Reset();

                    Current.JsonTypeInfo = jsonTypeInfo;
                    Current.DeclaredJsonPropertyInfo = jsonTypeInfo.PropertyInfoForTypeInfo;
                    // Allow number handling on property to win over handling on type.
                    Current.NumberHandling = numberHandling ?? Current.DeclaredJsonPropertyInfo.NumberHandling;
                }
            }
            else if (_continuationCount == 1)
            {
                // No need for a push since there is only one stack frame.
                Debug.Assert(_count == 1);
                _continuationCount = 0;
            }
            else
            {
                // A continuation, adjust the index.
                Current = _previous[_count - 1];

                // Check if we are done.
                if (_count == _continuationCount)
                {
                    _continuationCount = 0;
                }
                else
                {
                    _count++;
                }
            }
        }

        public void Pop(bool success)
        {
            Debug.Assert(_count > 0);

            if (!success)
            {
                // Check if we need to initialize the continuation.
                if (_continuationCount == 0)
                {
                    if (_count == 1)
                    {
                        // No need for a continuation since there is only one stack frame.
                        _continuationCount = 1;
                        _count = 1;
                    }
                    else
                    {
                        AddCurrent();
                        _count--;
                        _continuationCount = _count;
                        _count--;
                        Current = _previous[_count - 1];
                    }

                    return;
                }

                if (_continuationCount == 1)
                {
                    // No need for a pop since there is only one stack frame.
                    Debug.Assert(_count == 1);
                    return;
                }

                // Update the list entry to the current value.
                _previous[_count - 1] = Current;

                Debug.Assert(_count > 0);
            }
            else
            {
                Debug.Assert(_continuationCount == 0);

                if (Current.AsyncEnumerator is not null)
                {
                    // we have completed serialization of an AsyncEnumerator,
                    // pop from the stack and schedule for async disposal.
                    PendingAsyncDisposables ??= new List<IAsyncDisposable>();
                    PendingAsyncDisposables.Add(Current.AsyncEnumerator);
                }
            }

            if (_count > 1)
            {
                Current = _previous[--_count - 1];
            }
        }

        // Asynchronously dispose of any AsyncDisposables that have been scheduled for disposal
        public async ValueTask DisposePendingAsyncDisposables()
        {
            Debug.Assert(PendingAsyncDisposables?.Count > 0);
            Exception? exception = null;

            foreach (IAsyncDisposable asyncDisposable in PendingAsyncDisposables)
            {
                try
                {
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    exception = e;
                }
            }

            if (exception is not null)
            {
                ExceptionDispatchInfo.Capture(exception).Throw();
            }

            PendingAsyncDisposables.Clear();
        }

        /// <summary>
        /// Walks the stack cleaning up any leftover IDisposables
        /// in the event of an exception on serialization
        /// </summary>
        public void DisposePendingDisposablesOnException()
        {
            Exception? exception = null;

            Debug.Assert(Current.AsyncEnumerator is null);
            DisposeFrame(Current.CollectionEnumerator, ref exception);

            int stackSize = Math.Max(_count, _continuationCount);
            if (stackSize > 1)
            {
                for (int i = 0; i < stackSize - 1; i++)
                {
                    Debug.Assert(_previous[i].AsyncEnumerator is null);
                    DisposeFrame(_previous[i].CollectionEnumerator, ref exception);
                }
            }

            if (exception is not null)
            {
                ExceptionDispatchInfo.Capture(exception).Throw();
            }

            static void DisposeFrame(IEnumerator? collectionEnumerator, ref Exception? exception)
            {
                try
                {
                    if (collectionEnumerator is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                catch (Exception e)
                {
                    exception = e;
                }
            }
        }

        /// <summary>
        /// Walks the stack cleaning up any leftover I(Async)Disposables
        /// in the event of an exception on async serialization
        /// </summary>
        public async ValueTask DisposePendingDisposablesOnExceptionAsync()
        {
            Exception? exception = null;

            exception = await DisposeFrame(Current.CollectionEnumerator, Current.AsyncEnumerator, exception).ConfigureAwait(false);

            int stackSize = Math.Max(_count, _continuationCount);
            if (stackSize > 1)
            {
                for (int i = 0; i < stackSize - 1; i++)
                {
                    exception = await DisposeFrame(_previous[i].CollectionEnumerator, _previous[i].AsyncEnumerator, exception).ConfigureAwait(false);
                }
            }

            if (exception is not null)
            {
                ExceptionDispatchInfo.Capture(exception).Throw();
            }

            static async ValueTask<Exception?> DisposeFrame(IEnumerator? collectionEnumerator, IAsyncDisposable? asyncDisposable, Exception? exception)
            {
                Debug.Assert(!(collectionEnumerator is not null && asyncDisposable is not null));

                try
                {
                    if (collectionEnumerator is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    else if (asyncDisposable is not null)
                    {
                        await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                    }
                }
                catch (Exception e)
                {
                    exception = e;
                }

                return exception;
            }
        }

        // Return a property path as a simple JSONPath using dot-notation when possible. When special characters are present, bracket-notation is used:
        // $.x.y.z
        // $['PropertyName.With.Special.Chars']
        public string PropertyPath()
        {
            StringBuilder sb = new StringBuilder("$");

            // If a continuation, always report back full stack.
            int count = Math.Max(_count, _continuationCount);

            for (int i = 0; i < count - 1; i++)
            {
                AppendStackFrame(sb, _previous[i]);
            }

            if (_continuationCount == 0)
            {
                AppendStackFrame(sb, Current);
            }

            return sb.ToString();

            void AppendStackFrame(StringBuilder sb, in WriteStackFrame frame)
            {
                // Append the property name.
                string? propertyName = frame.DeclaredJsonPropertyInfo?.MemberInfo?.Name;
                if (propertyName == null)
                {
                    // Attempt to get the JSON property name from the property name specified in re-entry.
                    propertyName = frame.JsonPropertyNameAsString;
                }

                AppendPropertyName(sb, propertyName);
            }

            void AppendPropertyName(StringBuilder sb, string? propertyName)
            {
                if (propertyName != null)
                {
                    if (propertyName.IndexOfAny(ReadStack.SpecialCharacters) != -1)
                    {
                        sb.Append(@"['");
                        sb.Append(propertyName);
                        sb.Append(@"']");
                    }
                    else
                    {
                        sb.Append('.');
                        sb.Append(propertyName);
                    }
                }
            }
        }
    }
}
