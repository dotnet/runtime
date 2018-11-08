// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

#pragma warning disable 0414
#pragma warning disable 0067

namespace System.Reflection.Tests
{
    [System.Runtime.InteropServices.Guid("FD80F123-BEDD-4492-B50A-5D46AE94DD4E")]

    public class TypeInfoPropertyTests
    {
        // Verify BaseType() method
        [Fact]
        public static void TestBaseType1()
        {
            Type t = typeof(TypeInfoPropertyDerived).Project();
            TypeInfo ti = t.GetTypeInfo();

            Type basetype = ti.BaseType;

            Assert.Equal(basetype, typeof(TypeInfoPropertyBase).Project());
        }

        // Verify BaseType() method
        [Fact]
        public static void TestBaseType2()
        {
            Type t = typeof(TypeInfoPropertyBase).Project();
            TypeInfo ti = t.GetTypeInfo();

            Type basetype = ti.BaseType;

            Assert.Equal(basetype, typeof(object).Project());
        }

        // Verify ContainsGenericParameter 
        [Fact]
        public static void TestContainsGenericParameter1()
        {
            Type t = typeof(ClassWithConstraints<,>).Project();
            TypeInfo ti = t.GetTypeInfo();

            bool hasgenericParam = ti.ContainsGenericParameters;

            Assert.True(hasgenericParam, string.Format("Failed!! TestContainsGenericParameter did not return correct result. "));
        }

        // Verify ContainsGenericParameter 
        [Fact]
        public static void TestContainsGenericParameter2()
        {
            Type t = typeof(TypeInfoPropertyBase).Project();
            TypeInfo ti = t.GetTypeInfo();

            bool hasgenericParam = ti.ContainsGenericParameters;

            Assert.False(hasgenericParam, string.Format("Failed!! TestContainsGenericParameter did not return correct result. "));
        }

        // Verify FullName
        [Fact]
        public static void TestFullName()
        {
            Type t = typeof(int).Project();
            TypeInfo ti = t.GetTypeInfo();

            string fname = ti.FullName;
            Assert.Equal(fname, "System.Int32");
        }


        // Verify Guid
        [Fact]
        public static void TestGuid()
        {
            Type t = typeof(TypeInfoPropertyTests).Project();
            TypeInfo ti = t.GetTypeInfo();

            Guid myguid = ti.GUID;
            Assert.NotNull(myguid);
        }

        // Verify HasElementType
        [Fact]
        public static void TestHasElementType()
        {
            Type t = typeof(int).Project();
            TypeInfo ti = t.GetTypeInfo();

            Assert.False(ti.HasElementType, "Failed!! .HasElementType returned true for a type that does not contain element ");

            int[] nums = { 1, 1, 2, 3 };
            Type te = nums.GetType();
            TypeInfo tei = te.GetTypeInfo();

            Assert.True(tei.HasElementType, "Failed!! .HasElementType returned false for a type that contains element ");
        }

        //Verify IsAbstract
        [Fact]
        public static void TestIsAbstract()
        {
            Assert.True(typeof(abstractClass).Project().GetTypeInfo().IsAbstract, "Failed!! .IsAbstract returned false for a type that is abstract.");
            Assert.False(typeof(TypeInfoPropertyBase).Project().GetTypeInfo().IsAbstract, "Failed!! .IsAbstract returned true for a type that is not abstract.");
        }

        //Verify IsAnsiClass
        [Fact]
        public static void TestIsAnsiClass()
        {
            string mystr = "A simple string";
            Type t = mystr.GetType();
            TypeInfo ti = t.GetTypeInfo();
            Assert.True(ti.IsAnsiClass, "Failed!! .IsAnsiClass returned false.");
        }

        //Verify IsAray
        [Fact]
        public static void TestIsArray()
        {
            int[] myarray = { 1, 2, 3 };
            Type arraytype = myarray.GetType();
            Assert.True(arraytype.GetTypeInfo().IsArray, "Failed!! .IsArray returned false for a type that is array.");
            Assert.False(typeof(int).Project().GetTypeInfo().IsArray, "Failed!! .IsArray returned true for a type that is not an array.");
        }

