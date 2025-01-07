// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using SampleMetadata;
using Xunit;

namespace System.Reflection.Tests
{
    public static class TypeInvariants
    {
        [Fact]
        [ActiveIssue("https://github.com/mono/mono/issues/15344", TestRuntimes.Mono)]
        public static void TestInvariantCode()
        {
            // These run some *runtime*-implemented Type objects through our invariant battery.
            // This is to ensure that the invariant testing code is actually correct.

            typeof(object).TestTypeDefinitionInvariants();
            typeof(OuterType1.InnerType1).TestTypeDefinitionInvariants();
            typeof(IList<>).TestTypeDefinitionInvariants();
            typeof(IDictionary<,>).TestTypeDefinitionInvariants();
            typeof(object[]).TestSzArrayInvariants();
            typeof(object).MakeArrayType(1).TestMdArrayInvariants();
            typeof(object[,]).TestMdArrayInvariants();
            typeof(object[,,]).TestMdArrayInvariants();
            typeof(object).MakeByRefType().TestByRefInvariants();
            typeof(int).MakePointerType().TestPointerInvariants();
            typeof(IList<int>).TestConstructedGenericTypeInvariants();
            typeof(IDictionary<int, string>).TestConstructedGenericTypeInvariants();

            Type theT = typeof(IList<>).GetTypeInfo().GenericTypeParameters[0];
            theT.TestGenericTypeParameterInvariants();

            Type theV = typeof(IDictionary<,>).GetTypeInfo().GenericTypeParameters[1];
            theT.TestGenericTypeParameterInvariants();

            MethodInfo genericMethod = typeof(ClassWithGenericMethods1).GetMethod("GenericMethod1", BindingFlags.Public | BindingFlags.Instance);
            Debug.Assert(genericMethod != null);
            Type theM = genericMethod.GetGenericArguments()[0];
            theM.TestGenericMethodParameterInvariants();
            Type theN = genericMethod.GetGenericArguments()[1];
            theN.TestGenericMethodParameterInvariants();

            Type openSzArray = theN.MakeArrayType();
            openSzArray.TestSzArrayInvariants();

            Type openMdArrayRank1 = theN.MakeArrayType(1);
            openMdArrayRank1.TestMdArrayInvariants();

            Type openMdArrayRank2 = theN.MakeArrayType(2);
            openMdArrayRank2.TestMdArrayInvariants();

            Type openByRef = theN.MakeByRefType();
            openByRef.TestByRefInvariants();

            Type openPointer = theN.MakePointerType();
            openPointer.TestPointerInvariants();

            Type openDictionary = typeof(IDictionary<,>).MakeGenericType(typeof(int), theN);
            openDictionary.TestConstructedGenericTypeInvariants();
        }

        internal static void TestTypeInvariants(this Type type)
        {
            if (type.IsTypeDefinition())
                type.TestTypeDefinitionInvariants();
            else if (type.HasElementType)
                type.TestHasElementTypeInvariants();
            else if (type.IsConstructedGenericType)
                type.TestConstructedGenericTypeInvariants();
            else if (type.IsGenericParameter)
                type.TestGenericParameterInvariants();
            else
                Assert.Fail("Type does not identify as any of the known flavors: " + type);
        }

        internal static void TestTypeDefinitionInvariants(this Type type)
        {
            Assert.True(type.IsTypeDefinition());
            if (type.IsGenericTypeDefinition)
            {
                type.TestGenericTypeDefinitionInvariants();
            }
            else
            {
                type.TestTypeDefinitionCommonInvariants();
            }
        }

        internal static void TestGenericTypeDefinitionInvariants(this Type type)
        {
            Assert.True(type.IsGenericTypeDefinition);

            type.TestTypeDefinitionCommonInvariants();

            Type[] gps = type.GetTypeInfo().GenericTypeParameters;
            Assert.NotNull(gps);
            Assert.NotEqual(0, gps.Length);
            Type[] gps2 = type.GetTypeInfo().GenericTypeParameters;
            Assert.NotSame(gps, gps2);
            Assert.Equal<Type>(gps, gps2);

            for (int i = 0; i < gps.Length; i++)
            {
                Type gp = gps[i];
                Assert.Equal(i, gp.GenericParameterPosition);
                Assert.Equal(type, gp.DeclaringType);
            }
        }

