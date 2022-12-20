// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Cts = Internal.TypeSystem;
using Debug = System.Diagnostics.Debug;

namespace ILCompiler.Metadata
{
    /// <summary>
    /// Provides implementation of the <see cref="MetadataTransform"/> contract.
    /// This class is generic over the policy to make policy lookups cheap (policy being
    /// a struct means all the interface calls end up being constrained over the type
    /// and therefore fully inlineable).
    /// </summary>
    internal sealed partial class Transform<TPolicy> : MetadataTransform
        where TPolicy : struct, IMetadataPolicy
    {
        private TPolicy _policy;

        public Transform(TPolicy policy)
        {
            _policy = policy;
        }

        private bool IsBlocked(Cts.TypeDesc type)
        {
            switch (type.Category)
            {
                case Cts.TypeFlags.SzArray:
                case Cts.TypeFlags.Array:
                case Cts.TypeFlags.Pointer:
                case Cts.TypeFlags.ByRef:
                    return IsBlocked(((Cts.ParameterizedType)type).ParameterType);

                case Cts.TypeFlags.SignatureMethodVariable:
                case Cts.TypeFlags.SignatureTypeVariable:
                    return false;

                case Cts.TypeFlags.FunctionPointer:
                    {
                        Cts.MethodSignature pointerSignature = ((Cts.FunctionPointerType)type).Signature;

                        for (int i = 0; i < pointerSignature.Length; i++)
                            if (IsBlocked(pointerSignature[i]))
                                return true;

                        return IsBlocked(pointerSignature.ReturnType);
                    }
                default:
                    Debug.Assert(type.IsDefType);

                    if (!type.IsTypeDefinition)
                    {
                        if (IsBlocked(type.GetTypeDefinition()))
                            return true;

                        foreach (var arg in type.Instantiation)
                            if (IsBlocked(arg))
                                return true;

                        return false;
                    }

                    return _policy.IsBlocked((Cts.MetadataType)type);
            }
        }
    }
}
