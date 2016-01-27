// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Tests
{
    public class GetCustomAttributes_Compat
    {
        [Fact]
        public void GetCustomAttributes_thisAsm()
        {
            IEnumerable<Attribute> attributes;

            Assembly assembly = typeof(GetCustomAttributes_Compat).GetTypeInfo().Assembly;

            attributes = CustomAttributeExtensions.GetCustomAttributes(assembly, typeof(MyAttribute));
            CheckReturnType(attributes);

            attributes = CustomAttributeExtensions.GetCustomAttributes(assembly.ManifestModule, typeof(MyAttribute));
            CheckReturnType(attributes);

            TypeInfo ti = typeof(GetCustomAttributes_Compat).GetTypeInfo();
            attributes = CustomAttributeExtensions.GetCustomAttributes(ti, typeof(MyAttribute));
            CheckReturnType(attributes);

            attributes = CustomAttributeExtensions.GetCustomAttributes(ti, typeof(MyAttribute), true);
            CheckReturnType(attributes);

            ParameterInfo p = ti.GetDeclaredMethod("CheckReturnType").GetParameters()[0];

            attributes = CustomAttributeExtensions.GetCustomAttributes(p, typeof(MyAttribute));
            CheckReturnType(attributes);

            attributes = CustomAttributeExtensions.GetCustomAttributes(p, typeof(MyAttribute), true);
            CheckReturnType(attributes);
        }

        public void CheckReturnType(IEnumerable<Attribute> attributes)
        {
            Type expectedType = typeof(MyAttribute[]);
            Type actualType = attributes.GetType();
            Assert.Equal(expectedType, actualType);
        }

        public class MyAttribute : Attribute
        {
        }
    }
}
