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
            if (member.Name.StartsWith("To"))
            {
                yield return new MyAttribute();
            }
            // Keep existing attributes as well.
            foreach (var attr in declaredAttributes) yield return attr;
        }
    }

    class Program
    {
        [Fact]
        static void Main(string[] args)
        {
            MyCustomReflectionContext mc = new();
            Type t = typeof(String);

            // A representation of the type in the default reflection context.
            TypeInfo ti = t.GetTypeInfo();

            // A representation of the type in the customized reflection context.
            TypeInfo myTI = mc.MapType(ti);

            // Display all the members of the type and their attributes.
            foreach (MemberInfo m in myTI.DeclaredMembers)
            {
                Console.WriteLine(m.Name + ":");
                foreach (Attribute cd in m.GetCustomAttributes())
                {
                    Console.WriteLine(cd.GetType());
                }
            }

            Console.WriteLine();

            // The "ToString" member as represented in the default reflection context.
            MemberInfo mi1 = ti.GetDeclaredMethods("ToString").FirstOrDefault();

            Attribute[] defaultAttributes = mi1.GetCustomAttributes().Cast<Attribute>().ToArray();
            Assert.DoesNotContain(defaultAttributes, attribute => attribute is MyAttribute);

            // All the attributes of "ToString" in the default reflection context.
            Console.WriteLine("'ToString' Attributes in Default Reflection Context:");
            foreach (Attribute cd in defaultAttributes)
            {
                Console.WriteLine(cd.GetType());
            }

            Console.WriteLine();

            // The same member in the custom reflection context.
            mi1 = myTI.GetDeclaredMethods("ToString").FirstOrDefault();

            Attribute[] customAttributes = mi1.GetCustomAttributes().Cast<Attribute>().ToArray();
            Assert.Contains(customAttributes, attribute => attribute is MyAttribute);

            // All its attributes, for comparison. MyAttribute is now included.
            Console.WriteLine("'ToString' Attributes in Custom Reflection Context:");
            foreach (Attribute cd in customAttributes)
            {
                Console.WriteLine(cd.GetType());
            }
        }
    }
    #endregion Snippet1
}
