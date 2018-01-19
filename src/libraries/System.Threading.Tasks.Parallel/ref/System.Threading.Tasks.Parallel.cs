// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Threading.Tasks
{
    public static partial class Parallel
    {
        public static System.Threading.Tasks.ParallelLoopResult For(int fromInclusive, int toExclusive, System.Action<int, System.Threading.Tasks.ParallelLoopState> body) { throw null; }
        public static System.Threading.Tasks.ParallelLoopResult For(int fromInclusive, int toExclusive, System.Action<int> body) { throw null; }
        public static System.Threading.Tasks.ParallelLoopResult For(int fromInclusive, int toExclusive, System.Threading.Tasks.ParallelOptions parallelOptions, System.Action<int, System.Threading.Tasks.ParallelLoopState> body) { throw null; }
        public static System.Threading.Tasks.ParallelLoopResult For(int fromInclusive, int toExclusive, System.Threading.Tasks.ParallelOptions parallelOptions, System.Action<int> body) { throw null; }
        public static System.Threading.Tasks.ParallelLoopResult For(long fromInclusive, long toExclusive, System.Action<long, System.Threading.Tasks.ParallelLoopState> body) { throw null; }
        public static System.Threading.Tasks.ParallelLoopResult For(long fromInclusive, long toExclusive, System.Action<long> body) { throw null; }
        public static System.Threading.Tasks.ParallelLoopResult For(long fromInclusive, long toExclusive, System.Threading.Tasks.ParallelOptions parallelOptions, System.Action<long, System.Threading.Tasks.ParallelLoopState> body) { throw null; }
        public static System.Threading.Tasks.ParallelLoopResult For(long fromInclusive, long toExclusive, System.Threading.Tasks.ParallelOptions parallelOptions, System.Action<long> body) { throw null; }
        public static System.Threading.Tasks.ParallelLoopResult ForEach<TSource>(System.Collections.Concurrent.OrderablePartitioner<TSource> source, System.Action<TSource, System.Threading.Tasks.ParallelLoopState, long> body) { throw null; }
        public static System.Threading.Tasks.ParallelLoopResult ForEach<TSource>(System.Collections.Concurrent.OrderablePartitioner<TSource> source, System.Threading.Tasks.ParallelOptions parallelOptions, System.Action<TSource, System.Threading.Tasks.ParallelLoopState, long> body) { throw null; }
        public static System.Threading.Tasks.ParallelLoopResult ForEach<TSource>(System.Collections.Concurrent.Partitioner<TSource> source, System.Action<TSource, System.Threading.Tasks.ParallelLoopState> body) { throw null; }
        public static System.Threading.Tasks.ParallelLoopResult ForEach<TSource>(System.Collections.Concurrent.Partitioner<TSource> source, System.Action<TSource> body) { throw null; }
        public static System.Threading.Tasks.ParallelLoopResult ForEach<TSource>(System.Collections.Concurrent.Partitioner<TSource> source, System.Threading.Tasks.ParallelOptions parallelOptions, System.Action<TSource, System.Threading.Tasks.ParallelLoopState> body) { throw null; }
        public static System.Threading.Tasks.ParallelLoopResult ForEach<TSource>(System.Collections.Concurrent.Partitioner<TSource> source, System.Threading.Tasks.ParallelOptions parallelOptions, System.Action<TSource> body) { throw null; }
        public static System.Threading.Tasks.ParallelLoopResult ForEach<TSource>(System.Collections.Generic.IEnumerable<TSource> source, System.Action<TSource, System.Threading.Tasks.ParallelLoopState, long> body) { throw null; }
        public static System.Threading.Tasks.ParallelLoopResult ForEach<TSource>(System.Collections.Generic.IEnumerable<TSource> source, System.Action<TSource, System.Threading.Tasks.ParallelLoopState> body) { throw null; }
        public static System.Threading.Tasks.ParallelLoopResult ForEach<TSource>(System.Collections.Generic.IEnumerable<TSource> source, System.Action<TSource> body) { throw null; }
        public static System.Threading.Tasks.ParallelLoopResult ForEach<TSource>(System.Collections.Generic.IEnumerable<TSource> source, System.Threading.Tasks.ParallelOptions parallelOptions, System.Action<TSource, System.Threading.Tasks.ParallelLoopState, long> body) { throw null; }
        public static System.Threading.Tasks.ParallelLoopResult ForEach<TSource>(System.Collections.Generic.IEnumerable<TSource> source, System.Threading.Tasks.ParallelOptions parallelOptions, System.Action<TSource, System.Threading.Tasks.ParallelLoopState> body) { throw null; }
        public static System.Threading.Tasks.ParallelLoopResult ForEach<TSource>(System.Collections.Generic.IEnumerable<TSource> source, System.Threading.Tasks.ParallelOptions parallelOptions, System.Action<TSource> body) { throw null; }
        public static System.Threading.Tasks.ParallelLoopResult ForEach<TSource, TLocal>(System.Collections.Concurrent.OrderablePartitioner<TSource> source, System.Func<TLocal> localInit, System.Func<TSource, System.Threading.Tasks.ParallelLoopState, long, TLocal, TLocal> body, System.Action<TLocal> localFinally) { throw null; }
        public static System.Threading.Tasks.ParallelLoopResult ForEach<TSource, TLocal>(System.Collections.Concurrent.OrderablePartitioner<TSource> source, System.Threading.Tasks.ParallelOptions parallelOptions, System.Func<TLocal> localInit, System.Func<TSource, System.Threading.Tasks.ParallelLoopState, long, TLocal, TLocal> body, System.Action<TLocal> localFinally) { throw null; }
        public static System.Threading.Tasks.ParallelLoopResult ForEach<TSource, TLocal>(System.Collections.Concurrent.Partitioner<TSource> source, System.Func<TLocal> localInit, System.Func<TSource, System.Threading.Tasks.ParallelLoopState, TLocal, TLocal> body, System.Action<TLocal> localFinally) { throw null; }
        public static System.Threading.Tasks.ParallelLoopResult ForEach<TSource, TLocal>(System.Collections.Concurrent.Partitioner<TSource> source, System.Threading.Tasks.ParallelOptions parallelOptions, System.Func<TLocal> localInit, System.Func<TSource, System.Threading.Tasks.ParallelLoopState, TLocal, TLocal> body, System.Action<TLocal> localFinally) { throw null; }
        public static System.Threading.Tasks.ParallelLoopResult ForEach<TSource, TLocal>(System.Collections.Generic.IEnumerable<TSource> source, System.Func<TLocal> localInit, System.Func<TSource, System.Threading.Tasks.ParallelLoopState, long, TLocal, TLocal> body, System.Action<TLocal> localFinally) { throw null; }
        public static System.Threading.Tasks.ParallelLoopResult ForEach<TSource, TLocal>(System.Collections.Generic.IEnumerable<TSource> source, System.Func<TLocal> localInit, System.Func<TSource, System.Threading.Tasks.ParallelLoopState, TLocal, TLocal> body, System.Action<TLocal> localFinally) { throw null; }
        public static System.Threading.Tasks.ParallelLoopResult ForEach<TSource, TLocal>(System.Collections.Generic.IEnumerable<TSource> source, System.Threading.Tasks.ParallelOptions parallelOptions, System.Func<TLocal> localInit, System.Func<TSource, System.Threading.Tasks.ParallelLoopState, long, TLocal, TLocal> body, System.Action<TLocal> localFinally) { throw null; }
        public static System.Threading.Tasks.ParallelLoopResult ForEach<TSource, TLocal>(System.Collections.Generic.IEnumerable<TSource> source, System.Threading.Tasks.ParallelOptions parallelOptions, System.Func<TLocal> localInit, System.Func<TSource, System.Threading.Tasks.ParallelLoopState, TLocal, TLocal> body, System.Action<TLocal> localFinally) { throw null; }
        public static System.Threading.Tasks.ParallelLoopResult For<TLocal>(int fromInclusive, int toExclusive, System.Func<TLocal> localInit, System.Func<int, System.Threading.Tasks.ParallelLoopState, TLocal, TLocal> body, System.Action<TLocal> localFinally) { throw null; }
        public static System.Threading.Tasks.ParallelLoopResult For<TLocal>(int fromInclusive, int toExclusive, System.Threading.Tasks.ParallelOptions parallelOptions, System.Func<TLocal> localInit, System.Func<int, System.Threading.Tasks.ParallelLoopState, TLocal, TLocal> body, System.Action<TLocal> localFinally) { throw null; }
        public static System.Threading.Tasks.ParallelLoopResult For<TLocal>(long fromInclusive, long toExclusive, System.Func<TLocal> localInit, System.Func<long, System.Threading.Tasks.ParallelLoopState, TLocal, TLocal> body, System.Action<TLocal> localFinally) { throw null; }
        public static System.Threading.Tasks.ParallelLoopResult For<TLocal>(long fromInclusive, long toExclusive, System.Threading.Tasks.ParallelOptions parallelOptions, System.Func<TLocal> localInit, System.Func<long, System.Threading.Tasks.ParallelLoopState, TLocal, TLocal> body, System.Action<TLocal> localFinally) { throw null; }
        public static void Invoke(params System.Action[] actions) { }
        public static void Invoke(System.Threading.Tasks.ParallelOptions parallelOptions, params System.Action[] actions) { }
    }
    public partial struct ParallelLoopResult
    {
        private int _dummy;
        public bool IsCompleted { get { throw null; } }
        public System.Nullable<long> LowestBreakIteration { get { throw null; } }
    }
    public partial class ParallelLoopState
    {
        internal ParallelLoopState() { }
        public bool IsExceptional { get { throw null; } }
        public bool IsStopped { get { throw null; } }
        public System.Nullable<long> LowestBreakIteration { get { throw null; } }
        public bool ShouldExitCurrentIteration { get { throw null; } }
        public void Break() { }
        public void Stop() { }
    }
    public partial class ParallelOptions
    {
        public ParallelOptions() { }
        public System.Threading.CancellationToken CancellationToken { get { throw null; } set { } }
        public int MaxDegreeOfParallelism { get { throw null; } set { } }
        public System.Threading.Tasks.TaskScheduler TaskScheduler { get { throw null; } set { } }
    }
}
