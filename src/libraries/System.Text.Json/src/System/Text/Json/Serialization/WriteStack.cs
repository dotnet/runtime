// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace System.Text.Json
{
    [StructLayout(LayoutKind.Auto)]
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal struct WriteStack
    {
        public readonly int CurrentDepth => _count;

        /// <summary>
        /// Exposes the stackframe that is currently active.
        /// </summary>
        public WriteStackFrame Current;

        /// <summary>
        /// Gets the parent stackframe, if it exists.
        /// </summary>
        public ref WriteStackFrame Parent
        {
            get
            {
                Debug.Assert(_count - _indexOffset > 0);
                Debug.Assert(_stack is not null);
                return ref _stack[_count - _indexOffset - 1];
            }
        }

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
        /// Offset used to derive the index of the current frame in the stack buffer from the current value of <see cref="_count"/>,
        /// following the formula currentIndex := _count - _indexOffset.
        /// Value can vary between 0 or 1 depending on whether we need to allocate a new frame on the first Push() operation,
        /// which can happen if the root converter is polymorphic.
        /// </summary>
        private byte _indexOffset;

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
        public readonly bool IsContinuation => _continuationCount != 0;

        // The bag of preservable references.
        public ReferenceResolver ReferenceResolver;

        /// <summary>
        /// Internal flag to let us know that we need to read ahead in the inner read loop.
        /// </summary>
        public bool SupportContinuation;

        /// <summary>
        /// Internal flag indicating that async serialization is supported. Implies `SupportContinuation`.
        /// </summary>
        public bool SupportAsync;

        /// <summary>
        /// Stores a reference id that has been calculated for a newly serialized object.
        /// </summary>
        public string? NewReferenceId;

        /// <summary>
        /// Indicates that the next converter is polymorphic and must serialize a type discriminator.
        /// </summary>
        public object? PolymorphicTypeDiscriminator;

        /// <summary>
        /// The polymorphic type resolver used by the next converter.
        /// </summary>
        public PolymorphicTypeResolver? PolymorphicTypeResolver;

        /// <summary>
        /// Whether the current frame needs to write out any metadata.
        /// </summary>
        public readonly bool CurrentContainsMetadata => NewReferenceId != null || PolymorphicTypeDiscriminator != null;

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

        internal void Initialize(
            JsonTypeInfo jsonTypeInfo,
            object? rootValueBoxed = null,
            bool supportContinuation = false,
            bool supportAsync = false)
        {
            Debug.Assert(!supportAsync || supportContinuation, "supportAsync must imply supportContinuation");
            Debug.Assert(!IsContinuation);
            Debug.Assert(CurrentDepth == 0);

            Current.JsonTypeInfo = jsonTypeInfo;
            Current.JsonPropertyInfo = jsonTypeInfo.PropertyInfoForTypeInfo;
            Current.NumberHandling = Current.JsonPropertyInfo.EffectiveNumberHandling;
            SupportContinuation = supportContinuation;
            SupportAsync = supportAsync;

            JsonSerializerOptions options = jsonTypeInfo.Options;
            if (options.ReferenceHandlingStrategy != ReferenceHandlingStrategy.None)
            {
                Debug.Assert(options.ReferenceHandler != null);
                ReferenceResolver = options.ReferenceHandler.CreateResolver(writing: true);

                if (options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.IgnoreCycles &&
                    rootValueBoxed is not null && jsonTypeInfo.Type.IsValueType)
                {
                    // Root object is a boxed value type, we need to push it to the reference stack before starting the serializer.
                    ReferenceResolver.PushReferenceForCycleDetection(rootValueBoxed);
                }
            }
        }

        /// <summary>
        /// Gets the nested JsonTypeInfo before resolving any polymorphic converters
        /// </summary>
        public readonly JsonTypeInfo PeekNestedJsonTypeInfo()
        {
            Debug.Assert(Current.PolymorphicSerializationState != PolymorphicSerializationState.PolymorphicReEntryStarted);
            return _count == 0 ? Current.JsonTypeInfo : Current.JsonPropertyInfo!.JsonTypeInfo;
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
                    JsonTypeInfo jsonTypeInfo = Current.GetNestedJsonTypeInfo();
                    JsonNumberHandling? numberHandling = Current.NumberHandling;

                    EnsurePushCapacity();
                    _stack[_count - _indexOffset] = Current;
                    Current = default;
                    _count++;

                    Current.JsonTypeInfo = jsonTypeInfo;
                    Current.JsonPropertyInfo = jsonTypeInfo.PropertyInfoForTypeInfo;
                    // Allow number handling on property to win over handling on type.
                    Current.NumberHandling = numberHandling ?? Current.JsonPropertyInfo.EffectiveNumberHandling;
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
                    // reached the root, no need to copy frames.
                    return;
                }

                int currentIndex = _count - _indexOffset;
                _stack[currentIndex + 1] = Current;
                Current = _stack[currentIndex];
            }
            else
            {
                Debug.Assert(_continuationCount == 0);

                if (--_count > 0 || _indexOffset == 0)
                {
                    Current = _stack[_count - _indexOffset];
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
                0 => (_count - 1, true), // Not a continuation, report previous frames and Current.
                1 => (0, true), // Continuation of depth 1, just report Current frame.
                int c => (c, false) // Continuation of depth > 1, report the entire stack.
            };

            for (int i = 1; i <= frameCount; i++)
            {
                AppendStackFrame(sb, ref _stack[i - _indexOffset]);
            }

            if (includeCurrentFrame)
            {
                AppendStackFrame(sb, ref Current);
            }

            return sb.ToString();

            static void AppendStackFrame(StringBuilder sb, ref WriteStackFrame frame)
            {
                // Append the property name. Or attempt to get the JSON property name from the property name specified in re-entry.
                string? propertyName =
                    frame.JsonPropertyInfo?.MemberName ??
                    frame.JsonPropertyNameAsString;

                AppendPropertyName(sb, propertyName);
            }

            static void AppendPropertyName(StringBuilder sb, string? propertyName)
            {
                if (propertyName != null)
                {
                    if (propertyName.AsSpan().ContainsSpecialCharacters())
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
        private string DebuggerDisplay => $"Path:{PropertyPath()} Current: ConverterStrategy.{Current.JsonPropertyInfo?.EffectiveConverter.ConverterStrategy}, {Current.JsonTypeInfo?.Type.Name}";
    }
}
