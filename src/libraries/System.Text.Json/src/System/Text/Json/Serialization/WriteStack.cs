// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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
        /// Exposes the stackframe that is currently active.
        /// </summary>
        public WriteStackFrame Current;

        /// <summary>
        /// Stack buffer containing all frames in the stack. For performance it is only populated for serialization depths > 1.
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
        /// Offset used to derive the index of the current frame in the stack buffer from the current value of <see cref="_count"/>,
        /// following the formula currentIndex := _count - _indexOffset.
        /// Value can vary between 0 or 1 depending on whether we need to allocate a new frame on the first Push() operation,
        /// which can happen if the root converter is polymorphic.
        /// </summary>
        private int _indexOffset;

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

        /// <summary>
        /// The amount of bytes to write before the underlying Stream should be flushed and the
        /// current buffer adjusted to remove the processed bytes.
        /// </summary>
        public int FlushThreshold;

        public bool IsContinuation => _continuationCount != 0;

        /// <summary>
        /// Indicates that the next converter is polymorphic and must serialize a type discriminator.
        /// </summary>
        public string? PolymorphicTypeDiscriminator;

        // The bag of preservable references.
        public ReferenceResolver ReferenceResolver;

        /// <summary>
        /// Internal flag to let us know that we need to read ahead in the inner read loop.
        /// </summary>
        public bool SupportContinuation;

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

        /// <summary>
        /// Used to obtain the JsonTypeInfo for the current converter _before_ the call to WriteStack.Push() has been made.
        /// </summary>
        public JsonTypeInfo PeekNextJsonTypeInfo()
        {
            if (_count == 0 && Current.PolymorphicSerializationState != PolymorphicSerializationState.PolymorphicReEntryStarted)
            {
                return Current.JsonTypeInfo;
            }

            return Current.GetPolymorphicJsonPropertyInfo().RuntimeTypeInfo;
        }

        public void Push()
        {
            if (_continuationCount == 0)
            {
                Debug.Assert(Current.PolymorphicSerializationState != PolymorphicSerializationState.PolymorphicReEntrySuspended);

                if (_count == 0 && Current.PolymorphicSerializationState == PolymorphicSerializationState.None)
                {
                    // Perf enhancement: do not create a new stackframe on the first push operation
                    // unless the converter has primed the current frame for polymorphic dispatch.
                    _count = 1;
                    _indexOffset = 1; // currentIndex := _count - 1;
                }
                else
                {
                    JsonPropertyInfo jsonPropertyInfo = Current.GetPolymorphicJsonPropertyInfo();
                    JsonTypeInfo jsonTypeInfo = jsonPropertyInfo.RuntimeTypeInfo;
                    JsonNumberHandling? numberHandling = Current.NumberHandling;

                    EnsurePushCapacity();
                    _stack[_count - _indexOffset] = Current;
                    Current = default;
                    _count++;

                    Current.JsonTypeInfo = jsonTypeInfo;
                    Current.DeclaredJsonPropertyInfo = jsonTypeInfo.PropertyInfoForTypeInfo;
                    // Allow number handling on property to win over handling on type.
                    Current.NumberHandling = numberHandling ?? Current.DeclaredJsonPropertyInfo.NumberHandling;
                }
            }
            else
            {
                // We are re-entering a continuation, adjust indices accordingly

                if (_count++ > 0 || _indexOffset == 0)
                {
                    Current = _stack[_count - _indexOffset];
                }

                // check if we are done
                if (_continuationCount == _count)
                {
                    _continuationCount = 0;
                }
            }

            Debug.Assert(PropertyPath() is not null);
        }

        public void Pop(bool success)
        {
            Debug.Assert(_count > 0);
            Debug.Assert(PropertyPath() is not null);

            if (!success)
            {
                // Check if we need to initialize the continuation.
                if (_continuationCount == 0)
                {
                    if (_count == 1 && _indexOffset > 0)
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
                else if (--_count == 0 && _indexOffset > 0)
                {
                    // No need to copy any frames here.
                    return;
                }

                int currentIndex = _count - _indexOffset;
                _stack[currentIndex + 1] = Current;
                Current = _stack[currentIndex];
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

                if (--_count > 0 || _indexOffset == 0)
                {
                    Current = _stack[_count - _indexOffset];
                }
            }
        }

        private void EnsurePushCapacity()
        {
            if (_stack is null)
            {
                _stack = new WriteStackFrame[4];
            }
            else if (_count - _indexOffset == _stack.Length)
            {
                Array.Resize(ref _stack, 2 * _stack.Length);
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

            for (int i = 1; i < stackSize; i++)
            {
                ref WriteStackFrame frame = ref _stack[i - _indexOffset];
                Debug.Assert(frame.AsyncEnumerator is null);
                DisposeFrame(frame.CollectionEnumerator, ref exception);
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
            for (int i = 1; i < stackSize; i++)
            {
                WriteStackFrame frame = _stack[i - _indexOffset];
                exception = await DisposeFrame(frame.CollectionEnumerator, frame.AsyncEnumerator, exception).ConfigureAwait(false);
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

            for (int i = 1; i < count; i++)
            {
                AppendStackFrame(sb, ref _stack[i - _indexOffset]);
            }

            if (_continuationCount == 0)
            {
                AppendStackFrame(sb, ref Current);
            }

            return sb.ToString();

            static void AppendStackFrame(StringBuilder sb, ref WriteStackFrame frame)
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
    }
}
