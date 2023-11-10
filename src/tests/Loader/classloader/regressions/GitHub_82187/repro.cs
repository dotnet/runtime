using System;
using Xunit;

/* Regression test for https://github.com/dotnet/runtime/issues/78638
 * and https://github.com/dotnet/runtime/issues/82187 ensure AOT
 * cross-compiler and AOT runtime use the same name hashing for names
 * that include UTF-8 continuation bytes.
 */

[MySpecial(typeof(MeineTüre))]
public class Program
{
    [Fact]
    public static int TestEntryPoint()
    {
        var attr = (MySpecialAttribute)Attribute.GetCustomAttribute(typeof (Program), typeof(MySpecialAttribute), false);
        if (attr == null)
            return 101;
        if (attr.Type == null)
            return 102;
        if (attr.Type.FullName != "MeineTüre")
            return 103;
        return 100;
    }
}

public class MySpecialAttribute : Attribute
{
    public Type Type {get; private set; }
    public MySpecialAttribute(Type t) { Type = t; }
}

public class MeineTüre {}
