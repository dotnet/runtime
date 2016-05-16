// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Reflection;
using System.Collections.Generic;

#pragma warning disable 0414

namespace System.Reflection.Tests
{
    public class PropertyInfoGetSetMethodTests
    {

        [Theory]
        [InlineData(typeof(ReferenceTypeHelper), "PropertyGetterSetter", true, true)]
        [InlineData(typeof(ReferenceTypeHelper), "PropertyGetter", true, false)]
        [InlineData(typeof(ReferenceTypeHelper), "PropertySetter", false, true)]
        [InlineData(typeof(ReferenceTypeHelper), "Item", true, true)]
        [InlineData(typeof(ValueTypeHelper), "PropertyGetterSetter", true, true)]
        [InlineData(typeof(ValueTypeHelper), "PropertyGetter", true, false)]
        [InlineData(typeof(ValueTypeHelper), "PropertySetter", false, true)]
        [InlineData(typeof(ValueTypeHelper), "Item", true, false)]
        [InlineData(typeof(InterfaceHelper), "PropertyGetterSetter", true, true)]
        [InlineData(typeof(InterfaceHelper), "PropertyGetter", true, false)]
        [InlineData(typeof(InterfaceHelper), "PropertySetter", false, true)]
        [InlineData(typeof(InterfaceHelper), "Item", false, true)]
        public void GetSetMethod(Type type, string str, bool getter, bool setter)
        {
            VerifyGetSetMethod(type, str, getter, setter);
        }


        //Gets PropertyInfo object from a Type
        public static PropertyInfo getProperty(Type t, string property)
        {
            TypeInfo ti = t.GetTypeInfo();
            IEnumerator<PropertyInfo> allproperties = ti.DeclaredProperties.GetEnumerator();
            PropertyInfo pi = null;

            while (allproperties.MoveNext())
            {
                if (allproperties.Current.Name.Equals(property))
                {
                    //found property
                    pi = allproperties.Current;
                    break;
                }
            }
            return pi;
        }

        public static void VerifyGetSetMethod(Type type, String propertyName, Boolean getter, Boolean setter)
        {
            PropertyInfo pi = getProperty(type, propertyName);

            Assert.NotNull(pi);

            if (getter)
            {
                Assert.NotNull(pi.GetMethod);
            }

            if (setter)
            {
                Assert.NotNull(pi.SetMethod);
            }
        }
    }

    //Reflection Metadata  


    public class ReferenceTypeHelper
    {
        public int PropertyGetterSetter { get { return 1; } set { } }
        public String PropertyGetter { get { return "Test"; } }
        public Char PropertySetter { set { } }
        public int this[int index] { get { return 2; } set { } }

        public int PropertyPrivateGetterSetter { private get { return 1; } set { } }
        public int PropertyProtectedGetterSetter { protected get { return 1; } set { } }
        public int PropertyInternalGetterSetter { internal get { return 1; } set { } }

        public int PropertyGetterPrivateSetter { get { return 1; } private set { } }
        public int PropertyGetterProtectedSetter { get { return 1; } protected set { } }
        public int PropertyGetterInternalSetter { get { return 1; } internal set { } }
    }

    public struct ValueTypeHelper
    {
        public int PropertyGetterSetter { get { return 1; } set { } }
        public String PropertyGetter { get { return "Test"; } }
        public Char PropertySetter { set { } }
        public String this[int index] { get { return "name"; } }
    }

    public interface InterfaceHelper
    {
        int PropertyGetterSetter { get; set; }
        String PropertyGetter { get; }
        Char PropertySetter { set; }
        Char this[int index] { set; }
    }
}
