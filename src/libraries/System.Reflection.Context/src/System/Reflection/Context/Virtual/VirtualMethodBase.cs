// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.using System;

using System.Collections.Generic;
using System.Text;

namespace System.Reflection.Context.Virtual
{
    internal abstract class VirtualMethodBase : MethodInfo
    {
        private ParameterInfo _returnParameter;

        protected abstract Type[] GetParameterTypes();

        public override MethodAttributes Attributes
        {
            get { return MethodAttributes.Public | MethodAttributes.HideBySig; }
        }

        public override sealed CallingConventions CallingConvention
        {
            get { return CallingConventions.HasThis | CallingConventions.Standard; }
        }

        public override sealed bool ContainsGenericParameters
        {
            get { return false; }
        }

        public override sealed bool IsGenericMethod
        {
            get { return false; }
        }

        public override sealed bool IsGenericMethodDefinition
        {
            get { return false; }
        }

        public override RuntimeMethodHandle MethodHandle
        {
            get { throw new NotSupportedException(); }
        }

        public override sealed Module Module
        {
            get { return DeclaringType.Module; }
        }

        public override sealed Type ReflectedType
        {
            get { return DeclaringType; }
        }

        public override sealed ParameterInfo ReturnParameter
        {
            get { return _returnParameter ?? (_returnParameter = new VirtualReturnParameter(this)); }
        }

        public override sealed ICustomAttributeProvider ReturnTypeCustomAttributes
        {
            get { return ReturnParameter; }
        }

        public override sealed MethodInfo GetBaseDefinition()
        {
            return this;
        }

        public override sealed Type[] GetGenericArguments()
        {
            return CollectionServices.Empty<Type>();
        }

        public override sealed MethodInfo GetGenericMethodDefinition()
        {
            throw new InvalidOperationException();
        }

        public override sealed MethodImplAttributes GetMethodImplementationFlags()
        {
            return MethodImplAttributes.IL;
        }

        public override ParameterInfo[] GetParameters()
        {
            return CollectionServices.Empty<ParameterInfo>();
        }

        public override sealed MethodInfo MakeGenericMethod(params Type[] typeArguments)
        {
            throw new InvalidOperationException(SR.Format(SR.InvalidOperation_NotGenericMethodDefinition, this));
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return CollectionServices.Empty<object>();
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            return CollectionServices.Empty<object>();
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return CollectionServices.Empty<CustomAttributeData>();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return false;
        }

        public override bool Equals(object obj)
        {
            // We don't need to compare the invokees
            // But do we need to compare the contexts and return types?
            return obj is VirtualMethodBase other &&
                Name == other.Name &&
                DeclaringType.Equals(other.DeclaringType) &&
                CollectionServices.CompareArrays(GetParameterTypes(), other.GetParameterTypes());
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^
                DeclaringType.GetHashCode() ^
                CollectionServices.GetArrayHashCode(GetParameterTypes());
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append(ReturnType.ToString());
            sb.Append(" ");
            sb.Append(Name);
            sb.Append("(");

            Type[] parameterTypes = GetParameterTypes();

            string comma = "";

            foreach (Type t in parameterTypes)
            {
                sb.Append(comma);
                sb.Append(t.ToString());

                comma = ", ";
            }

            if ((CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs)
            {
                sb.Append(comma);
                sb.Append("...");
            }

            return sb.ToString();
        }
    }
}
