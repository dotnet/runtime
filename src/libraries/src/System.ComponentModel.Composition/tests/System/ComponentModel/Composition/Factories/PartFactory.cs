// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Primitives;

namespace System.ComponentModel.Composition.Factories
{
    internal static partial class PartFactory
    {
        public static Type GetAttributedExporterType()
        {
            return typeof(ExportValueTypeFactory);
        }

        public static ComposablePart CreateAttributed(Type type)
        {
            var definition = PartDefinitionFactory.CreateAttributed(type);

            return definition.CreatePart();
        }

        public static ComposablePart CreateAttributed(object instance)
        {
            return AttributedModelServices.CreatePart(instance);
        }

        public static ComposablePart Create()
        {
            return new NoOverridesComposablePart();
        }

        public static ComposablePart CreateExporter(string contractName, object value)
        {
            return CreateExporter(new MicroExport(contractName, value));
        }

        public static ComposablePart CreateExporter(params MicroExport[] exports)
        {
            ConcreteComposablePart part = new ConcreteComposablePart();

            if (exports != null)
            {
                foreach (var export in exports)
                {
                    foreach (object exportedValue in export.ExportedValues)
                    {
                        part.AddExport(ExportFactory.Create(export.ContractName, exportedValue, export.Metadata));
                    }
                }
            }

            return part;
        }

        public static ImportingComposablePart CreateImporter<T>()
        {
            string contractName = AttributedModelServices.GetContractName(typeof(T));

            return CreateImporter(contractName);
        }

        public static ImportingComposablePart CreateImporterExporter(string exportContractName, string importContractName)
        {
            return new ImportingComposablePart(exportContractName, ImportCardinality.ExactlyOne, false, importContractName);
        }

        public static ImportingComposablePart CreateImporter(params string[] contractNames)
        {
            return new ImportingComposablePart(ImportCardinality.ZeroOrMore, false, contractNames);
        }

        public static ImportingComposablePart CreateImporter(bool isRecomposable, params string[] contractNames)
        {
            return new ImportingComposablePart(ImportCardinality.ZeroOrMore, isRecomposable, contractNames);
        }

        public static ImportingComposablePart CreateImporter(string contractName)
        {
            return CreateImporter(contractName, ImportCardinality.ZeroOrMore);
        }

        public static ImportingComposablePart CreateImporter(string contractName, bool isRecomposable)
        {
            return CreateImporter(contractName, ImportCardinality.ZeroOrMore, isRecomposable);
        }

        public static ImportingComposablePart CreateImporter(string contractName, ImportCardinality cardinality)
        {
            return CreateImporter(contractName, cardinality, false);
        }

        public static ImportingComposablePart CreateImporter(string contractName, ImportCardinality cardinality, bool isRecomposable)
        {
            return new ImportingComposablePart(cardinality, isRecomposable, contractName);
        }

        public static ImportingComposablePart CreateImporter(params ImportDefinition[] importDefinitions)
        {
            return new ImportingComposablePart(importDefinitions);
        }

        public static ComposablePart CreateDisposable(Action<bool> disposeCallback)
        {
            return new DisposableComposablePart(disposeCallback);
        }
    }
}
