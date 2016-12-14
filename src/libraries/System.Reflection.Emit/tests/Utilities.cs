// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class EmptyNonGenericClass { }
    public class EmptyGenericClass<T> { }
    public sealed class SealedClass { }
    public static class StaticClass { }

    public struct EmptyNonGenericStruct { }
    public struct EmptyGenericStruct<T> { }

    public enum EmptyEnum { }
    public delegate EventHandler BasicDelegate();

    public interface EmptyNonGenericInterface1 { }
    public interface EmptyNonGenericInterface2 { }

    public interface EmptyGenericInterface<T> { }

    public class EmptyAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class IntAllAttribute : Attribute
    {
        public int _i;
        public IntAllAttribute(int i) { _i = i; }
    }

    public static class TypeExtensions
    {
        public static Type AsType(this Type type)
        {
            return type;
        }
    }

    public static class Helpers
    {
        public const BindingFlags AllFlags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static AssemblyBuilder DynamicAssembly(string name = "TestAssembly", AssemblyBuilderAccess access = AssemblyBuilderAccess.Run)
        {
            AssemblyName assemblyName = new AssemblyName(name);
            return AssemblyBuilder.DefineDynamicAssembly(assemblyName, access);
        }

        public static ModuleBuilder DynamicModule(string assemblyName = "TestAssembly", string moduleName = "TestModule")
        {
            return DynamicAssembly(assemblyName).DefineDynamicModule(moduleName);
        }

        public static TypeBuilder DynamicType(TypeAttributes attributes, string assemblyName = "TestAssembly", string moduleName = "TestModule", string typeName = "TestType")
        {
            return DynamicModule(assemblyName, moduleName).DefineType(typeName, attributes);
        }

        public static EnumBuilder DynamicEnum(TypeAttributes visibility, Type underlyingType, string enumName = "TestEnum", string assemblyName = "TestAssembly", string moduleName = "TestModule")
        {
            return DynamicModule(assemblyName, moduleName).DefineEnum(enumName, visibility, underlyingType);
        }

        public static void VerifyType(TypeBuilder type, Module module, TypeBuilder declaringType, string name, TypeAttributes attributes, Type baseType, int size, PackingSize packingSize, Type[] implementedInterfaces)
        {
            Assert.Equal(module, type.Module);
            Assert.Equal(module.Assembly, type.Assembly);

            Assert.Equal(name, type.Name);
            if (declaringType == null)
            {
                Assert.Equal(GetFullName(name), type.FullName);
            }
            else
            {
                Assert.Equal(GetFullName(declaringType.Name) + "+" + GetFullName(type.Name), type.FullName);
            }

            Assert.Equal(attributes, type.Attributes);

            Assert.Equal(declaringType?.AsType(), type.DeclaringType);
            Assert.Equal(baseType, type.BaseType);

            Assert.Equal(size, type.Size);
            Assert.Equal(packingSize, type.PackingSize);

            Assert.Equal(implementedInterfaces ?? new Type[0], type.GetInterfaces());

            if (declaringType == null && !type.IsInterface && (implementedInterfaces == null || implementedInterfaces.Length == 0))
            {
                Type createdType = type.CreateTypeInfo().AsType();
                Assert.Equal(createdType, module.GetType(name, false, false));
                Assert.Equal(createdType, module.GetType(name, true, false));
                
                Assert.Equal(type.AsType().GetNestedTypes(AllFlags), createdType.GetNestedTypes(AllFlags));
                Assert.Equal(type.AsType().GetNestedType(name, AllFlags), createdType.GetNestedType(name, AllFlags));

                // [ActiveIssue(10989, TestPlatforms.AnyUnix)]
                // Assert.Equal(createdType, module.GetType(name, true, true));
                // Assert.Equal(createdType, module.GetType(name.ToLowerInvariant(), true, true));
                // Assert.Equal(createdType, module.GetType(name.ToUpperInvariant(), true, true));
            }
        }

        public static void VerifyConstructor(ConstructorBuilder constructor, TypeBuilder type, MethodAttributes attributes, CallingConventions callingConvention, Type[] parameterTypes)
        {
            string expectedName = (attributes & MethodAttributes.Static) != 0 ? ConstructorInfo.TypeConstructorName : ConstructorInfo.ConstructorName;

            Assert.Equal(expectedName, constructor.Name);
            Assert.Equal(attributes | MethodAttributes.SpecialName, constructor.Attributes);
            Assert.Equal(CallingConventions.Standard, constructor.CallingConvention);
            Assert.Equal(type.AsType(), constructor.DeclaringType);
            Assert.Equal(type.Module, constructor.Module);
            Assert.Equal(MethodImplAttributes.IL, constructor.MethodImplementationFlags);

            Assert.Throws<NotSupportedException>(() => constructor.Invoke(null));
            Assert.Throws<NotSupportedException>(() => constructor.Invoke(null, null));

            Type createdType = type.CreateTypeInfo().AsType();
            Assert.Equal(type.AsType().GetConstructors(AllFlags), createdType.GetConstructors(AllFlags));
            Assert.Equal(type.AsType().GetConstructor(parameterTypes), createdType.GetConstructor(parameterTypes));

            ConstructorInfo createdConstructor = createdType.GetConstructors(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Single(ctor => ctor.IsStatic == constructor.IsStatic);

            CallingConventions expectedCallingConvention = CallingConventions.Standard;
            if ((callingConvention & CallingConventions.VarArgs) != 0)
            {
                expectedCallingConvention = CallingConventions.VarArgs;
            }
            if ((attributes & MethodAttributes.Static) == 0)
            {
                expectedCallingConvention |= CallingConventions.HasThis;
            }

            MethodAttributes expectedAttributes = attributes | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
            expectedAttributes &= ~MethodAttributes.RequireSecObject;

            Assert.Equal(expectedName, constructor.Name);
            Assert.Equal(expectedAttributes, createdConstructor.Attributes);
            Assert.Equal(expectedCallingConvention, createdConstructor.CallingConvention);
            Assert.Equal(createdType, createdConstructor.DeclaringType);
            Assert.Equal(MethodImplAttributes.IL, constructor.MethodImplementationFlags);
        }

        public static string GetFullName(string name)
        {
            int nullTerminatorIndex = name.IndexOf('\0');
            if (nullTerminatorIndex >= 0)
            {
                return name.Substring(0, nullTerminatorIndex);
            }
            return name;
        }
    }
}
