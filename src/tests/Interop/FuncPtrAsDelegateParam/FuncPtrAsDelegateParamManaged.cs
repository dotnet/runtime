using System;
using System.Text;
using System.Security;
using System.Runtime.InteropServices;
using TestLibrary;
using Xunit;

//Value Pass N-->M	M--->N
//Cdecl		 -1		 678
[ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
public class Test_FuncPtrAsDelegateParamManaged
{
    //TestMethod1
    [DllImport("FuncPtrAsDelegateParamNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool DoCallBack_Cdecl(Cdeclcaller caller);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int CdeFunc();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int Cdeclcaller(CdeFunc func);

    [SecuritySafeCritical]
    private static int TestMethod_ReversePInvoke_Cdecl(CdeFunc func)
    {
        if (-1 != func())
        {
            TestFramework.LogError("01","The Return Value(TestMethod_ReversePInvoke_Cdecl) is wrong");
        }
        return 678;
    }

    [SecuritySafeCritical]
    [Fact]
    public static int TestEntryPoint()
    {
        bool breturn = true;

        TestFramework.BeginScenario("ReversePInvoke Cdecl");
        if (!DoCallBack_Cdecl(new Cdeclcaller(TestMethod_ReversePInvoke_Cdecl)))
        {
            breturn = false;
            TestFramework.LogError("04","The Return value(DoCallBack_Cdecl) is wrong");
        }

        return breturn ? 100: 101;
    }
}
