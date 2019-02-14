// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;

namespace System.ComponentModel.Composition.Registration.Tests
{
    public static class InternalCalls
    {
        public static void BuildAttributes(this ExportBuilder builder, Type type, ref List<Attribute> attributes)
        {
            builder.GetType()
                .GetMethod(nameof(BuildAttributes), BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(builder, new object[] { type, attributes });
        }

        public static void BuildAttributes(this ImportBuilder builder, Type type, ref List<Attribute> attributes)
        {
            builder.GetType()
                .GetMethod(nameof(BuildAttributes), BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(builder, new object[] { type, attributes });
        }

        public static PartBuilder PartBuilder(Predicate<Type> selectType)
        {
            return (PartBuilder)typeof(PartBuilder)
                .GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(Predicate<Type>) }, null)
                .Invoke(new object[] { selectType });
        }

        public static PartBuilder<T> PartBuilder<T>(Predicate<Type> selectType)
        {
            return (PartBuilder<T>)typeof(PartBuilder<T>)
                .GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(Predicate<Type>) }, null)
                .Invoke(new object[] { selectType });
        }

        public static IEnumerable<Attribute> BuildTypeAttributes(this PartBuilder builder, Type type)
        {
            return (IEnumerable<Attribute>)builder.GetType()
                .GetMethod(nameof(BuildTypeAttributes), BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(builder, new object[] { type });
        }

        public static bool BuildConstructorAttributes(this PartBuilder builder, Type type, ref List<Tuple<object, List<Attribute>>> configuredMembers)
        {
            return (bool)builder.GetType()
                .GetMethod(nameof(BuildConstructorAttributes), BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(builder, new object[] { type, configuredMembers });
        }

        public static void BuildPropertyAttributes(this PartBuilder builder, Type type, ref List<Tuple<object, List<Attribute>>> configuredMembers)
        {
            builder.GetType()
                .GetMethod(nameof(BuildPropertyAttributes), BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(builder, new object[] { type, configuredMembers });
        }

        public static void PartBuilder_BuildDefaultConstructorAttributes(Type type, ref List<Tuple<object, List<Attribute>>> configuredMembers)
        {
            typeof(PartBuilder)
                .GetMethod("BuildDefaultConstructorAttributes", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { type, configuredMembers });
        }
    }
}
