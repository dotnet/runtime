// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class StringImport : Import
    {
        private readonly ModuleToken _token;

        public StringImport(ImportSectionNode table, ModuleToken token)
            : base(table, new StringImportSignature(token))
        {
            _token = token;
        }

        public override int ClassCode => 59575119;

        public override void EncodeData(ref ObjectDataBuilder dataBuilder, NodeFactory factory, bool relocsOnly)
        {
            // This needs to be an empty target pointer since it will be filled in with the string pointer
            // when loaded by CoreCLR
            dataBuilder.EmitZeroPointer();
        }

        public override bool RepresentsIndirectionCell => true;

        protected override string GetName(NodeFactory context)
        {
            return "StringCell: " + _token.ToString();
        }

        // This is just here in case of future extension (_token is already compared in the base CompareToImpl)
        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            return base.CompareToImpl(other, comparer);
        }
    }
}
