// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

#pragma warning disable 0067 // Unused event

namespace System.Reflection.Tests
{
    public class TypeInfoMethodTests
    {
        // Verify AsType() method
        [Fact]
        public static void TestAsType1()
        {
            Type runtimeType = typeof(MethodPublicClass).Project();
            TypeInfo typeInfo = runtimeType.GetTypeInfo();

            Type type = typeInfo.AsType();

            Assert.Equal(runtimeType, type);
        }

        // Verify AsType() method
        [Fact]
        public static void TestAsType2()
        {
            Type runtimeType = typeof(MethodPublicClass.PublicNestedType).Project();
            TypeInfo typeInfo = runtimeType.GetTypeInfo();

            Type type = typeInfo.AsType();

            Assert.Equal(runtimeType, type);
        }


        // Verify GetArrayRank() method
        [Fact]
        public static void TestGetArrayRank1()
        {
            int[] myArray = { 1, 2, 3, 4, 5, 6, 7 };
            int expectedRank = 1;

            Type type = myArray.GetType();
            TypeInfo typeInfo = type.GetTypeInfo();

            int rank = typeInfo.GetArrayRank();

            Assert.Equal(expectedRank, rank);
        }


        // Verify GetArrayRank() method
        [Fact]
        public static void TestGetArrayRank2()
        {
            string[] myArray = { "hello" };
            int expectedRank = 1;

            Type type = myArray.GetType();
            TypeInfo typeInfo = type.GetTypeInfo();

            int rank = typeInfo.GetArrayRank();

            Assert.Equal(expectedRank, rank);
        }

        // Verify GetDeclaredEvent() method
        [Fact]
        public static void TestGetDeclaredEvent1()
        {
            Type type = typeof(MethodPublicClass).Project();
            TypeInfo typeInfo = type.GetTypeInfo();

            VerifyGetDeclaredEvent(typeInfo, "EventPublic");
        }


        // Verify GetDeclaredEvent() method
        [Fact]
        public static void TestGetDeclaredEvent2()
        {
            Type type = typeof(MethodPublicClass).Project();
            TypeInfo typeInfo = type.GetTypeInfo();

            EventInfo ei = typeInfo.GetDeclaredEvent("NoSuchEvent");

            Assert.Null(ei);
        }


        // Verify GetDeclaredEvent() method
        [Fact]
        public static void TestGetDeclaredEvent3()
        {
            Type type = typeof(MethodPublicClass).Project();
            TypeInfo typeInfo = type.GetTypeInfo();

            Assert.Throws<ArgumentNullException>(() => { EventInfo ei = typeInfo.GetDeclaredEvent(null); });
        }


        // Verify GetDeclaredField() method
        [Fact]
        public static void TestGetDeclaredField1()
        {
            Type type = typeof(MethodPublicClass).Project();
            TypeInfo typeInfo = type.GetTypeInfo();

            VerifyGetDeclaredField(typeInfo, "PublicField");
        }


        // Verify GetDeclaredField() method
        [Fact]
        public static void TestGetDeclaredField2()
        {
            Type type = typeof(MethodPublicClass).Project();
            TypeInfo typeInfo = type.GetTypeInfo();

            VerifyGetDeclaredField(typeInfo, "PublicStaticField");
        }


        // Verify GetDeclaredField() method
        [Fact]
        public static void TestGetDeclaredField3()
        {
            Type type = typeof(MethodPublicClass).Project();
            TypeInfo typeInfo = type.GetTypeInfo();

            FieldInfo fi = typeInfo.GetDeclaredField("NoSuchField");

            Assert.Null(fi);
        }


        // Verify  GetDeclaredField() method
        [Fact]
        public static void TestGetDeclaredField4()
        {
            Type type = typeof(MethodPublicClass).Project();
            TypeInfo typeInfo = type.GetTypeInfo();

            Assert.Throws<ArgumentNullException>(() => { FieldInfo fi = typeInfo.GetDeclaredField(null); });
        }


        // Verify GetDeclaredMethod() method
        [Fact]
        public static void TestGetDeclaredMethod1()
        {
            Type type = typeof(MethodPublicClass).Project();
            TypeInfo typeInfo = type.GetTypeInfo();

            VerifyGetDeclaredMethod(typeInfo, "PublicMethod");
        }


