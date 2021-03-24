// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Remoting;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Tests
{
    public partial class ActivatorTests
    {
        [Fact]
        public static void CreateInstance()
        {
            // Passing null args is equivalent to an empty array of args.
            Choice1 c = (Choice1)(Activator.CreateInstance(typeof(Choice1), null));
            Assert.Equal(1, c.I);

            c = (Choice1)(Activator.CreateInstance(typeof(Choice1), new object[] { }));
            Assert.Equal(1, c.I);

            c = (Choice1)(Activator.CreateInstance(typeof(Choice1), new object[] { 42 }));
            Assert.Equal(2, c.I);

            c = (Choice1)(Activator.CreateInstance(typeof(Choice1), new object[] { "Hello" }));
            Assert.Equal(3, c.I);

            c = (Choice1)(Activator.CreateInstance(typeof(Choice1), new object[] { 5.1, "Hello" }));
            Assert.Equal(4, c.I);

            Activator.CreateInstance(typeof(StructTypeWithoutReflectionMetadata));
        }

        [Fact]
        public static void CreateInstance_ConstructorWithPrimitive_PerformsPrimitiveWidening()
        {
            // Primitive widening is allowed by the binder, but not by Dynamic.DelegateInvoke().
            Choice1 c = (Choice1)(Activator.CreateInstance(typeof(Choice1), new object[] { (short)-2 }));
            Assert.Equal(2, c.I);
        }

        [Fact]
        public static void CreateInstance_ConstructorWithParamsParameter()
        {
            // C# params arguments are honored by Activator.CreateInstance()
            Choice1 c = (Choice1)(Activator.CreateInstance(typeof(Choice1), new object[] { new VarArgs() }));
            Assert.Equal(5, c.I);

            c = (Choice1)(Activator.CreateInstance(typeof(Choice1), new object[] { new VarArgs(), "P1" }));
            Assert.Equal(5, c.I);

            c = (Choice1)(Activator.CreateInstance(typeof(Choice1), new object[] { new VarArgs(), "P1", "P2" }));
            Assert.Equal(5, c.I);

            c = (Choice1)(Activator.CreateInstance(typeof(Choice1), new object[] { new VarStringArgs() }));
            Assert.Equal(6, c.I);

            c = (Choice1)(Activator.CreateInstance(typeof(Choice1), new object[] { new VarStringArgs(), "P1" }));
            Assert.Equal(6, c.I);

            c = (Choice1)(Activator.CreateInstance(typeof(Choice1), new object[] { new VarStringArgs(), "P1", "P2" }));
            Assert.Equal(6, c.I);
        }

        [Fact]
        public void CreateInstance_ValueTypeWithPublicDefaultConstructor_Success()
        {
            // Activator holds a cache of constructors and the types to which they belong.
            // Test caching behaviour by activating multiple times.
            Assert.IsType<ValueTypeWithDefaultConstructor>(Activator.CreateInstance(typeof(ValueTypeWithDefaultConstructor)));
            Assert.IsType<ValueTypeWithDefaultConstructor>(Activator.CreateInstance(typeof(ValueTypeWithDefaultConstructor), nonPublic: true));
            Assert.IsType<ValueTypeWithDefaultConstructor>(Activator.CreateInstance(typeof(ValueTypeWithDefaultConstructor), nonPublic: false));
        }

        [Fact]
        public void CreateInstance_NonPublicTypeWithPrivateDefaultConstructor_Success()
        {
            // Activator holds a cache of constructors and the types to which they belong.
            // Test caching behaviour by activating multiple times.
            TypeWithPrivateDefaultConstructor c1 = (TypeWithPrivateDefaultConstructor)Activator.CreateInstance(typeof(TypeWithPrivateDefaultConstructor), nonPublic: true);
            Assert.Equal(-1, c1.Property);

            TypeWithPrivateDefaultConstructor c2 = (TypeWithPrivateDefaultConstructor)Activator.CreateInstance(typeof(TypeWithPrivateDefaultConstructor), nonPublic: true);
            Assert.Equal(-1, c2.Property);
        }

        [Fact]
        public void CreateInstance_PublicOnlyTypeWithPrivateDefaultConstructor_ThrowsMissingMethodException()
        {
            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance(typeof(TypeWithPrivateDefaultConstructor)));
            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance(typeof(TypeWithPrivateDefaultConstructor), nonPublic: false));

            // Put the private default constructor into the cache and make sure we still throw if public only.
            Assert.NotNull(Activator.CreateInstance(typeof(TypeWithPrivateDefaultConstructor), nonPublic: true));

            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance(typeof(TypeWithPrivateDefaultConstructor)));
            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance(typeof(TypeWithPrivateDefaultConstructor), nonPublic: false));
        }

        [Fact]
        public void CreateInstance_NullableType_ReturnsNull()
        {
            Assert.Null(Activator.CreateInstance(typeof(int?)));
        }

        [Fact]
        public void CreateInstance_NullType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("type", () => Activator.CreateInstance((Type)null));
            AssertExtensions.Throws<ArgumentNullException>("type", () => Activator.CreateInstance(null, new object[0]));
        }

        [Fact]
        public static void CreateInstance_MultipleMatchingConstructors_ThrowsAmbiguousMatchException()
        {
            Assert.Throws<AmbiguousMatchException>(() => Activator.CreateInstance(typeof(Choice1), new object[] { null }));
        }

        [Fact]
        public void CreateInstance_NotRuntimeType_ThrowsArgumentException()
        {
            // This cannot be a [Theory] due to https://github.com/xunit/xunit/issues/1325.
            foreach (Type nonRuntimeType in Helpers.NonRuntimeTypes)
            {
                AssertExtensions.Throws<ArgumentException>("type", () => Activator.CreateInstance(nonRuntimeType));
                AssertExtensions.Throws<ArgumentException>("type", () => Activator.CreateInstance(nonRuntimeType, new object[0]));
            }
        }

        public static IEnumerable<object[]> CreateInstance_ContainsGenericParameters_TestData()
        {
            yield return new object[] { typeof(List<>) };
            yield return new object[] { typeof(List<>).GetTypeInfo().GenericTypeParameters[0] };
        }

        [Theory]
        [MemberData(nameof(CreateInstance_ContainsGenericParameters_TestData))]
        public void CreateInstance_ContainsGenericParameters_ThrowsArgumentException(Type type)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Activator.CreateInstance(type));
            AssertExtensions.Throws<ArgumentException>(null, () => Activator.CreateInstance(type, new object[0]));
        }

        public static IEnumerable<object[]> CreateInstance_InvalidType_TestData()
        {
            yield return new object[] { typeof(void) };
            yield return new object[] { typeof(void).MakeArrayType() };
            yield return new object[] { typeof(ArgIterator) };
        }

        [Theory]
        [MemberData(nameof(CreateInstance_InvalidType_TestData))]
        public void CreateInstance_InvalidType_ThrowsNotSupportedException(Type type)
        {
            Assert.Throws<NotSupportedException>(() => Activator.CreateInstance(type));
            Assert.Throws<NotSupportedException>(() => Activator.CreateInstance(type, new object[0]));
        }

        [Fact]
        public void CreateInstance_DesignatedOptionalParameters_ThrowsMissingMemberException()
        {
            // C# designated optional parameters are not optional as far as Activator.CreateInstance() is concerned.
            Assert.ThrowsAny<MissingMemberException>(() => Activator.CreateInstance(typeof(Choice1), new object[] { 5.1 }));
            Assert.ThrowsAny<MissingMemberException>(() => Activator.CreateInstance(typeof(Choice1), new object[] { 5.1, Type.Missing }));
        }

        [Fact]
        public void CreateInstance_InvalidParamArgs_ThrowsMissingMemberException()
        {
            Assert.ThrowsAny<MissingMemberException>(() => Activator.CreateInstance(typeof(Choice1), new object[] { new VarStringArgs(), 5, 6 }));
        }

        [Fact]
        public void CreateInstance_PrimitiveWidening_ThrowsInvalidCastException()
        {
            // Primitive widening not supported for "params" arguments.
            //
            // (This is probably an accidental behavior on the .NET Framework as the default binder specifically checks to see if the params arguments are widenable to the
            // params array element type and gives it the go-ahead if it is. Unfortunately, the binder then bollixes itself by using Array.Copy() to copy
            // the params arguments. Since Array.Copy() doesn't tolerate this sort of type mismatch, it throws an InvalidCastException which bubbles out
            // out of Activator.CreateInstance. Accidental or not, we'll inherit that behavior on .NET Native.)
            Assert.Throws<InvalidCastException>(() => Activator.CreateInstance(typeof(Choice1), new object[] { new VarIntArgs(), 1, (short)2 }));
        }

        public static IEnumerable<object[]> CreateInstance_NoDefaultConstructor_TestData()
        {
            yield return new object[] { typeof(TypeWithoutDefaultCtor) };
            yield return new object[] { typeof(int[]) };
            yield return new object[] { typeof(int).MakeByRefType() };
            yield return new object[] { typeof(int).MakePointerType() };
        }

        [Theory]
        [MemberData(nameof(CreateInstance_NoDefaultConstructor_TestData))]
        public void CreateInstance_NoDefaultConstructor_ThrowsMissingMemberException(Type type)
        {
            Assert.ThrowsAny<MissingMemberException>(() => Activator.CreateInstance(type));
        }

        [Theory]
        [InlineData(typeof(AbstractTypeWithDefaultCtor))]
        [InlineData(typeof(Array))]
        public void CreateInstance_AbstractClass_ThrowsMissingMemberException(Type type)
        {
            Assert.ThrowsAny<MissingMemberException>(() => Activator.CreateInstance(type));
        }

        [Theory]
        [InlineData(typeof(TypedReference))]
        [InlineData(typeof(RuntimeArgumentHandle))]
        public void CreateInstance_BoxedByRefType_ThrowsNotSupportedException(Type type)
        {
            Assert.Throws<NotSupportedException>(() => Activator.CreateInstance(type));
        }

        [Fact]
        public void CreateInstance_Span_ThrowsNotSupportedException()
        {
            CreateInstance_BoxedByRefType_ThrowsNotSupportedException(typeof(Span<int>));
        }

        [Fact]
        public void CreateInstance_InterfaceType_ThrowsMissingMemberException()
        {
            Assert.ThrowsAny<MissingMemberException>(() => Activator.CreateInstance(typeof(IInterfaceType)));
        }

        public class SubMarshalByRefObject : MarshalByRefObject
        {
        }

        [Fact]
        public void CreateInstance_ConstructorThrows_ThrowsTargetInvocationException()
        {
            // Put the constructor into the cache and make sure we still throw if cached.
            Assert.Throws<TargetInvocationException>(() => Activator.CreateInstance(typeof(TypeWithDefaultCtorThatThrows)));
            Assert.Throws<TargetInvocationException>(() => Activator.CreateInstance(typeof(TypeWithDefaultCtorThatThrows)));
        }

        [Fact]
        public void CreateInstance_ConstructorThrowsFromCache_ThrowsTargetInvocationException()
        {
            // Put the constructor into the cache and make sure we still throw if cached.
            Assert.IsType<TypeWithDefaultCtorThatThrowsOnSecondCall>(Activator.CreateInstance(typeof(TypeWithDefaultCtorThatThrowsOnSecondCall)));
            Assert.Throws<TargetInvocationException>(() => Activator.CreateInstance(typeof(TypeWithDefaultCtorThatThrowsOnSecondCall)));
        }

        [Theory]
        [InlineData(typeof(MarshalByRefObject))]
        [InlineData(typeof(SubMarshalByRefObject))]
        public void CreateInstance_MarshalByRefObjectNetCore_ThrowsPlatformNotSupportedException(Type type)
        {
            // Activation Attributes are not supported in .NET Core.
            Assert.Throws<PlatformNotSupportedException>(() => Activator.CreateInstance(type, null, new object[] { 1 } ));
            Assert.Throws<PlatformNotSupportedException>(() => Activator.CreateInstance(type, null, new object[] { 1, 2 } ));
        }

        [Fact]
        public static void TestActivatorOnNonActivatableFinalizableTypes()
        {
            // On runtimes where the generic Activator is implemented with special codegen intrinsics, we might allocate
            // an uninitialized instance of the object before we realize there's no default constructor to run.
            // Make sure this has no observable side effects.
            Assert.ThrowsAny<MissingMemberException>(() => { Activator.CreateInstance<TypeWithPrivateDefaultCtorAndFinalizer>(); });

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.False(TypeWithPrivateDefaultCtorAndFinalizer.WasCreated);
        }

        private class PrivateType
        {
            public PrivateType() { }
        }

        class PrivateTypeWithDefaultCtor
        {
            private PrivateTypeWithDefaultCtor() { }
        }

        class PrivateTypeWithoutDefaultCtor
        {
            private PrivateTypeWithoutDefaultCtor(int x) { }
        }

        class PrivateTypeWithDefaultCtorThatThrows
        {
            public PrivateTypeWithDefaultCtorThatThrows() { throw new Exception(); }
        }

        [Fact]
        public static void CreateInstance_Type_Bool()
        {
            Assert.Equal(typeof(PrivateType), Activator.CreateInstance(typeof(PrivateType), true).GetType());
            Assert.Equal(typeof(PrivateType), Activator.CreateInstance(typeof(PrivateType), false).GetType());

            Assert.Equal(typeof(PrivateTypeWithDefaultCtor), Activator.CreateInstance(typeof(PrivateTypeWithDefaultCtor), true).GetType());
            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance(typeof(PrivateTypeWithDefaultCtor), false).GetType());

            Assert.Throws<TargetInvocationException>(() => Activator.CreateInstance(typeof(PrivateTypeWithDefaultCtorThatThrows), true).GetType());
            Assert.Throws<TargetInvocationException>(() => Activator.CreateInstance(typeof(PrivateTypeWithDefaultCtorThatThrows), false).GetType());

            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance(typeof(PrivateTypeWithoutDefaultCtor), true).GetType());
            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance(typeof(PrivateTypeWithoutDefaultCtor), false).GetType());
        }

        public class Choice1 : Attribute
        {
            public Choice1()
            {
                I = 1;
            }

            public Choice1(int i)
            {
                I = 2;
            }

            public Choice1(string s)
            {
                I = 3;
            }

            public Choice1(double d, string optionalS = "Hey")
            {
                I = 4;
            }

            public Choice1(VarArgs varArgs, params object[] parameters)
            {
                I = 5;
            }

            public Choice1(VarStringArgs varArgs, params string[] parameters)
            {
                I = 6;
            }

            public Choice1(VarIntArgs varArgs, params int[] parameters)
            {
                I = 7;
            }

            public int I;
        }

        public class VarArgs { }

        public class VarStringArgs { }

        public class VarIntArgs { }

        public struct ValueTypeWithDefaultConstructor
        {
        }

        public class TypeWithPrivateDefaultConstructor
        {
            public int Property { get; }

            private TypeWithPrivateDefaultConstructor()
            {
                Property = -1;
            }
        }

        public class TypeWithPrivateDefaultCtorAndFinalizer
        {
            public static bool WasCreated { get; private set; }

            private TypeWithPrivateDefaultCtorAndFinalizer() { }

            ~TypeWithPrivateDefaultCtorAndFinalizer()
            {
                WasCreated = true;
            }
        }

        private interface IInterfaceType
        {
        }

        public abstract class AbstractTypeWithDefaultCtor
        {
            public AbstractTypeWithDefaultCtor() { }
        }

        public struct StructTypeWithoutReflectionMetadata { }

        public class TypeWithoutDefaultCtor
        {
            private TypeWithoutDefaultCtor(int x) { }
        }

        public class TypeWithDefaultCtorThatThrows
        {
            public TypeWithDefaultCtorThatThrows() { throw new Exception(); }
        }

        public class TypeWithDefaultCtorThatThrowsOnSecondCall
        {
            private static int i;

            public TypeWithDefaultCtorThatThrowsOnSecondCall()
            {
                if (i != 0)
                {
                    throw new Exception();
                }

                i++;
            }
        }

        class ClassWithPrivateCtor
        {
            static ClassWithPrivateCtor() { Flag.Reset(100); }
            private ClassWithPrivateCtor() { Flag.Increase(200); }
            public ClassWithPrivateCtor(int i) { Flag.Increase(300); }
        }
        class ClassWithPrivateCtor2
        {
            static ClassWithPrivateCtor2() { Flag.Reset(100); }
            private ClassWithPrivateCtor2() { Flag.Increase(200); }
            public ClassWithPrivateCtor2(int i) { Flag.Increase(300); }
        }
        class ClassWithPrivateCtor3
        {
            static ClassWithPrivateCtor3() { Flag.Reset(100); }
            private ClassWithPrivateCtor3() { Flag.Increase(200); }
            public ClassWithPrivateCtor3(int i) { Flag.Increase(300); }
        }

        class HasPublicCtor
        {
            public int Value = 0;

            public HasPublicCtor(int value) => Value = value;
        }

        class HasPrivateCtor
        {
            public int Value = 0;

            private HasPrivateCtor(int value) => Value = value;
        }

        public class IsTestedAttribute : Attribute
        {
            private bool flag;
            public IsTestedAttribute(bool flag)
            {
                this.flag = flag;

            }
        }
        [IsTestedAttribute(false)]
        class ClassWithIsTestedAttribute { }
        [Serializable]
        class ClassWithSerializableAttribute { }
        [IsTestedAttribute(false)]
        class MBRWithIsTestedAttribute : MarshalByRefObject { }
        class Flag
        {
            public static int cnt = 0;
            public static void Reset(int i) { cnt = i; }
            public static void Increase(int i) { cnt += i; }
            public static bool Equal(int i) { return cnt == i; }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsInvokingStaticConstructorsSupported))]
        public static void TestingBindingFlags()
        {
            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance(typeof(ClassWithPrivateCtor), BindingFlags.Public | BindingFlags.Instance, null, null, CultureInfo.CurrentCulture));
            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance(typeof(ClassWithPrivateCtor), BindingFlags.Public | BindingFlags.Instance, null, new object[] { 1, 2, 3 }, CultureInfo.CurrentCulture));


            Flag.Reset(0); Assert.Equal(0, Flag.cnt);
            Activator.CreateInstance(typeof(ClassWithPrivateCtor), BindingFlags.Static | BindingFlags.NonPublic, null, null, CultureInfo.CurrentCulture);
            Assert.Equal(300, Flag.cnt);
            Activator.CreateInstance(typeof(ClassWithPrivateCtor), BindingFlags.Instance | BindingFlags.NonPublic, null, null, CultureInfo.CurrentCulture);
            Assert.Equal(500, Flag.cnt);

            Flag.Reset(0); Assert.Equal(0, Flag.cnt);
            Activator.CreateInstance(typeof(ClassWithPrivateCtor2), BindingFlags.Instance | BindingFlags.NonPublic, null, null, CultureInfo.CurrentCulture);
            Assert.Equal(300, Flag.cnt);
            Activator.CreateInstance(typeof(ClassWithPrivateCtor2), BindingFlags.Static | BindingFlags.NonPublic, null, null, CultureInfo.CurrentCulture);
            Assert.Equal(500, Flag.cnt);

            Flag.Reset(0); Assert.Equal(0, Flag.cnt);
            Activator.CreateInstance(typeof(ClassWithPrivateCtor3), BindingFlags.Instance | BindingFlags.Public, null, new object[] { 122 }, CultureInfo.CurrentCulture);
            Assert.Equal(400, Flag.cnt);
            Activator.CreateInstance(typeof(ClassWithPrivateCtor3), BindingFlags.Static | BindingFlags.NonPublic, null, null, CultureInfo.CurrentCulture);
            Assert.Equal(600, Flag.cnt);
            Activator.CreateInstance(typeof(ClassWithPrivateCtor3), BindingFlags.Instance | BindingFlags.Public, null, new object[] { 122 }, CultureInfo.CurrentCulture);
            Assert.Equal(900, Flag.cnt);

            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance(typeof(ClassWithPrivateCtor), BindingFlags.Public | BindingFlags.Static, null, null, CultureInfo.CurrentCulture));
            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance(typeof(ClassWithPrivateCtor), BindingFlags.Public | BindingFlags.Static, null, new object[] { 122 }, CultureInfo.CurrentCulture));
        }

        [Fact]
        public static void TestingBindingFlagsInstanceOnly()
        {
            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance(typeof(HasPublicCtor), default(BindingFlags), null, null, null));
            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance(typeof(HasPublicCtor), BindingFlags.NonPublic | BindingFlags.Instance, null, null, null));
            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance(typeof(HasPublicCtor), BindingFlags.Public | BindingFlags.Static, null, null, null));

            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance(typeof(HasPrivateCtor), default(BindingFlags), null, null, null));
            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance(typeof(HasPrivateCtor), BindingFlags.Public | BindingFlags.Instance, null, null, null));
            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance(typeof(HasPrivateCtor), BindingFlags.NonPublic | BindingFlags.Static, null, null, null));

            {
                HasPublicCtor a = (HasPublicCtor)Activator.CreateInstance(typeof(HasPublicCtor), BindingFlags.Public | BindingFlags.Instance, null, new object[] { 100 }, null);
                Assert.Equal(100, a.Value);
            }

            {
                HasPrivateCtor a = (HasPrivateCtor)Activator.CreateInstance(typeof(HasPrivateCtor), BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { 100 }, null);
                Assert.Equal(100, a.Value);
            }
        }

        [Fact]
        public static void TestingBindingFlags1()
        {
            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance(typeof(ClassWithPrivateCtor), BindingFlags.Public | BindingFlags.Instance, null, null, CultureInfo.CurrentCulture, null));
            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance(typeof(ClassWithPrivateCtor), BindingFlags.Public | BindingFlags.Instance, null, new object[] { 1, 2, 3 }, CultureInfo.CurrentCulture, null));

            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance(typeof(ClassWithPrivateCtor), BindingFlags.Public | BindingFlags.Static, null, null, CultureInfo.CurrentCulture, null));
            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance(typeof(ClassWithPrivateCtor), BindingFlags.Public | BindingFlags.Static, null, new object[] { 122 }, CultureInfo.CurrentCulture, null));
        }

        [Fact]
        public static void TestingActivationAttributes()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Activator.CreateInstance(typeof(ClassWithIsTestedAttribute), null, new object[] { new object() }));
            Assert.Throws<PlatformNotSupportedException>(() => Activator.CreateInstance(typeof(ClassWithIsTestedAttribute), null, new object[] { new IsTestedAttribute(true) }));
            Assert.Throws<PlatformNotSupportedException>(() => Activator.CreateInstance(typeof(ClassWithSerializableAttribute), null, new object[] { new ClassWithIsTestedAttribute() }));
            Assert.Throws<PlatformNotSupportedException>(() => Activator.CreateInstance(typeof(MBRWithIsTestedAttribute), null, new object[] { new IsTestedAttribute(true) }));

            Assert.Throws<PlatformNotSupportedException>(() => Activator.CreateInstance(typeof(ClassWithIsTestedAttribute), 0, null, null, CultureInfo.CurrentCulture, new object[] { new IsTestedAttribute(true) }));
            Assert.Throws<PlatformNotSupportedException>(() => Activator.CreateInstance(typeof(ClassWithSerializableAttribute), 0, null, null, CultureInfo.CurrentCulture, new object[] { new ClassWithIsTestedAttribute() }));
            Assert.Throws<PlatformNotSupportedException>(() => Activator.CreateInstance(typeof(MBRWithIsTestedAttribute), 0, null, null, CultureInfo.CurrentCulture, new object[] { new IsTestedAttribute(true) }));
        }

        [Fact]
        public static void TestingActivationAttributes1()
        {
            Activator.CreateInstance(typeof(ClassWithIsTestedAttribute), null, null);
            Activator.CreateInstance(typeof(ClassWithIsTestedAttribute), null, new object[] { });
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        public void CreateInstance_NonPublicValueTypeWithPrivateDefaultConstructor_Success()
        {
            AssemblyName assemblyName = new AssemblyName("Assembly");
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");
            TypeBuilder typeBuilder = moduleBuilder.DefineType("Type", TypeAttributes.Public, typeof(ValueType));

            FieldBuilder fieldBuilder = typeBuilder.DefineField("_field", typeof(int), FieldAttributes.Public);
            ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Private | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig, CallingConventions.Standard, new Type[0]);

            ILGenerator generator = constructorBuilder.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldc_I4, -1);
            generator.Emit(OpCodes.Stfld, fieldBuilder);
            generator.Emit(OpCodes.Ret);

            Type type = typeBuilder.CreateType();
            FieldInfo field = type.GetField("_field");

            // Activator holds a cache of constructors and the types to which they belong.
            // Test caching behaviour by activating multiple times.
            object v1 = Activator.CreateInstance(type, nonPublic: true);
            Assert.Equal(-1, field.GetValue(v1));

            object v2 = Activator.CreateInstance(type, nonPublic: true);
            Assert.Equal(-1, field.GetValue(v2));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        public void CreateInstance_PublicOnlyValueTypeWithPrivateDefaultConstructor_ThrowsMissingMethodException()
        {
            AssemblyName assemblyName = new AssemblyName("Assembly");
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");
            TypeBuilder typeBuilder = moduleBuilder.DefineType("Type", TypeAttributes.Public, typeof(ValueType));

            FieldBuilder fieldBuilder = typeBuilder.DefineField("_field", typeof(int), FieldAttributes.Public);
            ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Private | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig, CallingConventions.Standard, new Type[0]);

            ILGenerator generator = constructorBuilder.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldc_I4, -1);
            generator.Emit(OpCodes.Stfld, fieldBuilder);
            generator.Emit(OpCodes.Ret);

            Type type = typeBuilder.CreateType();

            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance(type));
            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance(type, nonPublic: false));

            // Put the private default constructor into the cache and make sure we still throw if public only.
            Assert.NotNull(Activator.CreateInstance(type, nonPublic: true));

            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance(type));
            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance(type, nonPublic: false));
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34030", TestPlatforms.Linux | TestPlatforms.Browser, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        [MemberData(nameof(TestingCreateInstanceFromObjectHandleData))]
        public static void TestingCreateInstanceFromObjectHandle(string physicalFileName, string assemblyFile, string type, string returnedFullNameType, Type exceptionType)
        {
            ObjectHandle oh = null;

            if (exceptionType != null)
            {
                Assert.Throws(exceptionType, () => Activator.CreateInstanceFrom(assemblyFile: assemblyFile, typeName: type));
            }
            else
            {
                oh = Activator.CreateInstanceFrom(assemblyFile: assemblyFile, typeName: type);
                CheckValidity(oh, returnedFullNameType);
            }

            if (exceptionType != null)
            {
                Assert.Throws(exceptionType, () => Activator.CreateInstanceFrom(assemblyFile: assemblyFile, typeName: type, null));
            }
            else
            {
                oh = Activator.CreateInstanceFrom(assemblyFile: assemblyFile, typeName: type, null);
                CheckValidity(oh, returnedFullNameType);
            }
            Assert.True(File.Exists(physicalFileName));
        }

        public static TheoryData<string, string, string, string, Type> TestingCreateInstanceFromObjectHandleData => new TheoryData<string, string, string, string, Type>()
        {
            // string physicalFileName, string assemblyFile, string typeName, returnedFullNameType, expectedException
            { "TestLoadAssembly.dll", "TestLoadAssembly.dll", "PublicClassSample", "PublicClassSample", null },
            { "TestLoadAssembly.dll", "testloadassembly.dll", "publicclasssample", "PublicClassSample", typeof(TypeLoadException) },

            { "TestLoadAssembly.dll", "TestLoadAssembly.dll", "PrivateClassSample", "PrivateClassSample", null },
            { "TestLoadAssembly.dll", "testloadassembly.dll", "privateclasssample", "PrivateClassSample", typeof(TypeLoadException) },

            { "TestLoadAssembly.dll", "TestLoadAssembly.dll", "PublicClassNoDefaultConstructorSample", "PublicClassNoDefaultConstructorSample", typeof(MissingMethodException) },
            { "TestLoadAssembly.dll", "testloadassembly.dll", "publicclassnodefaultconstructorsample", "PublicClassNoDefaultConstructorSample", typeof(TypeLoadException) }
        };

        [Theory]
        [MemberData(nameof(TestingCreateInstanceObjectHandleData))]
        public static void TestingCreateInstanceObjectHandle(string assemblyName, string type, string returnedFullNameType, Type exceptionType, bool returnNull)
        {
            ObjectHandle oh = null;

            if (exceptionType != null)
            {
                Assert.Throws(exceptionType, () => Activator.CreateInstance(assemblyName: assemblyName, typeName: type));
            }
            else
            {
                oh = Activator.CreateInstance(assemblyName: assemblyName, typeName: type);
                if (returnNull)
                {
                    Assert.Null(oh);
                }
                else
                {
                    CheckValidity(oh, returnedFullNameType);
                }
            }

            if (exceptionType != null)
            {
                Assert.Throws(exceptionType, () => Activator.CreateInstance(assemblyName: assemblyName, typeName: type, null));
            }
            else
            {
                oh = Activator.CreateInstance(assemblyName: assemblyName, typeName: type, null);
                if (returnNull)
                {
                    Assert.Null(oh);
                }
                else
                {
                    CheckValidity(oh, returnedFullNameType);
                }
            }
        }

        public static TheoryData<string, string, string, Type, bool> TestingCreateInstanceObjectHandleData => new TheoryData<string, string, string, Type, bool>()
        {
            // string assemblyName, string typeName, returnedFullNameType, expectedException
            { "TestLoadAssembly", "PublicClassSample", "PublicClassSample", null, false },
            { "testloadassembly", "publicclasssample", "PublicClassSample", typeof(TypeLoadException), false },

            { "TestLoadAssembly", "PrivateClassSample", "PrivateClassSample", null, false },
            { "testloadassembly", "privateclasssample", "PrivateClassSample", typeof(TypeLoadException), false },

            { "TestLoadAssembly", "PublicClassNoDefaultConstructorSample", "PublicClassNoDefaultConstructorSample", typeof(MissingMethodException), false },
            { "testloadassembly", "publicclassnodefaultconstructorsample", "PublicClassNoDefaultConstructorSample", typeof(TypeLoadException), false },

            { "mscorlib", "System.Nullable`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]", "", null, true }
        };

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34030", TestPlatforms.Linux | TestPlatforms.Browser, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        [MemberData(nameof(TestingCreateInstanceFromObjectHandleFullSignatureData))]
        public static void TestingCreateInstanceFromObjectHandleFullSignature(string physicalFileName, string assemblyFile, string type, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes, string returnedFullNameType)
        {
            ObjectHandle oh = Activator.CreateInstanceFrom(assemblyFile: assemblyFile, typeName: type, ignoreCase: ignoreCase, bindingAttr: bindingAttr, binder: binder, args: args, culture: culture, activationAttributes: activationAttributes);
            CheckValidity(oh, returnedFullNameType);
            Assert.True(File.Exists(physicalFileName));
        }

        public static IEnumerable<object[]> TestingCreateInstanceFromObjectHandleFullSignatureData()
        {
            // string physicalFileName, string assemblyFile, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes, returnedFullNameType
            yield return new object[] { "TestLoadAssembly.dll", "TestLoadAssembly.dll", "PublicClassSample", false, BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[0], CultureInfo.InvariantCulture, null, "PublicClassSample" };
            yield return new object[] { "TestLoadAssembly.dll", "testloadassembly.dll", "publicclasssample", true, BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[0], CultureInfo.InvariantCulture, null, "PublicClassSample" };
            yield return new object[] { "TestLoadAssembly.dll", "TestLoadAssembly.dll", "PublicClassSample", false, BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[1] { 1 }, CultureInfo.InvariantCulture, null, "PublicClassSample" };
            yield return new object[] { "TestLoadAssembly.dll", "testloadassembly.dll", "publicclasssample", true, BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[1] { 1 }, CultureInfo.InvariantCulture, null, "PublicClassSample" };

            yield return new object[] { "TestLoadAssembly.dll", "TestLoadAssembly.dll", "PrivateClassSample", false, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[0], CultureInfo.InvariantCulture, null, "PrivateClassSample" };
            yield return new object[] { "TestLoadAssembly.dll", "testloadassembly.dll", "privateclasssample", true, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[0], CultureInfo.InvariantCulture, null, "PrivateClassSample" };
            yield return new object[] { "TestLoadAssembly.dll", "TestLoadAssembly.dll", "PrivateClassSample", false, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[1] { 1 }, CultureInfo.InvariantCulture, null, "PrivateClassSample" };
            yield return new object[] { "TestLoadAssembly.dll", "testloadassembly.dll", "privateclasssample", true, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[1] { 1 }, CultureInfo.InvariantCulture, null, "PrivateClassSample" };
        }

        [Theory]
        [MemberData(nameof(TestingCreateInstanceObjectHandleFullSignatureData))]
        public static void TestingCreateInstanceObjectHandleFullSignature(string assemblyName, string type, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes, string returnedFullNameType, bool returnNull)
        {
            ObjectHandle oh = Activator.CreateInstance(assemblyName: assemblyName, typeName: type, ignoreCase: ignoreCase, bindingAttr: bindingAttr, binder: binder, args: args, culture: culture, activationAttributes: activationAttributes);
            if (returnNull)
            {
                Assert.Null(oh);
            }
            else
            {
                CheckValidity(oh, returnedFullNameType);
            }
        }

        public static IEnumerable<object[]> TestingCreateInstanceObjectHandleFullSignatureData()
        {
            // string assemblyName, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes, returnedFullNameType
            yield return new object[] { "TestLoadAssembly", "PublicClassSample", false, BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[0], CultureInfo.InvariantCulture, null, "PublicClassSample" , false };
            yield return new object[] { "testloadassembly", "publicclasssample", true, BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[0], CultureInfo.InvariantCulture, null, "PublicClassSample" , false };
            yield return new object[] { "TestLoadAssembly", "PublicClassSample", false, BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[1] { 1 }, CultureInfo.InvariantCulture, null, "PublicClassSample" , false };
            yield return new object[] { "testloadassembly", "publicclasssample", true, BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[1] { 1 }, CultureInfo.InvariantCulture, null, "PublicClassSample" , false };

            yield return new object[] { "TestLoadAssembly", "PrivateClassSample", false, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[0], CultureInfo.InvariantCulture, null, "PrivateClassSample", false };
            yield return new object[] { "testloadassembly", "privateclasssample", true, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[0], CultureInfo.InvariantCulture, null, "PrivateClassSample", false };
            yield return new object[] { "TestLoadAssembly", "PrivateClassSample", false, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[1] { 1 }, CultureInfo.InvariantCulture, null, "PrivateClassSample", false };
            yield return new object[] { "testloadassembly", "privateclasssample", true, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[1] { 1 }, CultureInfo.InvariantCulture, null, "PrivateClassSample", false };

            yield return new object[] { null, typeof(PublicType).FullName, false, BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[0], CultureInfo.InvariantCulture, null, typeof(PublicType).FullName, false };
            yield return new object[] { null, typeof(PrivateType).FullName, false, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[0], CultureInfo.InvariantCulture, null, typeof(PrivateType).FullName, false };

            yield return new object[] { "mscorlib", "System.Nullable`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]", true, BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[0], CultureInfo.InvariantCulture, null, "", true };
            yield return new object[] { "mscorlib", "SyStEm.NULLABLE`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]", true, BindingFlags.Public | BindingFlags.Instance, Type.DefaultBinder, new object[0], CultureInfo.InvariantCulture, null, "", true };
        }

        private static void CheckValidity(ObjectHandle instance, string expected)
        {
            Assert.NotNull(instance);
            Assert.Equal(expected, instance.Unwrap().GetType().FullName);
        }

        public class PublicType
        {
            public PublicType() { }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void CreateInstanceAssemblyResolve()
        {
            RemoteExecutor.Invoke(() =>
            {
                AppDomain.CurrentDomain.AssemblyResolve += (object sender, ResolveEventArgs args) => Assembly.LoadFile(Path.Combine(Directory.GetCurrentDirectory(), "TestLoadAssembly.dll"));
                Assert.Throws<FileLoadException>(() => Activator.CreateInstance(",,,,", "PublicClassSample"));
            }).Dispose();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        public void CreateInstance_TypeBuilder_ThrowsNotSupportedException()
        {
            AssemblyName assemblyName = new AssemblyName("Assembly");
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");
            TypeBuilder typeBuilder = moduleBuilder.DefineType("Type", TypeAttributes.Public);

            Assert.Throws<ArgumentException>("type", () => Activator.CreateInstance(typeBuilder));
            Assert.Throws<NotSupportedException>(() => Activator.CreateInstance(typeBuilder, new object[0]));
        }
    }
}
