// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class GenericTypeParameterBuilderSetInterfaceConstraints
    {
        [Fact]
        public void SetInterfaceConstraints_OneCustomInterface()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            string[] typeParamNames = new string[] { "TFirst" };
            GenericTypeParameterBuilder[] typeParams = type.DefineGenericParameters(typeParamNames);
            
            typeParams[0].SetInterfaceConstraints(typeof(EmptyNonGenericInterface1));
            Type resultType = type.CreateTypeInfo().AsType();
            Type[] genericTypeParams = resultType.GetGenericArguments();

            Assert.Equal(1, genericTypeParams.Length);
            Assert.Equal(new Type[] { typeof(EmptyNonGenericInterface1) }, genericTypeParams[0].GetTypeInfo().GetGenericParameterConstraints());
        }

        [Fact]
        public void SetInterfaceConstraints_Null()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            string[] typeParamNames = new string[] { "TFirst" };
            GenericTypeParameterBuilder[] typeParams = type.DefineGenericParameters(typeParamNames);
            
            typeParams[0].SetInterfaceConstraints(null);
            Type resultType = type.CreateTypeInfo().AsType();
            Type[] genericTypeParams = resultType.GetGenericArguments();

            Assert.Equal(1, genericTypeParams.Length);
            Assert.Equal(new Type[0], genericTypeParams[0].GetTypeInfo().GetGenericParameterConstraints());
        }

        [Fact]
        public void SetInterfaceConstraints_MultipleCustomInterfaces()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            string[] typeParamNames = new string[] { "TFirst" };
            GenericTypeParameterBuilder[] typeParams = type.DefineGenericParameters(typeParamNames);
            
            typeParams[0].SetInterfaceConstraints(new Type[] { typeof(EmptyNonGenericInterface1), typeof(EmptyNonGenericInterface2) });
            Type resultType = type.CreateTypeInfo().AsType();
            Type[] genericTypeParams = resultType.GetGenericArguments();

            Assert.Equal(1, genericTypeParams.Length);
            Assert.Equal(new Type[] { typeof(EmptyNonGenericInterface1), typeof(EmptyNonGenericInterface2) }, genericTypeParams[0].GetTypeInfo().GetGenericParameterConstraints());
        }
    }
}
