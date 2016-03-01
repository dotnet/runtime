// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace System.Reflection.Tests
{

    public class TypeInfo_Members
    {
        [Fact]
        public static void GetConstructors()
        {
            ConstructorInfo[] ctors;

            ctors = typeof(MyCustomType).GetTypeInfo().GetConstructors();
            Assert.NotNull(ctors);
            Assert.Equal(2, ctors.Length);
            Assert.True(ctors.All(m => m.IsPublic));

            ctors = typeof(MyCustomType).GetTypeInfo().GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(ctors);
            Assert.Equal(2, ctors.Length);
            Assert.True(ctors.All(m => m.IsPublic));

            ctors = typeof(MyCustomType).GetTypeInfo().GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(ctors);
            Assert.Equal(1, ctors.Length);
            Assert.True(ctors.All(m => m.IsPrivate));
            Assert.Equal(1, ctors[0].GetParameters().Length);
        }

        [Fact]
        public static void GetConstructor()
        {
            ConstructorInfo ctor;

            ctor = typeof(MyCustomType).GetTypeInfo().GetConstructor(Type.EmptyTypes);
            Assert.NotNull(ctor);
            Assert.True(ctor.IsPublic);
            Assert.Equal(0, ctor.GetParameters().Length);

            ctor = typeof(MyCustomType).GetTypeInfo().GetConstructor(new Type[] { typeof(int), typeof(string) });
            Assert.NotNull(ctor);
            Assert.True(ctor.IsPublic);
            Assert.Equal(2, ctor.GetParameters().Length);

            ctor = typeof(MyCustomType).GetTypeInfo().GetConstructor(new Type[] { typeof(string), typeof(int) });
            Assert.Null(ctor);

            ctor = typeof(MyCustomType).GetTypeInfo().GetConstructor(new Type[] { typeof(string) });
            Assert.Null(ctor);
        }

        [Fact]
        public static void FindMembers()
        {
            MemberInfo[] members = typeof(MyCustomType).GetTypeInfo().FindMembers(MemberTypes.All, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, (MemberInfo memberInfo, object c) => true, "notused");
            Assert.Equal(28, members.Length);

            members = typeof(MyCustomType).GetTypeInfo().FindMembers(MemberTypes.Constructor, BindingFlags.Public | BindingFlags.Instance, (MemberInfo memberInfo, object c) => true, "notused");
            Assert.Equal(2, members.Length);
            Assert.True(members.All(m => m.Name.Equals(".ctor")));

            members = typeof(MyCustomType).GetTypeInfo().FindMembers(MemberTypes.Constructor, BindingFlags.NonPublic | BindingFlags.Instance, (MemberInfo memberInfo, object c) => true, "notused");
            Assert.Equal(1, members.Length);
            Assert.Equal(1, ((ConstructorInfo)members[0]).GetParameters().Length);

            members = typeof(MyCustomType).GetTypeInfo().FindMembers(MemberTypes.Constructor, BindingFlags.NonPublic | BindingFlags.Instance, (MemberInfo memberInfo, object c) => ((ConstructorInfo)memberInfo).GetParameters().Length >= Convert.ToInt32(c), "1");
            Assert.Equal(1, members.Length);
            Assert.Equal(".ctor", members[0].Name);

            members = typeof(MyCustomType).GetTypeInfo().FindMembers(MemberTypes.Event, BindingFlags.NonPublic | BindingFlags.Instance, (MemberInfo memberInfo, object c) => true, "notused");
            Assert.Equal(1, members.Length);

            members = typeof(MyCustomType).GetTypeInfo().FindMembers(MemberTypes.Event, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, (MemberInfo memberInfo, object c) => memberInfo.Name.Contains(c.ToString()), "Event");
            Assert.Equal(2, members.Length);
            Assert.True(members.All(m => m.Name.Contains("Event")));

            members = typeof(MyCustomType).GetTypeInfo().FindMembers(MemberTypes.Property, BindingFlags.NonPublic | BindingFlags.Instance, (MemberInfo memberInfo, object c) => true, "notused");
            Assert.Equal(1, members.Length);
            Assert.Equal("PrivateProp", members[0].Name);

            members = typeof(MyCustomType).GetTypeInfo().FindMembers(MemberTypes.Property, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, (MemberInfo memberInfo, object c) => memberInfo.Name.Contains(c.ToString()), "Prop");
            Assert.Equal(2, members.Length);
            Assert.True(members.All(m => m.Name.Contains("Prop")));

            members = typeof(MyCustomType).GetTypeInfo().FindMembers(MemberTypes.Method, BindingFlags.NonPublic | BindingFlags.Instance, (MemberInfo memberInfo, object c) => true, "notused");
            Assert.Equal(7, members.Length);

            members = typeof(MyCustomType).GetTypeInfo().FindMembers(MemberTypes.Method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, (MemberInfo memberInfo, object c) => memberInfo.Name.Contains(c.ToString()), "get");
            Assert.Equal(2, members.Length);
            Assert.True(members.All(m => m.Name.Contains("Prop")));
            Assert.True(members.All(m => m.Name.Contains("get_")));

            members = typeof(MyCustomType).GetTypeInfo().FindMembers(MemberTypes.NestedType, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, (MemberInfo memberInfo, object c) => true, "notused");
            Assert.Equal(1, members.Length);
            Assert.True(members[0].Name.Contains("EventHandler"));
        }

        [Fact]
        public static void GetProperties()
        {
            PropertyInfo[] props = typeof(MyCustomType).GetTypeInfo().GetProperties();
            Assert.Equal(1, props.Length);

            props = typeof(MyCustomType).GetTypeInfo().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            Assert.Equal(1, props.Length);

            props = typeof(MyCustomType).GetTypeInfo().GetProperties(BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.Equal(1, props.Length);

            props = typeof(MyCustomType).GetTypeInfo().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.Equal(2, props.Length);
        }

        [Fact]
        public static void GetProperty()
        {
            PropertyInfo prop = typeof(MyCustomType).GetTypeInfo().GetProperty("PublicProp");
            Assert.NotNull(prop);

            prop = typeof(MyCustomType).GetTypeInfo().GetProperty("PublicProp", typeof(int));
            Assert.NotNull(prop);

            prop = typeof(MyCustomType).GetTypeInfo().GetProperty("PublicProp", Type.EmptyTypes);
            Assert.NotNull(prop);

            prop = typeof(MyCustomType).GetTypeInfo().GetProperty("PublicProp", typeof(int), Type.EmptyTypes);
            Assert.NotNull(prop);

            prop = typeof(MyCustomType).GetTypeInfo().GetProperty("PublicProp", typeof(int), Type.EmptyTypes, null);
            Assert.NotNull(prop);
        }

        [Fact]
        public static void GetMethod()
        {
            MethodInfo mi = typeof(MyCustomType).GetTypeInfo().GetMethod("PublicMethod");
            Assert.NotNull(mi);

            mi = typeof(MyCustomType).GetTypeInfo().GetMethod("PublicMethod", Type.EmptyTypes, null);
            Assert.NotNull(mi);

            Assert.Throws<ArgumentNullException>(() => typeof(MyCustomType).GetTypeInfo().GetMethod(null));
            Assert.Throws<ArgumentNullException>(() => typeof(MyCustomType).GetTypeInfo().GetMethod("p", null));
            Assert.Throws<ArgumentNullException>(() => typeof(MyCustomType).GetTypeInfo().GetMethod("p", new Type[]{typeof(int), null}));
        }

        [Fact]
        public static void GetMethods()
        {
            MethodInfo[] mis = typeof(MyCustomType).GetTypeInfo().GetMethods();
            Assert.Equal(9, mis.Length);

            mis = typeof(MyCustomType).GetTypeInfo().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.Equal(16, mis.Length);
        }

        [Fact]
        public static void GetNestedType()
        {
            Type type = typeof(MyCustomType).GetTypeInfo().GetNestedType("EventHandler");
            Assert.NotNull(type);

            type = typeof(MyCustomType).GetTypeInfo().GetNestedType("EventHandler", BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(type);
        }

        [Fact]
        public static void GetNestedTypes()
        {
            Type[] types = typeof(MyCustomType).GetTypeInfo().GetNestedTypes();
            Assert.Equal(1, types.Length);

            types = typeof(MyCustomType).GetTypeInfo().GetNestedTypes(BindingFlags.Public | BindingFlags.Instance);
            Assert.Equal(1, types.Length);
        }

        [Fact]
        public static void GetMember()
        {
            MemberInfo[] memberInfo = typeof(MyCustomType).GetTypeInfo().GetMember("Public*");
            Assert.Equal(4, memberInfo.Length);

            memberInfo = typeof(MyCustomType).GetTypeInfo().GetMember("EventHandler");
            Assert.Equal(1, memberInfo.Length);

            memberInfo = typeof(MyCustomType).GetTypeInfo().GetMember("P*", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.Equal(10, memberInfo.Length);

            memberInfo = typeof(MyCustomType).GetTypeInfo().GetMember(".ctor", MemberTypes.Constructor, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.Equal(3, memberInfo.Length);
        }

        [Fact]
        public static void GetMembers()
        {
            MemberInfo[] memberInfo = typeof(MyCustomType).GetTypeInfo().GetMembers();
            Assert.Equal(15, memberInfo.Length);

            memberInfo = typeof(MyCustomType).GetTypeInfo().GetMembers(BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.Equal(13, memberInfo.Length);

            memberInfo = typeof(MyCustomType).GetTypeInfo().GetMembers(BindingFlags.Public | BindingFlags.Instance);
            Assert.Equal(15, memberInfo.Length);
        }

        [Fact]
        public static void GetInterface()
        {
            Type t = typeof(MyCustomType).GetTypeInfo().GetInterface("IFace");
            Assert.NotNull(t);

            t = typeof(MyCustomType).GetTypeInfo().GetInterface("IFace2", true);
            Assert.NotNull(t);

            t = typeof(MyCustomType).GetTypeInfo().GetInterface("IFace2", false);
            Assert.Null(t);
        }

        [Fact]
        public static void GetInterfaces()
        {
            Type[] t = typeof(MyCustomType).GetTypeInfo().GetInterfaces();
            Assert.Equal(2, t.Length);

            t = typeof(Iface2).GetTypeInfo().GetInterfaces();
            Assert.Equal(0, t.Length);
        }

        [Fact]
        public static void GetGenericArguments()
        {
            Type[] t = typeof(List<>).GetTypeInfo().GetGenericArguments();
            Assert.Equal(1, t.Length);

            t = typeof(Dictionary<,>).GetTypeInfo().GetGenericArguments();
            Assert.Equal(2, t.Length);

            t = typeof(CustomGenericType<,>).GetTypeInfo().GetGenericArguments();
            Assert.Equal(2, t.Length);
            Assert.Equal("TKey", t[0].Name);
            Assert.Equal("TValue", t[1].Name);


            CustomGenericType<int, string> genType = new CustomGenericType<int, string>();
            t = genType.GetType().GetTypeInfo().GetGenericArguments();
            Assert.Equal(2, t.Length);
            Assert.Equal(typeof(int), t[0]);
            Assert.Equal(typeof(string), t[1]);
        }

        [Fact]
        public static void GetEvent()
        {
            EventInfo eventInfo;

            eventInfo = typeof(MyCustomType).GetTypeInfo().GetEvent("PublicEvent");
            Assert.NotNull(eventInfo);

            eventInfo = typeof(MyCustomType).GetTypeInfo().GetEvent("PrivateEvent");
            Assert.Null(eventInfo);

            eventInfo = typeof(MyCustomType).GetTypeInfo().GetEvent("PrivateEvent", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(eventInfo);

            Assert.Throws<ArgumentNullException>(() => typeof(MyCustomType).GetTypeInfo().GetEvent(null));
        }

        [Fact]
        public static void GetEvents()
        {
            EventInfo[] eventInfos;

            eventInfos = typeof(MyCustomType).GetTypeInfo().GetEvents();
            Assert.Equal(1, eventInfos.Length);

            eventInfos = typeof(MyCustomType).GetTypeInfo().GetEvents(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.Equal(2, eventInfos.Length);

            eventInfos = typeof(MyCustomType).GetTypeInfo().GetEvents(BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.Equal(1, eventInfos.Length);
            Assert.Equal("PrivateEvent", eventInfos[0].Name);
        }

        [Fact]
        public static void GetField()
        {
            FieldInfo field;

            field = typeof(MyCustomType).GetTypeInfo().GetField("PublicField");
            Assert.NotNull(field);
            Assert.True(field.IsPublic);

            field = typeof(MyCustomType).GetTypeInfo().GetField("PrivateField");
            Assert.Null(field);

            field = typeof(MyCustomType).GetTypeInfo().GetField("PrivateField", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);
            Assert.True(field.IsPrivate);

            Assert.Throws<ArgumentNullException>(()=> typeof(MyCustomType).GetTypeInfo().GetField(null));
        }

        [Fact]
        public static void GetFields()
        {
            FieldInfo[] fields;

            fields = typeof(MyCustomType).GetTypeInfo().GetFields();
            Assert.Equal(1, fields.Length);

            fields = typeof(MyCustomType).GetTypeInfo().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.Equal(4, fields.Length);

            fields = typeof(MyCustomType).GetTypeInfo().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.Equal(3, fields.Length);
        }



        [Fact]
        public static void GetDefaultMembers()
        {
            MemberInfo[] members = typeof(MyCustomType).GetTypeInfo().GetDefaultMembers();
            Assert.Equal(1, members.Length);
            Assert.Equal("PublicField", members[0].Name);

            members = typeof(IFace).GetTypeInfo().GetDefaultMembers();
            Assert.Equal(0, members.Length);

            members = typeof(MyCustomType.EventHandler).GetTypeInfo().GetDefaultMembers();
            Assert.Equal(0, members.Length);
        }

#pragma warning disable 0067, 0169
        public class CustomGenericType<TKey, TValue>
        {
            TKey key;
            TValue value;
        }

        public enum MyUint32Enum : uint
        {
            A = 1,
            B = 10
        }

        public enum MyCustomEnum
        {
            Enum1 = 1,
            Enum2 = 2,
            Enum10 = 10,
            Enum18 = 18,
            Enum45 = 45
        }

        public interface IFace { }
        public interface Iface2 { }

        [DefaultMemberAttribute("PublicField")]

        public class MyCustomType : IFace, Iface2
        {
            public int PublicField;
            private string PrivateField;

            public MyCustomType() { }
            public MyCustomType(int intField, string stringField) { }

            private MyCustomType(string stringField) { }

            public int PublicProp
            {
                get { return 10; }
                set { }
            }

            private string PrivateProp
            {
                get { return string.Empty; }
                set { }
            }

            public delegate void EventHandler(object sender, EventArgs e);

            public event EventHandler PublicEvent;
            private event EventHandler PrivateEvent;

            public void PublicMethod() { }
            private int PrivateMethod(int x, string y) { return default(int); }
        }
#pragma warning restore 0067, 0169
    }
}