        // Verify  GetDeclaredMethod() method
        [Fact]
        public static void TestGetDeclaredMethod2()
        {
            Type type = typeof(MethodPublicClass).Project();
            TypeInfo typeInfo = type.GetTypeInfo();

            VerifyGetDeclaredMethod(typeInfo, "PublicStaticMethod");
        }


        // Verify GetDeclaredMethod() method
        [Fact]
        public static void TestGetDeclaredMethod3()
        {
            Type type = typeof(MethodPublicClass).Project();
            TypeInfo typeInfo = type.GetTypeInfo();

            MethodInfo mi = typeInfo.GetDeclaredMethod("NoSuchMethod");

            Assert.Null(mi);
        }


        // Verify  GetDeclaredMethod() method
        [Fact]
        public static void TestGetDeclaredMethod4()
        {
            Type type = typeof(MethodPublicClass).Project();
            TypeInfo typeInfo = type.GetTypeInfo();

            Assert.Throws<ArgumentNullException>(() => { MethodInfo mi = typeInfo.GetDeclaredMethod(null); });
        }


        // Verify  GetDeclaredMethods() method
        [Fact]
        public static void TestGetDeclaredMethods1()
        {
            Type type = typeof(MethodPublicClass).Project();
            TypeInfo typeInfo = type.GetTypeInfo();

            VerifyGetDeclaredMethods(typeInfo, "overRiddenMethod", 4);
        }


        // Verify  GetDeclaredMethods() method
        [Fact]
        public static void TestGetDeclaredMethods2()
        {
            Type type = typeof(MethodPublicClass).Project();
            TypeInfo typeInfo = type.GetTypeInfo();

            VerifyGetDeclaredMethods(typeInfo, "NoSuchMethod", 0);
        }

        // Verify  GetDeclaredNestedType() method
        [Fact]
        public static void TestGetDeclaredNestedType1()
        {
            Type type = typeof(MethodPublicClass).Project();
            TypeInfo typeInfo = type.GetTypeInfo();

            VerifyGetDeclaredNestedType(typeInfo, "PublicNestedType");
        }



        // Verify GetDeclaredNestedType() method
        [Fact]
        public static void TestGetDeclaredNestedType2()
        {
            Type type = typeof(MethodPublicClass).Project();
            TypeInfo typeInfo = type.GetTypeInfo();

            TypeInfo nested_ti = typeInfo.GetDeclaredNestedType("NoSuchType");

            Assert.Null(nested_ti);
        }


        // Verify  GetDeclaredNestedType() method
        [Fact]
        public static void TestGetDeclaredNestedType3()
        {
            Type type = typeof(MethodPublicClass).Project();
            TypeInfo typeInfo = type.GetTypeInfo();

            Assert.Throws<ArgumentNullException>(() => { TypeInfo nested_ti = typeInfo.GetDeclaredNestedType(null); });
        }


        // Verify GetElementType() method
        [Fact]
        public static void TestGetElementType1()
        {
            Type runtimeType = typeof(MethodPublicClass).Project();
            TypeInfo typeInfo = runtimeType.GetTypeInfo();

            Type type = typeInfo.GetElementType();

            Assert.Null(type);
        }


        // Verify GetElementType() method
        [Fact]
        public static void TestGetElementType2()
        {
            string[] myArray = { "a", "b", "c" };
            Type runtimeType = myArray.GetType();
            TypeInfo typeInfo = runtimeType.GetTypeInfo();

            Type type = typeInfo.GetElementType();

            Assert.NotNull(type);
            Assert.Equal(typeof(string).Project().Name, type.Name);
        }

        // Verify GetElementType() method
        [Fact]
        public static void TestGetElementType3()
        {
            int[] myArray = { 1, 2, 3, 4 };
            Type runtimeType = myArray.GetType();
            TypeInfo typeInfo = runtimeType.GetTypeInfo();

            Type type = typeInfo.GetElementType();

            Assert.NotNull(type);
            Assert.Equal(typeof(int).Project().Name, type.Name);
        }


