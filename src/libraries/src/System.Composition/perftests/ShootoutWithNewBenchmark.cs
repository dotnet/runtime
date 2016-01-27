// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Composition.Hosting;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompositionThroughput
{
    [Export]
    public class X { }

    [Export]
    public class XFactory
    {
        private readonly ExportFactory<X> _xfactory;

        [ImportingConstructor]
        public XFactory(ExportFactory<X> xfactory)
        {
            _xfactory = xfactory;
        }

        public Export<X> CreateX()
        {
            return _xfactory.CreateExport();
        }
    }

    internal abstract class ShootoutWithNewBenchmark : Benchmark
    {
        public override bool SelfTest()
        {
            return true;
        }
    }

    internal class OperatorNewBenchmark : ShootoutWithNewBenchmark
    {
        public override Action GetOperation()
        {
            return () => new X();
        }
    }

    internal class LightweightNewBenchmark : ShootoutWithNewBenchmark
    {
        public override Action GetOperation()
        {
            var c = new ContainerConfiguration()
                .WithPart(typeof(X))
                .WithPart(typeof(XFactory))
                .CreateContainer();

            var xf = c.GetExport<XFactory>();
            return () =>
            {
                var x = xf.CreateX();
                var unused = x.Value;
                x.Dispose();
            };
        }
    }

    internal class LightweightNLNewBenchmark : ShootoutWithNewBenchmark
    {
        public override Action GetOperation()
        {
            var c = new ContainerConfiguration()
                .WithPart(typeof(X))
                .CreateContainer();

            return () =>
            {
                c.GetExport<X>();
            };
        }
    }
}
