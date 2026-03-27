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

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// This fixup instructs the runtime to validate that the IL found at runtime matches the hash of the IL computed at compile time.
    /// The <c>ilMethod</c> provides the IL body from metadata for computing the standalone metadata hash.
    /// The <c>signatureMethod</c> is the method identity encoded in the fixup signature that the runtime will decode
    /// back to a MethodDesc. These are the same for most methods, but differ for runtime-async methods where the inlinee
    /// is an AsyncMethodVariant: the IL comes from the target EcmaMethod, while the signature must identify the same method
    /// so the runtime can locate the IL at the method's RVA.
    /// </summary>
    public class ILBodyFixupSignature : Signature, IEquatable<ILBodyFixupSignature>
    {
        private readonly ReadyToRunFixupKind _fixupKind;
        private readonly EcmaMethod _ilMethod;
        private readonly MethodDesc _signatureMethod;

        public ILBodyFixupSignature(ReadyToRunFixupKind fixupKind, EcmaMethod ilMethod, MethodDesc signatureMethod)
        {
            Debug.Assert(signatureMethod.IsMethodDefinition);
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

                MethodWithToken method = new MethodWithToken(_ilMethod, moduleToken, null, unboxing: false, context: null);
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
