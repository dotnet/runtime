// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition.Primitives;
using Xunit;

namespace System.ComponentModel.Composition
{
    public interface IGetString
    {
        string GetString();
    }

    public class PublicComponentWithPublicExports
    {
        public const string PublicFieldExpectedValue = "PublicField";
        [Export("PublicField")]
        public string PublicField = PublicFieldExpectedValue;
        public const string PublicPropertyExpectedValue = "PublicProperty";
        [Export("PublicProperty")]
        public string PublicProperty { get { return PublicPropertyExpectedValue; } }
        public const string PublicMethodExpectedValue = "PublicMethod";
        [Export("PublicDelegate")]
        public string PublicMethod() { return PublicMethodExpectedValue; }
        public const string PublicNestedClassExpectedValue = "PublicNestedClass";
        [Export("PublicIGetString")]
        public class PublicNestedClass : IGetString
        {
            public string GetString() { return PublicNestedClassExpectedValue; }
        }
    }

    [Export]
    public class PublicImportsExpectingPublicExports
    {
        [Import("PublicField")]
        public string PublicImportPublicField { get; set; }
        [Import("PublicProperty")]
        public string PublicImportPublicProperty { get; set; }
        [Import("PublicDelegate")]
        public Func<string> PublicImportPublicMethod { get; set; }
        [Import("PublicIGetString")]
        public IGetString PublicImportPublicNestedClass { get; set; }

        public void VerifyIsBound()
        {
            Assert.Equal(PublicComponentWithPublicExports.PublicFieldExpectedValue, PublicImportPublicField);
            Assert.Equal(PublicComponentWithPublicExports.PublicPropertyExpectedValue, PublicImportPublicProperty);
            Assert.Equal(PublicComponentWithPublicExports.PublicMethodExpectedValue, PublicImportPublicMethod());
            Assert.Equal(PublicComponentWithPublicExports.PublicNestedClassExpectedValue, PublicImportPublicNestedClass.GetString());
        }
    }

    [Export]
    internal class InternalImportsExpectingPublicExports
    {
        [Import("PublicField")]
        internal string InternalImportPublicField { get; set; }
        [Import("PublicProperty")]
        internal string InternalImportPublicProperty { get; set; }
        [Import("PublicDelegate")]
        internal Func<string> InternalImportPublicMethod { get; set; }
        [Import("PublicIGetString")]
        internal IGetString InternalImportPublicNestedClass { get; set; }

        public void VerifyIsBound()
        {
            Assert.Equal(PublicComponentWithPublicExports.PublicFieldExpectedValue, InternalImportPublicField);
            Assert.Equal(PublicComponentWithPublicExports.PublicPropertyExpectedValue, InternalImportPublicProperty);
            Assert.Equal(PublicComponentWithPublicExports.PublicMethodExpectedValue, InternalImportPublicMethod());
            Assert.Equal(PublicComponentWithPublicExports.PublicNestedClassExpectedValue, InternalImportPublicNestedClass.GetString());
        }
    }

    public class PublicComponentWithInternalExports
    {
        public const string InternalFieldExpectedValue = "InternalField";
        [Export("InternalField")]
        internal string InternalField = InternalFieldExpectedValue;
        public const string InternalPropertyExpectedValue = "InternalProperty";
        [Export("InternalProperty")]
        internal string InternalProperty { get { return InternalPropertyExpectedValue; } }
        public const string InternalMethodExpectedValue = "InternalMethod";
        [Export("InternalDelegate")]
        internal string InternalMethod() { return InternalMethodExpectedValue; }
        public const string InternalNestedClassExpectedValue = "InternalNestedClass";
        [Export("InternalIGetString")]
        internal class InternalNestedClass : IGetString
        {
            public string GetString() { return InternalNestedClassExpectedValue; }
        }
    }

    [Export]
    public class PublicImportsExpectingInternalExports
    {
        [Import("InternalField")]
        public string PublicImportInternalField { get; set; }
        [Import("InternalProperty")]
        public string PublicImportInternalProperty { get; set; }
        [Import("InternalDelegate")]
        public Func<string> PublicImportInternalMethod { get; set; }
        [Import("InternalIGetString")]
        public IGetString PublicImportInternalNestedClass { get; set; }

