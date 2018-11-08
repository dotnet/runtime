// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

#pragma warning disable 0649  // Uninitialized field

namespace System.Reflection.Tests
{
    public static class TypeTests_GetMember
    {
        [Fact]
        public static void TestNull()
        {
            Type t = typeof(Mixed).Project();
            Assert.Throws<ArgumentNullException>(() => t.GetMember(null, MemberTypes.All, BindingFlags.Public | BindingFlags.Instance));
        }
    
        [Fact]
        public static void TestExtraBitsIgnored()
        {
            Type t = typeof(Mixed).Project();
            MemberInfo[] expectedMembers = t.GetMember("*", MemberTypes.All, BindingFlags.Public | BindingFlags.Instance);
            MemberInfo[] actualMembers = t.GetMember("*", (MemberTypes)(-1), BindingFlags.Public | BindingFlags.Instance);
            Assert.Equal(expectedMembers.Length, actualMembers.Length);
        }
    
        [Fact]
        public static void TestTypeInfoIsSynonymForNestedInfo()
        {
            Type t = typeof(Mixed).Project();
            MemberInfo[] expectedMembers = t.GetMember("*", MemberTypes.NestedType, BindingFlags.Public | BindingFlags.Instance);
            Assert.Equal(1, expectedMembers.Length);
            Assert.Equal(typeof(Mixed.MyType).Project(), expectedMembers[0]);
    
            MemberInfo[] actualMembers;
            actualMembers = t.GetMember("*", MemberTypes.TypeInfo, BindingFlags.Public | BindingFlags.Instance);
            Assert.Equal<MemberInfo>(expectedMembers, actualMembers);
    
            actualMembers = t.GetMember("*", MemberTypes.NestedType | MemberTypes.TypeInfo, BindingFlags.Public | BindingFlags.Instance);
            Assert.Equal<MemberInfo>(expectedMembers, actualMembers);
        }
    
        [Fact]
        public static void TestReturnType()
        {
            Type t = typeof(Mixed).Project();
    
            // Desktop compat: Type.GetMember() returns the most specific array type possible given the MemberType combinations passed in.
    
            for (MemberTypes memberType = (MemberTypes)0; memberType <= MemberTypes.All; memberType++)
            {
                MemberInfo[] m = t.GetMember("*", memberType, BindingFlags.Public | BindingFlags.Instance);
                Type actualElementType = m.GetType().GetElementType();
    
                switch (memberType)
                {
                    case MemberTypes.Constructor:
                        Assert.Equal(typeof(ConstructorInfo), actualElementType);
                        break;
    
                    case MemberTypes.Event:
                        Assert.Equal(typeof(EventInfo), actualElementType);
                        break;
    
                    case MemberTypes.Field:
                        Assert.Equal(typeof(FieldInfo), actualElementType);
                        break;
    
                    case MemberTypes.Method:
                        Assert.Equal(typeof(MethodInfo), actualElementType);
                        break;
    
                    case MemberTypes.Constructor | MemberTypes.Method:
                        Assert.Equal(typeof(MethodBase), actualElementType);
                        break;
    
                    case MemberTypes.Property:
                        Assert.Equal(typeof(PropertyInfo), actualElementType);
                        break;
    
                    case MemberTypes.NestedType:
                    case MemberTypes.TypeInfo:
                        Assert.Equal(typeof(Type), actualElementType);
                        break;
    
                    default:
                        Assert.Equal(typeof(MemberInfo), actualElementType);
                        break;
                }
            }
        }
    
