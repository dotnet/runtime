// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderEquals
    {
        public static IEnumerable<object[]> Equals_TestData()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            MethodBuilder method1 = type.DefineMethod("TestMethod1", MethodAttributes.Public | MethodAttributes.Static, typeof(int), new Type[0]);
            MethodBuilder method2 = type.DefineMethod("TestMethod2", MethodAttributes.Public | MethodAttributes.Static, typeof(int), new Type[0]);
            MethodBuilder method3 = type.DefineMethod("TestMethod1", MethodAttributes.Public, typeof(int), new Type[0]);

            MethodBuilder method4 = type.DefineMethod("TestMethod3", MethodAttributes.Public | MethodAttributes.Static);
            method4.SetSignature(typeof(int), new Type[] { typeof(string) }, null, null, null, null);

            MethodBuilder method5 = type.DefineMethod("TestMethod3", MethodAttributes.Public | MethodAttributes.Static);
            method5.SetSignature(typeof(int), new Type[] { typeof(object) }, null, null, null, null);

            MethodBuilder method6 = type.DefineMethod("TestMethod3", MethodAttributes.Public | MethodAttributes.Static);
            method6.SetSignature(typeof(int), new Type[] { typeof(string) }, null, null, null, null);

            MethodBuilder method7 = type.DefineMethod("TestMethod4", MethodAttributes.Public | MethodAttributes.Static);
            method7.SetSignature(typeof(int), new Type[] { typeof(string) }, null, null, null, null);
            method7.DefineGenericParameters(new string[] { "T", "U", "V" });

            MethodBuilder method8 = type.DefineMethod("TestMethod4", MethodAttributes.Public | MethodAttributes.Static);
            method8.SetSignature(typeof(int), new Type[] { typeof(string) }, null, null, null, null);
            method8.DefineGenericParameters(new string[] { "T", "U" });

            MethodBuilder method9 = type.DefineMethod("TestMethod5", MethodAttributes.Public);
            MethodBuilder method10 = type.DefineMethod("TestMethod5", MethodAttributes.Public);

            MethodBuilder method11 = type.DefineMethod("TestMethod6", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot);
            MethodBuilder method12 = type.DefineMethod("TestMethod6", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot);
            
            MethodBuilder method13 = type.DefineMethod("TestMethod7", MethodAttributes.Public, typeof(void), new Type[] { typeof(int), typeof(string) });
            MethodBuilder method14 = type.DefineMethod("TestMethod7", MethodAttributes.Public, typeof(void), new Type[] { typeof(int), typeof(string) });

            MethodBuilder method15 = type.DefineMethod("TestMethod8", MethodAttributes.Public, typeof(void), new Type[] { typeof(int), typeof(string) });
            MethodBuilder method16 = type.DefineMethod("TestMethod8", MethodAttributes.Public, typeof(int), new Type[] { typeof(int), typeof(string) });

            MethodBuilder method17 = type.DefineMethod("TestMethod9", MethodAttributes.Public, typeof(void), new Type[] { typeof(int), typeof(float) });
            MethodBuilder method18 = type.DefineMethod("TestMethod9", MethodAttributes.Public, typeof(void), new Type[] { typeof(string), typeof(float) });

            MethodBuilder method19 = type.DefineMethod("TestMethod10", MethodAttributes.Public, typeof(void), new Type[] { typeof(int), typeof(string) });
            MethodBuilder method20 = type.DefineMethod("TestMethod10", MethodAttributes.Public, typeof(void), new Type[] { typeof(string), typeof(int) });

            yield return new object[] { method1, type, false }; // Random object
            yield return new object[] { method1, null, false }; // Null

            yield return new object[] { method1, method2, false }; // Different name
            yield return new object[] { method1, method3, false }; // Different attributes
            yield return new object[] { method4, method5, false }; // Different signature
            yield return new object[] { method4, method6, true }; // Same signature
            yield return new object[] { method7, method8, false }; // Different generic parameters

            yield return new object[] { method9, method9, true }; // Same instance
            yield return new object[] { method9, method10, true }; // Same name
            yield return new object[] { method11, method12, true }; // Same name, same attributes
            yield return new object[] { method13, method14, true }; // Same name, same attributes, same parameters

            yield return new object[] { method15, method16, false }; // Different return type
            yield return new object[] { method17, method18, false }; // Different parameters
            yield return new object[] { method19, method20, false }; // Different parameter ordering

        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals(MethodBuilder builder, object obj, bool expected)
        {
            Assert.Equal(expected, builder.Equals(obj));
        }
    }
}
