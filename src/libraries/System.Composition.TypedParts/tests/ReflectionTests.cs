// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Composition.Hosting;
using System.Threading;
using Xunit;

namespace System.Composition.TypedParts.Tests
{
    /// <summary>
    /// Tests dealing with reflection usage in System.Composition
    /// </summary>
    public class ReflectionTests
    {
        public static bool HasMultiplerProcessors { get; } = Environment.ProcessorCount > 1;

        /// <summary>
        /// Regression test for https://github.com/dotnet/runtime/issues/16683
        /// System.Composition: Bug in Resolving Activators Due to the .NET Type System
        /// Non-deterministic
        /// </summary>
        /// <remarks>
        /// Because MethodInfo.GetParameters() lazy initializes it's return value,
        /// concurrent calls can create different ParameterInfo instances.
        /// This can cause arguments to an export's constructor to incorrectly
        /// revert to a default value during GetExport.
        /// </remarks>
        [ConditionalFact(nameof(HasMultiplerProcessors))]
        public void MultiThreadedGetExportsWorkWithImportingConstuctor()
        {
            var errors = new ConcurrentBag<Exception>();

            using (var evnt = new ManualResetEventSlim())
            {
                var threads = new Thread[Environment.ProcessorCount];
                for (var idx = 0; idx < threads.Length; idx++)
                {
                    threads[idx] = new Thread(() => Run(evnt, errors));
                }

                foreach (var thread in threads)
                {
                    thread.Start();
                }

                evnt.Set();

                foreach (var thread in threads)
                {
                    thread.Join();
                }
            }

            if (errors.Count > 0)
            {
                throw new AggregateException(errors);
            }
        }

        private static void Run(ManualResetEventSlim evnt, ConcurrentBag<Exception> errors)
        {
            try
            {
                evnt.Wait();
                GetExport();
            }
            catch (Exception ex)
            {
                errors.Add(ex);
            }
        }

        private static void GetExport()
        {
            CompositionContext cc = CreateContainer(typeof(A), typeof(B), typeof(C), typeof(D));
            D d = cc.GetExport<D>();
            Assert.IsAssignableFrom<A>(d.A);
            Assert.IsAssignableFrom<B>(d.B);
            Assert.IsAssignableFrom<A>(d.B.A);
            Assert.IsAssignableFrom<C>(d.C);
            Assert.IsAssignableFrom<A>(d.C.A);
            Assert.IsAssignableFrom<B>(d.C.B);
        }

        private static CompositionContext CreateContainer(params Type[] types)
        {
            return new ContainerConfiguration()
                .WithParts(types)
                .CreateContainer();
        }

        public interface IA { }

        [Export(typeof(IA))]
        public class A : IA { }

        public interface IB
        {
            IA A { get; }
        }

        [Export(typeof(IB))]
        public class B : IB
        {
            public IA A { get; }

            [ImportingConstructor]
            public B(IA ia)
            {
                A = ia;
            }
        }

        public interface IC
        {
            IA A { get; }
            IB B { get; }
        }

        [Export(typeof(IC))]
        public class C : IC
        {
            public IA A { get; }
            public IB B { get; }

            [ImportingConstructor]
            public C(IA ia, IB ib)
            {
                A = ia;
                B = ib;
            }
        }

        [Export]
        public class D
        {
            public IA A { get; }
            public IB B { get; }
            public IC C { get; }

            [ImportingConstructor]
            public D(IA ia, IB ib, IC ic)
            {
                A = ia;
                B = ib;
                C = ic;
            }
        }
    }
}
