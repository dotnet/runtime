// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Composition.Hosting;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CompositionThroughput
{
    internal abstract class WebBenchmark : Benchmark
    {
        public override Action GetOperation()
        {
            var op = GetCompositionOperation();
            return () => op().Item2();
        }

        public override bool SelfTest()
        {
            var op = GetCompositionOperation();
            var r1 = op();

            if (!(r1.Item1 is Web.OperationRoot))
                return false;

            r1.Item2();
            var or1 = (Web.OperationRoot)r1.Item1;
            if (!(or1.Long.TailA.IsDisposed &&
                or1.Long.TailA.TailB.IsDisposed &&
                or1.Long.TailA.TailB.TailC.IsDisposed))
                return false;

            if (or1.Wide.A1 != or1.Wide.A2)
                return false;

            var r2 = op();
            if (r2.Item1 == r1.Item1)
                return false;

            var or2 = (Web.OperationRoot)r2.Item1;
            if (or2.Long.TailA.IsDisposed)
                return false;

            if (or1.Wide.A1 == or2.Wide.A1)
                return false;

            if (or1.Wide.A1.GA != or2.Wide.A1.GA)
                return false;

            r2.Item2();
            return true;
        }

        public abstract Func<Tuple<object, Action>> GetCompositionOperation();
    }

    internal class LightweightWebBenchmark : WebBenchmark
    {
        [Export]
        private class WebServer
        {
#pragma warning disable 3016
            [Import, SharingBoundary(Web.Boundaries.Web)]
#pragma warning restore 3016
            public ExportFactory<Web.OperationRoot> WebScopeFactory { get; set; }
        }

        public override Func<Tuple<object, Action>> GetCompositionOperation()
        {
            var container = new ContainerConfiguration()
                .WithParts(new[]{
                    typeof(WebServer),
                    typeof(Web.OperationRoot),
                    typeof(Web.GlobalA),
                    typeof(Web.GlobalB),
                    typeof(Web.Transient),
                    typeof(Web.Wide),
                    typeof(Web.A),
                    typeof(Web.B),
                    typeof(Web.Long),
                    typeof(Web.TailA),
                    typeof(Web.TailB),
                    typeof(Web.TailC)})
                .CreateContainer();

            var sf = container.GetExport<WebServer>().WebScopeFactory;
            return () =>
            {
                var x = sf.CreateExport();
                return Tuple.Create<object, Action>(x.Value, x.Dispose);
            };
        }
    }

    internal class NativeCodeWebBenchmark : WebBenchmark
    {
        public override Func<Tuple<object, Action>> GetCompositionOperation()
        {
            var globalA = new Web.GlobalA();
            var globalB = new Web.GlobalB();
            return () =>
            {
                var tc = new Web.TailC();
                var tb = new Web.TailB(tc);
                var ta = new Web.TailA(tb);
                var a = new Web.A(globalA);
                var b = new Web.B(globalB);
                var transient = new Web.Transient();
                var w = new Web.Wide(a, a, b, transient);
                var l = new Web.Long(ta);
                var r = new Web.OperationRoot(w, l);
                return Tuple.Create<object, Action>(r, () => { ta.Dispose(); tb.Dispose(); tc.Dispose(); });
            };
        }
    }

    internal static class CompositionScope
    {
        public const string Global = "Global";
    }
}
