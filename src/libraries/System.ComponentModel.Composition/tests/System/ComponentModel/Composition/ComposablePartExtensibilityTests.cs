// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Reflection;
using System.UnitTesting;
using Xunit;

namespace System.ComponentModel.Composition
{
    public class ComposablePartExtensibilityTests
    {
        [Fact]
        public void PhaseTest()
        {
            CompositionContainer container = ContainerFactory.Create();
            CompositionBatch batch = new CompositionBatch();

            var part = new OrderingTestComposablePart();
            part.AddImport("Import1", ImportCardinality.ExactlyOne, true, false);
            part.AddExport("Export1", 1);
            part.CallOrder.Enqueue("Import:Import1");
            part.CallOrder.Enqueue("OnComposed");

            batch.AddExportedValue("Import1", 20);
            batch.AddPart(part);
            container.Compose(batch);

            // Export shouldn't be called until it is pulled on by someone.
            var export = container.GetExport<object>("Export1");

            part.CallOrder.Enqueue("Export:Export1");
            Assert.Equal(1, export.Value);

            Assert.True(part.CallOrder.Count == 0);
        }

        [Fact]
        public void ImportTest()
        {
            var exporter = new TestExportBinder();
            var importer = new TestImportBinder();

            CompositionContainer container = ContainerFactory.Create();
            CompositionBatch batch = new CompositionBatch();

            batch.AddPart(importer);
            batch.AddPart(exporter);
            container.Compose(batch);

            ExportsAssert.AreEqual(importer.SetImports["single"], 42);
            ExportsAssert.AreEqual(importer.SetImports["multi"], 1, 2, 3);
        }

        [Fact]
        public void ConstructorInjectionSimpleCase()
        {
            var container = ContainerFactory.Create();
            CompositionBatch batch = new CompositionBatch();

            batch.AddPart(new ConstructorInjectionComposablePart(typeof(Foo)));
            batch.AddExportedValue<IBar>(new Bar("Bar Value"));
            container.Compose(batch);

            var import = container.GetExport<Foo>();
            var foo = import.Value;

            Assert.Equal("Bar Value", foo.Bar.Value);
        }

        [Fact]
        public void ConstructorInjectionCycle()
        {
            var container = ContainerFactory.Create();
            CompositionBatch batch = new CompositionBatch();

            batch.AddPart(new ConstructorInjectionComposablePart(typeof(AClass)));
            batch.AddPart(new ConstructorInjectionComposablePart(typeof(BClass)));

            CompositionAssert.ThrowsErrors(ErrorId.ImportEngine_PartCannotSetImport,
                                           ErrorId.ImportEngine_PartCannotSetImport, RetryMode.DoNotRetry, () =>
            {
                container.Compose(batch);
            });
        }
    }

    internal class OrderingTestComposablePart : ConcreteComposablePart
    {
        public Queue<string> CallOrder = new Queue<string>();

        public OrderingTestComposablePart()
        {
        }

        public new  void AddExport(string contractName, object value)
        {
            var export = ExportFactory.Create(contractName, () =>
            {
                this.OnGetExport(contractName); return value;
            });

            base.AddExport(export);
        }

        private void OnGetExport(string contractName)
        {
            Assert.Equal("Export:" + contractName, CallOrder.Dequeue());
        }

        public override void SetImport(ImportDefinition definition, IEnumerable<Export> exports)
        {
            ContractBasedImportDefinition contractBasedImportDefinition = (ContractBasedImportDefinition)definition;
            Assert.Equal("Import:" + contractBasedImportDefinition.ContractName, CallOrder.Dequeue());
            base.SetImport(definition, exports);
        }

        public override void Activate()
        {
            Assert.Equal("OnComposed", CallOrder.Dequeue());
            base.Activate();
        }
    }

    internal class TestExportBinder : ConcreteComposablePart
    {
        public TestExportBinder()
        {
            AddExport("single", 42);
            AddExport("multi", 1);
            AddExport("multi", 2);
            AddExport("multi", 3);
        }
    }

    internal class TestImportBinder : ConcreteComposablePart
    {
        public TestImportBinder()
        {
            AddImport("single", ImportCardinality.ExactlyOne, true, false);
            AddImport("multi", ImportCardinality.ZeroOrMore, true, false);
        }
    }

    public class Foo
    {
        public Foo(IBar bar)
        {
            Bar = bar;
        }

        public IBar Bar { get; private set; }
    }

    public interface IBar
    {
        string Value { get; }
    }

    public class Bar : IBar
    {
        public Bar(string value)
        {
            Value = value;
        }
        public string Value { get; private set; }
    }

    public class FooBar
    {
        [Import("Foo")]
        public Foo Foo { get; set; }
    }

    public class AClass
    {
        public AClass(BClass b)
        {
        }

        public BClass B { get; private set; }
    }

    public class BClass
    {
        public BClass(AClass a)
        {
            this.A = a;
        }

        public AClass A { get; private set; }
    }

    internal class ConstructorInjectionComposablePart : ConcreteComposablePart
    {
        private Type _type;
        private ConstructorInfo _constructor;
        private Dictionary<ImportDefinition, object> _imports;

        public ConstructorInjectionComposablePart(Type type)
        {
            this._type = type;

            // Note that this just blindly takes the first constructor...
            this._constructor = this._type.GetConstructors().FirstOrDefault();
            Assert.NotNull(this._constructor);

            foreach (var param in this._constructor.GetParameters())
            {
                string name = AttributedModelServices.GetContractName(param.ParameterType);
                AddImport(name, ImportCardinality.ExactlyOne, true, true);
            }

            string contractName = AttributedModelServices.GetContractName(type);
            string typeIdentity = AttributedModelServices.GetTypeIdentity(type);
            var metadata = new Dictionary<string, object>();
            metadata.Add(CompositionConstants.ExportTypeIdentityMetadataName, typeIdentity);
            
            Export composableExport = ExportFactory.Create(
                contractName,
                metadata,
                GetInstance);
            this.AddExport(composableExport);

            this._imports = new Dictionary<ImportDefinition, object>();
        }

        private object GetInstance()
        {
            var result = CompositionResult.SucceededResult;

            // We only need this guard if we are pulling on the lazy exports during this call
            // but if we do the pulling in SetImport it isn't needed.
            //if (currentlyExecuting)
            //{
            //    var issue = CompositionError.Create("CM:CreationCycleDetected",
            //        "This failed because there is a creation cycle");
            //    return result.MergeIssue(issue).ToResult<object>(null);
            //}

            try
            {
                List<object> constructorArgs = new List<object>();

                foreach (ImportDefinition import in this.ImportDefinitions
                    .Where(i => i.IsPrerequisite))
                {
                    object importValue;
                    if (!this._imports.TryGetValue(import, out importValue))
                    {
                        result = result.MergeError(CompositionError.Create(CompositionErrorId.ImportNotSetOnPart,
                            "The import '{0}' is required for construction of '{1}'", import.ToString(), _type.FullName));

                        continue;
                    }

                    constructorArgs.Add(importValue);
                }

                if (!result.Succeeded)
                {
                    throw new CompositionException(result.Errors);
                }

                object obj = this._constructor.Invoke(constructorArgs.ToArray());

                return obj;
            }
            finally
            {
            }
        }

        public override void SetImport(ImportDefinition definition, IEnumerable<Export> exports)
        {
            _imports[definition] = exports.First().Value;
        }
    }
}

