// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Threading.Tasks.Dataflow
{
    public static partial class DataflowBlock
    {
        public static System.Collections.Generic.IAsyncEnumerable<TOutput> ReceiveAllAsync<TOutput>(this System.Threading.Tasks.Dataflow.IReceivableSourceBlock<TOutput> source, System.Threading.CancellationToken cancellationToken = default) { throw null; }
    }
	
	public sealed partial class TransformManyBlock<TInput, TOutput> : System.Threading.Tasks.Dataflow.IDataflowBlock, System.Threading.Tasks.Dataflow.IPropagatorBlock<TInput, TOutput>, System.Threading.Tasks.Dataflow.IReceivableSourceBlock<TOutput>, System.Threading.Tasks.Dataflow.ISourceBlock<TOutput>, System.Threading.Tasks.Dataflow.ITargetBlock<TInput>
    {
        public TransformManyBlock(System.Func<TInput, System.Collections.Generic.IAsyncEnumerable<TOutput>> transform) { }
        public TransformManyBlock(System.Func<TInput, System.Collections.Generic.IAsyncEnumerable<TOutput>> transform, System.Threading.Tasks.Dataflow.ExecutionDataflowBlockOptions dataflowBlockOptions) { }
    }
}
