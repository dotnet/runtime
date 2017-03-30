// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Tests;
using Xunit;

namespace System.Security.Permissions.Tests
{
    public class SerializationTests
    {
        public static IEnumerable<object[]> GetAllSerializableObjectsInAssembly()
        {
            IEnumerable<Type> serializableTypes = typeof(CodeAccessPermission).Assembly
                .GetTypes()
                .Where(t => !t.IsAbstract && (t.Attributes & TypeAttributes.Serializable) != 0);

            foreach (Type serializableType in serializableTypes)
            {
                // Create an instance of the object, with its default ctor if possible, or worst case unitialized (we don't
                // care about functionality for these types, so an uninitialized object suits our needs).
                object obj;
                try
                {
                    obj = Activator.CreateInstance(serializableType);
                }
                catch
                {
                    obj = FormatterServices.GetUninitializedObject(serializableType);
                }
                yield return new[] { serializableType, obj };
            }
        }

        [Theory]
        [MemberData(nameof(GetAllSerializableObjectsInAssembly))]
        public static void SerializeDeserialize_Succeeds(Type t, object obj)
        {
            // None of these objects are truly functional, so we don't need to verify equality
            // or the like.  We simply want to make sure that we're able to serialize and
            // deserialize without exceptions being thrown.
            Assert.IsType(t, BinaryFormatterHelpers.Clone(obj));
        }
    }
}
