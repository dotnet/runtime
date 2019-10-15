// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Primitives;
using Xunit;

namespace Tests.Integration
{
    public class DelegateCompositionTests
    {
        public delegate int SimpleDelegate();
        public delegate object DoWorkDelegate(int i, ref object o, out string s);

        public class MethodExporter
        {
            [Export]
            public int SimpleMethod()
            {
                return 1;
            }

            [Export(typeof(DoWorkDelegate))]
            public object DoWork(int i, ref object o, out string s)
            {
                s = "";
                return o;
            }

            [Export("ActionWith8Arguments")]
            [Export("ActionWith8Arguments", typeof(Delegate))]
            public void Action(int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8)
            {
            }

            [Export("FunctionWith8Arguments")]
            [Export("FunctionWith8Arguments", typeof(Delegate))]
            public int Function(int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8)
            {
                return i1 + i2 + i3 + i4 + i5 + i6 + i7 + i8;
            }

            [Export("FunctionWithDefaultValue")]
            public int FunctionWithDefaultValue(int i, string s = "")
            {
                return i;
            }
        }

        [Fact]
        public void Export_SimpleCustomDelegate_ShouldWork()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(
                typeof(MethodExporter));

            var contractName = AttributedModelServices.GetContractName(typeof(SimpleDelegate));

            var export1 = container.GetExportedValue<SimpleDelegate>();
            Assert.Equal(1, export1());

            var export2 = container.GetExportedValue<Func<int>>();
            Assert.Equal(1, export1());

            var export3 = (ExportedDelegate)container.GetExportedValue<object>(contractName);
            var export4 = (SimpleDelegate)export3.CreateDelegate(typeof(SimpleDelegate));
            Assert.Equal(1, export4());
        }

        [Fact]
        public void Export_CustomDelegateWithOutRefParams_ShouldWork()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(
                typeof(MethodExporter));

            var export1 = container.GetExportedValue<DoWorkDelegate>();

            int i = 0;
            object o = new object();
            string s;

            export1(i, ref o, out s);
        }

        [Fact]
        public void Export_FunctionWith8Arguments_ShouldWorkFine()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(
                typeof(MethodExporter));

            Assert.NotNull(container.GetExportedValue<Delegate>("FunctionWith8Arguments"));
        }

        [Fact]
        public void Export_ActionWith8Arguments_ShouldWorkFine()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(
                typeof(MethodExporter));

            Assert.NotNull(container.GetExportedValue<Delegate>("ActionWith8Arguments"));
        }

        [Fact]
        public void Export_FunctionWithDefaultValue_ShouldWorkFine()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(
                typeof(MethodExporter));

            var export = container.GetExportedValue<Func<int, string, int>>("FunctionWithDefaultValue");
            Assert.Equal(3, export(3, "a"));

            // Even though the string argument is optional it still cannot be cast to Func<int, int>.
            var export2 = (ExportedDelegate)container.GetExportedValue<object>("FunctionWithDefaultValue");
            var export3 = export2.CreateDelegate(typeof(Func<int, int>));

            Assert.Null(export3);
        }

        public delegate int DelegateOneArg(int i);
        public delegate int DelegateTwoArgs(int i, int j);

        public class CustomExportedDelegate : ExportedDelegate
        {
            private Func<int, int, int> _func;

            public CustomExportedDelegate(Func<int, int, int> func)
            {
                this._func = func;
            }

            public override Delegate CreateDelegate(Type delegateType)
            {
                if (delegateType == typeof(DelegateOneArg))
                {
                    return (DelegateOneArg)((i) => this._func(i, 0));
                }
                else if (delegateType == typeof(DelegateTwoArgs))
                {
                    return (DelegateTwoArgs)((i, j) => this._func(i, j));
                }

                return null;
            }
        }

        public class ExportCustomExportedDelegates
        {
            [Export("CustomExportedDelegate", typeof(DelegateOneArg))]
            [Export("CustomExportedDelegate", typeof(DelegateTwoArgs))]
            public ExportedDelegate MyExportedDelegate
            {
                get
                {
                    return new CustomExportedDelegate(DoWork);
                }
            }

            public int DoWork(int i, int j)
            {
                return i + j;
            }
        }

        [Export]
        public class ImportCustomExportedDelegates
        {
            [Import("CustomExportedDelegate")]
            public DelegateOneArg DelegateOneArg { get; set; }

            [Import("CustomExportedDelegate")]
            public DelegateTwoArgs DelegateTwoArgs { get; set; }

        }

        [Fact]
        public void CustomExportedDelegate_ShouldWork()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(
                typeof(ExportCustomExportedDelegates),
                typeof(ImportCustomExportedDelegates));

            var importer = container.GetExportedValue<ImportCustomExportedDelegates>();

            Assert.Equal(1, importer.DelegateOneArg(1));
            Assert.Equal(2, importer.DelegateTwoArgs(1, 1));
        }

        public delegate void GetRef(ref int i);
        public delegate void GetOut(out int i);

        public class RefOutMethodExporter
        {
            [Export]
            public void MyGetOut(out int i)
            {
                i = 29;
            }
        }

        [Fact]
        public void MethodWithOutParam_ShouldWorkWithRefParam()
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(
                typeof(RefOutMethodExporter));

            int i = 0;

            var export1 = container.GetExportedValue<GetRef>();

            export1(ref i);
            Assert.Equal(29, i);
            i = 0;

            var export2 = container.GetExportedValue<GetOut>();

            export2(out i);
            Assert.Equal(29, i);
        }
    }
}
