// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

public class Outside
{
    public class Inside
    {
        public void GenericMethod<T>() { }
        public void TwoGenericMethod<T, U>() { }
    }

    public void GenericMethod<T>() { }
    public void TwoGenericMethod<T, U>() { }
}

public class Outside<T>
{
    public class Inside<U>
    {
        public void GenericMethod<V>() { }
        public void TwoGenericMethod<V, W>() { }
    }

    public void GenericMethod<U>() { }
    public void TwoGenericMethod<U, V>() { }
}

namespace System.Tests
{
    public partial class TypeTests
    {
        [Fact]
        public void FilterName_Get_ReturnsExpected()
        {
            Assert.NotNull(Type.FilterName);
            Assert.Same(Type.FilterName, Type.FilterName);
            Assert.NotSame(Type.FilterName, Type.FilterNameIgnoreCase);
        }

        [Theory]
        [InlineData("FilterName_Invoke_DelegateFiltersExpectedMembers", true)]
        [InlineData("     FilterName_Invoke_DelegateFiltersExpectedMembers   ", true)]
        [InlineData("*", true)]
        [InlineData("     *   ", true)]
        [InlineData("     Filter*   ", true)]
        [InlineData("FilterName_Invoke_DelegateFiltersExpectedMembe*", true)]
        [InlineData("FilterName_Invoke_DelegateFiltersExpectedMember*", true)]
        [InlineData("filterName_Invoke_DelegateFiltersExpectedMembers", false)]
        [InlineData("FilterName_Invoke_DelegateFiltersExpectedMemberss*", false)]
        [InlineData("FilterName", false)]
        [InlineData("*FilterName", false)]
        [InlineData("", false)]
        [InlineData("     ", false)]
        public void FilterName_Invoke_DelegateFiltersExpectedMembers(string filterCriteria, bool expected)
        {
            MethodInfo mi = typeof(TypeTests).GetMethod(nameof(FilterName_Invoke_DelegateFiltersExpectedMembers));
            Assert.Equal(expected, Type.FilterName(mi, filterCriteria));
        }

        [Fact]
        public void FilterName_InvalidFilterCriteria_ThrowsInvalidFilterCriteriaException()
        {
            MethodInfo mi = typeof(TypeTests).GetMethod(nameof(FilterName_Invoke_DelegateFiltersExpectedMembers));
            Assert.Throws<InvalidFilterCriteriaException>(() => Type.FilterName(mi, null));
            Assert.Throws<InvalidFilterCriteriaException>(() => Type.FilterName(mi, new object()));
        }

        [Fact]
        public void FilterNameIgnoreCase_Get_ReturnsExpected()
        {
            Assert.NotNull(Type.FilterNameIgnoreCase);
            Assert.Same(Type.FilterNameIgnoreCase, Type.FilterNameIgnoreCase);
            Assert.NotSame(Type.FilterNameIgnoreCase, Type.FilterName);
        }

        [Theory]
        [InlineData("FilterNameIgnoreCase_Invoke_DelegateFiltersExpectedMembers", true)]
        [InlineData("filternameignorecase_invoke_delegatefiltersexpectedmembers", true)]
        [InlineData("     filterNameIgnoreCase_Invoke_DelegateFiltersexpectedMembers   ", true)]
        [InlineData("*", true)]
        [InlineData("     *   ", true)]
        [InlineData("     fIlTeR*   ", true)]
        [InlineData("FilterNameIgnoreCase_invoke_delegateFiltersExpectedMembe*", true)]
        [InlineData("FilterNameIgnoreCase_invoke_delegateFiltersExpectedMember*", true)]
        [InlineData("filterName_Invoke_DelegateFiltersExpectedMembers", false)]
        [InlineData("filterNameIgnoreCase_Invoke_DelegateFiltersExpectedMemberss", false)]
        [InlineData("FilterNameIgnoreCase_Invoke_DelegateFiltersExpectedMemberss*", false)]
        [InlineData("filterNameIgnoreCase", false)]
        [InlineData("*FilterNameIgnoreCase", false)]
        [InlineData("", false)]
        [InlineData("     ", false)]
        public void FilterNameIgnoreCase_Invoke_DelegateFiltersExpectedMembers(string filterCriteria, bool expected)
        {
            MethodInfo mi = typeof(TypeTests).GetMethod(nameof(FilterNameIgnoreCase_Invoke_DelegateFiltersExpectedMembers));
            Assert.Equal(expected, Type.FilterNameIgnoreCase(mi, filterCriteria));
        }

