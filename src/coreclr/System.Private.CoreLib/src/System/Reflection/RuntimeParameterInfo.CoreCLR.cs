// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using MdToken = System.Reflection.MetadataToken;

namespace System.Reflection
{
    internal sealed partial class RuntimeParameterInfo : ParameterInfo
    {
        #region Private Data Members
        private readonly int m_tkParamDef;
        private readonly MetadataImport m_scope;
        private readonly Signature? m_signature;
        #endregion

        #region Static Members
        internal static ParameterInfo[] GetParameters(IRuntimeMethodInfo method, MemberInfo member, Signature sig)
        {
            Debug.Assert(method is RuntimeMethodInfo || method is RuntimeConstructorInfo);

            return GetParameters(method, member, sig, out _, fetchReturnParameter: false);
        }

        internal static ParameterInfo GetReturnParameter(IRuntimeMethodInfo method, MemberInfo member, Signature sig)
        {
            Debug.Assert(method is RuntimeMethodInfo || method is RuntimeConstructorInfo);

            GetParameters(method, member, sig, out ParameterInfo? returnParameter, fetchReturnParameter: true);
            return returnParameter!;
        }

        private static ParameterInfo[] GetParameters(
            IRuntimeMethodInfo methodHandle, MemberInfo member, Signature sig, out ParameterInfo? returnParameter, bool fetchReturnParameter)
        {
            // The lifetime rules for MetadataImport expect these two objects to be the same instance.
            // See the lifetime of MetadataImport, acquired through IRuntimeMethodInfo, but extended
            // through the MemberInfo instance.
            Debug.Assert(ReferenceEquals(methodHandle, member));

            returnParameter = null;
            int sigArgCount = sig.Arguments.Length;
            ParameterInfo[] args =
                fetchReturnParameter ? null! :
                sigArgCount == 0 ? [] :
                new ParameterInfo[sigArgCount];

            int tkMethodDef = RuntimeMethodHandle.GetMethodDef(methodHandle);
            int cParamDefs = 0;

            // Not all methods have tokens. Arrays, pointers and byRef types do not have tokens as they
            // are generated on the fly by the runtime.
            if (!MdToken.IsNullToken(tkMethodDef))
            {
                MetadataImport scope = RuntimeMethodHandle.GetDeclaringType(methodHandle).GetRuntimeModule().MetadataImport;

                scope.EnumParams(tkMethodDef, out MetadataEnumResult tkParamDefs);

                cParamDefs = tkParamDefs.Length;

                // Not all parameters have tokens. Parameters may have no token
                // if they have no name and no attributes.
                if (cParamDefs > sigArgCount + 1 /* return type */)
                    throw new BadImageFormatException(SR.BadImageFormat_ParameterSignatureMismatch);

                for (int i = 0; i < cParamDefs; i++)
                {
                    #region Populate ParameterInfos
                    int tkParamDef = tkParamDefs[i];

                    scope.GetParamDefProps(tkParamDef, out int position, out ParameterAttributes attr);

                    position--;

                    if (fetchReturnParameter && position == -1)
                    {
                        // more than one return parameter?
                        if (returnParameter != null)
                            throw new BadImageFormatException(SR.BadImageFormat_ParameterSignatureMismatch);

                        returnParameter = new RuntimeParameterInfo(sig, scope, tkParamDef, position, attr, member);
                    }
                    else if (!fetchReturnParameter && position >= 0)
                    {
                        // position beyond sigArgCount?
                        if (position >= sigArgCount)
                            throw new BadImageFormatException(SR.BadImageFormat_ParameterSignatureMismatch);

                        args[position] = new RuntimeParameterInfo(sig, scope, tkParamDef, position, attr, member);
                    }
                    #endregion
                }
            }

            // Fill in empty ParameterInfos for those without tokens
            if (fetchReturnParameter)
            {
                returnParameter ??= new RuntimeParameterInfo(sig, default, 0, -1, (ParameterAttributes)0, member);
            }
            else
            {
                if (cParamDefs < args.Length + 1)
                {
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (args[i] != null)
                            continue;

                        args[i] = new RuntimeParameterInfo(sig, default, 0, i, (ParameterAttributes)0, member);
                    }
                }
            }

            return args;
        }
        #endregion

        #region Constructor
        private RuntimeParameterInfo(
            Signature signature, MetadataImport scope, int tkParamDef,
            int position, ParameterAttributes attributes, MemberInfo member)
        {
            Debug.Assert(member != null);
            Debug.Assert(MdToken.IsNullToken(tkParamDef) == scope.Equals((MetadataImport)default));
            Debug.Assert(MdToken.IsNullToken(tkParamDef) || MdToken.IsTokenOfType(tkParamDef, MetadataTokenType.ParamDef));

            PositionImpl = position;
            MemberImpl = member;
            m_signature = signature;
            m_tkParamDef = MdToken.IsNullToken(tkParamDef) ? (int)MetadataTokenType.ParamDef : tkParamDef;
            m_scope = scope;
            AttrsImpl = attributes;

            ClassImpl = null;
            NameImpl = null;
        }

        // ctor for no metadata MethodInfo in the DynamicMethod and RuntimeMethodInfo cases
        internal RuntimeParameterInfo(MethodInfo owner, string? name, Type parameterType, int position)
        {
            MemberImpl = owner;
            NameImpl = name;
            m_nameIsCached = true;
            m_noMetadata = true;
            ClassImpl = parameterType;
            PositionImpl = position;
            AttrsImpl = ParameterAttributes.None;
            m_tkParamDef = (int)MetadataTokenType.ParamDef;
            m_scope = default;
        }
        #endregion

        #region Public Methods
        internal RuntimeModule? GetRuntimeModule()
        {
            RuntimeMethodInfo? method = Member as RuntimeMethodInfo;
            RuntimeConstructorInfo? constructor = Member as RuntimeConstructorInfo;
            RuntimePropertyInfo? property = Member as RuntimePropertyInfo;

            if (method != null)
                return method.GetRuntimeModule();
            else if (constructor != null)
                return constructor.GetRuntimeModule();
            else if (property != null)
                return property.GetRuntimeModule();
            else
                return null;
        }

        public override int MetadataToken => m_tkParamDef;

        public override Type[] GetRequiredCustomModifiers()
        {
            return m_signature is null ? [] : m_signature.GetCustomModifiers(PositionImpl + 1, true);
        }

        public override Type[] GetOptionalCustomModifiers()
        {
            return m_signature is null ? [] : m_signature.GetCustomModifiers(PositionImpl + 1, false);
        }

        public override Type GetModifiedParameterType() =>
            ModifiedType.Create(unmodifiedType: ParameterType, m_signature, parameterIndex: PositionImpl + 1);
        #endregion
    }
}
