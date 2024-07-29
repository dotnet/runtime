using System;
using System.Reflection;
using Xunit;

namespace test86865;

public class test86865
{
    [Fact]
    public static int TestEntryPoint()
    {

        // Regression test for https://github.com/dotnet/runtime/issues/86865
        // Verify that the RuntimeHelpers.GetSpanDataFrom method underlying RuntimeHelpers.CreateSpan<T>
        // works correctly with enums.

        ReadOnlySpan<MyEnum> myEnums = new[]
        {
            MyEnum.A,
            MyEnum.B,
            MyEnum.C,
            MyEnum.B,
            MyEnum.C,
        };

        if (string.Join(", ", myEnums.ToArray()) != "A, B, C, B, C")
            return 1;

        var types = new Type[] {
            typeof(RuntimeFieldHandle),
            typeof(RuntimeTypeHandle),
            typeof(int).MakeByRefType(),
        };
        var mi = typeof(System.Runtime.CompilerServices.RuntimeHelpers).GetMethod("GetSpanDataFrom", BindingFlags.Static | BindingFlags.NonPublic, types);
        if (mi == null)
            return 2;

        var pid = typeof(MyEnum).Assembly.GetType("<PrivateImplementationDetails>");
        if (pid == null)
            return 3;

        var fi = pid.GetField("0B77DC554B4A81403D62BE25FB5404020AD451151D4203D544BF60E3FEDBD8AE4", BindingFlags.Static | BindingFlags.NonPublic);
        if (fi == null)
        {
            Console.WriteLine("Could not find the expected array data in <PrivateImplementationDetails>. The available static non-public fields are:");
            foreach (var f in pid.GetFields(BindingFlags.Static | BindingFlags.NonPublic)) {
                Console.WriteLine($" - '{f}'");
            }
            return 4;
        }

        var parms = new object[] {
            fi.FieldHandle,
            typeof(MyEnum).TypeHandle,
            new int()
        };
        var result = mi.Invoke(null, parms);
        if (result == null)
            return 6;
        if ((int)parms[2] != myEnums.Length)
            return 7;

        return 100;
    }
}
                
enum MyEnum
{
    A,
    B,
    C
}