        // VerifyIsByRef
        [Fact]
        public static void TestIsByRefType()
        {
            TypeInfo ti = typeof(int).Project().GetTypeInfo();
            Type byreftype = ti.MakeByRefType();

            Assert.NotNull(byreftype);
            Assert.True(byreftype.IsByRef, "Failed!!  IsByRefType() returned false");
            Assert.False(typeof(int).Project().GetTypeInfo().IsByRef, "Failed!!  IsByRefType() returned true");
        }


        // VerifyIsClass  
        [Fact]
        public static void TestIsClass()
        {
            Assert.True(typeof(TypeInfoPropertyBase).Project().GetTypeInfo().IsClass, "Failed!!  IsClass returned false for a class Type");
            Assert.False(typeof(MYENUM).Project().GetTypeInfo().IsClass, "Failed!!  IsClass returned true for a non-class Type");
        }


        // VerifyIsEnum  
        [Fact]
        public static void TestIsEnum()
        {
            Assert.False(typeof(TypeInfoPropertyBase).Project().GetTypeInfo().IsEnum, "Failed!!  IsEnum returned true for a class Type");
            Assert.True(typeof(MYENUM).Project().GetTypeInfo().IsEnum, "Failed!!  IsEnum returned false for a Enum Type");
        }

        // VerifyIsInterface  
        [Fact]
        public static void TestIsInterface()
        {
            Assert.False(typeof(TypeInfoPropertyBase).Project().GetTypeInfo().IsInterface, "Failed!!  IsInterface returned true for a class Type");
            Assert.True(typeof(ITest).Project().GetTypeInfo().IsInterface, "Failed!!   IsInterface returned false for a interface Type");
        }

        // VerifyIsNested  
        [Fact]
        public static void TestIsNested()
        {
            Assert.False(typeof(TypeInfoPropertyBase).Project().GetTypeInfo().IsNested, "Failed!!  IsNested returned true for a non nested class Type");
            Assert.True(typeof(PublicClass.PublicNestedType).Project().GetTypeInfo().IsNested, "Failed!!    IsNested returned false for a nested class Type");
        }


        // Verify IsPointer
        [Fact]
        public static void TestIsPointer()
        {
            TypeInfo ti = typeof(int).Project().GetTypeInfo();
            Type ptrtype = ti.MakePointerType();

            Assert.NotNull(ptrtype);
            Assert.True(ptrtype.IsPointer, "Failed!!  IsPointer returned false for pointer type");
            Assert.False(typeof(int).Project().GetTypeInfo().IsPointer, "Failed!!  IsPointer returned true for non -pointer type");
        }


        // VerifyIsPrimitive 
        [Fact]
        public static void TestIsPrimitive()
        {
            Assert.False(typeof(TypeInfoPropertyBase).Project().GetTypeInfo().IsPrimitive, "Failed!!  IsPrimitive returned true for a non primitive Type");
            Assert.True(typeof(int).Project().GetTypeInfo().IsPrimitive, "Failed!!    IsPrimitive returned true for a primitive Type");
            Assert.True(typeof(char).Project().GetTypeInfo().IsPrimitive, "Failed!!    IsPrimitive returned true for a primitive Type");
        }



        // VerifyIsPublic 
        [Fact]
        public static void TestIsPublic()
        {
            Assert.True(typeof(TypeInfoPropertyBase).Project().GetTypeInfo().IsPublic, "Failed!!  IsPublic returned false for a public Type");
            Assert.True(typeof(TypeInfoPropertyDerived).Project().GetTypeInfo().IsPublic, "Failed!!  IsPublic returned false for a public Type");
            Assert.True(typeof(PublicClass).Project().GetTypeInfo().IsPublic, "Failed!!  IsPublic returned false for a public Type");
            Assert.True(typeof(ITest).Project().GetTypeInfo().IsPublic, "Failed!!  IsPublic returned false for a public Type");
            Assert.True(typeof(ImplClass).Project().GetTypeInfo().IsPublic, "Failed!!  IsPublic returned false for a public Type");
        }


