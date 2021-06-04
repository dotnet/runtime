// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Collections.Generic;
using System.Composition;
using System.Composition.Hosting;
using System.Composition.Hosting.Providers;
using System.Linq;
using Xunit;

namespace System.Composition.UnitTests
{
    public interface ISharedTestingClass
    {
        void Method();
    }

    [Export(typeof(ISharedTestingClass))]
    public class NonSharedClass : ISharedTestingClass
    {
        [ImportingConstructor]
        public NonSharedClass()
        {
        }

        public void Method() { }
    }


    [Export(typeof(ISharedTestingClass))]
    [Shared]
    public class SharedClass : ISharedTestingClass
    {
        [ImportingConstructor]
        public SharedClass()
        {
        }

        public void Method()
        {
        }
    }

    [Export]
    [Shared]
    public class ClassWithExportFactoryShared
    {
        private readonly ExportFactory<ISharedTestingClass> _fact;
        [ImportingConstructor]
        public ClassWithExportFactoryShared(ExportFactory<ISharedTestingClass> factory)
        {
            _fact = factory;
        }

        public ISharedTestingClass Method()
        {
            using (var b = _fact.CreateExport())
            {
                return b.Value;
            }
        }
    }

    [Export]
    public class ClassWithExportFactoryNonShared
    {
        private readonly ExportFactory<ISharedTestingClass> _fact;
        [ImportingConstructor]
        public ClassWithExportFactoryNonShared(ExportFactory<ISharedTestingClass> factory)
        {
            _fact = factory;
        }

        public ISharedTestingClass Method()
        {
            using (var b = _fact.CreateExport())
            {
                return b.Value;
            }
        }
    }


    [Export]
    public class ClassWithExportFactoryAsAProperty
    {
        [Import]
        public ExportFactory<ISharedTestingClass> _fact { get; set; }
        public ClassWithExportFactoryAsAProperty()
        {
        }

        public ISharedTestingClass Method()
        {
            using (var b = _fact.CreateExport())
            {
                return b.Value;
            }
        }
    }

    internal interface IX { }
    internal interface IY { }

    [Export]

    [Shared("Boundary")]
    public class A
    {
        public int SharedState { get; set; }
    }

    [Export]
    public class B
    {
        public A InstanceA { get; set; }
        public D InstanceD { get; set; }
        [ImportingConstructor]
        public B(A a, D d)
        {
            InstanceA = a;
            InstanceD = d;
        }
    }

    [Export]
    public class D
    {
        public A InstanceA;
        [ImportingConstructor]
        public D(A a)
        {
            InstanceA = a;
        }
    }


    [Export]
    public class C
    {
        private readonly ExportFactory<B> _fact;
        [ImportingConstructor]
        public C([SharingBoundary("Boundary")]ExportFactory<B> fact)
        {
            _fact = fact;
        }

        public B CreateInstance()
        {
            using (var b = _fact.CreateExport())
            {
                return b.Value;
            }
        }
    }

    [Export]
    public class CPrime
    {
        private readonly ExportFactory<B> _fact;
        [ImportingConstructor]
        public CPrime(ExportFactory<B> fact)
        {
            _fact = fact;
        }

        public B CreateInstance()
        {
            using (var b = _fact.CreateExport())
            {
                return b.Value;
            }
        }
    }

    [Export]
    [Shared("Boundary")]
    public class CirC
    {
        public CirA DepA { get; private set; }
        [ImportingConstructor]
        public CirC(CirA a)
        {
            DepA = a;
        }
    }


    [Export]
    [Shared]
    public class CirA
    {
        public int SharedState;
        private readonly ExportFactory<CirB> _fact;
        [ImportingConstructor]
        public CirA([SharingBoundary("Boundary")]ExportFactory<CirB> b)
        {
            _fact = b;
        }

        public CirB CreateInstance()
        {
            using (var ins = _fact.CreateExport())
            {
                return ins.Value;
            }
        }
    }

    [Export]
    public class CirB
    {
        public CirC DepC { get; private set; }
        [ImportingConstructor]
        public CirB(CirC c)
        {
            DepC = c;
        }
    }

    public interface IProj { }


