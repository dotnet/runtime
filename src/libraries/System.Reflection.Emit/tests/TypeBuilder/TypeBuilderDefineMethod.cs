// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Runtime.Loader;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderDefineMethodTests
    {
        public static IEnumerable<object[]> TestData()
        {
            yield return new object[] { "Name", MethodAttributes.Abstract, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.Assembly, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.CheckAccessOnOverride, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.FamANDAssem, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.Family, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.FamORAssem, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.Final, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.HasSecurity, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.HideBySig, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.MemberAccessMask, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.NewSlot, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.PinvokeImpl, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.Private, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.PrivateScope, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.Public, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.RequireSecObject, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.ReuseSlot, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.RTSpecialName, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.SpecialName, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.Static, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.UnmanagedExport, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.Virtual, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.VtableLayoutMask, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.Abstract | MethodAttributes.Public | MethodAttributes.NewSlot | MethodAttributes.Virtual, CallingConventions.Standard, null, null };
            yield return new object[] { "A!?123C\"", MethodAttributes.Final | MethodAttributes.Private | MethodAttributes.SpecialName | MethodAttributes.Static, CallingConventions.Standard, null, null };

            // Static
            yield return new object[] { "`Name", MethodAttributes.Static, CallingConventions.Any, null, null };
            yield return new object[] { "Name", MethodAttributes.Static, CallingConventions.ExplicitThis, null, null };
            yield return new object[] { "Name", MethodAttributes.Static, CallingConventions.HasThis, null, null };
            yield return new object[] { "Name", MethodAttributes.Static, CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.Static, CallingConventions.VarArgs, null, null };
            yield return new object[] { "Name", MethodAttributes.Static, CallingConventions.Any | CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.Static, CallingConventions.Any | CallingConventions.VarArgs, null, null };
            yield return new object[] { "Name", MethodAttributes.Static, CallingConventions.HasThis | CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.Static, CallingConventions.HasThis | CallingConventions.ExplicitThis, null, null };
            yield return new object[] { "\uD800\uDC00", MethodAttributes.Static, (CallingConventions)(-1), null, null };

            // Instance
            yield return new object[] { "Name", MethodAttributes.Public, CallingConventions.Any, null, null };
            yield return new object[] { "Name", MethodAttributes.Public, CallingConventions.ExplicitThis, null, null };
            yield return new object[] { "Na.me", MethodAttributes.Public, CallingConventions.HasThis, null, null };
            yield return new object[] { "Name", MethodAttributes.Public, CallingConventions.Standard, null, null };
            yield return new object[] { "Na{m}e", MethodAttributes.Public, CallingConventions.VarArgs, null, null };
            yield return new object[] { "Na*me", MethodAttributes.Public, CallingConventions.Any | CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.Public, CallingConventions.Any | CallingConventions.VarArgs, null, null };
            yield return new object[] { "Name$", MethodAttributes.Public, CallingConventions.HasThis | CallingConventions.Standard, null, null };
            yield return new object[] { "Name", MethodAttributes.Public, CallingConventions.HasThis | CallingConventions.ExplicitThis, null, null };
            yield return new object[] { "42", MethodAttributes.Public, (CallingConventions)(-1), null, null };
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void DefineMethod(string name, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] parameterTypes)
        {
            bool defaultReturnTypeAndParameters = returnType == null && parameterTypes == null;
            if (callingConvention == CallingConventions.Standard)
            {
                if (defaultReturnTypeAndParameters)
                {
                    // Use DefineMethod(string, MethodAttributes)
                    TypeBuilder type1 = Helpers.DynamicType(TypeAttributes.Public);
                    MethodBuilder method1 = type1.DefineMethod(name, attributes);
                    VerifyMethod(type1, method1, name, attributes, callingConvention, returnType, parameterTypes);
                }
                // Use DefineMethod(string, MethodAttributes, Type, Type[])
                TypeBuilder type2 = Helpers.DynamicType(TypeAttributes.Public);
                MethodBuilder method2 = type2.DefineMethod(name, attributes, returnType, parameterTypes);
                VerifyMethod(type2, method2, name, attributes, callingConvention, returnType, parameterTypes);
            }
            if (defaultReturnTypeAndParameters)
            {
                // Use DefineMethod(string, MethodAttributes, CallingConventions)
                TypeBuilder type3 = Helpers.DynamicType(TypeAttributes.Public);
                MethodBuilder method3 = type3.DefineMethod(name, attributes, callingConvention);
                VerifyMethod(type3, method3, name, attributes, callingConvention, returnType, parameterTypes);
            }
            // Use DefineMethod(string, MethodAttributes, CallingConventions, Type, Type[])
            TypeBuilder type4 = Helpers.DynamicType(TypeAttributes.Public);
            MethodBuilder method4 = type4.DefineMethod(name, attributes, callingConvention, returnType, parameterTypes);
            VerifyMethod(type4, method4, name, attributes, callingConvention, returnType, parameterTypes);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [MemberData(nameof(TestData))]
        public void DefineMethodPersistedAssembly(string name, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] parameterTypes)
        {
            AssemblySaveTools.PopulateAssemblyBuilderAndTypeBuilder(out TypeBuilder type);
            bool defaultReturnTypeAndParameters = returnType == null && parameterTypes == null;
            if (callingConvention == CallingConventions.Standard)
            {
                if (defaultReturnTypeAndParameters)
                {
                    // Use DefineMethod(string, MethodAttributes)
                    MethodBuilder method1 = type.DefineMethod(name, attributes);
                    VerifyMethod(type, method1, name, attributes, callingConvention, returnType, parameterTypes);
                }
                // Use DefineMethod(string, MethodAttributes, Type, Type[])
                MethodBuilder method2 = type.DefineMethod(name, attributes, returnType, parameterTypes);
                VerifyMethod(type, method2, name, attributes, callingConvention, returnType, parameterTypes);
            }
            if (defaultReturnTypeAndParameters)
            {
                // Use DefineMethod(string, MethodAttributes, CallingConventions)
                MethodBuilder method3 = type.DefineMethod(name, attributes, callingConvention);
                VerifyMethod(type, method3, name, attributes, callingConvention, returnType, parameterTypes);
            }
            // Use DefineMethod(string, MethodAttributes, CallingConventions, Type, Type[])
            MethodBuilder method4 = type.DefineMethod(name, attributes, callingConvention, returnType, parameterTypes);
            VerifyMethod(type, method4, name, attributes, callingConvention, returnType, parameterTypes);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        public void DefineMethod_65536Methods()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            for (int i = 0; i < 65536; i++)
            {
                type.DefineMethod($"M_{i}", MethodAttributes.Public).GetILGenerator().Emit(OpCodes.Ret);
            }

            // System.TypeLoadException : Type 'TestType' from assembly 'TestAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
            // contains more methods than the current implementation allows.
            Assert.Throws<TypeLoadException>(() => type.CreateType()); 
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        public void DefineMethod_65536MethodsPersistedAssembly()
        {
            PersistedAssemblyBuilder ab = AssemblySaveTools.PopulateAssemblyBuilder(new AssemblyName("DefineMethod_65536MethodsPersistedAssembly"));
            TypeBuilder type = ab.DefineDynamicModule("MyModule").DefineType("MyType", TypeAttributes.Public);
            for (int i = 0; i < 65536; i++)
            {
                MethodBuilder method = type.DefineMethod($"M_{i}", MethodAttributes.Public | MethodAttributes.Static, typeof(int), Type.EmptyTypes);
                ILGenerator il = method.GetILGenerator();
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ret);
            }
            type.CreateType();

            using (var stream = new MemoryStream())
            {
                ab.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);
                var assembly = AssemblyLoadContext.Default.LoadFromStream(stream);
                // System.TypeLoadException : Type 'MyType' from assembly 'MyDynamicAssembly, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null'
                // contains more methods than the current implementation allows.
                Assert.Throws<TypeLoadException>(() => assembly.GetType("MyType"));

                using (MetadataLoadContext mlc = new MetadataLoadContext(new CoreMetadataAssemblyResolver()))
                {
                    Type createdType = mlc.LoadFromStream(stream).GetType("MyType"); // Loads with MLC
                    MethodInfo[] methods = createdType.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
                    Assert.Equal(65536, methods.Length);
                    Assert.Equal("M_65535", methods[65535].Name);
                }
            }
        }

        [Fact]
        public void DefineMethod_MultipleOverloads_Works()
        {
            const string Name = "Name";
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method1 = type.DefineMethod(Name, MethodAttributes.Public);
            Assert.Equal(Name, method1.Name);

            MethodBuilder method2 = type.DefineMethod(Name, MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual, typeof(void), new Type[] { typeof(int) });
            Assert.Equal(Name, method2.Name);
        }

        private static void VerifyMethod(TypeBuilder type, MethodBuilder method, string name, MethodAttributes attributes, CallingConventions callingConvention, Type returnType, Type[] parameterTypes)
        {
            CallingConventions expectedCallingConvention = callingConvention;
            if ((attributes & MethodAttributes.Static) == 0)
            {
                expectedCallingConvention |= CallingConventions.HasThis;
            }

            Assert.Equal(type.AsType(), method.DeclaringType);
            Assert.Equal(name, method.Name);
            Assert.Equal(attributes, method.Attributes);
            Assert.Equal(expectedCallingConvention, method.CallingConvention);
            Assert.Equal(returnType ?? typeof(void), method.ReturnType);
        }
    }
}
