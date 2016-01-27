// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*
* Regression test for Dev11 243742 [Triton]
* precommands:
* set COMPLUS_ZAPREQUIRE=2
* set CORECLR_PREJITType=MDIL
* del /q nitype.signal
*
* Execute:
* %CORE_ROOT%\fxprun.exe App.exe
*
* Expected:
* In the DLL.
* 
* DerivedType.RunGenericMethod<System.Int32,System.String>(22)
* Call completed successfully.
* returns 100
*
* Failure indicated by:
* App.exe prints "In the DLL." and then hits an AV during the RunGenericMethod call made in the Main method.
*/

using System;

namespace BadOverride1
{
    public class DerivedType : Dll.ParameterizedBase<DerivedType>
    {
        public override void RunGenericMethod<T1>(T1 value)
        {
            Console.Write(
                "DerivedType.RunGenericMethod<{0}>({1})\r\n",
                typeof(T1),
                value
            );

            return;
        }

        public override void RunGenericMethod<T1, T2>(T1 value)
        {
            Console.Write(
                "DerivedType.RunGenericMethod<{0},{1}>({2})\r\n",
                typeof(T1),
                typeof(T2),
                value
            );

            return;
        }
    }

    static class App
    {
        static int Main()
        {
            Dll.Apis.RunDllCode();
            Console.Write("\r\n");
            var derivedType = new DerivedType();
            derivedType.RunGenericMethod<int, string>(22);
            Console.Write("Call completed successfully.\r\n");
            return 100;
        }
    }
}