    [Export]
    public class SolA
    {
        private readonly IEnumerable<ExportFactory<IProj>> _fact;
        public List<IProj> Projects { get; private set; }
        [ImportingConstructor]
        public SolA([ImportMany][SharingBoundary("B1", "B2")] IEnumerable<ExportFactory<IProj>> c)
        {
            Projects = new List<IProj>();
            _fact = c;
        }

        public void SetupProject()
        {
            foreach (var fact in _fact)
            {
                using (var instance = fact.CreateExport())
                {
                    Projects.Add(instance.Value);
                }
            }
        }
    }


    [Export(typeof(IProj))]
    public class ProjA : IProj
    {
        [ImportingConstructor]
        public ProjA(DocA docA, ColA colA)
        {
        }
    }

    [Export(typeof(IProj))]
    public class ProjB : IProj
    {
        [ImportingConstructor]
        public ProjB(DocB docA, ColB colA)
        {
        }
    }

    [Export]
    public class DocA
    {
        [ImportingConstructor]
        public DocA(ColA colA)
        {
        }
    }



    [Export]
    public class DocB
    {
        [ImportingConstructor]
        public DocB(ColB colB)
        {
        }
    }

    [Export]
    [Shared("B1")]

    public class ColA
    {
        public ColA()
        { }
    }

    [Export]
    [Shared("B2")]
    public class ColB
    {
        public ColB()
        { }
    }


    public interface ICol { }


    [Export(typeof(IX)), Export(typeof(IY)), Shared]
    public class XY : IX, IY { }
    public class SharingTest : ContainerTests
    {
        /// <summary>
        /// Two issues here One is that the message could be improved
        /// "The component (unknown) cannot be created outside the Boundary sharing boundary."
        /// Second is we don`t fail when we getExport for CPrime
        /// we fail only when we create instance of B.. is that correct.
        /// </summary>
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50919", TestPlatforms.Android)]
        public void BoundaryExposedBoundaryButNoneImported()
        {
            try
            {
                var cc = CreateContainer(typeof(A), typeof(B), typeof(CPrime), typeof(D));
                var cInstance = cc.GetExport<CPrime>();
                var bIn1 = cInstance.CreateInstance();
            }
            catch (Exception ex)
            {
                Assert.Contains("The component (unknown) cannot be created outside the Boundary sharing boundary", ex.Message);
            }
        }

        /// <summary>
        /// Need a partcreationpolicy currently.
        /// Needs to be fixed so that specifying boundary would automatically create the shared
        /// </summary>
        [Fact]
        public void BoundarySharingTest()
        {
            var cc = CreateContainer(typeof(A), typeof(B), typeof(C), typeof(D));
            var cInstance = cc.GetExport<C>();
            var bIn1 = cInstance.CreateInstance();
            var bIn2 = cInstance.CreateInstance();
            bIn1.InstanceA.SharedState = 1;
            var val1 = bIn1.InstanceD.InstanceA;

            bIn2.InstanceA.SharedState = 5;
            var val2 = bIn2.InstanceD.InstanceA;

            Assert.True(val1.SharedState == 1);
            Assert.True(val2.SharedState == 5);
        }


        /// <summary>
        /// CirA root of the composition has to be shared explicitly.
        /// </summary>
        [Fact]
        public void CircularBoundarySharingTest()
        {
            var cc = CreateContainer(typeof(CirA), typeof(CirB), typeof(CirC));
            var cInstance = cc.GetExport<CirA>();
            cInstance.SharedState = 1;
            var bInstance1 = cInstance.CreateInstance();
            Assert.Equal(1, bInstance1.DepC.DepA.SharedState);
            bInstance1.DepC.DepA.SharedState = 10;
            cInstance.CreateInstance();
            Assert.Equal(10, bInstance1.DepC.DepA.SharedState);
        }

        /// <summary>
        /// Something is badly busted here.. I am getting a null ref exception
        /// </summary>
        [Fact]
        public void MultipleBoundarySpecified()
        {
            var cc = CreateContainer(typeof(ProjA), typeof(ProjB), typeof(SolA), typeof(DocA), typeof(DocB), typeof(ColA), typeof(ColB));
            var solInstance = cc.GetExport<SolA>();
            solInstance.SetupProject();
        }