        internal static void TestHasElementTypeInvariants(this Type type)
        {
            Assert.True(type.HasElementType);

            if (type.IsSZArray())
                type.TestSzArrayInvariants();
            else if (type.IsVariableBoundArray())
                type.TestMdArrayInvariants();
            else if (type.IsByRef)
                type.TestByRefInvariants();
            else if (type.IsPointer)
                type.TestPointerInvariants();
        }

        internal static void TestArrayInvariants(this Type type)
        {
            Assert.True(type.IsArray);

            if (type.IsSZArray())
                type.TestSzArrayInvariants();
            else if (type.IsVariableBoundArray())
                type.TestMdArrayInvariants();
            else
                Assert.Fail("Array type does not identify as either Sz or VariableBound: " + type);

            BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            MemberInfo[] mems;
            mems = type.GetEvents(bf);
            Assert.Equal(0, mems.Length);
            mems = type.GetFields(bf);
            Assert.Equal(0, mems.Length);
            mems = type.GetProperties(bf);
            Assert.Equal(0, mems.Length);
            mems = type.GetNestedTypes(bf);
            Assert.Equal(0, mems.Length);
        }

        internal static void TestSzArrayInvariants(this Type type)
        {
            Assert.True(type.IsSZArray());

            type.TestArrayCommonInvariants();

            Type et = type.GetElementType();

            string name = type.Name;
            string fullName = type.FullName;

            string suffix = "[]";
            Assert.Equal(et.Name + suffix, name);
            if (fullName != null)
            {
                Assert.Equal(et.FullName + suffix, fullName);
            }
        }

        internal static void TestMdArrayInvariants(this Type type)
        {
            Assert.True(type.IsVariableBoundArray());

            type.TestArrayCommonInvariants();

            string name = type.Name;
            string fullName = type.FullName;

            Type et = type.GetElementType();
            int rank = type.GetArrayRank();
            string suffix = (rank == 1) ? "[*]" : "[" + new string(',', rank - 1) + "]";
            Assert.Equal(et.Name + suffix, name);
            if (fullName != null)
            {
                Assert.Equal(et.FullName + suffix, fullName);
            }

            Type systemInt32 = type.BaseType.Assembly.GetType("System.Int32", throwOnError: true);
            ConstructorInfo[] cis = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
            Assert.Equal(2, cis.Length);
            ConstructorInfo c1 = cis.Single(c => c.GetParameters().Length == rank);
            foreach (ParameterInfo p in c1.GetParameters())
                Assert.Equal(p.ParameterType, systemInt32);
            ConstructorInfo c2 = cis.Single(c => c.GetParameters().Length == rank * 2);
            foreach (ParameterInfo p in c2.GetParameters())
                Assert.Equal(p.ParameterType, systemInt32);

        }

        internal static void TestByRefInvariants(this Type type)
        {
            Assert.True(type.IsByRef);

            type.TestHasElementTypeCommonInvariants();

            string name = type.Name;
            string fullName = type.FullName;

            Type et = type.GetElementType();
            string suffix = "&";
            Assert.Equal(et.Name + suffix, name);
            if (fullName != null)
            {
                Assert.Equal(et.FullName + suffix, fullName);
            }

            // No base type, interfaces
            Assert.Null(type.BaseType);
            Assert.Equal(0, type.GetInterfaces().Length);

            // No members
            BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            MemberInfo[] members = type.GetMembers(bf);
            Assert.Equal(0, members.Length);
        }

        internal static void TestPointerInvariants(this Type type)
        {
            Assert.True(type.IsPointer);

            type.TestHasElementTypeCommonInvariants();

            string name = type.Name;
            string fullName = type.FullName;

            Type et = type.GetElementType();
            string suffix = "*";
            Assert.Equal(et.Name + suffix, name);
            if (fullName != null)
            {
                Assert.Equal(et.FullName + suffix, fullName);
            }

            // No base type, interfaces
            Assert.Null(type.BaseType);
            Assert.Equal(0, type.GetInterfaces().Length);

            // No members
            BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            MemberInfo[] members = type.GetMembers(bf);
            Assert.Equal(0, members.Length);
        }

        internal static void TestConstructedGenericTypeInvariants(this Type type)
        {
            type.TestConstructedGenericTypeCommonInvariants();
        }

        internal static void TestGenericParameterInvariants(this Type type)
        {
            Assert.True(type.IsGenericParameter);

            type.TestTypeCommonInvariants();

            // No members
            BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            MemberInfo[] members = type.GetMembers(bf);
            Assert.Equal(0, members.Length);
        }