        public void VerifyIsBound()
        {
            Assert.Equal(PublicComponentWithInternalExports.InternalFieldExpectedValue, PublicImportInternalField);
            Assert.Equal(PublicComponentWithInternalExports.InternalPropertyExpectedValue, PublicImportInternalProperty);
            Assert.Equal(PublicComponentWithInternalExports.InternalMethodExpectedValue, PublicImportInternalMethod());
            Assert.Equal(PublicComponentWithInternalExports.InternalNestedClassExpectedValue, PublicImportInternalNestedClass.GetString());
        }
    }

    [Export]
    internal class InternalImportsExpectingInternalExports
    {
        [Import("InternalField")]
        internal string InternalImportInternalField { get; set; }
        [Import("InternalProperty")]
        internal string InternalImportInternalProperty { get; set; }
        [Import("InternalDelegate")]
        internal Func<string> InternalImportInternalMethod { get; set; }
        [Import("InternalIGetString")]
        internal IGetString InternalImportInternalNestedClass { get; set; }

        public void VerifyIsBound()
        {
            Assert.Equal(PublicComponentWithInternalExports.InternalFieldExpectedValue, InternalImportInternalField);
            Assert.Equal(PublicComponentWithInternalExports.InternalPropertyExpectedValue, InternalImportInternalProperty);
            Assert.Equal(PublicComponentWithInternalExports.InternalMethodExpectedValue, InternalImportInternalMethod());
            Assert.Equal(PublicComponentWithInternalExports.InternalNestedClassExpectedValue, InternalImportInternalNestedClass.GetString());
        }
    }

    public class PublicComponentWithProtectedExports
    {
        public const string ProtectedFieldExpectedValue = "ProtectedField";
        [Export("ProtectedField")]
        protected string ProtectedField = ProtectedFieldExpectedValue;
        public const string ProtectedPropertyExpectedValue = "ProtectedProperty";
        [Export("ProtectedProperty")]
        protected string ProtectedProperty { get { return ProtectedPropertyExpectedValue; } }
        public const string ProtectedMethodExpectedValue = "ProtectedMethod";
        [Export("ProtectedDelegate")]
        protected string ProtectedMethod() { return ProtectedMethodExpectedValue; }
        public const string ProtectedNestedClassExpectedValue = "ProtectedNestedClass";
        [Export("ProtectedIGetString")]
        protected class ProtectedNestedClass : IGetString
        {
            public string GetString() { return ProtectedNestedClassExpectedValue; }
        }
    }

    [Export]
    public class PublicImportsExpectingProtectedExports
    {
        [Import("ProtectedField")]
        public string PublicImportProtectedField { get; set; }
        [Import("ProtectedProperty")]
        public string PublicImportProtectedProperty { get; set; }
        [Import("ProtectedDelegate")]
        public Func<string> PublicImportProtectedMethod { get; set; }
        [Import("ProtectedIGetString")]
        public IGetString PublicImportProtectedNestedClass { get; set; }

        public void VerifyIsBound()
        {
            Assert.Equal(PublicComponentWithProtectedExports.ProtectedFieldExpectedValue, PublicImportProtectedField);
            Assert.Equal(PublicComponentWithProtectedExports.ProtectedPropertyExpectedValue, PublicImportProtectedProperty);
            Assert.Equal(PublicComponentWithProtectedExports.ProtectedMethodExpectedValue, PublicImportProtectedMethod());
            Assert.Equal(PublicComponentWithProtectedExports.ProtectedNestedClassExpectedValue, PublicImportProtectedNestedClass.GetString());
        }
    }

    [Export]
    internal class InternalImportsExpectingProtectedExports
    {
        [Import("ProtectedField")]
        internal string InternalImportProtectedField { get; set; }
        [Import("ProtectedProperty")]
        internal string InternalImportProtectedProperty { get; set; }
        [Import("ProtectedDelegate")]
        internal Func<string> InternalImportProtectedMethod { get; set; }
        [Import("ProtectedIGetString")]
        internal IGetString InternalImportProtectedNestedClass { get; set; }