        [Fact]
        public void FilterNameIgnoreCase_InvalidFilterCriteria_ThrowsInvalidFilterCriteriaException()
        {
            MethodInfo mi = typeof(TypeTests).GetMethod(nameof(FilterName_Invoke_DelegateFiltersExpectedMembers));
            Assert.Throws<InvalidFilterCriteriaException>(() => Type.FilterNameIgnoreCase(mi, null));
            Assert.Throws<InvalidFilterCriteriaException>(() => Type.FilterNameIgnoreCase(mi, new object()));
        }

        public static IEnumerable<object[]> FindMembers_TestData()
        {
            yield return new object[] { MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance, Type.FilterName, "HelloWorld", 0 };
            yield return new object[] { MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance, Type.FilterName, "FilterName_Invoke_DelegateFiltersExpectedMembers", 1 };
            yield return new object[] { MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance, Type.FilterName, "FilterName_Invoke_Delegate*", 1 };
            yield return new object[] { MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance, Type.FilterName, "filterName_Invoke_Delegate*", 0 };

            yield return new object[] { MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance, Type.FilterNameIgnoreCase, "HelloWorld", 0 };
            yield return new object[] { MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance, Type.FilterNameIgnoreCase, "FilterName_Invoke_DelegateFiltersExpectedMembers", 1 };
            yield return new object[] { MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance, Type.FilterNameIgnoreCase, "FilterName_Invoke_Delegate*", 1 };
            yield return new object[] { MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance, Type.FilterNameIgnoreCase, "filterName_Invoke_Delegate*", 1 };
        }

        [Theory]
        [MemberData(nameof(FindMembers_TestData))]
        public void FindMembers_Invoke_ReturnsExpected(MemberTypes memberType, BindingFlags bindingAttr, MemberFilter filter, object filterCriteria, int expectedLength)
        {
            Assert.Equal(expectedLength, typeof(TypeTests).FindMembers(memberType, bindingAttr, filter, filterCriteria).Length);
        }

        [Theory]
        [InlineData(typeof(int), typeof(int))]
        [InlineData(typeof(int[]), typeof(int[]))]
        [InlineData(typeof(Outside<int>), typeof(Outside<int>))]
        public void TypeHandle(Type t1, Type t2)
        {
            RuntimeTypeHandle r1 = t1.TypeHandle;
            RuntimeTypeHandle r2 = t2.TypeHandle;
            Assert.Equal(r1, r2);

            Assert.Equal(t1, Type.GetTypeFromHandle(r1));
            Assert.Equal(t1, Type.GetTypeFromHandle(r2));
        }

        [Fact]
        public void GetTypeFromDefaultHandle()
        {
            Assert.Null(Type.GetTypeFromHandle(default(RuntimeTypeHandle)));
        }

