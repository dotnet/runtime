// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Internal.TypeSystem.Interop;
using Internal.ReadyToRunConstants;
using Internal.CorConstants;
using Internal.JitInterface;

using ILCompiler.ReadyToRun.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// This fixup instructs the runtime to validate that the IL found at runtime matches the hash of the IL computed at compile time.
    /// </summary>
    /// <remarks>
    /// The fixup encodes two distinct pieces of information that may come from different methods:
    /// <list type="bullet">
    /// <item><c>_ilMethod</c> (EcmaMethod): The source of the IL body from metadata, used to compute the standalone
    /// metadata hash that the runtime will validate against.</item>
    /// <item><c>_signatureMethod</c> (MethodDesc): The method identity encoded in the fixup signature. The runtime
    /// decodes this back to a MethodDesc via <c>ZapSig::DecodeMethod</c> and then reads the IL at that method's RVA
    /// to compare against the hash.</item>
    /// </list>
    /// For most methods these are the same EcmaMethod. They differ for runtime-async methods: the JIT inlines an
    /// <c>AsyncMethodVariant</c> (which is a <c>MethodDelegator</c>, not an EcmaMethod), but the IL body lives on the
    /// underlying EcmaMethod. Since <c>AsyncMethodVariant</c> cannot be encoded as a MethodDef token, both
    /// <c>_ilMethod</c> and <c>_signatureMethod</c> point to the same EcmaMethod, and the runtime side
    /// (see <c>GetILHeaderForStandaloneMetadata</c> in readytorunstandalonemethodmetadata.cpp) handles the fact that
    /// the decoded MethodDesc is marked as an async thunk whose <c>MayHaveILHeader()</c> returns false.
    /// </remarks>
    public class ILBodyFixupSignature : Signature, IEquatable<ILBodyFixupSignature>
    {
        private readonly ReadyToRunFixupKind _fixupKind;

        /// <summary>The EcmaMethod whose IL body from metadata is hashed for the fixup validation.</summary>
        private readonly EcmaMethod _ilMethod;

        /// <summary>The method identity encoded in the fixup signature that the runtime decodes to locate the IL at its RVA.</summary>
        private readonly MethodDesc _signatureMethod;

        public ILBodyFixupSignature(ReadyToRunFixupKind fixupKind, EcmaMethod ilMethod, MethodDesc signatureMethod)
        {
            Debug.Assert(signatureMethod.IsMethodDefinition);
            Debug.Assert(signatureMethod.GetPrimaryMethodDesc() == ilMethod);
            _fixupKind = fixupKind;
            _ilMethod = ilMethod;
            _signatureMethod = signatureMethod;
        }

        public ILBodyFixupSignature(ReadyToRunFixupKind fixupKind, EcmaMethod ecmaMethod)
            : this(fixupKind, ecmaMethod, ecmaMethod)
        {
        }

        public override int ClassCode => 308579267;

        protected override void OnMarked(NodeFactory context)
        {
            context.AddMarkedILBodyFixupSignature(this);
        }

        public static void NotifyComplete(NodeFactory factory, List<ILBodyFixupSignature> completeListOfSigs)
        {
            completeListOfSigs.MergeSort(new ObjectNodeComparer(CompilerComparer.Instance));
            foreach (var ilbodyFixupSig in completeListOfSigs)
            {
                ilbodyFixupSig.GetModuleToken(factory);
            }
        }

        private ModuleToken GetModuleToken(NodeFactory factory)
        {
            if (factory.CompilationModuleGroup.VersionsWithMethodBody(_ilMethod))
                return new ModuleToken(_ilMethod.Module, _ilMethod.Handle);
            else
                return new ModuleToken(factory.ManifestMetadataTable._mutableModule, factory.ManifestMetadataTable._mutableModule.TryGetEntityHandle(_ilMethod.GetTypicalMethodDefinition()).Value);
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataSignatureBuilder dataBuilder = new ObjectDataSignatureBuilder(factory, relocsOnly);

            if (!relocsOnly)
            {
                dataBuilder.AddSymbol(this);

                ModuleToken moduleToken = GetModuleToken(factory);

                IEcmaModule targetModule = moduleToken.Module;
                SignatureContext innerContext = dataBuilder.EmitFixup(factory, _fixupKind, targetModule, factory.SignatureContext);

                var metadata = ReadyToRunStandaloneMethodMetadata.Compute(_ilMethod);
                dataBuilder.EmitUInt(checked((uint)metadata.ConstantData.Length));
                dataBuilder.EmitBytes(metadata.ConstantData);
                dataBuilder.EmitUInt(checked((uint)metadata.TypeRefs.Length));
                foreach (var typeRef in metadata.TypeRefs)
                {
                    if (factory.SignatureContext.Resolver.GetModuleTokenForType((EcmaType)typeRef, allowDynamicallyCreatedReference: true, throwIfNotFound: false).Module == null)
                    {
                        // If there isn't a module token yet for this type, force it to exist
                        factory.ManifestMetadataTable._mutableModule.TryGetEntityHandle(typeRef);
                    }
                    dataBuilder.EmitTypeSignature(typeRef, innerContext);
                }

                MethodWithToken method = new MethodWithToken(_signatureMethod.GetTypicalMethodDefinition(), moduleToken, null, unboxing: false, context: null);
                dataBuilder.EmitMethodSignature(method, enforceDefEncoding: false, enforceOwningType: false, innerContext, false);
            }

            return dataBuilder.ToObjectData();
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append($@"ILBodyFixupSignature({_fixupKind.ToString()}): ");
            sb.Append(nameMangler.GetMangledMethodName(_ilMethod));
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            ILBodyFixupSignature otherNode = (ILBodyFixupSignature)other;
            int result = ((int)_fixupKind).CompareTo((int)otherNode._fixupKind);
            if (result != 0)
                return result;

            return comparer.Compare(_ilMethod, otherNode._ilMethod);
        }

        public override string ToString()
        {
            return $"ILBodyFixupSignature {_fixupKind} {_ilMethod}";
        }

        public bool Equals(ILBodyFixupSignature other) => object.ReferenceEquals(other, this);
    }
}