        public void VerifyIsBound()
        {
            Assert.Equal(PublicComponentWithProtectedExports.ProtectedFieldExpectedValue, InternalImportProtectedField);
            Assert.Equal(PublicComponentWithProtectedExports.ProtectedPropertyExpectedValue, InternalImportProtectedProperty);
            Assert.Equal(PublicComponentWithProtectedExports.ProtectedMethodExpectedValue, InternalImportProtectedMethod());
            Assert.Equal(PublicComponentWithProtectedExports.ProtectedNestedClassExpectedValue, InternalImportProtectedNestedClass.GetString());
        }
    }

    public class PublicComponentWithProtectedInternalExports
    {
        public const string ProtectedInternalFieldExpectedValue = "ProtectedInternalField";
        [Export("ProtectedInternalField")]
        protected internal string ProtectedInternalField = ProtectedInternalFieldExpectedValue;
        public const string ProtectedInternalPropertyExpectedValue = "ProtectedInternalProperty";
        [Export("ProtectedInternalProperty")]
        protected internal string ProtectedInternalProperty { get { return ProtectedInternalPropertyExpectedValue; } }
        public const string ProtectedInternalMethodExpectedValue = "ProtectedInternalMethod";
        [Export("ProtectedInternalDelegate")]
        protected internal string ProtectedInternalMethod() { return ProtectedInternalMethodExpectedValue; }
        public const string ProtectedInternalNestedClassExpectedValue = "ProtectedInternalNestedClass";
        [Export("ProtectedInternalIGetString")]
        protected internal class ProtectedInternalNestedClass : IGetString
        {
            public string GetString() { return ProtectedInternalNestedClassExpectedValue; }
        }
    }

    [Export]
    public class PublicImportsExpectingProtectedInternalExports
    {
        [Import("ProtectedInternalField")]
        public string PublicImportProtectedInternalField { get; set; }
        [Import("ProtectedInternalProperty")]
        public string PublicImportProtectedInternalProperty { get; set; }
        [Import("ProtectedInternalDelegate")]
        public Func<string> PublicImportProtectedInternalMethod { get; set; }
        [Import("ProtectedInternalIGetString")]
        public IGetString PublicImportProtectedInternalNestedClass { get; set; }

        public void VerifyIsBound()
        {
            Assert.Equal(PublicComponentWithProtectedInternalExports.ProtectedInternalFieldExpectedValue, PublicImportProtectedInternalField);
            Assert.Equal(PublicComponentWithProtectedInternalExports.ProtectedInternalPropertyExpectedValue, PublicImportProtectedInternalProperty);
            Assert.Equal(PublicComponentWithProtectedInternalExports.ProtectedInternalMethodExpectedValue, PublicImportProtectedInternalMethod());
            Assert.Equal(PublicComponentWithProtectedInternalExports.ProtectedInternalNestedClassExpectedValue, PublicImportProtectedInternalNestedClass.GetString());
        }
    }

    [Export]
    internal class InternalImportsExpectingProtectedInternalExports
    {
        [Import("ProtectedInternalField")]
        internal string InternalImportProtectedInternalField { get; set; }
        [Import("ProtectedInternalProperty")]
        internal string InternalImportProtectedInternalProperty { get; set; }
        [Import("ProtectedInternalDelegate")]
        internal Func<string> InternalImportProtectedInternalMethod { get; set; }
        [Import("ProtectedInternalIGetString")]
        internal IGetString InternalImportProtectedInternalNestedClass { get; set; }

        public void VerifyIsBound()
        {
            Assert.Equal(PublicComponentWithProtectedInternalExports.ProtectedInternalFieldExpectedValue, InternalImportProtectedInternalField);
            Assert.Equal(PublicComponentWithProtectedInternalExports.ProtectedInternalPropertyExpectedValue, InternalImportProtectedInternalProperty);
            Assert.Equal(PublicComponentWithProtectedInternalExports.ProtectedInternalMethodExpectedValue, InternalImportProtectedInternalMethod());
            Assert.Equal(PublicComponentWithProtectedInternalExports.ProtectedInternalNestedClassExpectedValue, InternalImportProtectedInternalNestedClass.GetString());
        }
    }

