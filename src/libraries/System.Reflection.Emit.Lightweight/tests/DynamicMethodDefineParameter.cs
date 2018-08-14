// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class DynamicMethodDefineParameter
    {
        [Fact]
        public void DefineParameter_SetsParameterCorrectly()
        {
            Type[] mathArgs = { typeof(double), typeof(double) };

            var powerOf = new DynamicMethod("PowerOf",
                typeof(double),
                mathArgs,
                typeof(double).Module);

            ILGenerator il = powerOf.GetILGenerator(256);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, typeof(Math).GetMethod("Pow"));
            il.Emit(OpCodes.Ret);
            
            powerOf.DefineParameter(1, ParameterAttributes.In, "base");
            powerOf.DefineParameter(2, ParameterAttributes.Out, "exponent");
            
            object[] invokeArgs = { 2, 5 };
            object objRet = powerOf.Invoke(null, BindingFlags.ExactBinding, null, invokeArgs, new CultureInfo("en-us"));

            ParameterInfo[] parameters = powerOf.GetParameters();

            Assert.Equal(32.0, objRet);

            Assert.Equal("base", parameters[0].Name);
            Assert.Equal("exponent", parameters[1].Name);

            Assert.Equal(typeof(double), parameters[0].ParameterType);
            Assert.Equal(typeof(double), parameters[1].ParameterType);

            Assert.Equal(ParameterAttributes.In, parameters[0].Attributes);
            Assert.Equal(ParameterAttributes.Out, parameters[1].Attributes);
        }
    }
}
