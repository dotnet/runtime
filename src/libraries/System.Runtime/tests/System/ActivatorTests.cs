// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Reflection;
using Xunit;

namespace System.Tests
{
    public static class ActivatorTests
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
        public static void CreateInstance_Invalid()
        {
            Type nullType = null;
            AssertExtensions.Throws<ArgumentNullException>("type", () => Activator.CreateInstance(nullType)); // Type is null
            AssertExtensions.Throws<ArgumentNullException>("type", () => Activator.CreateInstance(null, new object[0])); // Type is null

            Assert.Throws<AmbiguousMatchException>(() => Activator.CreateInstance(typeof(Choice1), new object[] { null }));

            // C# designated optional parameters are not optional as far as Activator.CreateInstance() is concerned.
            Assert.ThrowsAny<MissingMemberException>(() => Activator.CreateInstance(typeof(Choice1), new object[] { 5.1 }));
            Assert.ThrowsAny<MissingMemberException>(() => Activator.CreateInstance(typeof(Choice1), new object[] { 5.1, Type.Missing }));

            // Invalid params args
            Assert.ThrowsAny<MissingMemberException>(() => Activator.CreateInstance(typeof(Choice1), new object[] { new VarStringArgs(), 5, 6 }));

            // Primitive widening not supported for "params" arguments.
            //
            // (This is probably an accidental behavior on the desktop as the default binder specifically checks to see if the params arguments are widenable to the
            // params array element type and gives it the go-ahead if it is. Unfortunately, the binder then bollixes itself by using Array.Copy() to copy
            // the params arguments. Since Array.Copy() doesn't tolerate this sort of type mismatch, it throws an InvalidCastException which bubbles out
            // out of Activator.CreateInstance. Accidental or not, we'll inherit that behavior on .NET Native.)
            Assert.Throws<InvalidCastException>(() => Activator.CreateInstance(typeof(Choice1), new object[] { new VarIntArgs(), 1, (short)2 }));

            Assert.ThrowsAny<MissingMemberException>(() => Activator.CreateInstance(typeof(TypeWithoutDefaultCtor))); // Type has no default constructor
            Assert.Throws<TargetInvocationException>(() => Activator.CreateInstance(typeof(TypeWithDefaultCtorThatThrows))); // Type has a default constructor throws an exception
            Assert.ThrowsAny<MissingMemberException>(() => Activator.CreateInstance(typeof(AbstractTypeWithDefaultCtor))); // Type is abstract
            Assert.ThrowsAny<MissingMemberException>(() => Activator.CreateInstance(typeof(IInterfaceType))); // Type is an interface

#if netcoreapp || uapaot
            foreach (Type nonRuntimeType in Helpers.NonRuntimeTypes)
            {
                // Type is not a valid RuntimeType
                AssertExtensions.Throws<ArgumentException>("type", () => Activator.CreateInstance(nonRuntimeType));
            }
#endif // netcoreapp || uapaot
        }

        [Fact]
        public static void CreateInstance_Generic()
        {
            Choice1 c = Activator.CreateInstance<Choice1>();
            Assert.Equal(1, c.I);

            Activator.CreateInstance<DateTime>();
            Activator.CreateInstance<StructTypeWithoutReflectionMetadata>();
        }

        [Fact]
        public static void CreateInstance_Generic_Invalid()
        {
            Assert.ThrowsAny<MissingMemberException>(() => Activator.CreateInstance<int[]>()); // Cannot create array type

            Assert.ThrowsAny<MissingMemberException>(() => Activator.CreateInstance<TypeWithoutDefaultCtor>()); // Type has no default constructor
            Assert.Throws<TargetInvocationException>(() => Activator.CreateInstance<TypeWithDefaultCtorThatThrows>()); // Type has a default constructor that throws
            Assert.ThrowsAny<MissingMemberException>(() => Activator.CreateInstance<AbstractTypeWithDefaultCtor>()); // Type is abstract
            Assert.ThrowsAny<MissingMemberException>(() => Activator.CreateInstance<IInterfaceType>()); // Type is an interface
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

        class PrivateType
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
        static void TestingBindingFlags()
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
        static void TestingBindingFlagsInstanceOnly()
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
        static void TestingBindingFlags1()
        {
            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance(typeof(ClassWithPrivateCtor), BindingFlags.Public | BindingFlags.Instance, null, null, CultureInfo.CurrentCulture, null));
            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance(typeof(ClassWithPrivateCtor), BindingFlags.Public | BindingFlags.Instance, null, new object[] { 1, 2, 3 }, CultureInfo.CurrentCulture, null));

            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance(typeof(ClassWithPrivateCtor), BindingFlags.Public | BindingFlags.Static, null, null, CultureInfo.CurrentCulture, null));
            Assert.Throws<MissingMethodException>(() => Activator.CreateInstance(typeof(ClassWithPrivateCtor), BindingFlags.Public | BindingFlags.Static, null, new object[] { 122 }, CultureInfo.CurrentCulture, null));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        static void TestingActivationAttributes()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Activator.CreateInstance(typeof(ClassWithIsTestedAttribute), null, new object[] { new Object() }));
            Assert.Throws<PlatformNotSupportedException>(() => Activator.CreateInstance(typeof(ClassWithIsTestedAttribute), null, new object[] { new IsTestedAttribute(true) }));
            Assert.Throws<PlatformNotSupportedException>(() => Activator.CreateInstance(typeof(ClassWithSerializableAttribute), null, new object[] { new ClassWithIsTestedAttribute() }));
            Assert.Throws<PlatformNotSupportedException>(() => Activator.CreateInstance(typeof(MBRWithIsTestedAttribute), null, new object[] { new IsTestedAttribute(true) }));

            Assert.Throws<PlatformNotSupportedException>(() => Activator.CreateInstance(typeof(ClassWithIsTestedAttribute), 0, null, null, CultureInfo.CurrentCulture, new object[] { new IsTestedAttribute(true) }));
            Assert.Throws<PlatformNotSupportedException>(() => Activator.CreateInstance(typeof(ClassWithSerializableAttribute), 0, null, null, CultureInfo.CurrentCulture, new object[] { new ClassWithIsTestedAttribute() }));
            Assert.Throws<PlatformNotSupportedException>(() => Activator.CreateInstance(typeof(MBRWithIsTestedAttribute), 0, null, null, CultureInfo.CurrentCulture, new object[] { new IsTestedAttribute(true) }));
        }

        [Fact]
        static void TestingActivationAttributes1()
        {
            Activator.CreateInstance(typeof(ClassWithIsTestedAttribute), null, null);
            Activator.CreateInstance(typeof(ClassWithIsTestedAttribute), null, new object[] { });

        }
    }
}