    public class PublicComponentWithPrivateExports
    {
        public const string PrivateFieldExpectedValue = "PrivateField";
        public const string PrivatePropertyExpectedValue = "PrivateProperty";
        [Export("PrivateProperty")]
        private string PrivateProperty { get { return PrivatePropertyExpectedValue; } }
        public const string PrivateMethodExpectedValue = "PrivateMethod";
        [Export("PrivateDelegate")]
        private string PrivateMethod() { return PrivateMethodExpectedValue; }
        public const string PrivateNestedClassExpectedValue = "PrivateNestedClass";
        [Export("PrivateIGetString")]
        private class PrivateNestedClass : IGetString
        {
            public string GetString() { return PrivateNestedClassExpectedValue; }
        }
    }

    [Export]
    public class PublicImportsExpectingPrivateExports
    {
        [Import("PrivateField")]
        public string PublicImportPrivateField { get; set; }
        [Import("PrivateProperty")]
        public string PublicImportPrivateProperty { get; set; }
        [Import("PrivateDelegate")]
        public Func<string> PublicImportPrivateMethod { get; set; }
        [Import("PrivateIGetString")]
        public IGetString PublicImportPrivateNestedClass { get; set; }

        public void VerifyIsBound()
        {
            Assert.Equal(PublicComponentWithPrivateExports.PrivateFieldExpectedValue, PublicImportPrivateField);
            Assert.Equal(PublicComponentWithPrivateExports.PrivatePropertyExpectedValue, PublicImportPrivateProperty);
            Assert.Equal(PublicComponentWithPrivateExports.PrivateMethodExpectedValue, PublicImportPrivateMethod());
            Assert.Equal(PublicComponentWithPrivateExports.PrivateNestedClassExpectedValue, PublicImportPrivateNestedClass.GetString());
        }
    }

    [Export]
    internal class InternalImportsExpectingPrivateExports
    {
        [Import("PrivateField")]
        internal string InternalImportPrivateField { get; set; }
        [Import("PrivateProperty")]
        internal string InternalImportPrivateProperty { get; set; }
        [Import("PrivateDelegate")]
        internal Func<string> InternalImportPrivateMethod { get; set; }
        [Import("PrivateIGetString")]
        internal IGetString InternalImportPrivateNestedClass { get; set; }

        public void VerifyIsBound()
        {
            Assert.Equal(PublicComponentWithPrivateExports.PrivateFieldExpectedValue, InternalImportPrivateField);
            Assert.Equal(PublicComponentWithPrivateExports.PrivatePropertyExpectedValue, InternalImportPrivateProperty);
            Assert.Equal(PublicComponentWithPrivateExports.PrivateMethodExpectedValue, InternalImportPrivateMethod());
            Assert.Equal(PublicComponentWithPrivateExports.PrivateNestedClassExpectedValue, InternalImportPrivateNestedClass.GetString());
        }
    }

    public class TestContractType
    {
        public const string TestContractName = "TestContractName";
    }

    public class TestContractTypeWithoutName { }

    public class TypeDerivingFromTestContractType : TestContractType { }

    [Export(typeof(TestContractType))]
    public class ExportedTypeWithContractType { }

    public class TypeDerivingFromExportedTypeWithContractType : ExportedTypeWithContractType { }

    [Export("ImportDefaultFunctions")]
    public class ImportDefaultFunctions
    {
        [Import("FunctionWith0Args")]
        public Func<int> MyFunction0;

        [Import("FunctionWith1Arg")]
        public Func<int, int> MyFunction1;

        [Import("FunctionWith2Args")]
        public Func<int, int, int> MyFunction2;

        [Import("FunctionWith3Args")]
        public Func<int, int, int, int> MyFunction3;

        [Import("FunctionWith4Args")]
        public Func<int, int, int, int, int> MyFunction4;

        [Import("ActionWith0Args")]
        public Action MyAction0;

        [Import("ActionWith1Arg")]
        public Action<int> MyAction1;

        [Import("ActionWith2Args")]
        public Action<int, int> MyAction2;

        [Import("ActionWith3Args")]
        public Action<int, int, int> MyAction3;

        [Import("ActionWith4Args")]
        public Action<int, int, int, int> MyAction4;