        // Verify GetGenericParameterConstraints() method
        [Fact]
        public static void TestGetGenericParameterConstraints1()
        {
            Type def = typeof(TypeInfoMethodClassWithConstraints<,>).Project();
            TypeInfo ti = def.GetTypeInfo();

            Type[] defparams = ti.GenericTypeParameters;

            Assert.Equal(2, defparams.Length);

            Type[] tpConstraints = defparams[0].GetTypeInfo().GetGenericParameterConstraints();

            Assert.Equal(2, tpConstraints.Length);
            Assert.Equal(typeof(TypeInfoMethodBase).Project(), tpConstraints[0]);

            Assert.Equal(typeof(MethodITest).Project(), tpConstraints[1]);
        }

        // Verify GetGenericParameterConstraints() method
        [Fact]
        public static void TestGetGenericParameterConstraints2()
        {
            Type def = typeof(TypeInfoMethodClassWithConstraints<,>).Project();
            TypeInfo ti = def.GetTypeInfo();

            Type[] defparams = ti.GenericTypeParameters;

            Assert.Equal(2, defparams.Length);

            Type[] tpConstraints = defparams[1].GetTypeInfo().GetGenericParameterConstraints();

            Assert.Equal(0, tpConstraints.Length);
        }


        // Verify GetGenericTypeDefinition() method
        [Fact]
        public static void TestGetGenericTypeDefinition()
        {
            TypeInfoMethodGenericClass<int> genericObj = new TypeInfoMethodGenericClass<int>();
            Type type = genericObj.GetType().Project();

            Type generictype = type.GetTypeInfo().GetGenericTypeDefinition();

            Assert.NotNull(generictype);

            Assert.Equal(typeof(TypeInfoMethodGenericClass<>).Project(), generictype);
        }

        // Verify IsSubClassOf() method
        [Fact]
        public static void TestIsSubClassOf1()
        {
            TypeInfo t1 = typeof(TypeInfoMethodBase).Project().GetTypeInfo();
            TypeInfo t2 = typeof(TypeInfoMethodDerived).Project().GetTypeInfo();

            bool isSubClass = t1.IsSubclassOf(t2.AsType());

            Assert.False(isSubClass);
        }


        // Verify IsSubClassOf() method
        [Fact]
        public static void TestIsSubClassOf2()
        {
            TypeInfo t1 = typeof(TypeInfoMethodBase).Project().GetTypeInfo();
            TypeInfo t2 = typeof(TypeInfoMethodDerived).Project().GetTypeInfo();

            bool isSubClass = t2.IsSubclassOf(t1.AsType());

            Assert.True(isSubClass, "Failed! isSubClass returned False when this class derives from input class ");
        }

        // Verify IsAssignableFrom() method
        [Fact]
        public static void TestIsAssignableFrom1()
        {
            TypeInfo t1 = typeof(TypeInfoMethodBase).Project().GetTypeInfo();
            TypeInfo t2 = typeof(TypeInfoMethodDerived).Project().GetTypeInfo();

            bool isAssignable = t1.IsAssignableFrom(t2);

            Assert.True(isAssignable, "Failed! IsAssignableFrom returned False");
        }


        // Verify IsAssignableFrom() method
        [Fact]
        public static void TestIsAssignableFrom2()
        {
            TypeInfo t1 = typeof(TypeInfoMethodBase).Project().GetTypeInfo();
            TypeInfo t2 = typeof(TypeInfoMethodDerived).Project().GetTypeInfo();

            bool isAssignable = t2.IsAssignableFrom(t1);

            Assert.False(isAssignable, "Failed! IsAssignableFrom returned True");
        }

        // Verify IsAssignableFrom() method
        [Fact]
        public static void TestIsAssignableFrom3()
        {
            TypeInfo t1 = typeof(TypeInfoMethodBase).Project().GetTypeInfo();
            TypeInfo t2 = typeof(TypeInfoMethodBase).Project().GetTypeInfo();

            bool isAssignable = t2.IsAssignableFrom(t2);

            Assert.True(isAssignable, "Failed! IsAssignableFrom returned False for same Type");
        }


