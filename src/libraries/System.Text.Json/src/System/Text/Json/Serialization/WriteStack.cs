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
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal struct WriteStack
    {
        public int CurrentDepth => _count;

        /// <summary>
        /// Exposes the stackframe that is currently active.
        /// </summary>
        public WriteStackFrame Current;

        /// <summary>
        /// Buffer containing all frames in the stack. For performance it is only populated for serialization depths > 1.
        /// </summary>
        private WriteStackFrame[] _stack;

        /// <summary>
        /// Tracks the current depth of the stack.
        /// </summary>
        private int _count;

        /// <summary>
        /// If not zero, indicates that the stack is part of a re-entrant continuation of given depth.
        /// </summary>
        private int _continuationCount;

        /// <summary>
        /// Cancellation token used by converters performing async serialization (e.g. IAsyncEnumerable)
        /// </summary>
        public CancellationToken CancellationToken;

        /// <summary>
        /// In the case of async serialization, used by resumable converters to signal that
        /// the current buffer contents should not be flushed to the underlying stream.
        /// </summary>
        public bool SuppressFlush;

        /// <summary>
        /// Stores a pending task that a resumable converter depends on to continue work.
        /// It must be awaited by the root context before serialization is resumed.
        /// </summary>
        public Task? PendingTask;

        /// <summary>
        /// List of completed IAsyncDisposables that have been scheduled for disposal by converters.
        /// </summary>
        public List<IAsyncDisposable>? CompletedAsyncDisposables;

        /// <summary>
        /// The amount of bytes to write before the underlying Stream should be flushed and the
        /// current buffer adjusted to remove the processed bytes.
        /// </summary>
        public int FlushThreshold;

        /// <summary>
        /// Indicates that the state still contains suspended frames waiting re-entry.
        /// </summary>
        public bool IsContinuation => _continuationCount != 0;

        // The bag of preservable references.
        public ReferenceResolver ReferenceResolver;

        /// <summary>
        /// Internal flag to let us know that we need to read ahead in the inner read loop.
        /// </summary>
        public bool SupportContinuation;

        /// <summary>
        /// Stores a reference id that has been calculated for a newly serialized object.
        /// </summary>
        public string? NewReferenceId;

        private void EnsurePushCapacity()
        {
            if (_stack is null)
            {
                _stack = new WriteStackFrame[4];
            }
            else if (_count - 1 == _stack.Length)
            {
                Array.Resize(ref _stack, 2 * _stack.Length);
            }
        }

        /// <summary>
        /// Initialize the state without delayed initialization of the JsonTypeInfo.
        /// </summary>
        public JsonConverter Initialize(Type type, JsonSerializerOptions options, bool supportContinuation)
        {
            JsonTypeInfo jsonTypeInfo = options.GetOrAddJsonTypeInfoForRootType(type);
            return Initialize(jsonTypeInfo, supportContinuation);
        }

        internal JsonConverter Initialize(JsonTypeInfo jsonTypeInfo, bool supportContinuation)
        {
            Current.JsonTypeInfo = jsonTypeInfo;
            Current.JsonPropertyInfo = jsonTypeInfo.PropertyInfoForTypeInfo;
            Current.NumberHandling = Current.JsonPropertyInfo.NumberHandling;

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
                    // Performance optimization: reuse the first stackframe on the first push operation.
                    // NB need to be careful when making writes to Current _before_ the first `Push`
                    // operation is performed.
                    _count = 1;
                }
                else
                {
                    JsonTypeInfo jsonTypeInfo = Current.GetNestedJsonTypeInfo();
                    JsonNumberHandling? numberHandling = Current.NumberHandling;

                    EnsurePushCapacity();
                    _stack[_count - 1] = Current;
                    Current = default;
                    _count++;

                    Current.JsonTypeInfo = jsonTypeInfo;
                    Current.JsonPropertyInfo = jsonTypeInfo.PropertyInfoForTypeInfo;
                    // Allow number handling on property to win over handling on type.
                    Current.NumberHandling = numberHandling ?? Current.JsonPropertyInfo.NumberHandling;
                }
            }
            else
            {
                // We are re-entering a continuation, adjust indices accordingly
                if (_count++ > 0)
                {
                    Current = _stack[_count - 1];
                }

                // check if we are done
                if (_continuationCount == _count)
                {
                    _continuationCount = 0;
                }
            }