        public void VerifyIsBound()
        {
            Assert.Equal(0, MyFunction0.Invoke());
            Assert.Equal(1, MyFunction1.Invoke(1));
            Assert.Equal(3, MyFunction2.Invoke(1, 2));
            Assert.Equal(6, MyFunction3.Invoke(1, 2, 3));
            Assert.Equal(10, MyFunction4.Invoke(1, 2, 3, 4));

            MyAction0.Invoke();
            MyAction1.Invoke(1);
            MyAction2.Invoke(1, 2);
            MyAction3.Invoke(1, 2, 3);
            MyAction4.Invoke(1, 2, 3, 4);
        }
    }

    public class ExportDefaultFunctions
    {
        [Export("FunctionWith0Args")]
        public int MyFunction0()
        {
            return 0;
        }

        [Export("FunctionWith1Arg")]
        public int MyFunction1(int i1)
        {
            return i1;
        }

        [Export("FunctionWith2Args")]
        public int MyFunction2(int i1, int i2)
        {
            return i1 + i2;
        }

        [Export("FunctionWith3Args")]
        public int MyFunction3(int i1, int i2, int i3)
        {
            return i1 + i2 + i3;
        }

        [Export("FunctionWith4Args")]
        public int MyFunction4(int i1, int i2, int i3, int i4)
        {
            return i1 + i2 + i3 + i4;
        }

        [Export("ActionWith0Args")]
        public void MyAction0()
        {
        }

        [Export("ActionWith1Arg")]
        public void MyAction1(int i1)
        {
            Assert.Equal(i1, 1);
        }

        [Export("ActionWith2Args")]
        public void MyAction2(int i1, int i2)
        {
            Assert.Equal(i1, 1);
            Assert.Equal(i2, 2);
        }

        [Export("ActionWith3Args")]
        public void MyAction3(int i1, int i2, int i3)
        {
            Assert.Equal(i1, 1);
            Assert.Equal(i2, 2);
            Assert.Equal(i3, 3);
        }

        [Export("ActionWith4Args")]
        public void MyAction4(int i1, int i2, int i3, int i4)
        {
            Assert.Equal(i1, 1);
            Assert.Equal(i2, 2);
            Assert.Equal(i3, 3);
            Assert.Equal(i4, 4);
        }
    }

    [Export]
    public class CatalogComponentTest
    {
    }

    [Export]
    [PartNotDiscoverable]
    public class CatalogComponentTestNonComponentPart
    {
    }

    public interface ICatalogComponentTest
    {
    }

    [Export(typeof(ICatalogComponentTest))]
    public class CatalogComponentInterfaceTest1 : ICatalogComponentTest
    {
    }

    public class CatalogComponentInterfaceTest2
    {
        [Export]
        public ICatalogComponentTest ExportedInterface
        {
            get { return new CatalogComponentInterfaceTest1(); }
        }
    }

    public static class StaticExportClass
    {
        [Export("StaticString")]
        public static string StaticString { get { return "StaticString"; } }
    }

    [Export]
    public class DisposableExportClass : IDisposable
    {
        public bool IsDisposed { get; set; }
        public void Dispose()
        {
            Assert.False(IsDisposed);
            IsDisposed = true;
        }
    }

    public interface IServiceView
    {
        int GetSomeInt();
    }

    [Export("service1")]
    public class Service
    {
        public int GetSomeInt()
        {
            return 5;
        }
    }

    public class Client
    {
        private IServiceView mySerivce;

        [Import("service1")]
        public IServiceView MyService
        {
            get { return mySerivce; }
            set { mySerivce = value; }
        }
        public int GetSomeValue()
        {
            return MyService.GetSomeInt() * 2;
        }
    }

    [Export]
    public class TrivialExporter
    {
        public bool done = false;
    }

    [Export]
    public class TrivialImporter : IPartImportsSatisfiedNotification
    {
        [Import]
        public TrivialExporter checker;

        public void OnImportsSatisfied()
        {
            checker.done = true;
        }
    }

    [Export]
    public class UnnamedImportAndExport
    {
        [Import]
        public IUnnamedExport ImportedValue;
    }

