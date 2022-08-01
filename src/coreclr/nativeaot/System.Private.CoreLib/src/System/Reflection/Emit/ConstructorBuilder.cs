// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace System.Reflection.Emit
{
    public sealed class ConstructorBuilder : ConstructorInfo
    {
        internal ConstructorBuilder()
        {
            // Prevent generating a default constructor
        }

        public override MethodAttributes Attributes
        {
            get
            {
                return default;
            }
        }

        public override CallingConventions CallingConvention
        {
            get
            {
                return default;
            }
        }

        public override Type DeclaringType
        {
            get
            {
                return default;
            }
        }

        public bool InitLocals
        {
            get
            {
                return default;
            }
            set
            {
            }
        }

        public override RuntimeMethodHandle MethodHandle
        {
            get
            {
                return default;
            }
        }

        public override Module Module
        {
            get
            {
                return default;
            }
        }

        public override string Name
        {
            get
            {
                return default;
            }
        }

        public override Type ReflectedType
        {
            get
            {
                return default;
            }
        }

        public ParameterBuilder DefineParameter(int iSequence, ParameterAttributes attributes, string strParamName)
        {
            return default;
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            return default;
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return default;
        }

        public ILGenerator GetILGenerator()
        {
            return default;
        }

        public ILGenerator GetILGenerator(int streamSize)
        {
            return default;
        }

        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            return default;
        }

        public override ParameterInfo[] GetParameters()
        {
            return default;
        }

        public override object? Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
        {
            return default;
        }

        public override object Invoke(BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
        {
            return default;
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return default;
        }

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
        }

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
        }

        public void SetImplementationFlags(MethodImplAttributes attributes)
        {
        }

        public override string ToString()
        {
            return default;
        }
    }
}
