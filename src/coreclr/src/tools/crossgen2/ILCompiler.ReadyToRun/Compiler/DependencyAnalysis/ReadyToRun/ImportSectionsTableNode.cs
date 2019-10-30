// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class ImportSectionsTableNode : ArrayOfEmbeddedDataNode<ImportSectionNode>, ISignatureEmitter
    {
        private readonly ReadyToRunCodegenNodeFactory _r2rFactory;

        private bool _materializedSignature;

        public ImportSectionsTableNode(ReadyToRunCodegenNodeFactory r2rFactory)
            : base("ImportSectionsTableStart", "ImportSectionsTableEnd", null)
        {
            _r2rFactory = r2rFactory;
            _r2rFactory.ManifestMetadataTable.RegisterEmitter(this);
        }

        public void MaterializeSignature()
        {
            if (!_materializedSignature)
            {
                foreach (ImportSectionNode importSection in NodesList)
                {
                    importSection.MaterializeSignature(_r2rFactory);
                }
                _materializedSignature = true;
            }
        }

        protected override void GetElementDataForNodes(ref ObjectDataBuilder builder, NodeFactory factory, bool relocsOnly)
        {
            builder.RequireInitialPointerAlignment();
            int index = 0;
            foreach (ImportSectionNode node in NodesList)
            {
                if (!relocsOnly)
                {
                    node.InitializeOffsetFromBeginningOfArray(builder.CountBytes);
                    node.InitializeIndexFromBeginningOfArray(index++);
                }

                node.EncodeData(ref builder, factory, relocsOnly);
                if (node is ISymbolDefinitionNode symbolDef)
                {
                    builder.AddSymbol(symbolDef);
                }
            }
        }

        public override int ClassCode => 787556329;
    }
}
