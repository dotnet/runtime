// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace System.Reflection.Emit
{
    public sealed class ConstructorBuilder : ConstructorInfo
    {
        private readonly MethodBuilder m_methodBuilder;
        internal bool m_isDefaultConstructor;

        #region Constructor

        internal ConstructorBuilder(string name, MethodAttributes attributes, CallingConventions callingConvention,
            Type[]? parameterTypes, Type[][]? requiredCustomModifiers, Type[][]? optionalCustomModifiers, ModuleBuilder mod, TypeBuilder type)
        {
            int sigLength;
            byte[] sigBytes;
            MethodToken token;

            m_methodBuilder = new MethodBuilder(name, attributes, callingConvention, null, null, null,
                parameterTypes, requiredCustomModifiers, optionalCustomModifiers, mod, type, false);

            type.m_listMethods.Add(m_methodBuilder);

            sigBytes = m_methodBuilder.GetMethodSignature().InternalGetSignature(out sigLength);

            token = m_methodBuilder.GetToken();
        }

        internal ConstructorBuilder(string name, MethodAttributes attributes, CallingConventions callingConvention,
            Type[]? parameterTypes, ModuleBuilder mod, TypeBuilder type) :
            this(name, attributes, callingConvention, parameterTypes, null, null, mod, type)
        {
        }

        #endregion

        #region Internal
        internal override Type[] GetParameterTypes()
        {
            return m_methodBuilder.GetParameterTypes();
        }

        private TypeBuilder GetTypeBuilder()
        {
            return m_methodBuilder.GetTypeBuilder();
        }
        #endregion

        #region Object Overrides
        public override string ToString()
        {
            return m_methodBuilder.ToString();
        }

        #endregion

        #region MemberInfo Overrides
        internal int MetadataTokenInternal
        {
            get { return m_methodBuilder.MetadataTokenInternal; }
        }

        public override Module Module
        {
            get { return m_methodBuilder.Module; }
        }

        public override Type? ReflectedType
        {
            get { return m_methodBuilder.ReflectedType; }
        }

        public override Type? DeclaringType
        {
            get { return m_methodBuilder.DeclaringType; }
        }

        public override string Name
        {
            get { return m_methodBuilder.Name; }
        }

        #endregion

        #region MethodBase Overrides
        public override object Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
        {
            throw new NotSupportedException(SR.NotSupported_DynamicModule);
        }

        public override ParameterInfo[] GetParameters()
        {
            ConstructorInfo rci = GetTypeBuilder().GetConstructor(m_methodBuilder.m_parameterTypes!)!;
            return rci.GetParameters();
        }

        public override MethodAttributes Attributes
        {
            get { return m_methodBuilder.Attributes; }
        }

        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            return m_methodBuilder.GetMethodImplementationFlags();
        }

        public override RuntimeMethodHandle MethodHandle
        {
            get { return m_methodBuilder.MethodHandle; }
        }

        #endregion

        #region ConstructorInfo Overrides
        public override object Invoke(BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
        {
            throw new NotSupportedException(SR.NotSupported_DynamicModule);
        }

        #endregion

        #region ICustomAttributeProvider Implementation
        public override object[] GetCustomAttributes(bool inherit)
        {
            return m_methodBuilder.GetCustomAttributes(inherit);
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return m_methodBuilder.GetCustomAttributes(attributeType, inherit);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return m_methodBuilder.IsDefined(attributeType, inherit);
        }

        #endregion

        #region Public Members
        public MethodToken GetToken()
        {
            return m_methodBuilder.GetToken();
        }

        public ParameterBuilder DefineParameter(int iSequence, ParameterAttributes attributes, string? strParamName)
        {
            // Theoretically we shouldn't allow iSequence to be 0 because in reflection ctors don't have 
            // return parameters. But we'll allow it for backward compatibility with V2. The attributes 
            // defined on the return parameters won't be very useful but won't do much harm either.

            // MD will assert if we try to set the reserved bits explicitly
            attributes = attributes & ~ParameterAttributes.ReservedMask;
            return m_methodBuilder.DefineParameter(iSequence, attributes, strParamName);
        }

        public ILGenerator GetILGenerator()
        {
            if (m_isDefaultConstructor)
                throw new InvalidOperationException(SR.InvalidOperation_DefaultConstructorILGen);

            return m_methodBuilder.GetILGenerator();
        }

        public ILGenerator GetILGenerator(int streamSize)
        {
            if (m_isDefaultConstructor)
                throw new InvalidOperationException(SR.InvalidOperation_DefaultConstructorILGen);

            return m_methodBuilder.GetILGenerator(streamSize);
        }

        public override CallingConventions CallingConvention
        {
            get
            {
                if (DeclaringType!.IsGenericType)
                    return CallingConventions.HasThis;

                return CallingConventions.Standard;
            }
        }

        public Module GetModule()
        {
            return m_methodBuilder.GetModule();
        }

#pragma warning disable CS8609 // TODO-NULLABLE: Covariant return types (https://github.com/dotnet/roslyn/issues/23268)
        // This always returns null. Is that what we want?
        internal override Type? GetReturnType()
        {
            return m_methodBuilder.ReturnType;
        }
#pragma warning restore CS8609

        public string Signature
        {
            get { return m_methodBuilder.Signature; }
        }

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            m_methodBuilder.SetCustomAttribute(con, binaryAttribute);
        }

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            m_methodBuilder.SetCustomAttribute(customBuilder);
        }

        public void SetImplementationFlags(MethodImplAttributes attributes)
        {
            m_methodBuilder.SetImplementationFlags(attributes);
        }

        public bool InitLocals
        {
            get { return m_methodBuilder.InitLocals; }
            set { m_methodBuilder.InitLocals = value; }
        }

        #endregion
    }
}