        // Verify IsAssignableFrom() method
        [Fact]
        public static void TestIsAssignableFrom4()
        {
            TypeInfo t1 = typeof(MethodITest).Project().GetTypeInfo();
            TypeInfo t2 = typeof(TypeInfoMethodImplClass).Project().GetTypeInfo();

            bool isAssignable = t1.IsAssignableFrom(t2);

            Assert.True(isAssignable, "Failed! IsAssignableFrom returned False");
        }


        // Verify MakeArrayType() method
        [Fact]
        public static void TestMakeArrayType1()
        {
            TypeInfo ti = typeof(string).Project().GetTypeInfo();
            Type arraytype = ti.MakeArrayType();

            string[] strArray = { "a", "b", "c" };

            Assert.NotNull(arraytype);

            Assert.Equal(strArray.GetType().Project(), arraytype);
        }


        // Verify MakeArrayType() method
        [Fact]
        public static void TestMakeArrayType2()
        {
            TypeInfo ti = typeof(Int32).Project().GetTypeInfo();
            Type arraytype = ti.MakeArrayType();

            int[] intArray = { 1, 2, 3 };

            Assert.NotNull(arraytype);

            Assert.Equal(intArray.GetType().Project(), arraytype);
        }


        // Verify MakeArrayType(int rank) method
        [Fact]
        public static void TestMakeArrayTypeWithRank1()
        {
            TypeInfo ti = typeof(string).Project().GetTypeInfo();
            Type arraytype = ti.MakeArrayType(1);


            Assert.NotNull(arraytype);
            Assert.True(arraytype.IsArray, "Failed!!  MakeArrayType() returned type that is not Array");
        }


        // Verify MakeArrayType(int rank) method
        [Fact]
        public static void TestMakeArrayTypeWithRank2()
        {
            GC.KeepAlive(typeof(int[,]).Project());

            TypeInfo ti = typeof(int).Project().GetTypeInfo();
            Type arraytype = ti.MakeArrayType(2);


            Assert.NotNull(arraytype);
            Assert.True(arraytype.IsArray, "Failed!!  MakeArrayType() returned type that is not Array");
        }

        // Verify MakeArrayType() method
        [Fact]
        public static void TestMakeArrayOfPointerType1()
        {
            TypeInfo ti = typeof(char*).Project().GetTypeInfo();
            Type arraytype = ti.MakeArrayType(3);

            Assert.NotNull(arraytype);

            Assert.True(arraytype.IsArray);
            Assert.Equal(arraytype.GetArrayRank(), 3);
            Assert.Equal(arraytype.GetElementType(), typeof(char*).Project());
        }


        // Verify MakeArrayType(int rank) method
        [Fact]
        public static void TestMakeArrayTypeWithRank3()
        {
            GC.KeepAlive(typeof(int[,,]).Project());

            TypeInfo ti = typeof(int).Project().GetTypeInfo();
            Type arraytype = ti.MakeArrayType(3);

            Assert.NotNull(arraytype);
            Assert.True(arraytype.IsArray, "Failed!!  MakeArrayType() returned type that is not Array");
        }

        // Verify MakeByRefType method
        [Fact]
        public static void TestMakeByRefType1()
        {
            TypeInfo ti = typeof(int).Project().GetTypeInfo();
            Type byreftype = ti.MakeByRefType();


            Assert.NotNull(byreftype);
            Assert.True(byreftype.IsByRef, "Failed!!  MakeByRefType() returned type that is not ByRef");
        }

        // Verify MakeByRefType method
        [Fact]
        public static void TestMakeByRefType2()
        {
            TypeInfo ti = typeof(string).Project().GetTypeInfo();
            Type byreftype = ti.MakeByRefType();


            Assert.NotNull(byreftype);
            Assert.True(byreftype.IsByRef, "Failed!!  MakeByRefType() returned type that is not ByRef");
        }


        // Verify MakePointerType method
        [Fact]
        public static void TestMakePointerType1()
        {
            TypeInfo ti = typeof(int).Project().GetTypeInfo();
            Type ptrtype = ti.MakePointerType();


            Assert.NotNull(ptrtype);
            Assert.True(ptrtype.IsPointer, "Failed!!  MakePointerType() returned type that is not Pointer");
        }

