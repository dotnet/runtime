// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Internal.Text;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// This class represents a single indirection cell used to call delay load helpers.
    /// </summary>
    public class DelayLoadHelperImport : Import
    {
        private readonly ReadyToRunHelper _helper;

        private readonly bool _useVirtualCall;

        private readonly ImportThunk _delayLoadHelper;

        public DelayLoadHelperImport(
            ReadyToRunCodegenNodeFactory factory, 
            ImportSectionNode importSectionNode, 
            ReadyToRunHelper helper, 
            Signature instanceSignature, 
            bool useVirtualCall = false, 
            string callSite = null)
            : base(importSectionNode, instanceSignature, callSite)
        {
            _helper = helper;
            _useVirtualCall = useVirtualCall;
            _delayLoadHelper = new ImportThunk(helper, factory, this, useVirtualCall);
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("DelayLoadHelperImport(");
            if (_useVirtualCall)
            {
                sb.Append("[VSD] ");
            }
            sb.Append(_helper.ToString());
            sb.Append(") -> ");
            ImportSignature.AppendMangledName(nameMangler, sb);
            if (CallSite != null)
            {
                sb.Append(" @ ");
                sb.Append(CallSite);
            }
        }

        public override int ClassCode => 667823013;

        public override void EncodeData(ref ObjectDataBuilder dataBuilder, NodeFactory factory, bool relocsOnly)
        {
            // This needs to be an empty target pointer since it will be filled in with Module*
            // when loaded by CoreCLR
            dataBuilder.EmitReloc(_delayLoadHelper,
                factory.Target.PointerSize == 4 ? RelocType.IMAGE_REL_BASED_HIGHLOW : RelocType.IMAGE_REL_BASED_DIR64);
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            return new DependencyListEntry[] 
            {
                new DependencyListEntry(_delayLoadHelper, "Delay load helper thunk for ready-to-run fixup import"),
                new DependencyListEntry(ImportSignature, "Signature for ready-to-run fixup import"),
            };
        }
    }
}