        [Theory]
        [InlineData(typeof(int[]), 1)]
        [InlineData(typeof(int[,,]), 3)]
        public void GetArrayRank_Get_ReturnsExpected(Type t, int expected)
        {
            Assert.Equal(expected, t.GetArrayRank());
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(IList<int>))]
        [InlineData(typeof(IList<>))]
        public void GetArrayRank_NonArrayType_ThrowsArgumentException(Type t)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => t.GetArrayRank());
        }

        [Theory]
        [InlineData(typeof(int), typeof(int[]))]
        public void MakeArrayType_Invoke_ReturnsExpected(Type t, Type tArrayExpected)
        {
            Type tArray = t.MakeArrayType();

            Assert.Equal(tArrayExpected, tArray);
            Assert.Equal(t, tArray.GetElementType());

            Assert.True(tArray.IsArray);
            Assert.True(tArray.HasElementType);

            string s1 = t.ToString();
            string s2 = tArray.ToString();
            Assert.Equal(s2, s1 + "[]");
        }

        [Theory]
        [InlineData(typeof(int))]
        public void MakeByRefType_Invoke_ReturnsExpected(Type t)
        {
            Type tRef1 = t.MakeByRefType();
            Type tRef2 = t.MakeByRefType();

            Assert.Equal(tRef1, tRef2);

            Assert.True(tRef1.IsByRef);
            Assert.True(tRef1.HasElementType);

            Assert.Equal(t, tRef1.GetElementType());

            string s1 = t.ToString();
            string s2 = tRef1.ToString();
            Assert.Equal(s2, s1 + "&");
        }

        [Theory]
        [InlineData("System.Nullable`1[System.Int32]", typeof(int?))]
        [InlineData("System.Int32*", typeof(int*))]
        [InlineData("System.Int32**", typeof(int**))]
        [InlineData("Outside`1", typeof(Outside<>))]
        [InlineData("Outside`1+Inside`1", typeof(Outside<>.Inside<>))]
        [InlineData("Outside[]", typeof(Outside[]))]
        [InlineData("Outside[,,]", typeof(Outside[,,]))]
        [InlineData("Outside[][]", typeof(Outside[][]))]
        [InlineData("Outside`1[System.Nullable`1[System.Boolean]]", typeof(Outside<bool?>))]
        public void GetTypeByName_ValidType_ReturnsExpected(string typeName, Type expectedType)
        {
            Assert.Equal(expectedType, Type.GetType(typeName, throwOnError: false, ignoreCase: false));
            Assert.Equal(expectedType, Type.GetType(typeName.ToLower(), throwOnError: false, ignoreCase: true));
        }

        [Theory]
        [InlineData("system.nullable`1[system.int32]", typeof(TypeLoadException), false)]
        [InlineData("System.NonExistingType", typeof(TypeLoadException), false)]
        [InlineData("", typeof(TypeLoadException), false)]
        [InlineData("System.Int32[,*,]", typeof(ArgumentException), false)]
        [InlineData("Outside`2", typeof(TypeLoadException), false)]
        [InlineData("Outside`1[System.Boolean, System.Int32]", typeof(ArgumentException), true)]
        public void GetTypeByName_Invalid(string typeName, Type expectedException, bool alwaysThrowsException)
        {
            if (!alwaysThrowsException)
            {
                Assert.Null(Type.GetType(typeName, throwOnError: false, ignoreCase: false));
            }

            Assert.Throws(expectedException, () => Type.GetType(typeName, throwOnError: true, ignoreCase: false));
        }

        [Fact]
        public void GetTypeByName_InvokeViaReflection_Success()
        {
            MethodInfo method = typeof(Type).GetMethod("GetType", new[] { typeof(string) });
            object result = method.Invoke(null, new object[] { "System.Tests.TypeTests" });
            Assert.Equal(typeof(TypeTests), result);
        }

        [Fact]
        public void Delimiter()
        {
            Assert.Equal('.', Type.Delimiter);
        }

        [Theory]
        [InlineData(typeof(bool), TypeCode.Boolean)]
        [InlineData(typeof(byte), TypeCode.Byte)]
        [InlineData(typeof(char), TypeCode.Char)]
        [InlineData(typeof(DateTime), TypeCode.DateTime)]
        [InlineData(typeof(decimal), TypeCode.Decimal)]
        [InlineData(typeof(double), TypeCode.Double)]
        [InlineData(null, TypeCode.Empty)]
        [InlineData(typeof(short), TypeCode.Int16)]
        [InlineData(typeof(int), TypeCode.Int32)]
        [InlineData(typeof(long), TypeCode.Int64)]
        [InlineData(typeof(object), TypeCode.Object)]
        [InlineData(typeof(System.Nullable), TypeCode.Object)]
        [InlineData(typeof(Nullable<int>), TypeCode.Object)]
        [InlineData(typeof(Dictionary<,>), TypeCode.Object)]
        [InlineData(typeof(Exception), TypeCode.Object)]
        [InlineData(typeof(sbyte), TypeCode.SByte)]
        [InlineData(typeof(float), TypeCode.Single)]
        [InlineData(typeof(string), TypeCode.String)]
        [InlineData(typeof(ushort), TypeCode.UInt16)]
        [InlineData(typeof(uint), TypeCode.UInt32)]
        [InlineData(typeof(ulong), TypeCode.UInt64)]
        public void GetTypeCode_ValidType_ReturnsExpected(Type t, TypeCode typeCode)
        {
            Assert.Equal(typeCode, Type.GetTypeCode(t));
        }

        public void ReflectionOnlyGetType()
        {
            AssertExtensions.Throws<ArgumentNullException>("typeName", () => Type.ReflectionOnlyGetType(null, true, false));
            Assert.Throws<TypeLoadException>(() => Type.ReflectionOnlyGetType("", true, true));
            Assert.Throws<NotSupportedException>(() => Type.ReflectionOnlyGetType("System.Tests.TypeTests", false, true));
        }
    }

    public class TypeTestsExtended    {
        public class ContextBoundClass : ContextBoundObject
        {
            public string Value = "The Value property.";
        }

        static string s_testAssemblyPath = Path.Combine(Environment.CurrentDirectory, "TestLoadAssembly.dll");
        static string testtype = "System.Collections.Generic.Dictionary`2[[Program, Foo], [Program, Foo]]";

        private static Func<AssemblyName, Assembly> assemblyloader = (aName) => aName.Name == "TestLoadAssembly" ?
                           Assembly.LoadFrom(@".\TestLoadAssembly.dll") :
                           null;
        private static Func<Assembly, string, bool, Type> typeloader = (assem, name, ignore) => assem == null ?
                             Type.GetType(name, false, ignore) :
                                 assem.GetType(name, false, ignore);
        [Fact]
        public void GetTypeByName()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            RemoteExecutor.Invoke(() =>
               {
                   string test1 = testtype;
                   Type t1 = Type.GetType(test1,
                             (aName) => aName.Name == "Foo" ?
                                   Assembly.LoadFrom(s_testAssemblyPath) : null,
                             typeloader,
                             true
                     );

                   Assert.NotNull(t1);

                   string test2 = "System.Collections.Generic.Dictionary`2[[Program, TestLoadAssembly], [Program, TestLoadAssembly]]";
                   Type t2 = Type.GetType(test2, assemblyloader, typeloader, true);

                   Assert.NotNull(t2);
                   Assert.Equal(t1, t2);

                   return RemoteExecutor.SuccessExitCode;
               }, options).Dispose();
        }

        [Theory]
        [InlineData("System.Collections.Generic.Dictionary`2[[Program, TestLoadAssembly], [Program2, TestLoadAssembly]]")]
        [InlineData("")]
        public void GetTypeByName_NoSuchType_ThrowsTypeLoadException(string typeName)
        {
            RemoteExecutor.Invoke(marshalledTypeName =>
            {
                Assert.Throws<TypeLoadException>(() => Type.GetType(marshalledTypeName, assemblyloader, typeloader, true));
                Assert.Null(Type.GetType(marshalledTypeName, assemblyloader, typeloader, false));

                return RemoteExecutor.SuccessExitCode;
            }, typeName).Dispose();
        }

        [Fact]
        public void GetTypeByNameCaseSensitiveTypeloadFailure()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            RemoteExecutor.Invoke(() =>
               {
                   //Type load failure due to case sensitive search of type Ptogram
                   string test3 = "System.Collections.Generic.Dictionary`2[[Program, TestLoadAssembly], [program, TestLoadAssembly]]";
                   Assert.Throws<TypeLoadException>(() =>
                                Type.GetType(test3,
                                            assemblyloader,
                                            typeloader,
                                            true,
                                            false     //case sensitive
                   ));

                   //non throwing version
                   Type t2 = Type.GetType(test3,
                                          assemblyloader,
                                          typeloader,
                                          false,  //no throw
                                          false
                  );

                   Assert.Null(t2);

                   return RemoteExecutor.SuccessExitCode;
               }, options).Dispose();
        }

        [Fact]
        public void IsContextful()
        {
            Assert.True(!typeof(TypeTestsExtended).IsContextful);
            Assert.True(!typeof(ContextBoundClass).IsContextful);
        }

