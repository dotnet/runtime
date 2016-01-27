// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Linq;

using Xunit;

public static unsafe class AttributeTests
{
    [ActiveIssue("https://github.com/dotnet/coreclr/issues/2037", PlatformID.AnyUnix)]
    [Fact]
    [StringValue("\uDFFF")]
    public static void StringArgument_InvalidCodeUnits_FallbackUsed()
    {
        MethodInfo thisMethod = typeof(AttributeTests).GetTypeInfo().GetDeclaredMethod("StringArgument_InvalidCodeUnits_FallbackUsed");
        Assert.NotNull(thisMethod);

        CustomAttributeData cad = thisMethod.CustomAttributes.Where(ca => ca.AttributeType == typeof(StringValueAttribute)).FirstOrDefault();
        Assert.NotNull(cad);

        string stringArg = cad.ConstructorArguments[0].Value as string;
        Assert.NotNull(stringArg);

        Assert.Equal("\uFFFD\uFFFD", stringArg);
    }

    [AttributeUsage(AttributeTargets.Method)]
    private sealed class StringValueAttribute : Attribute
    {
        public string Text;
        public StringValueAttribute(string text) { Text = text; }
    }
}
