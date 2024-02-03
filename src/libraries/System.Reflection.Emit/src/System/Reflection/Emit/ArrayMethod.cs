// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace System.Reflection.Emit
{
    internal sealed class ArrayMethod : MethodInfo
    {
        #region Private Data Members
        private readonly ModuleBuilder _module;
        private readonly Type _containingType;
        private readonly string _name;
        private readonly CallingConventions _callingConvention;
        private readonly Type _returnType;
        private readonly Type[] _parameterTypes;
        #endregion

        #region Constructor
        // This is a kind of MethodInfo to represent methods for array type of unbaked type
        internal ArrayMethod(ModuleBuilder module, Type arrayClass, string methodName,
            CallingConventions callingConvention, Type? returnType, Type[]? parameterTypes)
        {
            _returnType = returnType ?? typeof(void);
            if (parameterTypes != null)
            {
                _parameterTypes = new Type[parameterTypes.Length];
                for (int i = 0; i < parameterTypes.Length; i++)
                {
                    ArgumentNullException.ThrowIfNull(_parameterTypes[i] = parameterTypes[i], nameof(parameterTypes));
                }
            }
            else
            {
                _parameterTypes = Type.EmptyTypes;
            }

            _module = module;
            _containingType = arrayClass;
            _name = methodName;
            _callingConvention = callingConvention;
        }
        #endregion

        #region Internal Members
        internal Type[] ParameterTypes => _parameterTypes;
        #endregion

        #region MemberInfo Overrides
        //public override int MetadataToken => m_token;

        public override Module Module => _module;

        public override Type? ReflectedType => _containingType;

        public override string Name => _name;

        public override Type? DeclaringType => _containingType;
        #endregion

        #region MethodBase Overrides
        public override ParameterInfo[] GetParameters()
        {
            throw new NotSupportedException(SR.NotSupported_SymbolMethod);
        }

        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            throw new NotSupportedException(SR.NotSupported_SymbolMethod);
        }

        public override MethodAttributes Attributes => MethodAttributes.PrivateScope;

        public override CallingConventions CallingConvention => _callingConvention;

        public override RuntimeMethodHandle MethodHandle => throw new NotSupportedException(SR.NotSupported_SymbolMethod);

        #endregion

        #region MethodInfo Overrides
        public override Type ReturnType => _returnType;

        public override ICustomAttributeProvider ReturnTypeCustomAttributes => throw new NotSupportedException(SR.NotSupported_SymbolMethod);

        public override object Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
        {
            throw new NotSupportedException(SR.NotSupported_SymbolMethod);
        }

        public override MethodInfo GetBaseDefinition()
        {
            return this;
        }
        #endregion

        #region ICustomAttributeProvider Implementation
        public override object[] GetCustomAttributes(bool inherit)
        {
            throw new NotSupportedException(SR.NotSupported_SymbolMethod);
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotSupportedException(SR.NotSupported_SymbolMethod);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotSupportedException(SR.NotSupported_SymbolMethod);
        }

        #endregion
    }
}
