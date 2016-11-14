// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Net
{
    //
    // Preserve the original request buffer & sizes for user IO requests.
    // This is returned as an IAsyncResult to the application.
    //
    internal sealed class BufferAsyncResult : LazyAsyncResult
    {
        /// <summary>Stored into LazyAsyncResult.Result to indicate a completed result.</summary>
        public static readonly object ResultSentinal = nameof(BufferAsyncResult) + "." + nameof(ResultSentinal);
        /// <summary>Stores the input count or the output result of the operation.</summary>
        private int _countOrResult;
#if DEBUG
        /// <summary>true if <see cref="_countOrResult"/> backs <see cref="Int32Result"/>, false if <see cref="Count"/>.</summary>
        private bool _countOrResultIsResult;
#endif

        public BufferAsyncResult(object asyncObject, byte[] buffer, int offset, int count, object asyncState, AsyncCallback asyncCallback)
            : base(asyncObject, asyncState, asyncCallback)
        {
            Buffer = buffer;
            Offset = offset;
            _countOrResult = count;
        }

        public byte[] Buffer { get; }
        public int Offset { get; }
        public int Count
        {
            get
            {
#if DEBUG
                Debug.Assert(!_countOrResultIsResult, "Trying to get count after it's already the result");
#endif
                return _countOrResult;
            }
        }

        public int Int32Result // Int32Result to differentiate from the base's "object Result"
        {
            get
            {
#if DEBUG
                Debug.Assert(_countOrResultIsResult, "Still represents the count, not the result");
                Debug.Assert(ReferenceEquals(Result, ResultSentinal), "Expected the base object Result to be the sentinel");
#endif
                return _countOrResult;
            }
            set
            {
#if DEBUG
                Debug.Assert(!_countOrResultIsResult, "Should only be set when result hasn't yet been set");
                _countOrResultIsResult = true;
#endif
                _countOrResult = value;
            }
        }
    }
}
