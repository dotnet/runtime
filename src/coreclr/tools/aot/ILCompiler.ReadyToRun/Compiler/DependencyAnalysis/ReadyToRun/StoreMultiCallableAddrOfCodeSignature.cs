// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.ReadyToRunConstants;
using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// Signature node for the READYTORUN_FIXUP_StoreMultiCallableAddrOfCode fixup.
    ///
    /// This method-load-time fixup stores the runtime MultiCallableAddrOfCode of a target method
    /// into a location embedded in the R2R image (a slot in the compiled method's read-only data
    /// blob). It is used on WebAssembly where a callable code pointer cannot be materialized at
    /// compile time (the pointer representation is only known at runtime).
    ///
    /// The signature following the fixup kind byte is:
    ///   - target code RVA (4 bytes): points at the target method's code. On WebAssembly this is a
    ///     function-table index relative to the image (WASM_TABLE_INDEX_REL_I32); elsewhere an
    ///     imageBase RVA (IMAGE_REL_BASED_ADDR32NB). This matches the encoding used by
    ///     <see cref="ResumptionStubEntryPointSignature"/> so that the resulting entry point value
    ///     matches the one registered by the ResumptionStubEntryPoint fixup.
    ///   - location RVA (4 bytes): an IMAGE_REL_BASED_ADDR32NB RVA pointing at the location that the
    ///     runtime must overwrite with the MultiCallableAddrOfCode value.
    /// </summary>
    internal class StoreMultiCallableAddrOfCodeSignature : Signature
    {
        private readonly MethodWithGCInfo _target;
        private readonly ISymbolNode _location;
        private readonly int _locationOffset;

        public StoreMultiCallableAddrOfCodeSignature(MethodWithGCInfo target, ISymbolNode location, int locationOffset)
        {
            _target = target;
            _location = location;
            _locationOffset = locationOffset;
        }

        // The ClassCode must be greater than ResumptionStubEntryPointSignature.ClassCode (1927438562)
        // so that this fixup sorts (and therefore is processed) AFTER the ResumptionStubEntryPoint
        // fixup for the same method. That fixup registers the target (resumption stub) entry point so
        // that this fixup can resolve it to a MethodDesc at runtime. Imports within an import section
        // are ordered by CompilerComparer, which orders distinct node classes by ClassCode, and the
        // fixup blob is emitted in ascending import-offset order.
        public override int ClassCode => 1976543219;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataSignatureBuilder builder = new ObjectDataSignatureBuilder(factory, relocsOnly);
            builder.AddSymbol(this);
            builder.EmitByte((byte)ReadyToRunFixupKind.StoreMultiCallableAddrOfCode);

            // On wasm the target code is a function-table index (WASM_TABLE_INDEX_REL_I32); elsewhere
            // an imageBase RVA. Keep this in sync with ResumptionStubEntryPointSignature.
            RelocType codeRelocType = factory.Target.Architecture == TargetArchitecture.Wasm32
                ? RelocType.WASM_TABLE_INDEX_REL_I32
                : RelocType.IMAGE_REL_BASED_ADDR32NB;
            builder.EmitReloc(_target, codeRelocType);

            // The location to update is always a data address expressed as an imageBase RVA.
            builder.EmitReloc(_location, RelocType.IMAGE_REL_BASED_ADDR32NB, delta: _locationOffset);

            return builder.ToObjectData();
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append("StoreMultiCallableAddrOfCode_"u8);
            sb.Append(nameMangler.GetMangledMethodName(_target.Method));
            sb.Append("_"u8);
            _location.AppendMangledName(nameMangler, sb);
            sb.Append("+"u8);
            sb.Append(_locationOffset);
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            StoreMultiCallableAddrOfCodeSignature otherNode = (StoreMultiCallableAddrOfCodeSignature)other;
            int result = comparer.Compare(_target.Method, otherNode._target.Method);
            if (result != 0)
                return result;

            result = comparer.Compare((ISortableNode)_location, (ISortableNode)otherNode._location);
            if (result != 0)
                return result;

            return _locationOffset.CompareTo(otherNode._locationOffset);
        }
    }
}
