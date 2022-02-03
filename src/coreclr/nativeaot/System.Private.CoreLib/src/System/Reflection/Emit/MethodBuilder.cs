// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Emit
{
    public sealed class MethodBuilder : MethodInfo
    {
        internal MethodBuilder()
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

        public override bool ContainsGenericParameters
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

        public override bool IsGenericMethod
        {
            get
            {
                return default;
            }
        }

        public override bool IsGenericMethodDefinition
        {
            get
            {
                return default;
            }
        }

        public override bool IsConstructedGenericMethod
        {
            get
            {
                return default;
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

        public override ParameterInfo ReturnParameter
        {
            get
            {
                return default;
            }
        }

        public override Type ReturnType
        {
            get
            {
                return default;
            }
        }

        public override ICustomAttributeProvider ReturnTypeCustomAttributes
        {
            get
            {
                return default;
            }
        }

        public GenericTypeParameterBuilder[] DefineGenericParameters(params string[] names)
        {
            return default;
        }

        public ParameterBuilder DefineParameter(int position, ParameterAttributes attributes, string strParamName)
        {
            return default;
        }

        public override bool Equals(object? obj)
        {
            return default;
        }

        public override MethodInfo GetBaseDefinition()
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

        public override Type[] GetGenericArguments()
        {
            return default;
        }

        public override MethodInfo GetGenericMethodDefinition()
        {
            return default;
        }

        public override int GetHashCode()
        {
            return default;
        }

        public ILGenerator GetILGenerator()
        {
            return default;
        }

        public ILGenerator GetILGenerator(int size)
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

        public override object? Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, Globalization.CultureInfo? culture)
        {
            return default;
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return default;
        }

        [RequiresDynamicCode("The native code for this instantiation might not be available at runtime.")]
        [RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
        public override MethodInfo MakeGenericMethod(params Type[] typeArguments)
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

        public void SetParameters(params Type[] parameterTypes)
        {
        }

        public void SetReturnType(Type returnType)
        {
        }

        public void SetSignature(Type returnType, Type[] returnTypeRequiredCustomModifiers, Type[] returnTypeOptionalCustomModifiers, Type[] parameterTypes, Type[][] parameterTypeRequiredCustomModifiers, Type[][] parameterTypeOptionalCustomModifiers)
        {
        }

        public override string ToString()
        {
            return default;
        }
    }
}
