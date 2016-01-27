// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class EnumBuilderDefineLiteral
    {
        private AssemblyBuilder _myAssemblyBuilder;
        private ModuleBuilder _myModuleBuilder;
        private EnumBuilder _myEnumBuilder;

        private void CreateCallee()
        {
            AssemblyName myAssemblyName = new AssemblyName();
            myAssemblyName.Name = "EnumAssembly";
            _myAssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(myAssemblyName, AssemblyBuilderAccess.Run);
            _myModuleBuilder = TestLibrary.Utilities.GetModuleBuilder(_myAssemblyBuilder, "EnumModule.mod");
        }

        [Fact]
        public void TestDefineLiteral()
        {
            CreateCallee();
            _myEnumBuilder = _myModuleBuilder.DefineEnum("myEnum", TypeAttributes.Public, typeof(int));
            FieldBuilder myField = _myEnumBuilder.DefineLiteral("FieldOne", 1);
            _myEnumBuilder.AsType();
            Assert.True(myField.IsLiteral);
            Assert.True(myField.IsPublic);
            Assert.True(myField.IsStatic);
        }
    }
}
