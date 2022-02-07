// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;
using Xunit.Abstractions;

namespace System.Tests
{
    public static class Helpers
    {
        private static Type s_refEmitType;

        public static IEnumerable<Type> NonRuntimeTypes
        {
            get
            {
                if (PlatformDetection.IsReflectionEmitSupported)
                    yield return RefEmitType();

                yield return new NonRuntimeType();
            }
        }

        private static Type RefEmitType()
        {
            if (s_refEmitType == null)
            {
                AssemblyName assemblyName = new AssemblyName("AssemblyName");
                AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
                ModuleBuilder mboduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);

                TypeBuilder typeBuilder = mboduleBuilder.DefineType("TestType", TypeAttributes.Public);

                GenericTypeParameterBuilder[] typeParams = typeBuilder.DefineGenericParameters("T");
                s_refEmitType = typeParams[0].UnderlyingSystemType;
            }

            return s_refEmitType;
        }

        private sealed class NonRuntimeType : MockType
        {
            public sealed override Type UnderlyingSystemType => this;
        }
    }
}