    [Export]
    public class StaticExport
    {
    }

    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class NonStaticExport
    {
    }

    public interface IUnnamedExport
    {
    }

    [Export(typeof(IUnnamedExport))]
    public class UnnamedExport : IUnnamedExport
    {
    }

    public interface IExportableTest
    {
        string Var1 { get; }
    }

    [AttributeUsage(AttributeTargets.All)]
    [MetadataAttribute]
    public class ExportableTestAttribute : Attribute
    {
        private string var1;

        public string Var1
        {
            get { return var1; }
            set { var1 = value; }
        }
    }

    [AttributeUsage(AttributeTargets.All)]
    [MetadataAttribute]
    public class MetadataWithCollectionPropertyAttribute : Attribute
    {
        private List<string> values;

        public IEnumerable<string> Values
        {
            get { return values; }
        }

        public MetadataWithCollectionPropertyAttribute(params string[] values)
        {
            this.values = new List<string>(values);
        }
    }

    [Export]
    [MetadataWithCollectionProperty("One", "two", "3")]
    [PartNotDiscoverable]
    public class ComponentWithCollectionProperty
    {
    }

    public interface ICollectionOfStrings
    {
        IEnumerable<string> Values { get; }
    }

    public class SubtractProvider
    {
        [Export("One")]
        public int One = 1;

        [Export("Two")]
        public int Two { get { return 2; } }

        [Export("Add")]
        [ExportableTest(Var1 = "sub")]
        public Func<int, int, int> Subtract = (x, y) => x - y;
    }

    public class RealAddProvider
    {
        [Export("One")]
        public int One = 1;

        [Export("Two")]
        public int Two { get { return 2; } }

        [Export("Add")]
        [ExportMetadata("Var1", "add")]
        public int Add(int x, int y)
        {
            return x + y;
        }
    }

    public class Consumer
    {
        [Import("One")]
        public int a;
        [Import("Two")]
        public int b;
        [Import("Add")]
        public Func<int, int, int> op;
        [Import("Add", AllowDefault = true)]
        public Lazy<Func<int, int, int>> opInfo;
    }

    public class ConsumerOfMultiple
    {
        [ImportMany("Add")]
        public IEnumerable<Lazy<Func<int, int, int>>> opInfo;
    }

    public interface IStrongValueMetadata
    {
        int value { get; set; }
    }

    public class UntypedExportImporter
    {
        [Import("untyped")]
        public Export Export;
    }

    public class UntypedExportsImporter
    {
        [ImportMany("untyped")]
        public IEnumerable<Export> Exports;
    }

    public class DerivedExport : Export
    {
    }

    public class DerivedExportImporter
    {
        [Import("derived")]
        public DerivedExport Export;

    }

    public class DerivedExportsImporter
    {
        [ImportMany("derived")]
        public IEnumerable<DerivedExport> Exports;
    }

    [Export]
    public class NotSoUniqueName
    {
        public int MyIntProperty { get { return 23; } }
    }

    public class NotSoUniqueName2
    {
        [Export]
        public class NotSoUniqueName
        {
            public virtual string MyStringProperty { get { return "MyStringProperty"; } }
        }
    }

    [Export]
    public class MyExport
    {
    }

    [Export]
    public class MySharedPartExport
    {
        [Import("Value", AllowRecomposition = true)]
        public int Value { get; set; }
    }

    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class MyNonSharedPartExport
    {
        [Import("Value")]
        public int Value { get; set; }
    }

    public class ExportThatCantBeActivated
    {
        [Export("ExportMyString")]
        public string MyString { get { return "MyString"; } }

        [Import("ContractThatShouldNotexist")]
        public string MissingImport { get; set; }
    }

    public class GenericContract1<T>
    {
        public class GenericContract2
        {
            public class GenericContract3<N>
            {
            }
        }
    }

    public class GenericContract4<T, K>
    {
        public class GenericContract5<A, B>
        {
            public class GenericContract6<N, M>
            {
            }
        }
    }

    public class OuterClassWithGenericNested
    {
        public class GenericNested<T>
        {
        }
    }

    public class GenericContract7 :
        GenericContract4<string, string>.GenericContract5<int, int>.GenericContract6<double, double>
    { }