        internal static void TestGenericTypeParameterInvariants(this Type type)
        {
            Assert.True(type.IsGenericTypeParameter());

            type.TestGenericParameterCommonInvariants();

            Assert.Null(type.DeclaringMethod);

            int position = type.GenericParameterPosition;
            Type declaringType = type.DeclaringType;
            Assert.NotNull(declaringType);
            Assert.True(declaringType.IsGenericTypeDefinition);
            Type[] gps = declaringType.GetTypeInfo().GenericTypeParameters;
            Assert.Equal(gps[position], type);
        }

        internal static void TestGenericMethodParameterInvariants(this Type type)
        {
            Assert.True(type.IsGenericMethodParameter());

            type.TestGenericParameterCommonInvariants();

            int position = type.GenericParameterPosition;
            MethodInfo declaringMethod = (MethodInfo)type.DeclaringMethod;
            Assert.NotNull(declaringMethod);
            Assert.True(declaringMethod.IsGenericMethodDefinition);
            Type declaringType = type.DeclaringType;
            Assert.NotNull(declaringType);
            Assert.Equal(declaringMethod.DeclaringType, declaringType);
            Type[] gps = declaringMethod.GetGenericArguments();
            Assert.Equal(gps[position], type);

            // There is only one generic parameter instance even if it was queried from a method from an instantiated type or
            // a method with an alternate ReflectedType.
            Assert.False(declaringType.IsConstructedGenericType);
            Assert.Equal(declaringType, declaringMethod.ReflectedType);
        }

