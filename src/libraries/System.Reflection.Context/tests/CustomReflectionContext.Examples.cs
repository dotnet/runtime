// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Context;
using Xunit;

namespace System.Reflection.Context.Examples
{
    #region Snippet1
    // A blank example attribute.
    class MyAttribute : Attribute
    {
    }

    // Reflection context with custom rules.
    class MyCustomReflectionContext : CustomReflectionContext
    {
        // Called whenever the reflection context checks for custom attributes.
        protected override IEnumerable<object> GetCustomAttributes(MemberInfo member, IEnumerable<object> declaredAttributes)
        {
            // Add example attribute to "To*" members.
            if (member.Name.StartsWith("To", StringComparison.Ordinal))
            {
                yield return new MyAttribute();
            }
            // Keep existing attributes as well.
            foreach (object attr in declaredAttributes)
            {
                yield return attr;
            }
        }
    }
    #endregion Snippet1

    public class CustomReflectionContextExamples
    {
        [Fact]
        public static void AddCustomAttribute()
        {
            #region Snippet2
            MyCustomReflectionContext mc = new();
            Type t = typeof(string);

            // A representation of the type in the default reflection context.
            TypeInfo ti = t.GetTypeInfo();

            // A representation of the type in the customized reflection context.
            TypeInfo myTI = mc.MapType(ti);

            // Display affected members of the type ("To*") and their attributes.
            foreach (MemberInfo m in myTI.DeclaredMembers.Where(static m => m.Name.StartsWith("To", StringComparison.Ordinal)))
            {
                Console.WriteLine($"{m.Name}:");
                foreach (Attribute cd in m.GetCustomAttributes())
                {
                    Console.WriteLine(cd.GetType());
                }
            }

            Console.WriteLine();

            // The "ToString" member as represented in the default reflection context.
            MemberInfo mi1 = ti.GetDeclaredMethod(nameof(object.ToString));

            Attribute[] defaultAttributes = mi1.GetCustomAttributes().ToArray();

            // All the attributes of "ToString" in the default reflection context.
            Console.WriteLine("'ToString' Attributes in Default Reflection Context:");
            foreach (Attribute cd in defaultAttributes)
            {
                Console.WriteLine(cd.GetType());
            }

            Console.WriteLine();

            // The same member in the custom reflection context.
            mi1 = myTI.GetDeclaredMethod(nameof(object.ToString));

            Attribute[] customAttributes = mi1.GetCustomAttributes().ToArray();

            // All its attributes, for comparison. MyAttribute is now included.
            Console.WriteLine("'ToString' Attributes in Custom Reflection Context:");
            foreach (Attribute cd in customAttributes)
            {
                Console.WriteLine(cd.GetType());
            }
            #endregion Snippet2

            Assert.DoesNotContain(defaultAttributes, attribute => attribute is MyAttribute);
            Assert.Contains(customAttributes, attribute => attribute is MyAttribute);
        }
    }
}
