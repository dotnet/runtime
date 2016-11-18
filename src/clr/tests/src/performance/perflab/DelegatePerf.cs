// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Xunit.Performance;
using System;
using Xunit;

namespace PerfLabTests
{

    internal delegate long DelegateLong(Object obj, long x, long y);
    internal delegate void MultiDelegate(Object obj, long x, long y);

    internal delegate int SerializeDelegate();

    public class DelegatePerf
    {
        [Benchmark(InnerIterationCount = 200000)]
        public void DelegateInvoke()
        {
            DelegateLong dl = new DelegateLong(this.Invocable1);
            Object obj = new Object();

            long ret = dl(obj, 100, 100);

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        ret = dl(obj, 100, 100);
        }

        [Benchmark(InnerIterationCount = 1000)]
        public void MulticastDelegateCombineInvoke()
        {
            MultiDelegate md = null;
            Object obj = new Object();

            foreach (var iteration in Benchmark.Iterations)
            {
                MultiDelegate md1 = new MultiDelegate(this.Invocable2);
                MultiDelegate md2 = new MultiDelegate(this.Invocable2);
                MultiDelegate md3 = new MultiDelegate(this.Invocable2);
                MultiDelegate md4 = new MultiDelegate(this.Invocable2);
                MultiDelegate md5 = new MultiDelegate(this.Invocable2);
                MultiDelegate md6 = new MultiDelegate(this.Invocable2);
                MultiDelegate md7 = new MultiDelegate(this.Invocable2);
                MultiDelegate md8 = new MultiDelegate(this.Invocable2);
                MultiDelegate md9 = new MultiDelegate(this.Invocable2);
                MultiDelegate md10 = new MultiDelegate(this.Invocable2);

                using (iteration.StartMeasurement())
                {
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                    {
                        md = (MultiDelegate)Delegate.Combine(md1, md);
                        md = (MultiDelegate)Delegate.Combine(md2, md);
                        md = (MultiDelegate)Delegate.Combine(md3, md);
                        md = (MultiDelegate)Delegate.Combine(md4, md);
                        md = (MultiDelegate)Delegate.Combine(md5, md);
                        md = (MultiDelegate)Delegate.Combine(md6, md);
                        md = (MultiDelegate)Delegate.Combine(md7, md);
                        md = (MultiDelegate)Delegate.Combine(md8, md);
                        md = (MultiDelegate)Delegate.Combine(md9, md);
                        md = (MultiDelegate)Delegate.Combine(md10, md);
                    }
                }
            }

            md(obj, 100, 100);
        }

        [Benchmark(InnerIterationCount = 10000)]
        [InlineData(100)]
        [InlineData(1000)]
        public void MulticastDelegateInvoke(int length)
        {
            MultiDelegate md = null;
            Object obj = new Object();

            for (long i = 0; i < length; i++)
                md = (MultiDelegate)Delegate.Combine(new MultiDelegate(this.Invocable2), md);

            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        md(obj, 100, 100);
        }

        internal virtual long Invocable1(Object obj, long x, long y)
        {
            long i = x + y;
            return x;
        }

        internal virtual void Invocable2(Object obj, long x, long y)
        {
            long i = x + y;
        }
    }
}
