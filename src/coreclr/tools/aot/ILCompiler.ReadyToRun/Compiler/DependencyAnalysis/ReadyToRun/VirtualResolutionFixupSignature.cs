// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
    public class VirtualResolutionFixupSignature : Signature, IEquatable<VirtualResolutionFixupSignature>
    {
        private readonly ReadyToRunFixupKind _fixupKind;

        private readonly MethodWithToken _declMethod;
        private readonly TypeDesc _implType;
        private readonly MethodWithToken _implMethod;

        public VirtualResolutionFixupSignature(ReadyToRunFixupKind fixupKind, MethodWithToken declMethod, TypeDesc implType, MethodWithToken implMethod)
        {
            _fixupKind = fixupKind;
            _declMethod = declMethod;
            _implType = implType;
            _implMethod = implMethod;

            // Ensure types in signature are loadable and resolvable, otherwise we'll fail later while emitting the signature
            CompilerTypeSystemContext compilerContext = (CompilerTypeSystemContext)declMethod.Method.Context;
            compilerContext.EnsureLoadableMethod(declMethod.Method);
            compilerContext.EnsureLoadableType(implType);
            if (implMethod != null)
                compilerContext.EnsureLoadableMethod(implMethod.Method);
        }

        public override int ClassCode => 1092747257;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataSignatureBuilder dataBuilder = new ObjectDataSignatureBuilder();

            if (!relocsOnly)
            {
                dataBuilder.AddSymbol(this);

                SignatureContext innerContext = dataBuilder.EmitFixup(factory, _fixupKind, _declMethod.Token.Module, factory.SignatureContext);
                dataBuilder.EmitUInt((uint)(_implMethod != null ? ReadyToRunVirtualFunctionOverrideFlags.VirtualFunctionOverriden : ReadyToRunVirtualFunctionOverrideFlags.None));
                dataBuilder.EmitMethodSignature(_declMethod, enforceDefEncoding: false, enforceOwningType: false, innerContext, isInstantiatingStub: false);
                dataBuilder.EmitTypeSignature(_implType, innerContext);
                if (_implMethod != null)
                    dataBuilder.EmitMethodSignature(_implMethod, enforceDefEncoding: false, enforceOwningType: false, innerContext, isInstantiatingStub: false);
            }

            return dataBuilder.ToObjectData();
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append($@"VirtualResolutionFixupSignature({_fixupKind.ToString()}): ");
            _declMethod.AppendMangledName(nameMangler, sb);
            sb.Append(":");
            sb.Append(nameMangler.GetMangledTypeName(_implType));
            sb.Append(":");
            if (_implMethod == null)
                sb.Append("(null)");
            else
                _implMethod.AppendMangledName(nameMangler, sb);
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            VirtualResolutionFixupSignature otherNode = (VirtualResolutionFixupSignature)other;
            int result = ((int)_fixupKind).CompareTo((int)otherNode._fixupKind);
            if (result != 0)
                return result;

            result = comparer.Compare(_implType, otherNode._implType);
            if (result != 0)
                return result;

            result = _declMethod.CompareTo(otherNode._declMethod, comparer);
            if (result != 0)
                return result;

            // Handle null _implMethod scenario
            if (_implMethod == otherNode._implMethod)
                return 0;

            return _implMethod.CompareTo(otherNode._implMethod, comparer);
        }

        public override string ToString()
        {
            return $"VirtualResolutionFixupSignature {_fixupKind} {_declMethod} {_implType} {_implMethod}";
        }

        public bool Equals(VirtualResolutionFixupSignature other) => object.ReferenceEquals(other, this);
    }
}
