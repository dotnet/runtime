// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Threading.Tasks.Dataflow.Tests
{
    internal sealed class DelegateDisposable : IDisposable
    {
        public Action DisposeDelegate = null;

        void IDisposable.Dispose()
        {
            if (DisposeDelegate != null)
                DisposeDelegate();
        }
    }

    internal class DelegatePropagator<TInput, TOutput> : IPropagatorBlock<TInput, TOutput>
    {
        public delegate TOutput ConsumeMessageFunc(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target, out bool messageConsumed);

        public Func<DataflowMessageHeader, TInput, ISourceBlock<TInput>, bool, DataflowMessageStatus> OfferMessageDelegate = null;
        public Func<Task> CompletionDelegate = null;
        public Action CompleteDelegate = null;
        public Action<Exception> FaultDelegate = null;
        public Func<ITargetBlock<TOutput>, DataflowLinkOptions, IDisposable> LinkToDelegate = null;
        public ConsumeMessageFunc ConsumeMessageDelegate = null;
        public Func<DataflowMessageHeader, ITargetBlock<TOutput>, bool> ReserveMessageDelegate = null;
        public Action<DataflowMessageHeader, ITargetBlock<TOutput>> ReleaseMessageDelegate = null;

        public DataflowMessageStatus OfferMessage(
            DataflowMessageHeader messageHeader, TInput messageValue, ISourceBlock<TInput> source, bool consumeToAccept)
        {
            return OfferMessageDelegate != null ?
                OfferMessageDelegate(messageHeader, messageValue, source, consumeToAccept) :
                DataflowMessageStatus.Accepted;
        }

        public Task Completion 
        {
            get 
            { 
                return CompletionDelegate != null ? 
                    CompletionDelegate() : 
                    null; 
            } 
        }

        public void Complete() 
        {
            if (CompleteDelegate != null) 
                CompleteDelegate(); 
        }

        public void Fault(Exception exception) 
        {
            if (FaultDelegate != null)
                FaultDelegate(exception); 
        }

        public IDisposable LinkTo(ITargetBlock<TOutput> target, DataflowLinkOptions linkOptions)
        {
            return LinkToDelegate != null ? 
                LinkToDelegate(target, linkOptions) : 
                new DelegateDisposable();
        }

        public TOutput ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target, out bool messageConsumed)
        {
            messageConsumed = false;
            return ConsumeMessageDelegate != null ?
                ConsumeMessageDelegate(messageHeader, target, out messageConsumed) :
                default(TOutput);
        }

        public bool ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target)
        {
            return ReserveMessageDelegate != null ?
                ReserveMessageDelegate(messageHeader, target) :
                true;
        }

        public void ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<TOutput> target)
        {
            if (ReleaseMessageDelegate != null)
                ReleaseMessageDelegate(messageHeader, target);
        }
    }

    internal sealed class DelegateReceivablePropagator<TInput, TOutput> : DelegatePropagator<TInput, TOutput>, IReceivableSourceBlock<TOutput>
    {
        public delegate bool TryReceiveFunc(Predicate<TOutput> filter, out TOutput item);
        public delegate bool TryReceiveAllFunc(out IList<TOutput> items);

        public TryReceiveFunc TryReceiveDelegate = null;
        public TryReceiveAllFunc TryReceiveAllDelegate = null;

        public bool TryReceive(Predicate<TOutput> filter, out TOutput item)
        {
            if (TryReceiveDelegate != null)
            {
                return TryReceiveDelegate(filter, out item);
            }
            item = default(TOutput);
            return false;
        }

        public bool TryReceiveAll(out IList<TOutput> items)
        {
            if (TryReceiveAllDelegate != null)
            {
                return TryReceiveAllDelegate(out items);
            }
            items = default(IList<TOutput>);
            return false;
        }
    }

    internal sealed class DelegateObserver<T> : IObserver<T>
    {
        public Action<T> OnNextDelegate = null;
        public Action<Exception> OnErrorDelegate = null;
        public Action OnCompletedDelegate = null;

        void IObserver<T>.OnNext(T value) 
        { 
            if (OnNextDelegate != null) 
                OnNextDelegate(value); 
        }
        
        void IObserver<T>.OnError(Exception error) 
        {
            if (OnErrorDelegate != null)
                OnErrorDelegate(error); 
        }

        void IObserver<T>.OnCompleted() 
        {
            if (OnCompletedDelegate != null) 
                OnCompletedDelegate(); 
        }
    }

    internal sealed class DelegateObservable<T> : IObservable<T>
    {
        public Func<IObserver<T>, IDisposable> SubscribeDelegate = null;

        IDisposable IObservable<T>.Subscribe(IObserver<T> observer)
        {
            return SubscribeDelegate != null ? 
                SubscribeDelegate(observer) : 
                null;
        }
    }

    internal sealed class DelegateTaskScheduler : TaskScheduler
    {
        public Action<Task> QueueTaskDelegate = null; 
        public Func<Task, bool, bool> TryExecuteTaskInlineDelegate = null;

        protected override void QueueTask(Task task)
        {
            if (QueueTaskDelegate != null)
                QueueTaskDelegate(task);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return TryExecuteTaskInlineDelegate != null ?
                TryExecuteTaskInlineDelegate(task, taskWasPreviouslyQueued) :
                false;
        }

        protected override Collections.Generic.IEnumerable<Task> GetScheduledTasks() { return null; }
    }
}
