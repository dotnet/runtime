// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.TypeLoading
{
    /// <summary>
    /// Base class for all RoMethod objects created by a MetadataLoadContext that appear on arrays.
    /// </summary>
    internal sealed partial class RoSyntheticMethod : RoMethod
    {
        private readonly RoType _declaringType;
        private readonly int _uniquifier;   // Since all array methods have the same "MetadataToken", this serves as a distinguisher so they don't all compare Equal
        private readonly string _name;
        private readonly RoType _returnType;
        private readonly RoType[] _parameterTypes;

        internal RoSyntheticMethod(RoType declaringType, int uniquifier, string name, RoType returnType, params RoType[] parameterTypes)
            : base(declaringType)
        {
            Debug.Assert(declaringType != null);
            _declaringType = declaringType;
            _uniquifier = uniquifier;
            _name = name;
            _returnType = returnType;
            _parameterTypes = parameterTypes;
        }

        internal sealed override RoType GetRoDeclaringType() => _declaringType;
        internal sealed override RoModule GetRoModule() => GetRoDeclaringType().GetRoModule();

        protected sealed override string ComputeName() => _name;
        public sealed override int MetadataToken => 0x06000000;
        public sealed override IEnumerable<CustomAttributeData> CustomAttributes => Array.Empty<CustomAttributeData>();
        protected sealed override MethodAttributes ComputeAttributes() => MethodAttributes.PrivateScope | MethodAttributes.Public;
        protected sealed override CallingConventions ComputeCallingConvention() => CallingConventions.Standard | CallingConventions.HasThis;
        protected sealed override MethodImplAttributes ComputeMethodImplementationFlags() => MethodImplAttributes.IL;

        protected sealed override MethodSig<RoParameter> ComputeMethodSig()
        {
            int parameterCount = _parameterTypes.Length;
            MethodSig<RoParameter> sig = new MethodSig<RoParameter>(parameterCount);
            sig[-1] = new RoThinMethodParameter(this, -1, _returnType);
            for (int position = 0; position < parameterCount; position++)
            {
                sig[position] = new RoThinMethodParameter(this, position, _parameterTypes[position]);
            }
            return sig;
        }

        public sealed override MethodBody? GetMethodBody() => null;

        protected sealed override MethodSig<string> ComputeMethodSigStrings()
        {
            int parameterCount = _parameterTypes.Length;
            MethodSig<string> sig = new MethodSig<string>(parameterCount);
            MethodSig<RoParameter> psig = ComputeMethodSig();
            for (int position = -1; position < parameterCount; position++)
            {
                sig[position] = psig[position].ParameterType.ToString();
            }
            return sig;
        }

        protected sealed override MethodSig<RoType> ComputeCustomModifiers() => new MethodSig<RoType>(_parameterTypes.Length);

        public sealed override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (!(obj is RoSyntheticMethod other))
                return false;

            if (DeclaringType != other.DeclaringType)
                return false;

            if (_uniquifier != other._uniquifier)
                return false;

            return true;
        }

        public sealed override int GetHashCode() => DeclaringType.GetHashCode() ^ _uniquifier.GetHashCode();

        public sealed override bool IsGenericMethodDefinition => false;
        public sealed override bool IsConstructedGenericMethod => false;
        public sealed override MethodInfo GetGenericMethodDefinition() => throw new InvalidOperationException();
        public sealed override MethodInfo MakeGenericMethod(params Type[] typeArguments) => throw new InvalidOperationException(SR.Format(SR.Arg_NotGenericMethodDefinition, this));
        protected sealed override RoType[] ComputeGenericArgumentsOrParameters() => Array.Empty<RoType>();
        internal sealed override RoType[] GetGenericTypeArgumentsNoCopy() => Array.Empty<RoType>();
        internal sealed override RoType[] GetGenericTypeParametersNoCopy() => Array.Empty<RoType>();

        public sealed override TypeContext TypeContext => default;
    }
}