        [Fact]
        public void SharedPartExportingMultipleContractsSharesAnInstance()
        {
            var cc = CreateContainer(typeof(XY));
            var x = cc.GetExport<IX>();
            var y = cc.GetExport<IY>();
            Assert.Same(x, y);
        }

        [Fact]
        public void GetExportsCreatesInstancedObjectByDefault()
        {
            var cc = CreateContainer(typeof(NonSharedClass));
            var val1 = cc.GetExport<ISharedTestingClass>();
            var val2 = cc.GetExport<ISharedTestingClass>();
            Assert.NotSame(val1, val2);
        }

        [Fact]
        public void GetExportsCreatesSharedObjectsWhenSpecified()
        {
            var cc = CreateContainer(typeof(SharedClass));
            var val1 = cc.GetExport<ISharedTestingClass>();
            var val2 = cc.GetExport<ISharedTestingClass>();
            Assert.Same(val1, val2);
        }

        /// <summary>
        /// Class with export factory that, which is shared that has a part that is non shared
        /// verify that GetExport returns only one instance regardless of times it is called
        /// verify that On Method call different instances are returned.
        /// </summary>
        [Fact]
        public void ExportFactoryCreatesNewInstances()
        {
            var cc = CreateContainer(typeof(ClassWithExportFactoryShared), typeof(NonSharedClass));
            var b1 = cc.GetExport<ClassWithExportFactoryShared>();
            var b2 = cc.GetExport<ClassWithExportFactoryShared>();
            var inst1 = b1.Method();
            var inst2 = b1.Method();
            Assert.Same(b1, b2);
            Assert.NotSame(inst1, inst2);
        }

        /// <summary>
        /// ExportFactory should be importable as a property
        /// </summary>
        [Fact]
        public void ClassWithExportFactoryAsAProperty()
        {
            var cc = CreateContainer(typeof(ClassWithExportFactoryAsAProperty), typeof(NonSharedClass));
            var b1 = cc.GetExport<ClassWithExportFactoryAsAProperty>();
            var inst1 = b1.Method();
            var inst2 = b1.Method();
            Assert.NotNull(b1._fact);
            Assert.NotSame(inst1, inst2);
        }

        /// <summary>
        /// ExportFactory class is itself shared and
        /// will still respect the CreationPolicyAttribute on a part.  If the export factory
        /// is creating a part which is shared, it will return back the same instance of the part.
        /// </summary>
        [Fact]
        public void ClassWithExportFactoryAndSharedExport()
        {
            var cc = CreateContainer(typeof(ClassWithExportFactoryShared), typeof(SharedClass));
            var b1 = cc.GetExport<ClassWithExportFactoryShared>();
            var b2 = cc.GetExport<ClassWithExportFactoryShared>();
            var inst1 = b1.Method();
            var inst2 = b2.Method();
            Assert.Same(b1, b2);
            Assert.Same(inst1, inst2);
        }

        /// <summary>
        /// Class which is nonShared has an exportFactory in it for a shared part.
        /// Two instances of the root class are created , the part created using export factory should not be shared
        /// </summary>
        [Fact]
        public void ClassWithNonSharedExportFactoryCreatesSharedInstances()
        {
            var cc = CreateContainer(typeof(ClassWithExportFactoryNonShared), typeof(SharedClass));
            var b1 = cc.GetExport<ClassWithExportFactoryNonShared>();
            var b2 = cc.GetExport<ClassWithExportFactoryNonShared>();
            var inst1 = b1.Method();
            var inst2 = b2.Method();
            Assert.NotSame(b1, b2);
            Assert.Same(inst1, inst2);
        }

        [Shared, Export]
        public class ASharedPart { }

        [Fact]
        public void ConsistentResultsAreReturneWhenResolvingLargeNumbersOfSharedParts()
        {
            var config = new ContainerConfiguration();

            // Chosen to cause overflows in SmallSparseInitOnlyArray
            for (var i = 0; i < 1000; ++i)
                config.WithPart<ASharedPart>();

            Assert.NotEqual(new ASharedPart(), new ASharedPart());

            var container = config.CreateContainer();
            var first = container.GetExports<ASharedPart>().ToList();
            var second = container.GetExports<ASharedPart>().ToList();
            Assert.Equal(first, second);
        }
    }
}
