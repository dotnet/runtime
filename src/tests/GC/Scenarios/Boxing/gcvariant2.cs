// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/**************************************************************
/*This tests covered asigning all basic types to Objects
/*Delete the reference of Varaints from Object arrays/Object
/*to see if GC can work fine with them. The most of Object Object's
/*lifetime is shorter than theirs in GCObject3.
/**************************************************************/

namespace DefaultNamespace {
    using System;

    internal class GCVariant2
    {
        internal Object [] G_Vart;
        public static int Main(String [] Args)
        {
            int iRep = 0;
            int iObj = 0;
            Console.WriteLine("Test should return with ExitCode 100 ...");

            switch( Args.Length )
            {
                case 1:
                    Int32.TryParse( Args[0], out iRep);
                goto default;
                case 2:
                    if (!Int32.TryParse( Args[0], out iRep))
                    {
                        goto default;
                    }
                    Int32.TryParse( Args[1], out iObj);
                goto default;
                default:
                    if (iRep == 0)
                        iRep = 10;
                    if (iObj == 0)
                        iObj = 40000;
                break;
            }

            Console.Write("iRep= ");
            Console.Write(iRep);
            Console.Write(" ; iObj= ");
            Console.WriteLine(iObj);

            GCVariant2 Mv_Obj = new GCVariant2();
            if(Mv_Obj.runTest(iRep, iObj ))
            {
                Console.WriteLine("Test Passed");
                return 100;
            }

            Console.WriteLine("Test Failed");
            return 1;
        }


        public bool runTest(int iRep, int iObj)
        {
            Object TmpV;

            for(int i= 0; i< iRep; i++)
            {
                G_Vart = new Object[iObj];
                for(int j=0; j< iObj; j++)
                {
                    switch(j%8)
                    {
                        case 0:
                            G_Vart[j] = (j);
                            TmpV = (j);
                        break;
                        case 1:
                            G_Vart[j] = (true);
                            TmpV = (false);
                        break;
                        case 2:
                            G_Vart[j] = ((float)j/3);
                            TmpV = ((float)j/3);
                        break;
                        case 3:
                            G_Vart[j] = ((byte)j);
                            TmpV = ((byte)j);
                        break;
                        case 4:
                            G_Vart[j] = ((short)j);
                            TmpV = ((short)j);
                        break;
                        case 5:
                            G_Vart[j] = ((long)j);
                            TmpV = ((long)j);
                        break;
                        case 6:
                            G_Vart[j] = ((double)j/0.33);
                            TmpV = ((double)j/0.33);
                        break;
                        case 7:
                            G_Vart[j] = ((char)j);
                            TmpV = ((char)j);
                        break;

                    }
                }
                GC.Collect();
            }
            return true;
        }
    }
}
