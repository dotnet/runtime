// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class AssemblySaveILGeneratorTests
    {
        [Fact]
        public void MethodWithEmptyBody()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndSaveMethod(out MethodInfo saveMethod);
                TypeBuilder type = ab.DefineDynamicModule("MyModule").DefineType("MyType", TypeAttributes.Public);

                var methodBuilder = type.DefineMethod("EmptyMethod", MethodAttributes.Public, typeof(void), new[] { typeof(Version) });
                var il = methodBuilder.GetILGenerator();
                il.Emit(OpCodes.Ret);
                saveMethod.Invoke(ab, new object[] { file.Path });

                Assembly assemblyFromDisk = AssemblySaveTools.LoadAssemblyFromPath(file.Path);
                Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                MethodInfo method = typeFromDisk.GetMethod("EmptyMethod");
                MethodBody body = method.GetMethodBody();
                Assert.Empty(body.LocalVariables);
                Assert.Empty(body.ExceptionHandlingClauses);
                byte[]? bodyBytes = body.GetILAsByteArray();
                Assert.NotNull(bodyBytes);
                Assert.Equal(OpCodes.Ret.Value, bodyBytes[0]);

            }
        }

        [Theory]
        [InlineData(20)]
        [InlineData(-10)]
        public void MethodReturning_Int(int size)
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderAndSaveMethod(out MethodInfo saveMethod);
                TypeBuilder type = ab.DefineDynamicModule("MyModule").DefineType("MyType", TypeAttributes.Public);
                MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public | MethodAttributes.Static, typeof(int), Type.EmptyTypes);

                ILGenerator ilGenerator = method.GetILGenerator(size);
                int expectedReturn = 5;
                ilGenerator.Emit(OpCodes.Ldc_I4, expectedReturn);
                ilGenerator.Emit(OpCodes.Ret);
                saveMethod.Invoke(ab, new object[] { file.Path });

                Assembly assemblyFromDisk = AssemblySaveTools.LoadAssemblyFromPath(file.Path);
                Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                MethodInfo methodFromFile = typeFromDisk.GetMethod("TestMethod");
                MethodBody body = methodFromFile.GetMethodBody();
                byte[]? bodyBytes = body.GetILAsByteArray();
                Assert.NotNull(bodyBytes);
                Assert.Equal(OpCodes.Ldc_I4_5.Value, bodyBytes[0]);
                Assert.Equal(OpCodes.Ret.Value, bodyBytes[1]);
            }
        }
    }
}
