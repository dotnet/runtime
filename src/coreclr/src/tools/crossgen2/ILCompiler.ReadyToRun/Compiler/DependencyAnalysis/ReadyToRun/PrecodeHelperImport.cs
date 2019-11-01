// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Internal.Text;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// This class represents a single indirection cell resolved using fixup table
    /// at function startup.
    /// </summary>
    public class PrecodeHelperImport : Import
    {
        public PrecodeHelperImport(ReadyToRunCodegenNodeFactory factory, Signature signature)
            : base(factory.PrecodeImports, signature)
        {
        }

        protected override string GetName(NodeFactory factory)
        {
            return "PrecodeHelperImport->" + ImportSignature.GetMangledName(factory.NameMangler);
        }

        public override int ClassCode => 667823013;

        public override void EncodeData(ref ObjectDataBuilder dataBuilder, NodeFactory factory, bool relocsOnly)
        {
            // This needs to be an empty target pointer since it will be filled in with Module*
            // when loaded by CoreCLR
            dataBuilder.EmitZeroPointer();
        }
    }
}
