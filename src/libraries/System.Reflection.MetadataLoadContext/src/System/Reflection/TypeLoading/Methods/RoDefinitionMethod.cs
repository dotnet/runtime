// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.TypeLoading
{
    /// <summary>
    /// Base class for all RoMethod objects created by a MetadataLoadContext that has a MethodDef token associated with it
    /// and for which IsConstructedGenericMethod returns false.
    /// </summary>
    internal abstract class RoDefinitionMethod : RoMethod
    {
        protected RoDefinitionMethod(Type reflectedType)
            : base(reflectedType)
        {
            Debug.Assert(reflectedType != null);
        }

        internal abstract MethodSig<RoParameter> SpecializeMethodSig(IRoMethodBase member);
        internal abstract MethodSig<RoType> SpecializeCustomModifiers(in TypeContext typeContext);
        internal abstract MethodSig<string> SpecializeMethodSigStrings(in TypeContext typeContext);
        internal abstract MethodBody? SpecializeMethodBody(IRoMethodBase owner);
    }

    /// <summary>
    /// Class for all RoMethod objects created by a MetadataLoadContext that has a MethodDef token associated with it
    /// and for which IsConstructedGenericMethod returns false.
    /// </summary>
    internal sealed partial class RoDefinitionMethod<TMethodDecoder> : RoDefinitionMethod where TMethodDecoder : IMethodDecoder
    {
        private readonly RoInstantiationProviderType _declaringType;
        private readonly TMethodDecoder _decoder;

        internal RoDefinitionMethod(RoInstantiationProviderType declaringType, Type reflectedType, TMethodDecoder decoder)
            : base(reflectedType)
        {
            Debug.Assert(declaringType != null);
            _declaringType = declaringType;
            _decoder = decoder;
        }

        internal sealed override RoType GetRoDeclaringType() => _declaringType;
        internal sealed override RoModule GetRoModule() => _decoder.GetRoModule();

        protected sealed override string ComputeName() => _decoder.ComputeName();
        public sealed override int MetadataToken => _decoder.MetadataToken;

        public sealed override IEnumerable<CustomAttributeData> CustomAttributes
        {
            get
            {
                foreach (CustomAttributeData cad in _decoder.ComputeTrueCustomAttributes())
                    yield return cad;

                if ((MethodImplementationFlags & MethodImplAttributes.PreserveSig) != 0)
                {
                    ConstructorInfo? ci = Loader.TryGetPreserveSigCtor();
                    if (ci != null)
                        yield return new RoPseudoCustomAttributeData(ci);
                }

                CustomAttributeData? dllImportCustomAttribute = ComputeDllImportCustomAttributeDataIfAny();
                if (dllImportCustomAttribute != null)
                    yield return dllImportCustomAttribute;
            }
        }

        protected sealed override MethodAttributes ComputeAttributes() => _decoder.ComputeAttributes();
        protected sealed override CallingConventions ComputeCallingConvention() => _decoder.ComputeCallingConvention();
        protected sealed override MethodImplAttributes ComputeMethodImplementationFlags() => _decoder.ComputeMethodImplementationFlags();
        protected sealed override MethodSig<RoParameter> ComputeMethodSig() => _decoder.SpecializeMethodSig(this);
        public sealed override MethodBody? GetMethodBody() => _decoder.SpecializeMethodBody(this);
        protected sealed override MethodSig<string> ComputeMethodSigStrings() => _decoder.SpecializeMethodSigStrings(TypeContext);
        protected sealed override MethodSig<RoType> ComputeCustomModifiers() => _decoder.SpecializeCustomModifiers(TypeContext);

        public sealed override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (!(obj is RoDefinitionMethod<TMethodDecoder> other))
                return false;

            if (MetadataToken != other.MetadataToken)
                return false;

            if (DeclaringType != other.DeclaringType)
                return false;

            if (ReflectedType != other.ReflectedType)
                return false;

            return true;
        }

        public sealed override int GetHashCode() => MetadataToken.GetHashCode() ^ DeclaringType.GetHashCode();

        public sealed override bool IsConstructedGenericMethod => false;
        public sealed override bool IsGenericMethodDefinition => GetGenericTypeParametersNoCopy().Length != 0;
        public sealed override MethodInfo GetGenericMethodDefinition() => IsGenericMethodDefinition ? this : throw new InvalidOperationException(); // Very uninformative but compatible exception

        [RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
        public sealed override MethodInfo MakeGenericMethod(params Type[] typeArguments)
        {
            if (typeArguments == null)
                throw new ArgumentNullException(nameof(typeArguments));

            if (!IsGenericMethodDefinition)
                throw new InvalidOperationException(SR.Format(SR.Arg_NotGenericMethodDefinition, this));

            int count = typeArguments.Length;
            RoType[] roTypeArguments = new RoType[count];
            for (int i = 0; i < count; i++)
            {
                Type typeArgument = typeArguments[i];
                if (typeArgument == null)
                    throw new ArgumentNullException();

                if (!(typeArgument is RoType roTypeArgument && roTypeArgument.Loader == Loader))
                    throw new ArgumentException(SR.Format(SR.MakeGenericType_NotLoadedByMetadataLoadContext, typeArgument));

                roTypeArguments[i] = roTypeArgument;
            }

            if (count != GetGenericTypeParametersNoCopy().Length)
                throw new ArgumentException(SR.Argument_GenericArgsCount, nameof(typeArguments));

            return new RoConstructedGenericMethod(this, roTypeArguments);
        }

        internal sealed override RoType[] GetGenericTypeArgumentsNoCopy() => Array.Empty<RoType>();
        internal sealed override RoType[] GetGenericTypeParametersNoCopy() => GetGenericArgumentsOrParametersNoCopy();

        protected sealed override RoType[] ComputeGenericArgumentsOrParameters() => _decoder.ComputeGenericArgumentsOrParameters();

        // Used by RoConstructedGenericMethod to construct instantiated versions of method properties.
        internal sealed override MethodSig<RoParameter> SpecializeMethodSig(IRoMethodBase member) => _decoder.SpecializeMethodSig(member);
        internal sealed override MethodSig<RoType> SpecializeCustomModifiers(in TypeContext typeContext) => _decoder.SpecializeCustomModifiers(typeContext);
        internal sealed override MethodSig<string> SpecializeMethodSigStrings(in TypeContext typeContext) => _decoder.SpecializeMethodSigStrings(typeContext);
        internal sealed override MethodBody? SpecializeMethodBody(IRoMethodBase owner) => _decoder.SpecializeMethodBody(owner);

        public sealed override TypeContext TypeContext => new TypeContext(_declaringType.Instantiation, GetGenericTypeParametersNoCopy());
    }
}