#region GetInterfaceMap tests
        public static IEnumerable<object[]> GetInterfaceMap_TestData()
        {
            yield return new object[]
            {
                typeof(ISimpleInterface),
                typeof(SimpleType),
                new Tuple<MethodInfo, MethodInfo>[]
                {
                    new Tuple<MethodInfo, MethodInfo>(typeof(ISimpleInterface).GetMethod("Method"), typeof(SimpleType).GetMethod("Method")),
                    new Tuple<MethodInfo, MethodInfo>(typeof(ISimpleInterface).GetMethod("GenericMethod"), typeof(SimpleType).GetMethod("GenericMethod"))
                }
            };
            yield return new object[]
            {
                typeof(IGenericInterface<object>),
                typeof(DerivedType),
                new Tuple<MethodInfo, MethodInfo>[]
                {
                    new Tuple<MethodInfo, MethodInfo>(typeof(IGenericInterface<object>).GetMethod("Method"), typeof(DerivedType).GetMethod("Method", new Type[] { typeof(object) })),
                }
            };
            yield return new object[]
            {
                typeof(IGenericInterface<string>),
                typeof(DerivedType),
                new Tuple<MethodInfo, MethodInfo>[]
                {
                    new Tuple<MethodInfo, MethodInfo>(typeof(IGenericInterface<string>).GetMethod("Method"), typeof(DerivedType).GetMethod("Method", new Type[] { typeof(string) })),
                }
            };
        }

        [Theory]
        [MemberData(nameof(GetInterfaceMap_TestData))]
        public void GetInterfaceMap(Type interfaceType, Type classType, Tuple<MethodInfo, MethodInfo>[] expectedMap)
        {
            InterfaceMapping actualMapping = classType.GetInterfaceMap(interfaceType);

            Assert.Equal(interfaceType, actualMapping.InterfaceType);
            Assert.Equal(classType, actualMapping.TargetType);

            Assert.Equal(expectedMap.Length, actualMapping.InterfaceMethods.Length);
            Assert.Equal(expectedMap.Length, actualMapping.TargetMethods.Length);

            for (int i = 0; i < expectedMap.Length; i++)
            {
                Assert.Contains(expectedMap[i].Item1, actualMapping.InterfaceMethods);

                int index = Array.IndexOf(actualMapping.InterfaceMethods, expectedMap[i].Item1);
                Assert.Equal(expectedMap[i].Item2, actualMapping.TargetMethods[index]);
            }
        }

        interface ISimpleInterface
        {
            void Method();
            void GenericMethod<T>();
        }

        class SimpleType : ISimpleInterface
        {
            public void Method() { }
            public void GenericMethod<T>() { }
        }

        interface IGenericInterface<T>
        {
            void Method(T arg);
        }

        class GenericBaseType<T> : IGenericInterface<T>
        {
            public void Method(T arg) { }
        }

        class DerivedType : GenericBaseType<object>, IGenericInterface<string>
        {
            public void Method(string arg) { }
        }
#endregion
    }

    public class NonGenericClass { }

    public class NonGenericSubClassOfNonGeneric : NonGenericClass { }

    public class GenericClass<T> { }

    public class NonGenericSubClassOfGeneric : GenericClass<string> { }

    public class GenericClass<T, U> { }
    public abstract class AbstractClass { }

    public struct NonGenericStruct { }

    public ref struct RefStruct { }

    public struct GenericStruct<T> { }
    public struct GenericStruct<T, U> { }

    public interface NonGenericInterface { }
    public interface GenericInterface<T> { }
    public interface GenericInterface<T, U> { }
}