        private static void TestTypeCommonInvariants(this Type type)
        {
            // Ensure that ToString() doesn't throw and that it returns some non-null value. Exact contents are considered implementation detail.
            string typeString = type.ToString();
            Assert.NotNull(typeString);

            // These properties are mutually exclusive and exactly one of them must be true.
            int isCount = 0;
            isCount += type.IsTypeDefinition() ? 1 : 0;
            isCount += type.IsSZArray() ? 1 : 0;
            isCount += type.IsVariableBoundArray() ? 1 : 0;
            isCount += type.IsByRef ? 1 : 0;
            isCount += type.IsPointer ? 1 : 0;
            isCount += type.IsConstructedGenericType ? 1 : 0;
            isCount += type.IsGenericTypeParameter() ? 1 : 0;
            isCount += type.IsGenericMethodParameter() ? 1 : 0;
            Assert.Equal(1, isCount);

            Assert.Equal(type.IsGenericType, type.IsGenericTypeDefinition || type.IsConstructedGenericType);
            Assert.Equal(type.HasElementType, type.IsArray || type.IsByRef || type.IsPointer);
            Assert.Equal(type.IsArray, type.IsSZArray() || type.IsVariableBoundArray());
            Assert.Equal(type.IsGenericParameter, type.IsGenericTypeParameter() || type.IsGenericMethodParameter());

            Assert.Same(type, type.GetTypeInfo());
            Assert.Same(type, type.GetTypeInfo().AsType());
            Assert.Same(type, type.UnderlyingSystemType);
            Assert.Same(type.DeclaringType, type.ReflectedType);

            Assert.Equal(type.IsPublic || type.IsNotPublic ? MemberTypes.TypeInfo : MemberTypes.NestedType, type.MemberType);

            Assert.False(type.IsCOMObject);

            Module module = type.Module;
            Assembly assembly = type.Assembly;
            Assert.Equal(module.Assembly, assembly);

            string name = type.Name;
            string ns = type.Namespace;
            string fullName = type.FullName;
            string aqn = type.AssemblyQualifiedName;

            if (type.ContainsGenericParameters && !type.IsGenericTypeDefinition)
            {
                // Open types return null for FullName as such types cannot roundtrip through Type.GetType(string)
                Assert.Null(fullName);
                Assert.Null(aqn);
            }
            else
            {
                Assert.NotNull(fullName);
                Assert.NotNull(aqn);
                string expectedAqn = fullName + ", " + assembly.FullName;
                Assert.Equal(expectedAqn, aqn);
            }

            if (fullName != null)
            {
                Type roundTrip = assembly.GetType(fullName, throwOnError: true, ignoreCase: false);
                Assert.Same(type, roundTrip);

                Type roundTrip2 = module.GetType(fullName, throwOnError: true, ignoreCase: false);
                Assert.Same(type, roundTrip2);
            }

            Assert.Equal<Type>(type.GetInterfaces(), type.GetTypeInfo().ImplementedInterfaces);

            TestUtils.AssertNewObjectReturnedEachTime(() => type.GenericTypeArguments);
            TestUtils.AssertNewObjectReturnedEachTime(() => type.GetTypeInfo().GenericTypeParameters);
            TestUtils.AssertNewObjectReturnedEachTime(() => type.GetGenericArguments());
            TestUtils.AssertNewObjectReturnedEachTime(() => type.GetInterfaces());
            TestUtils.AssertNewObjectReturnedEachTime(() => type.GetTypeInfo().ImplementedInterfaces);
            CustomAttributeTests.ValidateCustomAttributesAllocatesFreshObjectsEachTime(() => type.CustomAttributes);

            const BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;
            foreach (MemberInfo mem in type.GetMember("*", MemberTypes.All, bf))
            {
                // Workaround: Do not try to test Array.Initialize, since one of its locals is a function pointer.
                // Delete this workaround when https://github.com/dotnet/runtime/issues/69273 is addressed.
                if (mem.DeclaringType == mem.DeclaringType.Assembly.GetType("System.Array") && mem.Name == "Initialize")
                {
                    continue;
                }

                string s = mem.ToString();
                Assert.Equal(type, mem.ReflectedType);
                Type declaringType = mem.DeclaringType;
                Assert.True(type == declaringType || type.IsSubclassOf(declaringType));

                if (mem is MethodBase methodBase)
                    methodBase.TestMethodBaseInvariants();

                if (type.Assembly.ReflectionOnly)
                {
                    ICustomAttributeProvider icp = mem;
                    Assert.Throws<InvalidOperationException>(() => icp.IsDefined(null, inherit: false));
                    Assert.Throws<InvalidOperationException>(() => icp.GetCustomAttributes(null, inherit: false));
                    Assert.Throws<InvalidOperationException>(() => icp.GetCustomAttributes(inherit: false));

                    if (mem is MethodBase mb)
                    {
                        Assert.Throws<InvalidOperationException>(() => mb.MethodHandle);
                        Assert.Throws<InvalidOperationException>(() => mb.Invoke(null,null));
                    }
                }
            }

            TestUtils.AssertNewObjectReturnedEachTime(() => type.GetMember("*", MemberTypes.All, bf));

            // Test some things that common to types that are not of a particular bucket.
            // (The Test*CommonInvariants() methods will cover the other half.)
            if (!type.IsTypeDefinition())
            {
                Assert.False(type.IsGenericTypeDefinition);
                Assert.Equal(0, type.GetTypeInfo().GenericTypeParameters.Length);
            }

            if (!type.IsGenericTypeDefinition)
            {
                Assert.Throws<InvalidOperationException>(() => type.MakeGenericType(new Type[3]));
            }

            if (!type.HasElementType)
            {
                Assert.Null(type.GetElementType());
            }

            if (!type.IsArray)
            {
                Assert.False(type.IsSZArray() || type.IsVariableBoundArray());
                Assert.Throws<ArgumentException>(() => type.GetArrayRank());
            }

            if (!type.IsByRef)
            {
            }

            if (!type.IsPointer)
            {
            }

            if (!type.IsConstructedGenericType)
            {
                Assert.Equal(0, type.GenericTypeArguments.Length);
                if (!type.IsGenericTypeDefinition)
                {
                    Assert.Throws<InvalidOperationException>(() => type.GetGenericTypeDefinition());
                }
            }

            if (!type.IsGenericParameter)
            {
                Assert.Throws<InvalidOperationException>(() => type.GenericParameterAttributes);
                Assert.Throws<InvalidOperationException>(() => type.GenericParameterPosition);
                Assert.Throws<InvalidOperationException>(() => type.GetGenericParameterConstraints());
                Assert.Throws<InvalidOperationException>(() => type.DeclaringMethod);
            }
        }

        private static void TestTypeDefinitionCommonInvariants(this Type type)
        {
            type.TestTypeCommonInvariants();

            Assert.True(type.IsTypeDefinition());
            Assert.Equal(type.IsGenericTypeDefinition, type.ContainsGenericParameters);

            string name = type.Name;
            string ns = type.Namespace;
            string fullName = type.FullName;

            Assert.NotNull(name);
            Assert.NotNull(fullName);


            string expectedFullName;
            if (type.IsNested)
            {
                expectedFullName = type.DeclaringType.FullName + "+" + name;
            }
            else
            {
                expectedFullName = (ns == null) ? name : ns + "." + name;
            }

            Assert.Equal(expectedFullName, fullName);

            Type declaringType = type.DeclaringType;
            if (declaringType != null)
            {
                Assert.True(declaringType.IsTypeDefinition());
                Type[] nestedTypes = declaringType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic);
                Assert.Contains(nestedTypes, nt => object.ReferenceEquals(nt, type));
            }

