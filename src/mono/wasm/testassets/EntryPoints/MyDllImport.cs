using System.Runtime.InteropServices;
namespace ##NAMESPACE##;

public static class MyDllImports
{
    [DllImport("mylib")]
    public static extern int cpp_add(int a, int b);
}
