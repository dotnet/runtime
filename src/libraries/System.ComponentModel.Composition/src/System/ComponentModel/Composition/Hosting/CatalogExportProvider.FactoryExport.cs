// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.ComponentModel.Composition.ReflectionModel;
using System.Linq;

namespace System.ComponentModel.Composition.Hosting
{
    public partial class CatalogExportProvider
    {
        internal abstract class FactoryExport : Export
        {
            private readonly ComposablePartDefinition _partDefinition;
            private readonly ExportDefinition _exportDefinition;
            private readonly ExportDefinition _factoryExportDefinition;
            private FactoryExportPartDefinition? _factoryExportPartDefinition;

            public FactoryExport(ComposablePartDefinition partDefinition, ExportDefinition exportDefinition)
            {
                _partDefinition = partDefinition;
                _exportDefinition = exportDefinition;
                _factoryExportDefinition = new PartCreatorExportDefinition(_exportDefinition);
            }

            public override ExportDefinition Definition
            {
                get { return _factoryExportDefinition; }
            }

            protected override object GetExportedValueCore()
            {
                if (_factoryExportPartDefinition == null)
                {
                    _factoryExportPartDefinition = new FactoryExportPartDefinition(this);
                }
                return _factoryExportPartDefinition;
            }

            protected ComposablePartDefinition UnderlyingPartDefinition
            {
                get
                {
                    return _partDefinition;
                }
            }

            protected ExportDefinition UnderlyingExportDefinition
            {
                get
                {
                    return _exportDefinition;
                }
            }

            public abstract Export CreateExportProduct();

            private sealed class FactoryExportPartDefinition : ComposablePartDefinition
            {
                private readonly FactoryExport _FactoryExport;

                public FactoryExportPartDefinition(FactoryExport FactoryExport)
                {
                    _FactoryExport = FactoryExport;
                }

                public override IEnumerable<ExportDefinition> ExportDefinitions
                {
                    get { return new ExportDefinition[] { _FactoryExport.Definition }; }
                }

                public override IEnumerable<ImportDefinition> ImportDefinitions
                {
                    get { return Enumerable.Empty<ImportDefinition>(); }
                }

                public ExportDefinition FactoryExportDefinition
                {
                    get { return _FactoryExport.Definition; }
                }

                public Export CreateProductExport()
                {
                    return _FactoryExport.CreateExportProduct();
                }

                public override ComposablePart CreatePart()
                {
                    return new FactoryExportPart(this);
                }
            }

            private sealed class FactoryExportPart : ComposablePart, IDisposable
            {
                private readonly FactoryExportPartDefinition _definition;
                private readonly Export _export;

                public FactoryExportPart(FactoryExportPartDefinition definition)
                {
                    _definition = definition;
                    _export = definition.CreateProductExport();
                }

                public override IEnumerable<ExportDefinition> ExportDefinitions
                {
                    get { return _definition.ExportDefinitions; }
                }

                public override IEnumerable<ImportDefinition> ImportDefinitions
                {
                    get { return _definition.ImportDefinitions; }
                }

                public override object? GetExportedValue(ExportDefinition definition)
                {
                    if (definition != _definition.FactoryExportDefinition)
                    {
                        throw ExceptionBuilder.CreateExportDefinitionNotOnThisComposablePart(nameof(definition));
                    }

                    return _export.Value;
                }

                public override void SetImport(ImportDefinition definition, IEnumerable<Export> exports)
                {
                    throw ExceptionBuilder.CreateImportDefinitionNotOnThisComposablePart(nameof(definition));
                }

                public void Dispose()
                {
                    if (_export is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }
        }
    }
}