        [Fact]
        public static void TestMemberTypeCombos()
        {
            Type t = typeof(Mixed);
    
            for (MemberTypes memberType = (MemberTypes)0; memberType <= MemberTypes.All; memberType++)
            {
                MemberInfo[] members = t.GetMember("*", memberType, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
    
                int constructors = 0;
                int events = 0;
                int fields = 0;
                int methods = 0;
                int nestedTypes = 0;
                int properties = 0;
    
                foreach (MemberInfo member in members)
                {
                    switch (member.MemberType)
                    {
                        case MemberTypes.Constructor: constructors++; break;
                        case MemberTypes.Event: events++; break;
                        case MemberTypes.Field: fields++; break;
                        case MemberTypes.Method: methods++; break;
                        case MemberTypes.NestedType: nestedTypes++; break;
                        case MemberTypes.Property: properties++; break;
                        default:
                            Assert.True(false, "Bad member type.");
                            break;
                    }
                }
    
                int expectedConstructors = ((memberType & MemberTypes.Constructor) == 0) ? 0 : 1;
                int expectedEvents = ((memberType & MemberTypes.Event) == 0) ? 0 : 1;
                int expectedFields = ((memberType & MemberTypes.Field) == 0) ? 0 : 1;
                int expectedMethods = ((memberType & MemberTypes.Method) == 0) ? 0 : 4;
                int expectedNestedTypes = ((memberType & (MemberTypes.NestedType | MemberTypes.TypeInfo)) == 0) ? 0 : 1;
                int expectedProperties = ((memberType & MemberTypes.Property) == 0) ? 0 : 1;
    
                Assert.Equal(expectedConstructors, constructors);
                Assert.Equal(expectedEvents, events);
                Assert.Equal(expectedFields, fields);
                Assert.Equal(expectedMethods, methods);
                Assert.Equal(expectedNestedTypes, nestedTypes);
                Assert.Equal(expectedProperties, properties);
            }
        }
    
        [Fact]
        public static void TestZeroMatch()
        {
            Type t = typeof(Mixed).Project();
            MemberInfo[] members = t.GetMember("NOSUCHMEMBER", MemberTypes.All, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            Assert.Equal(0, members.Length);
        }
    
    
        [Fact]
        public static void TestCaseSensitive1()
        {
            Type t = typeof(Mixed).Project();
            MemberInfo[] members = t.GetMember("MyField", MemberTypes.All, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            Assert.Equal(1, members.Length);
            Assert.True(members[0] is FieldInfo);
            Assert.Equal("MyField", members[0].Name);
        }
    
        [Fact]
        public static void TestCaseSensitive2()
        {
            Type t = typeof(Mixed).Project();
            MemberInfo[] members = t.GetMember("MYFIELD", MemberTypes.All, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            Assert.Equal(0, members.Length);
        }
    
        [Fact]
        public static void TestCaseInsensitive1()
        {
            Type t = typeof(Mixed).Project();
            MemberInfo[] members = t.GetMember("MyField", MemberTypes.All, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.IgnoreCase);
            Assert.Equal(1, members.Length);
            Assert.True(members[0] is FieldInfo);
            Assert.Equal("MyField", members[0].Name);
        }
    
        [Fact]
        public static void TestCaseInsensitive2()
        {
            Type t = typeof(Mixed).Project();
            MemberInfo[] members = t.GetMember("MYfiELD", MemberTypes.All, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.IgnoreCase);
            Assert.Equal(1, members.Length);
            Assert.True(members[0] is FieldInfo);
            Assert.Equal("MyField", members[0].Name);
        }
    
        [Fact]
        public static void TestPrefixCaseSensitive1()
        {
            Type t = typeof(Mixed).Project();
            MemberInfo[] members = t.GetMember("MyFi*", MemberTypes.All, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            Assert.Equal(1, members.Length);
            Assert.True(members[0] is FieldInfo);
            Assert.Equal("MyField", members[0].Name);
        }
    
        [Fact]
        public static void TestPrefixCaseSensitive2()
        {
            Type t = typeof(Mixed).Project();
            MemberInfo[] members = t.GetMember("MYFI*", MemberTypes.All, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            Assert.Equal(0, members.Length);
        }
    
        [Fact]
        public static void TestPrefixCaseInsensitive1()
        {
            Type t = typeof(Mixed).Project();
            MemberInfo[] members = t.GetMember("MyFi*", MemberTypes.All, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.IgnoreCase);
            Assert.Equal(1, members.Length);
            Assert.True(members[0] is FieldInfo);
            Assert.Equal("MyField", members[0].Name);
        }
    
        [Fact]
        public static void TestPrefixCaseInsensitive2()
        {
            Type t = typeof(Mixed).Project();
            MemberInfo[] members = t.GetMember("MYFI*", MemberTypes.All, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.IgnoreCase);
            Assert.Equal(1, members.Length);
            Assert.True(members[0] is FieldInfo);
            Assert.Equal("MyField", members[0].Name);
        }
    
        private class Mixed
        {
            public Mixed() { }
            public event Action MyEvent { add { } remove { } }
            public int MyField;
            public void MyMethod() { }
            public class MyType { }
            public int MyProperty { get; }
        }
    }
}
