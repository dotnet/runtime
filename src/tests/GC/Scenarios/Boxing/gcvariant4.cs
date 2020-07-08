// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/**************************************************************
/*This tests' porpuse is to test GC with local Object objects
/*This tests covered asigning all basic types to Objects
/*and converting Objects to basic types. Delete the reference
/*of Objects from Object arrays and make local objects of Object
/*in MakeLeak() see if GC can work fine
/**************************************************************/

namespace DefaultNamespace {
    using System;

    internal class GCVariant4
    {
        internal Object [] G_Vart1;
        internal Object [] G_Vart3;

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
                        iObj = 400;
                break;
            }

            Console.Write("iRep= ");
            Console.Write(iRep);
            Console.Write(" ; iObj= ");
            Console.WriteLine(iObj);

            GCVariant4 Mv_Obj = new GCVariant4();
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
            G_Vart3 = new Object[iObj];

            for(int i= 0; i< iRep; i++)
            {
                G_Vart1 = new Object[iObj];
                for(int j=0; j< iObj; j++)
                {
                    switch(j%8)
                    {
                        case 0:
                            G_Vart1[j] = (j);
                            G_Vart3[j] = (int)(G_Vart1[j]);

                        break;
                        case 1:
                            G_Vart1[j] = (true);
                            G_Vart3[j] = (bool)(G_Vart1[j]);

                        break;
                        case 2:
                            G_Vart1[j] = ((float)j/3);
                            G_Vart3[j] = (float)(G_Vart1[j]);
                        break;
                        case 3:
                            G_Vart1[j] = ((byte)j);
                            G_Vart3[j] = (byte)(G_Vart1[j]);
                        break;
                        case 4:
                            G_Vart1[j] = ((short)j);
                            G_Vart3[j] = (short)(G_Vart1[j]);
                        break;
                        case 5:
                            G_Vart1[j] = ((long)j);
                            G_Vart3[j] = (long)(G_Vart1[j]);
                        break;
                        case 6:
                            G_Vart1[j] = ((double)j/0.33);
                            G_Vart3[j] = (double)(G_Vart1[j]);
                        break;
                        case 7:
                            G_Vart1[j] = ((char)j);
                            G_Vart3[j] = (char)(G_Vart1[j]);
                        break;

                    }
                    MakeLeak(j);
                }
                GC.Collect();
                
            }

            return true;
        }


        public void MakeLeak(int value)
        {
            int iTmp = value;
            bool bTmp = true;
            long lTmp = (long)value;
            double dTmp = (double)value*0.0035;
            byte btTmp = (byte)value;
            short sTmp = (short)value;
            char cTmp = (char)value;
            float fTmp = (float)value/99;
            Object V1 = (iTmp);
            Object V2 = (bTmp);
            Object V3 = (lTmp);
            Object V4 = (dTmp);
            Object V5 = (btTmp);
            Object V6 = (sTmp);
            Object V7 = (cTmp);
            Object V8 = (fTmp);
        }

    }
}
