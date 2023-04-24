// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class AssemblySaveConstructorBuilderTests
    {
        [Fact]
        public void InitLocals()
        {
            using (TempFile file = TempFile.Create())
            {
                AssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilderTypeBuilderAndSaveMethod(out TypeBuilder type, out MethodInfo saveMethod);
                ConstructorBuilder constructor = type.DefineDefaultConstructor(MethodAttributes.Public);
                saveMethod.Invoke(ab, new object[] { file.Path });
                Console.WriteLine(file.Path);

                //ConstructorBuilder constructor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, null);
                Assembly assemblyFromDisk = AssemblySaveTools.LoadAssemblyFromPath(file.Path);
                Type typeFromDisk = assemblyFromDisk.Modules.First().GetType("MyType");
                ConstructorInfo ctor = typeFromDisk.GetConstructors(BindingFlags.Instance | BindingFlags.Public)[0];
                Assert.Equal(0, ctor.GetParameters().Length);
            }
        }
    }
}