#if DEBUG
            // Ensure the method is always exercised in debug builds.
            _ = PropertyPath();
#endif
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
                        // No need to copy any frames here.
                        _continuationCount = 1;
                        _count = 0;
                        return;
                    }

                    // Need to push the Current frame to the stack,
                    // ensure that we have sufficient capacity.
                    EnsurePushCapacity();
                    _continuationCount = _count--;
                }
                else if (--_count == 0)
                {
                    // reached the root, no need to copy frames.
                    return;
                }

                _stack[_count] = Current;
                Current = _stack[_count - 1];
            }
            else
            {
                Debug.Assert(_continuationCount == 0);

                if (--_count > 0)
                {
                    Current = _stack[_count - 1];
                }
            }
        }

        public void AddCompletedAsyncDisposable(IAsyncDisposable asyncDisposable)
            => (CompletedAsyncDisposables ??= new List<IAsyncDisposable>()).Add(asyncDisposable);

        // Asynchronously dispose of any AsyncDisposables that have been scheduled for disposal
        public async ValueTask DisposeCompletedAsyncDisposables()
        {
            Debug.Assert(CompletedAsyncDisposables?.Count > 0);
            Exception? exception = null;

            foreach (IAsyncDisposable asyncDisposable in CompletedAsyncDisposables)
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

            CompletedAsyncDisposables.Clear();
        }

        /// <summary>
        /// Walks the stack cleaning up any leftover IDisposables
        /// in the event of an exception on serialization
        /// </summary>
        public void DisposePendingDisposablesOnException()
        {
            Exception? exception = null;

            Debug.Assert(Current.AsyncDisposable is null);
            DisposeFrame(Current.CollectionEnumerator, ref exception);

            int stackSize = Math.Max(_count, _continuationCount);
            for (int i = 0; i < stackSize - 1; i++)
            {
                Debug.Assert(_stack[i].AsyncDisposable is null);
                DisposeFrame(_stack[i].CollectionEnumerator, ref exception);
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

            exception = await DisposeFrame(Current.CollectionEnumerator, Current.AsyncDisposable, exception).ConfigureAwait(false);

            int stackSize = Math.Max(_count, _continuationCount);
            for (int i = 0; i < stackSize - 1; i++)
            {
                exception = await DisposeFrame(_stack[i].CollectionEnumerator, _stack[i].AsyncDisposable, exception).ConfigureAwait(false);
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

            (int frameCount, bool includeCurrentFrame) = _continuationCount switch
            {
                0 => (_count - 1, true), // Not a countinuation, report previous frames and Current.
                1 => (0, true), // Continuation of depth 1, just report Current frame.
                int c => (c, false) // Continuation of depth > 1, report the entire stack.
            };

            for (int i = 0; i < frameCount; i++)
            {
                AppendStackFrame(sb, ref _stack[i]);
            }

            if (includeCurrentFrame)
            {
                AppendStackFrame(sb, ref Current);
            }

            return sb.ToString();

            static void AppendStackFrame(StringBuilder sb, ref WriteStackFrame frame)
            {
                // Append the property name.
                string? propertyName = frame.JsonPropertyInfo?.ClrName;
                if (propertyName == null)
                {
                    // Attempt to get the JSON property name from the property name specified in re-entry.
                    propertyName = frame.JsonPropertyNameAsString;
                }

                AppendPropertyName(sb, propertyName);
            }

            static void AppendPropertyName(StringBuilder sb, string? propertyName)
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

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay => $"Path:{PropertyPath()} Current: ConverterStrategy.{Current.JsonPropertyInfo?.ConverterStrategy}, {Current.JsonTypeInfo?.Type.Name}";
    }
}