        // VerifyIsNotPublic 
        [Fact]
        public static void TestIsNotPublic()
        {
            Assert.False(typeof(TypeInfoPropertyBase).Project().GetTypeInfo().IsNotPublic, "Failed!!  IsNotPublic returned false for a public Type");
            Assert.False(typeof(TypeInfoPropertyDerived).Project().GetTypeInfo().IsNotPublic, "Failed!!  IsNotPublic returned false for a public Type");
            Assert.False(typeof(PublicClass).Project().GetTypeInfo().IsNotPublic, "Failed!! IsNotPublic returned false for a public Type");
            Assert.False(typeof(ITest).Project().GetTypeInfo().IsNotPublic, "Failed!!  IsNotPublic returned false for a public Type");
            Assert.False(typeof(ImplClass).Project().GetTypeInfo().IsNotPublic, "Failed!!  IsNotPublic returned false for a public Type");
        }


        // VerifyIsNestedPublic 
        [Fact]
        public static void TestIsNestedPublic()
        {
            Assert.True(typeof(PublicClass.publicNestedClass).Project().GetTypeInfo().IsNestedPublic, "Failed!!  IsNestedPublic returned false for a nested public Type");
        }

        // VerifyIsNestedPrivate 
        [Fact]
        public static void TestIsNestedPrivate()
        {
            Assert.False(typeof(PublicClass.publicNestedClass).Project().GetTypeInfo().IsNestedPrivate, "Failed!!  IsNestedPrivate returned true for a nested public Type");
        }


        // Verify IsSealed 
        [Fact]
        public static void TestIsSealed()
        {
            Assert.False(typeof(TypeInfoPropertyBase).Project().GetTypeInfo().IsSealed, "Failed!!  IsSealed returned true for a Type that is not sealed");
            Assert.True(typeof(sealedClass).Project().GetTypeInfo().IsSealed, "Failed!!  IsSealed returned false for a Type that is sealed");
        }


        // Verify IsSerializable
        [Fact]
        public static void TestIsSerializable()
        {
            Assert.False(typeof(TypeInfoPropertyBase).Project().GetTypeInfo().IsSerializable, "Failed!!  IsSerializable returned true for a Type that is not serializable");
        }

        // VerifyIsValueType
        [Fact]
        public static void TestIsValueType()
        {
            Assert.False(typeof(TypeInfoPropertyBase).Project().GetTypeInfo().IsValueType, "Failed!!  IsValueType returned true for a class Type");
            Assert.True(typeof(MYENUM).Project().GetTypeInfo().IsValueType, "Failed!!  IsValueType returned false for a Enum Type");
        }

        // VerifyIsValueType
        [Fact]
        public static void TestIsVisible()
        {
            Assert.True(typeof(TypeInfoPropertyBase).Project().GetTypeInfo().IsVisible, "Failed!!  IsVisible returned false");
            Assert.True(typeof(ITest).Project().GetTypeInfo().IsVisible, "Failed!!  IsVisible returned false");
            Assert.True(typeof(MYENUM).Project().GetTypeInfo().IsVisible, "Failed!!  IsVisible returned false");
            Assert.True(typeof(PublicClass).Project().GetTypeInfo().IsVisible, "Failed!!  IsVisible returned false");
            Assert.True(typeof(PublicClass.publicNestedClass).Project().GetTypeInfo().IsVisible, "Failed!!  IsVisible returned false");
        }


        // Verify Namespace property
        [Fact]
        public static void TestNamespace()
        {
            Assert.Equal(typeof(TypeInfoPropertyBase).Project().GetTypeInfo().Namespace, "System.Reflection.Tests");
            Assert.Equal(typeof(ITest).Project().GetTypeInfo().Namespace, "System.Reflection.Tests");
            Assert.Equal(typeof(MYENUM).Project().GetTypeInfo().Namespace, "System.Reflection.Tests");
            Assert.Equal(typeof(PublicClass).Project().GetTypeInfo().Namespace, "System.Reflection.Tests");
            Assert.Equal(typeof(PublicClass.publicNestedClass).Project().GetTypeInfo().Namespace, "System.Reflection.Tests");
            Assert.Equal(typeof(int).Project().GetTypeInfo().Namespace, "System");
        }

