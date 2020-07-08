// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace DefaultNamespace
{
    //@BEGINRENAME; Verify this renames
    //@ENDRENAME; Verify this renames
    using System;

    class X
    {
        public static int Main(String[] argv)
        {
            Object[,] obj = new Object[1, 1];
            //			IL_0000:  ldc.i4.1
            //    		IL_0001:  ldc.i4.1
            // 	 		IL_0002:  newobj instance void class System.Object[,]::.ctor(int32,int32)
            // 			IL_0007:  stloc.0

            obj[0, 0] = new Object();
            //        	IL_0008:  ldloc.0
            //    		IL_0009:  ldc.i4.0
            //    		IL_000a:  ldc.i4.0
            //    		IL_000b:  newobj instance void System.Object::.ctor()
            //   	 	IL_0010:  call instance void class System.Object[,]::Set(int32,int32,class System.Object)

            //    		IL_0015:  ret 

            return 100;
        } // main

    } // X

}
