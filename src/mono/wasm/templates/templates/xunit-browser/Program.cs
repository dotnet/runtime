using System;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

try
{
    await ThreadlessXunitTestRunner.RunAsync(new[] { typeof(Program).Assembly });
    return 0;
}
catch (Exception e)
{
    Console.WriteLine(e.ToString());
    return 1;
}

public class TestClass
{
    [Fact]
    public static void TestMethod()
    {
        Assert.Equal(1, 1);
    }
}