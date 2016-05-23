// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;

namespace System.Tests
{
    public static class Helpers
    {
        private static Type s_nonRuntimeType;

        public static Type NonRuntimeType()
        {
            if (s_nonRuntimeType == null)
            {
                AssemblyName assemblyName = new AssemblyName("AssemblyName");
                AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
                ModuleBuilder mboduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);

                TypeBuilder typeBuilder = mboduleBuilder.DefineType("TestType", TypeAttributes.Public);

                GenericTypeParameterBuilder[] typeParams = typeBuilder.DefineGenericParameters("T");
                s_nonRuntimeType = typeParams[0].UnderlyingSystemType;
            }

            return s_nonRuntimeType;
        }

        public static void PerformActionWithCulture(CultureInfo culture, Action test)
        {
            CultureInfo originalCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = culture;
                test();
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }
    }
}
