// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class EnumBuilderAssemblyQualifiedName
    {
        private AssemblyBuilder _myAssemblyBuilder;

        private ModuleBuilder CreateCallee()
        {
            AssemblyName myAssemblyName = new AssemblyName();
            myAssemblyName.Name = "EnumAssembly";
            _myAssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(myAssemblyName, AssemblyBuilderAccess.Run);
            return TestLibrary.Utilities.GetModuleBuilder(_myAssemblyBuilder, "EnumModule.mod");
        }

        [Fact]
        public void TestAssemblyQualifiedNameProperty()
        {
            var myModuleBuilder = CreateCallee();
            var myEnumBuilder = myModuleBuilder.DefineEnum("myEnum", TypeAttributes.Public, typeof(int));
            string myAssemQualified = myEnumBuilder.AssemblyQualifiedName;
            Assert.Equal("myEnum, " + _myAssemblyBuilder.FullName, myAssemQualified);
        }
    }
}