        // VerifyIsImport 
        [Fact]
        public static void TestIsImport()
        {
            Assert.False(typeof(TypeInfoPropertyBase).Project().GetTypeInfo().IsImport, "Failed!!  IsImport returned true for a class Type that is not imported.");
            Assert.False(typeof(MYENUM).Project().GetTypeInfo().IsImport, "Failed!!  IsImport returned true for a non-class Type that is not imported.");
        }

        // VerifyIsUnicodeClass 
        [Fact]
        public static void TestIsUnicodeClass()
        {
            string str = "mystring";
            Type type = str.GetType();
            Type ref_type = type.MakeByRefType();

            Assert.False(type.GetTypeInfo().IsUnicodeClass, "Failed!!  IsUnicodeClass returned true for string.");
            Assert.False(ref_type.GetTypeInfo().IsUnicodeClass, "Failed!!  IsUnicodeClass returned true for string.");
        }

        // Verify IsAutoClass 
        [Fact]
        public static void TestIsAutoClass()
        {
            string str = "mystring";
            Type type = str.GetType();
            Type ref_type = type.MakeByRefType();

            Assert.False(type.GetTypeInfo().IsAutoClass, "Failed!!  IsAutoClass returned true for string.");
            Assert.False(ref_type.GetTypeInfo().IsAutoClass, "Failed!!  IsAutoClass returned true for string.");
        }


        [Fact]
        public static void TestIsMarshalByRef()
        {
            string str = "mystring";
            Type type = str.GetType();
            Type ptr_type = type.MakePointerType();
            Type ref_type = type.MakeByRefType();

            Assert.False(type.GetTypeInfo().IsMarshalByRef, "Failed!!  IsMarshalByRef returned true.");
            Assert.False(ptr_type.GetTypeInfo().IsMarshalByRef, "Failed!!  IsMarshalByRef returned true.");
            Assert.False(ref_type.GetTypeInfo().IsMarshalByRef, "Failed!!  IsMarshalByRef returned true.");
        }



        // VerifyIsNestedAssembly  
        [Fact]
        public static void TestIsNestedAssembly()
        {
            Assert.False(typeof(PublicClass).Project().GetTypeInfo().IsNestedAssembly, "Failed!!  IsNestedAssembly returned true for a class with public visibility.");
        }


        // VerifyIsNestedFamily  
        [Fact]
        public static void TestIsNestedFamily()
        {
            Assert.False(typeof(PublicClass).Project().GetTypeInfo().IsNestedFamily, "Failed!!  IsNestedFamily returned true for a class with private visibility.");
        }


        // VerifyIsNestedFamANDAssem 
        [Fact]
        public static void TestIsNestedFamAndAssem()
        {
            Assert.False(typeof(PublicClass).Project().GetTypeInfo().IsNestedFamANDAssem, "Failed!!  IsNestedFamAndAssem returned true for a class with private visibility.");
        }


        // VerifyIsNestedFamOrAssem  
        [Fact]
        public static void TestIsNestedFamOrAssem()
        {
            Assert.False(typeof(PublicClass).Project().GetTypeInfo().IsNestedFamORAssem, "Failed!!  IsNestedFamOrAssem returned true for a class with private visibility.");
        }
    }

    //Metadata for Reflection
    public class PublicClass
    {
        public int PublicField;
        public static int PublicStaticField;

        public PublicClass() { }


        public void PublicMethod() { }
        public void overRiddenMethod() { }
        public void overRiddenMethod(int i) { }
        public void overRiddenMethod(string s) { }
        public void overRiddenMethod(object o) { }

        public static void PublicStaticMethod() { }
        public class PublicNestedType { }

        public int PublicProperty { get { return default(int); } set { } }

        public class publicNestedClass { }
    }

    public sealed class sealedClass { }
    public abstract class abstractClass { }
    public interface ITest { }
    public class TypeInfoPropertyBase { }
    public class TypeInfoPropertyDerived : TypeInfoPropertyBase { }
    public class ImplClass : ITest { }
    public class TypeInfoPropertyGenericClass<T> { }
    public class ClassWithConstraints<T, U>
        where T : TypeInfoPropertyBase, ITest
        where U : class, new()
    { }
    public enum MYENUM { one = 1, Two = 2 }
}
