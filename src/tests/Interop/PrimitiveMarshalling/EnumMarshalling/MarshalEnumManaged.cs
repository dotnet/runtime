using System;
using System.Collections.Generic;
using System.Text;
using TestLibrary;
using System.Runtime.InteropServices;
using Xunit;

namespace EnumRoundtrip
{
    public class MarshalEnumManaged
    {
        [Flags]
        public enum DialogResult
        {
            None = 0x01,
            OK = 0x02,
            Cancel = 0x03
        }

        #region pinvoke declarations
        //cdecl

        [DllImport("MarshalEnumNative", CallingConvention = CallingConvention.Cdecl)]
        public static extern int CdeclEnum(DialogResult r, ref bool result);

        [DllImport("MarshalEnumNative", EntryPoint = "GetFptr")]
        [return: MarshalAs(UnmanagedType.FunctionPtr)]
        public static extern CdeclEnumDelegate GetFptrCdeclEnum();


        #endregion

        #region delegate pinvoke
        //cdecl
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int CdeclEnumDelegate(DialogResult r, ref bool result);

        #endregion
        [System.Security.SecuritySafeCritical]
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
        public static int TestEntryPoint()
        {
            bool result = true;
            int r = 0;

            TestFramework.BeginScenario("\n\nTest #1 (Roundtrip of enum).");

            TestFramework.LogInformation("Direct p/invoke cdecl calling convention");
            //direct pinvoke - cdecl
            r = CdeclEnum(DialogResult.None | DialogResult.OK, ref result);
            if ((!result) || (r != 3))
            {
                TestFramework.LogError("02", "Main : Returned value of enum doesn't match with the value passed in to pinvoke call");
                return 101;
            }

            TestFramework.LogInformation("Delegate p/invoke cdecl calling convention");
            //delegate pincoke - cdecl
            CdeclEnumDelegate cdecdel = GetFptrCdeclEnum();
            r = cdecdel(DialogResult.None | DialogResult.OK, ref result);
            if ((!result) || (r != 3))
            {
                TestFramework.LogError("04", "Main : Returned value of enum doesn't match with the value passed in to pinvoke call");
                return 101;
            }

            return 100;
        }
    }
}