    public class GenericContract8<T> : GenericContract1<string>.GenericContract2.GenericContract3<T> { }

    public class NestedParent
    {
        public class NestedChild { }
    }

    public class ImporterOfExporterNotifyPropertyChanged
    {
        [Import("value", AllowRecomposition = true)]
        public int Value;

        [Import("secondValue")]
        public int SecondValue;
    }

    public class ExporterNotifyPropertyChanged : System.ComponentModel.INotifyPropertyChanged
    {
        int theValue = 42;

        [Export("value")]
        public int Value
        {
            get { return theValue; }
            set
            {
                theValue = value;
                FirePropertyChange("Value");
            }
        }

        [Export("secondValue")]
        public int second = 2;

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        public void FirePropertyChange(string prop)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(prop));
            }
        }
    }

    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class DirectCycleNonSharedPart
    {
        [Import]
        public DirectCycleNonSharedPart NonSharedPart { get; set; }
    }

    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class CycleNonSharedPart1
    {
        [Import]
        public CycleNonSharedPart2 NonSharedPart2 { get; set; }
    }

    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class CycleNonSharedPart2
    {
        [Import]
        public CycleNonSharedPart1 NonSharedPart1 { get; set; }
    }

    [Export]
    public class CycleNonSharedPart
    {
        [Import]
        public CycleNonSharedPart1 NonSharedPart1 { get; set; }
    }

    [Export]
    public class CycleSharedPart1
    {
        [Import]
        public CycleSharedPart2 SharedPart2 { get; set; }
    }

    [Export]
    public class CycleSharedPart2
    {
        [Import]
        public CycleSharedPart1 SharedPart2 { get; set; }
    }

    [Export]
    public class CycleSharedPart
    {
        [Import]
        public CycleSharedPart1 SharedPart1 { get; set; }

        [Import]
        public CycleSharedPart2 SharedPart2 { get; set; }
    }

    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class NoCycleNonSharedPart
    {
        [Import]
        public SharedPartWithNoCycleNonSharedPart SharedPart { get; set; }
    }

    [Export]
    public class SharedPartWithNoCycleNonSharedPart
    {
        [Import]
        public NoCycleNonSharedPart NonSharedPart { get; set; }
    }

    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class CycleWithSharedPartAndNonSharedPart
    {
        [Import]
        public SharedPartWithNoCycleNonSharedPart BeforeNonSharedPart { get; set; }

        [Import]
        public CycleWithNonSharedPartOnly NonSharedPart { get; set; }

        [Import]
        public SharedPartWithNoCycleNonSharedPart SharedPart { get; set; }
    }

    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class CycleWithNonSharedPartOnly
    {
        [Import]
        public CycleWithSharedPartAndNonSharedPart NonSharedPart { get; set; }

    }

    [InheritedExport]
    public class ExportWithGenericParameter<T>
    {
    }

    public class ExportWithGenericParameterOfInt
    {
        [Export]
        public ExportWithGenericParameter<int> GenericExport { get { return new ExportWithGenericParameter<int>(); } }
    }

    [Export]
    public static class StaticExportWithGenericParameter<T>
    {
    }

    [Export]
    public class ExportWhichInheritsFromGeneric : ExportWithGenericParameter<string>
    {

    }

    [Export]
    public class ExportWithExceptionDuringConstruction
    {
        public ExportWithExceptionDuringConstruction()
        {
            throw new NotImplementedException();
        }
    }

    [Export]
    public class SimpleConstructorInjectedObject
    {
        [ImportingConstructor]
        public SimpleConstructorInjectedObject([Import("CISimpleValue")]int value)
        {
            CISimpleValue = value;
        }

        public int CISimpleValue { get; private set; }
    }

    [Export]
    public class ClassWithNoMarkedOrDefaultConstructor
    {
        public ClassWithNoMarkedOrDefaultConstructor(int blah) { }
    }

    public class ClassWhichOnlyHasImportingConstructorWithOneArgument
    {
        [ImportingConstructor]
        public ClassWhichOnlyHasImportingConstructorWithOneArgument(int blah) { }
    }

    public class ClassWhichOnlyHasImportingConstructor
    {
        [ImportingConstructor]
        public ClassWhichOnlyHasImportingConstructor() { }
    }

    public class ClassWhichOnlyHasDefaultConstructor
    {
        public ClassWhichOnlyHasDefaultConstructor() { }
    }

    [Export]
    public class BaseExportForImportingConstructors
    {

    }

    public class ClassWithOnlyHasImportingConstructorButInherits : BaseExportForImportingConstructors
    {
        [ImportingConstructor]
        public ClassWithOnlyHasImportingConstructorButInherits(int blah) { }
    }

    public class ClassWithOnlyHasMultipleImportingConstructorButInherits : BaseExportForImportingConstructors
    {
        [ImportingConstructor]
        public ClassWithOnlyHasMultipleImportingConstructorButInherits(int blah) { }

        [ImportingConstructor]
        public ClassWithOnlyHasMultipleImportingConstructorButInherits(string blah) { }
    }

    [Export]
    public class ClassWithMultipleMarkedConstructors
    {
        [ImportingConstructor]
        public ClassWithMultipleMarkedConstructors(int i) { }

        [ImportingConstructor]
        public ClassWithMultipleMarkedConstructors(string s) { }

        public ClassWithMultipleMarkedConstructors() { }
    }

    [Export]
    public class ClassWithOneMarkedAndOneDefaultConstructor
    {
        [ImportingConstructor]
        public ClassWithOneMarkedAndOneDefaultConstructor(int i) { }

        public ClassWithOneMarkedAndOneDefaultConstructor() { }
    }

    [Export]
    public class ClassWithTwoZeroParameterConstructors
    {
        public ClassWithTwoZeroParameterConstructors() { }

        static ClassWithTwoZeroParameterConstructors() { }
    }

    [Export]
    public class ExceptionDuringINotifyImport : IPartImportsSatisfiedNotification
    {
        [ImportMany("Value")]
        public IEnumerable<int> ValuesJustUsedToGetImportCompletedCalled { get; set; }

        public void OnImportsSatisfied()
        {
            throw new NotImplementedException();
        }
    }

    [Export]
    public class ClassWithOptionalPostImport
    {
        [Import(AllowDefault = true)]
        public IFormattable Formatter { get; set; }
    }

    [Export]
    public class ClassWithOptionalPreImport
    {
        [ImportingConstructor]
        public ClassWithOptionalPreImport([Import(AllowDefault = true)] IFormattable formatter)
        {
            this.Formatter = formatter;
        }

        public IFormattable Formatter { get; private set; }
    }

    [MetadataAttribute]
    public class ThisIsMyMetadataMetadataAttribute : Attribute
    {
        public string Argument1 { get; set; }
        public int Argument2 { get; set; }
        public double Argument3 { get; set; }
        public string Argument4 { get; set; }

        public ThisIsMyMetadataMetadataAttribute()
        {
        }

        public ThisIsMyMetadataMetadataAttribute(string Argument1, int Argument2)
        {
            this.Argument1 = Argument1;
            this.Argument2 = Argument2;
        }
    }

    [Export]
    [ThisIsMyMetadataMetadataAttribute("One", 2, Argument3 = 3.0)]
    public class ExportedTypeWithConcreteMetadata
    {
    }

    public class Int32CollectionImporter
    {

        public Int32CollectionImporter()
        {
            Values = new Collection<int>();
        }

        [ImportMany("Value")]
        public Collection<int> Values { get; private set; }
    }

    [PartNotDiscoverable]
    public class Int32Exporter
    {

        public Int32Exporter(int value)
        {
            Value = value;
        }

        [Export("Value")]
        public int Value { get; set; }

    }

    [PartNotDiscoverable]
    public class Int32ExporterInternal
    {

        public Int32ExporterInternal(int value)
        {
            Value = value;
        }

        [Export("Value")]
        public int Value { get; set; }

    }

    public class Int32Importer
    {

        public Int32Importer()
        {
        }

        [Import("Value", AllowRecomposition = true)]
        public int Value { get; set; }
    }

    public class Int32ImporterInternal
    {

        public Int32ImporterInternal()
        {
        }

        [Import("Value")]
        public int Value { get; set; }
    }

}
