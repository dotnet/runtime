// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection.Emit;
using System.Reflection;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderGetGenericTypeDefinition
    {
        [Fact]
        public void TestGetGenericTypeDefinition()
        {
            TypeBuilder myTypebuilder = GetGenericTypeBuilder();
            Type myTypebuilder1 = myTypebuilder.GetGenericTypeDefinition();
            Assert.Equal("Sample", myTypebuilder1.Name);
        }

        [Fact]
        public void TestThrowsExceptionForNonGenericType()
        {
            TypeBuilder myTypebuilder = GetTypeBuilder();
            Assert.Throws<InvalidOperationException>(() => { Type myTypebuilder1 = myTypebuilder.GetGenericTypeDefinition(); });
        }

        private ModuleBuilder GetModuleBuilder()
        {
            ModuleBuilder myModuleBuilder;
            AssemblyBuilder myAssemblyBuilder;
            // Get the current application domain for the current thread.
            AssemblyName myAssemblyName = new AssemblyName();
            myAssemblyName.Name = "TempAssembly";

            // Define a dynamic assembly in the current domain.
            myAssemblyBuilder =
               AssemblyBuilder.DefineDynamicAssembly
                           (myAssemblyName, AssemblyBuilderAccess.Run);
            // Define a dynamic module in "TempAssembly" assembly.
            myModuleBuilder = TestLibrary.Utilities.GetModuleBuilder(myAssemblyBuilder, "Module1");
            return myModuleBuilder;
        }

        private TypeBuilder GetGenericTypeBuilder()
        {
            ModuleBuilder myModuleBuilder = GetModuleBuilder();

            TypeBuilder myType = myModuleBuilder.DefineType("Sample",
               TypeAttributes.Class | TypeAttributes.Public);

            // Add a type parameter, making the type generic.
            string[] typeParamNames = { "T" };
            GenericTypeParameterBuilder[] typeParams =
                myType.DefineGenericParameters(typeParamNames);

            return myType;
        }
        private TypeBuilder GetTypeBuilder()
        {
            ModuleBuilder myModuleBuilder = GetModuleBuilder();

            TypeBuilder myType = myModuleBuilder.DefineType("Sample",
               TypeAttributes.Class | TypeAttributes.Public);

            return myType;
        }
    }
}
