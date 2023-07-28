// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.Text;
using Internal.TypeSystem;
using Internal.ReadyToRunConstants;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// This class represents a single indirection cell used to call delay load helpers.
    /// </summary>
    public class DelayLoadHelperImport : Import
    {
        private readonly ReadyToRunHelper _helper;

        private readonly bool _useVirtualCall;
        private readonly bool _useJumpableStub;

        private readonly ImportThunk _delayLoadHelper;

        public DelayLoadHelperImport(
            NodeFactory factory, 
            ImportSectionNode importSectionNode, 
            ReadyToRunHelper helper, 
            Signature instanceSignature, 
            bool useVirtualCall = false, 
            bool useJumpableStub = false,
            MethodDesc callingMethod = null)
            : base(importSectionNode, instanceSignature, callingMethod)
        {
            _helper = helper;
            _useVirtualCall = useVirtualCall;
            _useJumpableStub = useJumpableStub;
            _delayLoadHelper = factory.ImportThunk(helper, importSectionNode, useVirtualCall, useJumpableStub);
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("DelayLoadHelperImport(");
            if (_useVirtualCall)
            {
                sb.Append("[VSD] ");
            }
            if (_useJumpableStub)
            {
                sb.Append("[JMP] ");
            }
            sb.Append(_helper.ToString());
            sb.Append(") -> ");
            ImportSignature.AppendMangledName(nameMangler, sb);
            if (CallingMethod != null)
            {
                sb.Append(" @ ");
                sb.Append(nameMangler.GetMangledMethodName(CallingMethod));
            }
        }

        public override int ClassCode => 667823013;

        public override void EncodeData(ref ObjectDataBuilder dataBuilder, NodeFactory factory, bool relocsOnly)
        {
            // This needs to be an empty target pointer since it will be filled in with Module*
            // when loaded by CoreCLR
            dataBuilder.EmitReloc(_delayLoadHelper,
                factory.Target.PointerSize == 4 ? RelocType.IMAGE_REL_BASED_HIGHLOW : RelocType.IMAGE_REL_BASED_DIR64, factory.Target.CodeDelta);
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            return new DependencyListEntry[] 
            {
                new DependencyListEntry(_delayLoadHelper, "Delay load helper thunk for ready-to-run fixup import"),
                new DependencyListEntry(ImportSignature, "Signature for ready-to-run fixup import"),
            };
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            DelayLoadHelperImport otherNode = (DelayLoadHelperImport)other;
            int result = _useJumpableStub.CompareTo(otherNode._useJumpableStub);
            if (result != 0)
                return result;

            result = _useVirtualCall.CompareTo(otherNode._useVirtualCall);
            if (result != 0)
                return result;

            result = _helper.CompareTo(otherNode._helper);
            if (result != 0)
                return result;

            return base.CompareToImpl(other, comparer);
        }
    }
}
