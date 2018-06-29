// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Tests
{
    public class RuntimeReflectionExtensionsTests
    {
        [Fact]
        public void GetRuntimeMethods()
        {
            var types = GetTypes();

            Assert.Throws<ArgumentNullException>(() =>
            {
                RuntimeReflectionExtensions.GetRuntimeMethods(null);
            });


            List<string> methods = new List<string>();

            foreach (TypeInfo type in types)
            {
                if (!type.Namespace.Equals("MethodDefinitions", StringComparison.Ordinal))
                    continue;

                methods.Clear();
                methods.AddRange((IEnumerable<string>)type.GetDeclaredField("DeclaredMethodNames").GetValue(null));
                methods.AddRange((IEnumerable<string>)type.GetDeclaredField("InheritedMethodNames").GetValue(null));
                if (type.GetDeclaredField("NewMethodNames") != null)
                    methods.AddRange((IEnumerable<string>)type.GetDeclaredField("NewMethodNames").GetValue(null));

                //inherited from object
                methods.Add("System.String ToString()");
                methods.Add("Boolean Equals(System.Object)");
                methods.Add("Int32 GetHashCode()");
                methods.Add("System.Type GetType()");

                methods.Add("Void Finalize()");
                methods.Add("System.Object MemberwiseClone()");

                Assert.All(type.AsType().GetRuntimeMethods(), m => Assert.True(methods.Remove(m.ToString())));
                Assert.Empty(methods);
            }
        }

        [Fact]
        public void GetRuntimeFields()
        {
            var types = GetTypes();

            Assert.Throws<ArgumentNullException>(() =>
            {
                RuntimeReflectionExtensions.GetRuntimeFields(null);
            });

            List<string> fields = new List<string>();

            foreach (TypeInfo type in types)
            {
                if (!type.Namespace.Equals("FieldDefinitions", StringComparison.Ordinal))
                    continue;

                fields.Clear();
                fields.AddRange((IEnumerable<string>)type.GetDeclaredField("DeclaredFieldNames").GetValue(null));
                fields.AddRange((IEnumerable<string>)type.GetDeclaredField("InheritedFieldNames").GetValue(null));
                if (type.GetDeclaredField("NewFieldNames") != null)
                    fields.AddRange((IEnumerable<string>)type.GetDeclaredField("NewFieldNames").GetValue(null));

                Assert.All(type.AsType().GetRuntimeFields(), f => Assert.True(fields.Remove(f.Name)));
                Assert.Empty(fields);
            }
        }

        [Fact]
        public void GetRuntimeProperty()
        {
            var types = GetTypes();

            Assert.Throws<ArgumentNullException>(() =>
            {
                RuntimeReflectionExtensions.GetRuntimeProperty(null, "foo");
            });



            Assert.Throws<ArgumentNullException>(() =>
            {
                typeof(RuntimeReflectionExtensionsTests).GetRuntimeProperty(null);
            });


            List<string> properties = new List<string>();

            foreach (TypeInfo type in types)
            {
                if (!type.Namespace.Equals("PropertyDefinitions", StringComparison.Ordinal))
                    continue;

                properties.Clear();
                properties.AddRange((IEnumerable<string>)type.GetDeclaredField("PublicPropertyNames").GetValue(null));

                foreach (string propertyName in properties)
                {
                    bool exceptionExpected = propertyName.Equals("Item");

                    // Slight duplication of code her to allow use of Assert.Throws
                    if (exceptionExpected == true)
                    {
                        Assert.Throws<AmbiguousMatchException>(() =>
                        {
                            PropertyInfo pi = type.AsType().GetRuntimeProperty(propertyName);
                        });
                    }
                    else
                    {
                        PropertyInfo pi = type.AsType().GetRuntimeProperty(propertyName);
                        Assert.NotNull(pi);
                    }
                }
            }
        }

        [Fact]
        public void GetRuntimeEvent()
        {
            var types = GetTypes();

            Assert.Throws<ArgumentNullException>(() =>
            {
                RuntimeReflectionExtensions.GetRuntimeEvent(null, "foo");
            });


            Assert.Throws<ArgumentNullException>(() =>
            {
                typeof(RuntimeReflectionExtensionsTests).GetRuntimeEvent(null);
            });

            List<string> events = new List<string>();

            foreach (TypeInfo type in types)
            {
                if (!type.Namespace.Equals("EventDefinitions", StringComparison.Ordinal))
                    continue;

                if (type.IsInterface)
                    continue;

                events.Clear();
                events.AddRange((IEnumerable<string>)type.GetDeclaredField("PublicEvents").GetValue(null));

                foreach (string eventName in events)
                {
                    Assert.NotNull(type.AsType().GetRuntimeEvent(eventName));
                }
            }
        }

        [Fact]
        public void GetRuntimeMethod()
        {
            var types = GetTypes();

            Assert.Throws<ArgumentNullException>(() =>
            {
                RuntimeReflectionExtensions.GetRuntimeMethod(null, "foo", new Type[0]);
            });


            Assert.Throws<ArgumentNullException>(() =>
            {
                typeof(RuntimeReflectionExtensionsTests).GetRuntimeMethod(null, new Type[0]);
            });


            Assert.Throws<ArgumentNullException>(() =>
            {
                typeof(RuntimeReflectionExtensionsTests).GetRuntimeMethod("RunTest_GetRuntimeMethod", null);
            });

            List<string> methods = new List<string>();

            foreach (TypeInfo type in types)
            {
                if (!type.Namespace.Equals("MethodDefinitions", StringComparison.Ordinal))
                    continue;

                methods.Clear();
                methods.AddRange((IEnumerable<string>)type.GetDeclaredField("PublicMethodNames").GetValue(null));

                foreach (string method in methods)
                {
                    string methodName = GetMethodName(method);
                    Type[] parameters = GetMethodParameters(method);
                    Assert.NotNull(type.AsType().GetRuntimeMethod(methodName, parameters));
                }
            }
        }

        [Fact]
        public void GetRuntimeField()
        {
            var types = GetTypes();

            Assert.Throws<ArgumentNullException>(() =>
            {
                RuntimeReflectionExtensions.GetRuntimeField(null, "foo");
            });


            Assert.Throws<ArgumentNullException>(() =>
            {
                typeof(RuntimeReflectionExtensionsTests).GetRuntimeField(null);
            });

            List<string> fields = new List<string>();

            foreach (TypeInfo type in types)
            {
                if (!type.Namespace.Equals("FieldDefinitions", StringComparison.Ordinal))
                    continue;

                fields.Clear();
                fields.AddRange((IEnumerable<string>)type.GetDeclaredField("PublicFieldNames").GetValue(null));

                foreach (string fieldName in fields)
                {
                    Assert.NotNull(type.AsType().GetRuntimeField(fieldName));
                }
            }
        }

        private static string GetMethodName(string methodName)
        {
            int startIndex = methodName.IndexOf(" ") + 1;
            int endIndex = methodName.IndexOf("(");
            return methodName.Substring(startIndex, endIndex - startIndex);
        }

        private static Type[] GetMethodParameters(string methodName)
        {
            int startIndex = methodName.IndexOf("(") + 1;
            int endIndex = methodName.IndexOf(")");
            if (endIndex <= startIndex)
                return new Type[0];

            string[] parameters = methodName.Substring(startIndex, endIndex - startIndex).Split(',');
            List<Type> parameterList = new List<Type>();
            foreach (string parameter in parameters)
                parameterList.Add(Type.GetType(parameter.Trim()));
            return parameterList.ToArray();
        }


        private static TypeInfo[] GetTypes()
        {
            Assembly asm = typeof(PropertyTestBaseClass).GetTypeInfo().Assembly;
            var list = new List<TypeInfo>();
            foreach (var t in asm.DefinedTypes)
            {
                if (t.Namespace == null) continue; // These are classes in the global namespace; not under test
                list.Add(t);
            }
            return list.ToArray();
        }
    }
}