            Assert.Equal<Type>(type.GetTypeInfo().GenericTypeParameters, type.GetGenericArguments());

            int metadataToken = type.MetadataToken;
            Assert.Equal(0x02000000, metadataToken & 0xff000000);
        }

        private static void TestHasElementTypeCommonInvariants(this Type type)
        {
            type.TestTypeCommonInvariants();

            Assert.True(type.HasElementType);
            Assert.True(type.IsArray || type.IsByRef || type.IsPointer);
            Type et = type.GetElementType();
            Assert.NotNull(et);

            string ns = type.Namespace;
            Assert.Equal(et.Namespace, ns);

            Assert.Equal(et.Assembly, type.Assembly);
            Assert.Equal(et.Module, type.Module);
            Assert.Equal(et.ContainsGenericParameters, type.ContainsGenericParameters);

            Assert.Null(type.DeclaringType);

            Assert.False(type.IsByRefLike());

            Assert.Equal(default(Guid), type.GUID);
            Assert.Equal(0x02000000, type.MetadataToken);

            Type rootElementType = type;
            while (rootElementType.HasElementType)
            {
                rootElementType = rootElementType.GetElementType();
            }
            Assert.Equal<Type>(rootElementType.GetGenericArguments(), type.GetGenericArguments());
        }

        private static void TestArrayCommonInvariants(this Type type)
        {
            type.TestHasElementTypeCommonInvariants();

            Assert.True(type.IsArray);
        }

        private static void TestConstructedGenericTypeCommonInvariants(this Type type)
        {
            Assert.True(type.IsConstructedGenericType);

            type.TestTypeCommonInvariants();

            string name = type.Name;
            string ns = type.Namespace;
            string fullName = type.FullName;
            Type gd = type.GetGenericTypeDefinition();

            Assert.Equal(gd.Name, name);
            Assert.Equal(gd.Namespace, ns);
            if (fullName != null)
            {
                StringBuilder expectedFullName = new StringBuilder();
                expectedFullName.Append(gd.FullName);
                expectedFullName.Append('[');

                Type[] genericTypeArguments = type.GenericTypeArguments;
                for (int i = 0; i < genericTypeArguments.Length; i++)
                {
                    if (i != 0)
                        expectedFullName.Append(',');

                    expectedFullName.Append('[');
                    expectedFullName.Append(genericTypeArguments[i].AssemblyQualifiedName);
                    expectedFullName.Append(']');
                }
                expectedFullName.Append(']');

                Assert.Equal(expectedFullName.ToString(), fullName);
            }

            Assert.Equal(type.GenericTypeArguments.Any(gta => gta.ContainsGenericParameters), type.ContainsGenericParameters);

            Type[] gas = type.GenericTypeArguments;
            Assert.NotNull(gas);
            Assert.NotEqual(0, gas.Length);
            Type[] gas2 = type.GenericTypeArguments;
            Assert.NotSame(gas, gas2);
            Assert.Equal<Type>(gas, gas2);

            Assert.Same(type.GetGenericTypeDefinition().DeclaringType, type.DeclaringType);

            Assert.Equal<Type>(type.GenericTypeArguments, type.GetGenericArguments());

            Assert.Equal(type.GetGenericTypeDefinition().MetadataToken, type.MetadataToken);
        }

        private static void TestGenericParameterCommonInvariants(this Type type)
        {
            type.TestTypeCommonInvariants();

            Assert.True(type.IsGenericParameter);
            Assert.True(type.ContainsGenericParameters);

            string ns = type.Namespace;
            Assert.Equal(type.DeclaringType.Namespace, ns);
            Assert.Null(type.FullName);

            // Make sure these don't throw.
            int position = type.GenericParameterPosition;
            Assert.True(position >= 0);
            GenericParameterAttributes attributes = type.GenericParameterAttributes;

            Assert.Equal<Type>(Type.EmptyTypes, type.GetGenericArguments());

            Assert.False(type.IsByRefLike());

            Assert.Equal(default(Guid), type.GUID);

            int metadataToken = type.MetadataToken;
            Assert.Equal(0x2a000000, metadataToken & 0xff000000);

            // No members
            BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            MemberInfo[] members = type.GetMembers(bf);
            Assert.Equal(0, members.Length);
        }
    }
}