        // Verify MakePointerType method
        [Fact]
        public static void TestMakePointerType2()
        {
            TypeInfo ti = typeof(string).Project().GetTypeInfo();
            Type ptrtype = ti.MakePointerType();


            Assert.NotNull(ptrtype);
            Assert.True(ptrtype.IsPointer, "Failed!!  MakePointerType() returned type that is not Pointer");
        }

        // Verify MakeGenericType() method
        [Fact]
        public static void TestMakeGenericType()
        {
            Type type = typeof(List<>).Project();
            Type[] typeArgs = { typeof(string).Project() };
            TypeInfo typeInfo = type.GetTypeInfo();

            Type generictype = typeInfo.MakeGenericType(typeArgs);

            Assert.NotNull(generictype);
            Assert.True(generictype.GetTypeInfo().IsGenericType, "Failed!!  MakeGenericType() returned type that is not generic");
        }


        // Verify ToString() method
        [Fact]
        public static void TestToString1()
        {
            Type type = typeof(string).Project();
            TypeInfo typeInfo = type.GetTypeInfo();

            Assert.Equal(typeInfo.ToString(), "System.String");
        }


        // Verify ToString() method
        [Fact]
        public static void TestToString2()
        {
            Type type = typeof(int).Project();
            TypeInfo typeInfo = type.GetTypeInfo();

            Assert.Equal(typeInfo.ToString(), "System.Int32");
        }



        //Private Helper Methods

        private static void VerifyGetDeclaredEvent(TypeInfo ti, string eventName)
        {
            EventInfo ei = ti.GetDeclaredEvent(eventName);

            Assert.NotNull(ei);
            Assert.Equal(eventName, ei.Name);
        }

        private static void VerifyGetDeclaredField(TypeInfo ti, string fieldName)
        {
            FieldInfo fi = ti.GetDeclaredField(fieldName);

            Assert.NotNull(fi);
            Assert.Equal(fieldName, fi.Name);
        }

        private static void VerifyGetDeclaredMethod(TypeInfo ti, string methodName)
        {
            MethodInfo mi = ti.GetDeclaredMethod(methodName);

            Assert.NotNull(mi);
            Assert.Equal(methodName, mi.Name);
        }


        private static void VerifyGetDeclaredMethods(TypeInfo ti, string methodName, int count)
        {
            IEnumerator<MethodInfo> alldefinedMethods = ti.GetDeclaredMethods(methodName).GetEnumerator();
            MethodInfo mi = null;
            int numMethods = 0;

            while (alldefinedMethods.MoveNext())
            {
                mi = alldefinedMethods.Current;

                Assert.Equal(methodName, mi.Name);
                numMethods++;
            }

            Assert.Equal(count, numMethods);
        }

        private static void VerifyGetDeclaredNestedType(TypeInfo ti, string name)
        {
            TypeInfo nested_ti = ti.GetDeclaredNestedType(name);

            Assert.NotNull(nested_ti);
            Assert.Equal(name, nested_ti.Name);
        }

        private static void VerifyGetDeclaredProperty(TypeInfo ti, string name)
        {
            PropertyInfo pi = ti.GetDeclaredProperty(name);

            Assert.NotNull(pi);
            Assert.Equal(name, pi.Name);
        }
    }

    //Metadata for Reflection
    public class MethodPublicClass
    {
        public int PublicField;
        public static int PublicStaticField;

        public MethodPublicClass() { }

        public void PublicMethod() { }
        public void overRiddenMethod() { }
        public void overRiddenMethod(int i) { }
        public void overRiddenMethod(string s) { }
        public void overRiddenMethod(object o) { }

        public static void PublicStaticMethod() { }
        public class PublicNestedType { }

        public int PublicProperty { get { return default(int); } set { } }

        public event System.EventHandler EventPublic;
    }

    public interface MethodITest { }
    public class TypeInfoMethodBase { }
    public class TypeInfoMethodDerived : TypeInfoMethodBase { }
    public class TypeInfoMethodImplClass : MethodITest { }
    public class TypeInfoMethodGenericClass<T> { }
    public class TypeInfoMethodClassWithConstraints<T, U>
        where T : TypeInfoMethodBase, MethodITest
        where U : class, new()
    { }
}
