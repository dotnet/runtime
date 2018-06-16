// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace System.Reflection.Context.Tests
{
    internal class TestCustomReflectionContext : CustomReflectionContext
    {
        private readonly Dictionary<string, object> _properties = new Dictionary<string, object>
        {
            { "text", "abc" },
            { "boolean", true }
        };
        
        protected override IEnumerable<PropertyInfo> AddProperties(Type type)
        {
            base.AddProperties(type);
            
            if (type == typeof(TestObject))
            {
                foreach (KeyValuePair<string, object> prop in _properties)
                {
                    Type newType = MapType(prop.Value.GetType().GetTypeInfo());

                    yield return CreateProperty(newType, prop.Key,
                        _ => _properties[prop.Key],
                        (_, value) => _properties[prop.Key] = value);
                }

                Type numberType = MapType(typeof(int).GetTypeInfo());
                yield return CreateProperty(numberType, "number", _ => 42, (a, b) => { },
                    new Attribute[] { new TestPropertyAttribute() },
                    new Attribute[] { new TestGetterSetterAttribute() },
                    new Attribute[] { new TestGetterSetterAttribute() });
            }
        }

        protected override IEnumerable<object> GetCustomAttributes(ParameterInfo parameter, IEnumerable<object> declaredAttributes)
        {
            base.GetCustomAttributes(parameter, declaredAttributes);

            if (parameter.Name == "a")
            {
                yield return new TestAttribute();
            }
        }

        protected override IEnumerable<object> GetCustomAttributes(MemberInfo member, IEnumerable<object> declaredAttributes)
        {
            base.GetCustomAttributes(member, declaredAttributes);

            if (member.Name == "GetMessage")
            {
                yield return new TestAttribute();
            }
        }
    }

    internal class FaultyTestCustomReflectionContext : CustomReflectionContext
    {
        public FaultyTestCustomReflectionContext() : base(null)
        {
        }
    }

    internal class VirtualPropertyInfoCustomReflectionContext : CustomReflectionContext
    {
        protected override IEnumerable<PropertyInfo> AddProperties(Type type)
        {
            base.AddProperties(type);

            if (type == typeof(NullGetterAndSetterCase))
            {
                Type newType = MapType(typeof(NullGetterAndSetterCase).GetTypeInfo());
                yield return CreateProperty(newType, "type", null, null);
            }

            if (type == typeof(WrongContextCase))
            {
                yield return CreateProperty(typeof(int), "type", _ => 42, null);
            }

            if (type == typeof(NullPropertyNameCase))
            {
                Type newType = MapType(typeof(NullPropertyNameCase).GetTypeInfo());
                yield return CreateProperty(newType, null, _ => 42, null);
            }

            if (type == typeof(EmptyPropertyNameCase))
            {
                Type newType = MapType(typeof(EmptyPropertyNameCase).GetTypeInfo());
                yield return CreateProperty(newType, "", _ => 42, null);
            }

            if (type == typeof(NullPropertyTypeCase))
            {
                yield return CreateProperty(null, "type", _ => 42, null);
            }

            if (type == typeof(TestObject))
            {
                Type numberType = MapType(typeof(int).GetTypeInfo());
                yield return CreateProperty(numberType, "number", _ => 42, (a, b) => { },
                    new Attribute[] { new TestPropertyAttribute() },
                    new Attribute[] { new TestGetterSetterAttribute() },
                    new Attribute[] { new TestGetterSetterAttribute() });

                yield return CreateProperty(numberType, "number2", null, (a, b) => { });
                yield return CreateProperty(numberType, "number3", _ => 42, null);
            }
        }
    }

    internal struct NullGetterAndSetterCase { }

    internal struct WrongContextCase { }

    internal struct NullPropertyNameCase { }

    internal struct EmptyPropertyNameCase { }

    internal struct NullPropertyTypeCase { }

    [DataContract]
    internal class TestObject
    {
        public TestObject([TestParameter] string a)
        {
            A = a;
        }

        [DataMember]
        public string A { get; }

        [OnSerialized]
        public string GetMessage()
        {
            return A;
        }

        public int this[int index]
        {
            get => 42;
            set { }
        }

    }

    internal class TestAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    internal class TestParameterAttribute : Attribute
    {
    }

    internal class TestPropertyAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    internal class TestGetterSetterAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Assembly)]
    internal class TestAssemblyAttribute : Attribute
    {
    }
}
