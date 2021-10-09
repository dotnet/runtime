// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/**************************************************************
/*This tests covered asigning all basic types to Variants
/*and converting Variants to basic types. Delete the reference
/*of Varaints from Variant arrays to see if GC can work fine
/*with them. Because this test using Variant to save Variant
/*the Variant Object's life time is longer than theirs in GCVariant2
/**************************************************************/

namespace DefaultNamespace {
    using System;

    internal class GCVariant3
    {

// disabling unused variable warning
#pragma warning disable 0414
        internal Object [] G_Vart1;
        internal Object [] G_Vart2;
        internal Object [] G_Vart3;
        internal Object [] G_Vart4;
        internal Object [] G_Vart5;
        internal Object [] G_Vart6;
#pragma warning restore 0414

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
            Console.WriteLine(iObj );
            GCVariant3 Mv_Obj = new GCVariant3();
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

            G_Vart2 = new Object[iObj];
            G_Vart3 = new Object[iObj];
            G_Vart4 = new Object[iObj];
            G_Vart5 = new Object[iObj];

            int iTmp;
            bool bTmp;
            long lTmp;
            double dTmp;
            byte btTmp;
            short sTmp;
            char cTmp;
            float fTmp;
            for(int i= 0; i< iRep; i++)
            {
                G_Vart1 = new Object[iObj];
                G_Vart6 = new Object[iObj];
                for(int j=0; j< iObj; j++)
                {
                    switch(j%8)
                    {
                        case 0:
                            G_Vart1[j] = (j);
                            G_Vart2[j] = ((char)j);
                            iTmp = (int)G_Vart1[j];
                            G_Vart3[j] = (iTmp);

                        break;
                        case 1:
                            G_Vart1[j] = (true);
                            G_Vart2[j]= ((double)j/0.33);
                            bTmp = (bool)G_Vart1[j] ;
                            G_Vart3[j] = (bTmp);

                        break;
                        case 2:
                            G_Vart1[j] = ((float)j/3);
                            G_Vart2[j] = ((long)j);
                            fTmp = (float)G_Vart1[j] ;
                            G_Vart3[j] = (fTmp);
                        break;
                        case 3:
                            G_Vart1[j] = ((byte)j);
                            G_Vart2[j] = ((short)j);
                            btTmp = (byte)G_Vart1[j] ;
                            G_Vart3[j] = (btTmp);
                        break;
                        case 4:
                            G_Vart1[j] = ((short)j);
                            char[] carr= new char[1];
                            carr[0] = (char)j;
                            G_Vart2[j] = new string(carr);
                            sTmp = (short)G_Vart1[j]  ;
                            G_Vart3[j] = (sTmp);
                        break;
                        case 5:
                            G_Vart1[j] = ((long)j);
                            G_Vart2[j] = ((double)j/0.33);
                            lTmp = (long)G_Vart1[j]  ;
                            G_Vart3[j] = (lTmp);
                        break;
                        case 6:
                            G_Vart1[j] = ((double)j/0.33);
                            G_Vart2[j] = ((char)j);
                            dTmp = (double)G_Vart1[j] ;
                            G_Vart3[j] = (dTmp);
                        break;
                        case 7:
                            G_Vart1[j] = ((char)j);
                            G_Vart2[j] = ((float)j/3);
                            cTmp = (char)G_Vart1[j]  ;
                            G_Vart3[j] = (cTmp);
                        break;

                    }

                }
                GC.Collect();
            }
            return true;
        }

    }
}
