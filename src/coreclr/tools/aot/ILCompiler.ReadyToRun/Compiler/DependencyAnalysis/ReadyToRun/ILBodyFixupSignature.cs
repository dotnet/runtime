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
    /// The fixup encodes two distinct pieces of information:
    /// <list type="bullet">
    /// <item>The IL body hash, computed from the <see cref="ILMethod"/> (the underlying EcmaMethod whose metadata
    /// contains the IL). This is derived from _signatureMethod via GetPrimaryMethodDesc().</item>
    /// <item>The method identity (_signatureMethod), encoded in the fixup signature. The runtime decodes this
    /// back to a MethodDesc via ZapSig::DecodeMethod and reads the IL at that method's RVA to compare against
    /// the hash.</item>
    /// </list>
    /// For most methods, _signatureMethod is already the EcmaMethod. For runtime-async methods, the JIT inlines
    /// an AsyncMethodVariant. The EcmaMethod for AsyncMethodVariant can be retrieved with GetPrimaryMethodDesc().
    /// </remarks>
    public class ILBodyFixupSignature : Signature, IEquatable<ILBodyFixupSignature>
    {
        private readonly ReadyToRunFixupKind _fixupKind;

        /// <summary>The method identity encoded in the fixup signature that the runtime decodes to locate the IL at its RVA.</summary>
        private readonly MethodDesc _signatureMethod;

        /// <summary>The underlying EcmaMethod whose IL body from metadata is hashed for the fixup validation.</summary>
        private EcmaMethod ILMethod => (EcmaMethod)_signatureMethod.GetPrimaryMethodDesc();

        public ILBodyFixupSignature(ReadyToRunFixupKind fixupKind, MethodDesc signatureMethod)
        {
            Debug.Assert(signatureMethod.IsTypicalMethodDefinition);
            Debug.Assert(!signatureMethod.IsCompilerGeneratedILBodyForAsync());
            Debug.Assert(signatureMethod.GetPrimaryMethodDesc() is EcmaMethod);
            _fixupKind = fixupKind;
            _signatureMethod = signatureMethod;
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
            EcmaMethod ilMethod = ILMethod;
            if (factory.CompilationModuleGroup.VersionsWithMethodBody(ilMethod))
                return new ModuleToken(ilMethod.Module, ilMethod.Handle);
            else
                return new ModuleToken(factory.ManifestMetadataTable._mutableModule, factory.ManifestMetadataTable._mutableModule.TryGetEntityHandle(ilMethod.GetTypicalMethodDefinition()).Value);
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

                var metadata = ReadyToRunStandaloneMethodMetadata.Compute(ILMethod);
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

                MethodWithToken method = new MethodWithToken(_signatureMethod, moduleToken, null, unboxing: false, context: null);
                dataBuilder.EmitMethodSignature(method, enforceDefEncoding: false, enforceOwningType: false, innerContext, false);
            }

            return dataBuilder.ToObjectData();
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append($@"ILBodyFixupSignature({_fixupKind.ToString()}): ");
            sb.Append(nameMangler.GetMangledMethodName(ILMethod));
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            ILBodyFixupSignature otherNode = (ILBodyFixupSignature)other;
            int result = ((int)_fixupKind).CompareTo((int)otherNode._fixupKind);
            if (result != 0)
                return result;

            return comparer.Compare(_signatureMethod, otherNode._signatureMethod);
        }

        public override string ToString()
        {
            return $"ILBodyFixupSignature {_fixupKind} {_signatureMethod}";
        }

        public bool Equals(ILBodyFixupSignature other) => object.ReferenceEquals(other, this);
    }
}
