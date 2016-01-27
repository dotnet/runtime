// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Composition.Hosting;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CompositionThroughput.HugeGraph;

namespace CompositionThroughput
{
    internal abstract class HugeGraphBenchmark : Benchmark
    {
        public override bool SelfTest()
        {
            return true;
        }

        protected IEnumerable<Type> GetHugeGraphTypes(Type example)
        {
            return example.GetTypeInfo().Assembly.DefinedTypes.Where(t => t.Namespace == example.Namespace).Select(ti => ti.AsType());
        }
    }

    internal abstract class LightweightHugeGraphBenchmark : HugeGraphBenchmark
    {
        private ExportFactory<T> ConfigureContainer<T>()
        {
            return new ContainerConfiguration()
                .WithParts(GetHugeGraphTypes(typeof(T)))
                .CreateContainer()
                .GetExport<ExportFactory<T>>();
        }

        protected Action GetOperationFor<T>()
        {
            var c = ConfigureContainer<T>();
            return () =>
            {
                var scope = c.CreateExport();
                var x = scope.Value;
                scope.Dispose();
            };
        }

        public override Version Version
        {
            get
            {
                return typeof(CompositionContext).GetTypeInfo().Assembly.GetName().Version;
            }
        }
    }

    internal class LightweightHugeGraphABenchmark : LightweightHugeGraphBenchmark
    {
        public override Action GetOperation()
        {
            return GetOperationFor<TestClassA1>();
        }
    }

    internal class LightweightLongGraphBBenchmark : LightweightHugeGraphBenchmark
    {
        public override Action GetOperation()
        {
            return GetOperationFor<TestClassB1>();
        }
    }

    internal class LightweightHugeGraphCBenchmark : LightweightHugeGraphBenchmark
    {
        public override Action GetOperation()
        {
            return GetOperationFor<TestClassC1>();
        }
    }

    internal class LightweightHugeGraph4Benchmark : LightweightHugeGraphBenchmark
    {
        public override Action GetOperation()
        {
            return GetOperationFor<HugeGraph4.TestClassA1>();
        }
    }
}
