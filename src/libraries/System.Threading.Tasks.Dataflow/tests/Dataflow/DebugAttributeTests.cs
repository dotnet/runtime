// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Threading.Tasks.Dataflow.Tests
{
    public class DebugAttributeTests
    {
        [Fact]
        public void TestDebuggerDisplaysAndTypeProxies()
        {
            // Test both canceled and non-canceled
            foreach (var ct in new[] { new CancellationToken(false), new CancellationToken(true) })
            {
                // Some blocks have different code paths for whether they're greedy or not.
                // This helps with code-coverage.
                var dboBuffering = new DataflowBlockOptions();
                var dboNoBuffering = new DataflowBlockOptions() { BoundedCapacity = 1 };
                var dboExBuffering = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 2, CancellationToken = ct };
                var dboExSpsc = new ExecutionDataflowBlockOptions { SingleProducerConstrained = true };
                var dboExNoBuffering = new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 2, BoundedCapacity = 1, CancellationToken = ct };
                var dboGroupGreedy = new GroupingDataflowBlockOptions();
                var dboGroupNonGreedy = new GroupingDataflowBlockOptions { Greedy = false };

                // Item1 == test Debuggerdisplay, Item2 == test DebuggerTypeProxy, Item3 == object
                var objectsToTest = new Tuple<bool, bool, object>[]
                {
                    // Primary Blocks
                    Tuple.Create<bool,bool,object>(true, true, new ActionBlock<int>(i => {})),
                    Tuple.Create<bool,bool,object>(true, true, new ActionBlock<int>(i => {}, dboExBuffering)),
                    Tuple.Create<bool,bool,object>(true, true, new ActionBlock<int>(i => {}, dboExSpsc)),
                    Tuple.Create<bool,bool,object>(true, true, SendAsyncMessages(new ActionBlock<int>(i => {}, dboExNoBuffering), 2)),
                    Tuple.Create<bool,bool,object>(true, true, new TransformBlock<int,int>(i => i)),
                    Tuple.Create<bool,bool,object>(true, true, new TransformBlock<int,int>(i => i, dboExBuffering)),
                    Tuple.Create<bool,bool,object>(true, true, SendAsyncMessages(new TransformBlock<int,int>(i => i, dboExNoBuffering),2)),
                    Tuple.Create<bool,bool,object>(true, true, new TransformManyBlock<int,int>(i => new [] { i })),
                    Tuple.Create<bool,bool,object>(true, true, new TransformManyBlock<int,int>(i => new [] { i }, dboExBuffering)),
                    Tuple.Create<bool,bool,object>(true, true, SendAsyncMessages(new TransformManyBlock<int,int>(i => new [] { i }, dboExNoBuffering),2)),
                    Tuple.Create<bool,bool,object>(true, true, new BufferBlock<int>()),
                    Tuple.Create<bool,bool,object>(true, true, new BufferBlock<int>(new DataflowBlockOptions() { NameFormat = "none" })),
                    Tuple.Create<bool,bool,object>(true, true, new BufferBlock<int>(new DataflowBlockOptions() { NameFormat = "foo={0}, bar={1}" })),
                    Tuple.Create<bool,bool,object>(true, true, new BufferBlock<int>(new DataflowBlockOptions() { NameFormat = "foo={0}, bar={1}, kaboom={2}" })),
                    Tuple.Create<bool,bool,object>(true, true, new BufferBlock<int>(dboBuffering)),
                    Tuple.Create<bool,bool,object>(true, true, SendAsyncMessages(new BufferBlock<int>(new DataflowBlockOptions { BoundedCapacity = 10 }), 20)),
                    Tuple.Create<bool,bool,object>(true, true, new BroadcastBlock<int>(i => i)),
                    Tuple.Create<bool,bool,object>(true, true, new BroadcastBlock<int>(i => i, dboBuffering)),
                    Tuple.Create<bool,bool,object>(true, true, SendAsyncMessages(new BroadcastBlock<int>(i => i, dboNoBuffering), 20)),
                    Tuple.Create<bool,bool,object>(true, true, new WriteOnceBlock<int>(i => i)),
                    Tuple.Create<bool,bool,object>(true, true, SendAsyncMessages(new WriteOnceBlock<int>(i => i), 1)),
                    Tuple.Create<bool,bool,object>(true, true, new WriteOnceBlock<int>(i => i, dboBuffering)),
                    Tuple.Create<bool,bool,object>(true, true, new JoinBlock<int,int>()),
                    Tuple.Create<bool,bool,object>(true, true, new JoinBlock<int,int>(dboGroupGreedy)),
                    Tuple.Create<bool,bool,object>(true, true, new JoinBlock<int,int>(dboGroupNonGreedy)),
                    Tuple.Create<bool,bool,object>(true, true, new JoinBlock<int,int,int>()),
                    Tuple.Create<bool,bool,object>(true, true, new JoinBlock<int,int,int>(dboGroupGreedy)),
                    Tuple.Create<bool,bool,object>(true, true, new JoinBlock<int,int,int>(dboGroupNonGreedy)),
                    Tuple.Create<bool,bool,object>(true, true, new BatchedJoinBlock<int,int>(42)),
                    Tuple.Create<bool,bool,object>(true, true, new BatchedJoinBlock<int,int>(42, dboGroupGreedy)),
                    Tuple.Create<bool,bool,object>(true, true, new BatchedJoinBlock<int,int,int>(42, dboGroupGreedy)),
                    Tuple.Create<bool,bool,object>(true, true, new BatchBlock<int>(42)),
                    Tuple.Create<bool,bool,object>(true, true, new BatchBlock<int>(42, dboGroupGreedy)),
                    Tuple.Create<bool,bool,object>(true, true, new BatchBlock<int>(42, dboGroupNonGreedy)),
                    Tuple.Create<bool,bool,object>(true, true, DataflowBlock.Encapsulate<int,int>(new BufferBlock<int>(),new BufferBlock<int>())),
                    Tuple.Create<bool,bool,object>(true, true, new BufferBlock<int>().AsObservable()),

                    // Supporting and Internal Types
                    Tuple.Create<bool,bool,object>(true, false, GetFieldValue(new ActionBlock<int>(i => {}, dboExBuffering), "_defaultTarget")),
                    Tuple.Create<bool,bool,object>(true, false, GetFieldValue(new ActionBlock<int>(i => {}, dboExNoBuffering), "_defaultTarget")),
                    Tuple.Create<bool,bool,object>(true, false, GetFieldValue(GetFieldValue(new ActionBlock<int>(i => {}), "_defaultTarget"), "_messages")),
                    Tuple.Create<bool,bool,object>(true, false, GetFieldValue(new ActionBlock<int>(i => {}, dboExSpsc), "_spscTarget")),
                    Tuple.Create<bool,bool,object>(true, false, GetFieldValue(GetFieldValue(new ActionBlock<int>(i => {}, dboExSpsc), "_spscTarget"), "_messages")),
                    Tuple.Create<bool,bool,object>(true, false, GetFieldValue(new BufferBlock<int>(), "_source")),
                    Tuple.Create<bool,bool,object>(true, false, GetFieldValue(new BufferBlock<int>(new DataflowBlockOptions { BoundedCapacity = 10 }), "_source")),
                    Tuple.Create<bool,bool,object>(true, false, GetFieldValue(new TransformBlock<int,int>(i => i, dboExBuffering), "_source")),
                    Tuple.Create<bool,bool,object>(true, true, GetFieldValue(new TransformBlock<int,int>(i => i, dboExNoBuffering), "_reorderingBuffer")),
                    Tuple.Create<bool,bool,object>(true, true, GetFieldValue(GetFieldValue(new TransformBlock<int,int>(i => i, dboExBuffering), "_source"), "_targetRegistry")),
                    Tuple.Create<bool,bool,object>(true, true, GetFieldValue(GetFieldValue(WithLinkedTarget<TransformBlock<int,int>,int>(new TransformBlock<int,int>(i => i, dboExNoBuffering)), "_source"), "_targetRegistry")),
                    Tuple.Create<bool,bool,object>(true, true, new JoinBlock<int,int>().Target1),
                    Tuple.Create<bool,bool,object>(true, true, new JoinBlock<int,int>(dboGroupGreedy).Target1),
                    Tuple.Create<bool,bool,object>(true, true, new JoinBlock<int,int>(dboGroupNonGreedy).Target1),
                    Tuple.Create<bool,bool,object>(true, false, GetFieldValue(new JoinBlock<int,int>().Target1, "_sharedResources")),
                    Tuple.Create<bool,bool,object>(true, true, new BatchedJoinBlock<int,int>(42).Target1),
                    Tuple.Create<bool,bool,object>(true, true, new BatchedJoinBlock<int,int>(42, dboGroupGreedy).Target1),
                    Tuple.Create<bool,bool,object>(true, false, GetFieldValue(new BatchBlock<int>(42), "_target")),
                    Tuple.Create<bool,bool,object>(true, false, GetFieldValue(new BatchBlock<int>(42, dboGroupGreedy), "_target")),
                    Tuple.Create<bool,bool,object>(true, false, GetFieldValue(new BatchBlock<int>(42, dboGroupNonGreedy), "_target")),
                    Tuple.Create<bool,bool,object>(true, false, new BufferBlock<int>().LinkTo(new ActionBlock<int>(i => {}))), // ActionOnDispose
                    Tuple.Create<bool,bool,object>(true, false, GetFieldValue(new BroadcastBlock<int>(i => i), "_source")),
                    Tuple.Create<bool,bool,object>(true, false, GetFieldValue(new BroadcastBlock<int>(i => i, dboGroupGreedy), "_source")),
                    Tuple.Create<bool,bool,object>(true, false, GetFieldValue(new BroadcastBlock<int>(i => i, dboGroupNonGreedy), "_source")),
                    Tuple.Create<bool,bool,object>(true, true, CreateNopLinkSource<int>()),
                    Tuple.Create<bool,bool,object>(true, true, CreateFilteringSource<int>()),
                    Tuple.Create<bool,bool,object>(true, true, CreateSendSource<int>()),
                    Tuple.Create<bool,bool,object>(true, false, CreateReceiveTarget<int>()),
                    Tuple.Create<bool,bool,object>(true, false, CreateOutputAvailableTarget()),
                    Tuple.Create<bool,bool,object>(true, false, CreateChooseTarget<int>()),
                    Tuple.Create<bool,bool,object>(true, false, new BufferBlock<int>().AsObservable().Subscribe(DataflowBlock.NullTarget<int>().AsObserver())),

                    // Other
                    Tuple.Create<bool,bool,object>(true, false, new DataflowMessageHeader(1)),
                };

                // Test all DDAs and DTPAs
                Assert.All(from obj in objectsToTest where obj.Item1 select obj.Item3, obj => TestDebuggerDisplayReferences(obj));
                Assert.All(from obj in objectsToTest where obj.Item2 select obj.Item3, obj => TestDebuggerTypeProxyProperties(obj));
            }
        }

        private static object SendAsyncMessages<T>(ITargetBlock<T> target, int numMessages)
        {
            for (int i = 0; i < numMessages; i++) target.SendAsync(default(T));
            return target;
        }

        private static TBlock WithLinkedTarget<TBlock, T>(TBlock block) where TBlock : ISourceBlock<T>
        {
            block.LinkTo(DataflowBlock.NullTarget<T>());
            return block;
        }

        private static object GetFieldValue(object obj, string fieldName)
        {
            Type t = obj.GetType();
            FieldInfo fi = null;
            while (t != null)
            {
                fi = t.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

                if (fi != null) break;
                t = t.GetTypeInfo().BaseType;
            }
            return fi.GetValue(obj);
        }

        private static void TestDebuggerTypeProxyProperties(object obj)
        {
            var attrs = obj.GetType().GetTypeInfo().GetCustomAttributes(typeof(DebuggerTypeProxyAttribute), false).ToArray();
            Assert.Equal(expected: 1, actual: attrs.Length);

            DebuggerTypeProxyAttribute dtpa = (DebuggerTypeProxyAttribute)attrs[0];
            string attrText = dtpa.ProxyTypeName;
            var proxyType = Type.GetType(attrText);
            var genericArguments = obj.GetType().GetGenericArguments();
            if (genericArguments.Any())
            {
                proxyType = proxyType.MakeGenericType(genericArguments);
            }
            object proxyInstance = Activator.CreateInstance(proxyType, obj); // make sure we can instantiate it

            PropertyInfo[] propertyInfos = proxyInstance.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var pi in propertyInfos)
            {
                pi.GetValue(proxyInstance, null); // make sure we can access all its properties
            }
        }

        private static void TestDebuggerDisplayReferences(object obj)
        {
            var attrs = obj.GetType().GetTypeInfo().GetCustomAttributes(typeof(DebuggerDisplayAttribute), false).ToArray();
            Assert.Equal(expected: 1, actual: attrs.Length);

            DebuggerDisplayAttribute dda = (DebuggerDisplayAttribute)attrs[0];
            string attrText = dda.Value;

            var references = new List<string>();
            int pos = 0;
            while (true)
            {
                int openBrace = attrText.IndexOf('{', pos);
                if (openBrace < pos) break;
                int closeBrace = attrText.IndexOf('}', openBrace);
                if (closeBrace < openBrace) break;

                string reference = attrText.Substring(openBrace + 1, closeBrace - openBrace - 1).Replace(",nq", "");
                pos = closeBrace + 1;

                references.Add(reference);
            }
            Assert.NotEqual(0, actual: references.Count);

            foreach (var reference in references)
            {
                PropertyInfo pi = obj.GetType().GetProperty(reference, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                FieldInfo fi = obj.GetType().GetField(reference, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.False(pi == null & fi == null); // must be either a property or a field
                object result = pi != null ? pi.GetValue(obj, null) : fi.GetValue(obj); // make sure we can access the property or field
            }
        }

        private static ISourceBlock<T> CreateNopLinkSource<T>()
        {
            var bb = new BufferBlock<T>();
            var sos = new StoreOfferingSource<T>();
            using (bb.LinkTo(sos)) bb.LinkTo(sos);
            bb.Post(default(T));
            return sos.GetOfferingSource();
        }

        private static ISourceBlock<T> CreateFilteringSource<T>()
        {
            var bb = new BufferBlock<T>();
            var sos = new StoreOfferingSource<T>();
            bb.LinkTo(sos, i => true);
            bb.Post(default(T));
            return sos.GetOfferingSource();
        }

        private static ITargetBlock<T> CreateReceiveTarget<T>()
        {
            var slt = new StoreLinkedTarget<T>();
            slt.ReceiveAsync();
            return slt.GetLinkedTarget();
        }

        private static ITargetBlock<bool> CreateOutputAvailableTarget()
        {
            var slt = new StoreLinkedTarget<bool>();
            slt.OutputAvailableAsync();
            return slt.GetLinkedTarget();
        }

        private static ISourceBlock<T> CreateSendSource<T>()
        {
            var sos = new StoreOfferingSource<T>();
            sos.SendAsync(default(T));
            return sos.GetOfferingSource();
        }

        private static ITargetBlock<T> CreateChooseTarget<T>()
        {
            var slt = new StoreLinkedTarget<T>();
            DataflowBlock.Choose(slt, i => { }, new BufferBlock<T>(), i => { });
            return slt.GetLinkedTarget();
        }

        private class StoreOfferingSource<T> : ITargetBlock<T>
        {
            private TaskCompletionSource<ISourceBlock<T>> _m_offeringSource = new TaskCompletionSource<ISourceBlock<T>>();

            public ISourceBlock<T> GetOfferingSource() { return _m_offeringSource.Task.Result; }

            public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, T messageValue, ISourceBlock<T> source, bool consumeToAccept)
            {
                if (source != null)
                {
                    _m_offeringSource.TrySetResult(source);
                    return DataflowMessageStatus.Postponed;
                }
                return DataflowMessageStatus.Declined;
            }

            public bool Post(T item) { return false; }
            public Task Completion { get { return null; } }
            public void Complete() { }
            void IDataflowBlock.Fault(Exception exception) { throw new NotSupportedException(); }
        }

        private class StoreLinkedTarget<T> : IReceivableSourceBlock<T>
        {
            private TaskCompletionSource<ITargetBlock<T>> _m_linkedTarget = new TaskCompletionSource<ITargetBlock<T>>();
            public ITargetBlock<T> GetLinkedTarget() { return _m_linkedTarget.Task.Result; }

            public IDisposable LinkTo(ITargetBlock<T> target, DataflowLinkOptions linkOptions)
            {
                _m_linkedTarget.TrySetResult(target);
                return new NopDisposable();
            }

            private class NopDisposable : IDisposable { public void Dispose() { } }

            public bool TryReceive(Predicate<T> filter, out T item) { item = default(T); return false; }
            public bool TryReceiveAll(out System.Collections.Generic.IList<T> items) { items = default(System.Collections.Generic.IList<T>); return false; }
            public T ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<T> target, out Boolean messageConsumed) { messageConsumed = true; return default(T); }
            public bool ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<T> target) { return false; }
            public void ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<T> target) { }
            public Task Completion { get { return null; } }
            public void Complete() { }
            void IDataflowBlock.Fault(Exception exception) { throw new NotSupportedException(); }
        }
    }
}
