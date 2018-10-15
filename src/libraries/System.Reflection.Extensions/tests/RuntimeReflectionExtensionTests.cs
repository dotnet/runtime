// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.Reflection.Tests
{
    public class RuntimeReflectionExtensionsTests
    {
        [Fact]
        public void GetRuntimeEvents()
        {
            AssertExtensions.Throws<ArgumentNullException>("type", () => default(Type).GetRuntimeEvents());

            List<EventInfo> events = typeof(TestType).GetRuntimeEvents().ToList();
            Assert.Equal(1, events.Count);
            Assert.Equal("StuffHappened", events[0].Name);
        }

        [Fact]
        public void GetRuntimeMethods()
        {
            var types = GetTypes();

            AssertExtensions.Throws<ArgumentNullException>("type", () =>
            {
                RuntimeReflectionExtensions.GetRuntimeMethods(default(Type));
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

            AssertExtensions.Throws<ArgumentNullException>("type", () =>
            {
                RuntimeReflectionExtensions.GetRuntimeFields(default(Type));
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
        public void GetRuntimeProperties()
        {
            AssertExtensions.Throws<ArgumentNullException>("type", () => default(Type).GetRuntimeProperties());

            List<PropertyInfo> properties = typeof(TestType).GetRuntimeProperties().ToList();
            List<string> propertyNames = properties.Select(p => p.Name).Distinct().ToList();
            Assert.Equal(5, properties.Count);
            Assert.Contains("Length", propertyNames);
            Assert.Contains("Position", propertyNames);
            Assert.Contains("CanRead", propertyNames);
            Assert.Contains("CanWrite", propertyNames);
            Assert.Contains("CanSeek", propertyNames);
        }        

        [Fact]
        public void GetRuntimeProperty()
        {
            var types = GetTypes();

            AssertExtensions.Throws<ArgumentNullException>("type", () =>
            {
                RuntimeReflectionExtensions.GetRuntimeProperty(default(Type), "foo");
            });

            AssertExtensions.Throws<ArgumentNullException>("name", () =>
            {
                typeof(RuntimeReflectionExtensionsTests).GetRuntimeProperty(null);
            });

            Assert.Null(typeof(TestType).GetRuntimeProperty(""));

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

            Assert.Equal(typeof(TestType).GetProperty("Length"), typeof(TestType).GetRuntimeProperty("Length"));
        }

        [Fact]
        public void GetRuntimeEvent()
        {
            var types = GetTypes();

            AssertExtensions.Throws<ArgumentNullException>("type", () =>
            {
                RuntimeReflectionExtensions.GetRuntimeEvent(default(Type), "foo");
            });


            Assert.Throws<ArgumentNullException>(null, () =>
            {
                typeof(RuntimeReflectionExtensionsTests).GetRuntimeEvent(null);
            });

            Assert.Null(typeof(TestType).GetRuntimeEvent(""));            

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

            Assert.Equal(typeof(TestType).GetEvent("StuffHappened"), typeof(TestType).GetRuntimeEvent("StuffHappened"));
        }

        [Fact]
        public void GetRuntimeMethod()
        {
            var types = GetTypes();

            AssertExtensions.Throws<ArgumentNullException>("type", () =>
            {
                RuntimeReflectionExtensions.GetRuntimeMethod(default(Type), "foo", Type.EmptyTypes);
            });


            AssertExtensions.Throws<ArgumentNullException>("name", () =>
            {
                typeof(RuntimeReflectionExtensionsTests).GetRuntimeMethod(null, Type.EmptyTypes);
            });


            AssertExtensions.Throws<ArgumentNullException>("types", () =>
            {
                typeof(RuntimeReflectionExtensionsTests).GetRuntimeMethod("RunTest_GetRuntimeMethod", null);
            });

            Assert.Null(typeof(TestType).GetRuntimeMethod("", Type.EmptyTypes));

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

            Assert.Equal(typeof(TestType).GetMethod("Flush"), typeof(TestType).GetRuntimeMethod("Flush", Array.Empty<Type>()));
        }

        [Fact]
        public void GetRuntimeField()
        {
            var types = GetTypes();

            AssertExtensions.Throws<ArgumentNullException>("type", () =>
            {
                RuntimeReflectionExtensions.GetRuntimeField(default(Type), "foo");
            });


            Assert.Throws<ArgumentNullException>(null, () =>
            {
                typeof(RuntimeReflectionExtensionsTests).GetRuntimeField(null);
            });

            Assert.Null(typeof(TestType).GetRuntimeField(""));

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

            Assert.Equal(typeof(TestType).GetField("_pizzaSize"), typeof(TestType).GetRuntimeField("_pizzaSize"));
        }

        [Fact]
        public void GetMethodInfo()
        {
            AssertExtensions.Throws<ArgumentNullException>("del", () => default(Action).GetMethodInfo());
            Assert.Equal(typeof(RuntimeReflectionExtensionsTests).GetMethod("GetMethodInfo"), ((Action)GetMethodInfo).GetMethodInfo());
        }        

        [Fact]
        public void GetRuntimeBaseDefinition()
        {
            Assert.Throws<ArgumentNullException>(() => default(MethodInfo).GetRuntimeBaseDefinition());

            MethodInfo derivedFoo = typeof(TestDerived).GetMethod(nameof(TestDerived.Foo));
            MethodInfo baseFoo = typeof(TestBase).GetMethod(nameof(TestBase.Foo));
            MethodInfo actual = derivedFoo.GetRuntimeBaseDefinition();
            Assert.Equal(baseFoo, actual);
        }

        [Fact]
        public void GetRuntimeInterfaceMap()
        {
            AssertExtensions.Throws<ArgumentNullException>("typeInfo", () => default(TypeInfo).GetRuntimeInterfaceMap(typeof(ICloneable)));
            AssertExtensions.Throws<ArgumentNullException>("ifaceType", () => typeof(TestType).GetTypeInfo().GetRuntimeInterfaceMap(null));
            Assert.Throws<ArgumentException>(() => typeof(TestType).GetTypeInfo().GetRuntimeInterfaceMap(typeof(ICloneable)));
            Assert.Throws<ArgumentException>(() => typeof(TestType).GetTypeInfo().GetRuntimeInterfaceMap(typeof(string)));
            
            InterfaceMapping map = typeof(TestType).GetTypeInfo().GetRuntimeInterfaceMap(typeof(IDisposable));
            Assert.Same(typeof(TestType), map.TargetType);
            Assert.Same(typeof(IDisposable), map.InterfaceType);
            Assert.Equal(1, map.InterfaceMethods.Length);
            Assert.Equal(1, map.TargetMethods.Length);
            MethodInfo ifaceDispose = map.InterfaceMethods[0];
            MethodInfo targetDispose = map.TargetMethods[0];
            Assert.Equal(ifaceDispose.CallingConvention, targetDispose.CallingConvention);
            Assert.Equal(ifaceDispose.Name, targetDispose.Name);
            Assert.Same(ifaceDispose.ReturnType, targetDispose.ReturnType);
            Assert.Equal(ifaceDispose.GetParameters().Length, targetDispose.GetParameters().Length);
            Assert.Same(typeof(TestTypeBase), targetDispose.DeclaringType);
            Assert.Same(typeof(IDisposable), ifaceDispose.DeclaringType);
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

        private abstract class TestBase
        {
            public abstract void Foo();
        }

        private class TestDerived : TestBase
        {
            public override void Foo() { throw null; }
        }

        abstract class TestTypeBase : IDisposable
        {
            public abstract bool CanRead { get; }
            public abstract bool CanWrite { get; }
            public abstract bool CanSeek { get; }
            public abstract long Length { get; }
            public abstract long Position { get; set; }

            public virtual Task FlushAsync()
            {
                throw null;
            }

            public abstract void Flush();

            public virtual Task<int> ReadAsync(byte[] buffer, int offset, int count)
            {
                throw null;
            }

            public abstract int Read(byte[] buffer, int offset, int count);

            public abstract long Seek(long offset, SeekOrigin origin);

            public abstract void SetLength(long value);

            public abstract void Write(byte[] buffer, int offset, int count);
            public virtual Task WriteAsync(byte[] buffer, int offset, int count)
            {
                throw null;
            }

            public void Dispose()
            {
                throw null;
            }
        }

        class TestType : TestTypeBase
        {
            public TestType()
            {
            }

            public class Nested
            {

            }

    #pragma warning disable 0067 // event never used
            public event Action<int> StuffHappened;
    #pragma warning restore 0067

    #pragma warning disable 0169 // field never used
            private int _pizzaSize;
    #pragma warning restore 0169

            public override bool CanRead => false;

            public override bool CanSeek => false;

            public override bool CanWrite => false;

            public override long Length
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public override long Position
            {
                get
                {
                    throw new NotImplementedException();
                }

                set
                {
                    throw new NotImplementedException();
                }
            }

            public override void Flush()
            {
                throw new NotImplementedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException();
            }

            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }
        }        
    }
}
