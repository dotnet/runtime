// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace DefaultNamespace {
using System;

/*************************************************************/
/* test: MulDimJagAry.cs
/* Purpose: Test GC with Multiple dimentions array
/* Coverage: int[][], Object[][], Object[][][], Variant[][][],
/*           take Mul_Dimention array as function argument.
/*************************************************************/


    class MulDimJagAry
    {
        public static int Main(String []args)
        {
            int iDim1 = 100;
            int iDim2 = 100;
            int iRep = 30;
            Console.WriteLine("Test should return with ExitCode 100 ...");

            MulDimJagAry mv_Obj = new MulDimJagAry();

            int [][] iJag;
            for(int j=0; j<iRep; j++ )
            {
                iJag = new int[iDim1][];
                for( int i=0; i< iDim2; i++ )
                {
                    iJag[i] = new int[i];
                    if( i>= 1 )
                    {
                        iJag[i][0] = 0;
                        iJag[i][i-1] = i;
                    }
                }
                //if( GC.GetTotalMemory(false) >= 1024*1024*6 )
                //{
                //    Console.WriteLine( "HeapSize before GC: "+ GC.GetTotalMemory(false) );
                //    GC.Collect();
                //    Console.WriteLine( "HeapSize after GC: "+ GC.GetTotalMemory(false) );
                //}
            }

            Object[][] oJag;
            for(int j=0; j<iRep; j++ )
            {
                oJag = new Object[iDim1][];
                for( int i=0; i< iDim1; i++ )
                {
                    oJag[i] = new Object[i];
                    if( i>= 1 )
                    {
                        oJag[i][0] = (0);
                        oJag[i][i-1] = new long[i];
                    }

                }
                //if( GC.GetTotalMemory(false) >= 1024*1024*6 )
                //{
                //    Console.WriteLine( "HeapSize before GC: "+ GC.GetTotalMemory(false) );
                //    GC.Collect();
                //    Console.WriteLine( "HeapSize after GC: "+ GC.GetTotalMemory(false) );
                //}
            }

            Object[][][] oJag3 = new Object[iDim1][][];
            oJag3[3] = new Object[iDim2][];
            oJag3[4] = new Object[iDim2][];
            for (int i = 0; i < iDim2; i ++)
            {
                oJag3[4][i] = new Object[iDim1];
            }

            for(int j=0; j<iRep; j++ )
            {
                oJag3 = new Object[iDim1][][];
                for( int i=0; i< iDim1; i++ )
                {
                    oJag3[i] = new Object[iDim2][];
                    for(int k=0; k<iDim2; k++)
                    {
                        oJag3[i][k] = new Object[k];
                        for(int l = 0; l< k; l++ )
                        {
                            if( l>= 1 )
                            {
                                oJag3[i][k][0] = (0);
                                oJag3[i][k][l-1] = new long[l];
                            }
                        }
                    }

                }
                //if( GC.GetTotalMemory(false) >= 1024*1024*6 )
                //{
                //    Console.WriteLine( "HeapSize before GC: "+ GC.GetTotalMemory(false) );
                //    GC.Collect();
                //    Console.WriteLine( "HeapSize after GC: "+ GC.GetTotalMemory(false) );
                //}
            }

            for(int j=0; j<iRep; j++ )
            {
                oJag3 = new Object[iDim1][][];
                mv_Obj.SetThreeDimJagAry( oJag3, iDim1, iDim2 );
                //if( GC.GetTotalMemory(false) >= 1024*1024*6 )
                //{
                //    Console.WriteLine( "HeapSize before GC: "+ GC.GetTotalMemory(false) );
                //    GC.Collect();
                //    Console.WriteLine( "HeapSize after GC: "+ GC.GetTotalMemory(false) );
                //}
            }


            Object[][][] vJag;
            for(int j=0; j<iRep; j++ )
            {
                vJag = new Object[iDim1][][];
                mv_Obj.SetThreeDimJagVarAry( vJag, iDim1, iDim2 );
                //if( GC.GetTotalMemory(false) >= 1024*1024*6 )
                //{
                //    Console.WriteLine( "HeapSize before GC: "+ GC.GetTotalMemory(false) );
                //    GC.Collect();
                //    Console.WriteLine( "HeapSize after GC: "+ GC.GetTotalMemory(false) );
                //}
            }


            return 100;

        }

        public void SetThreeDimJagAry( Object [][][] oJag, int iDim1, int iDim2 )
        {
            for( int i=0; i< iDim1; i++ )
            {
                oJag[i] = new Object[iDim2][];
                for(int k=0; k<iDim2; k++)
                {
                    oJag[i][k] = new Object[k];
                    for(int l = 0; l< k; l++ )
                    {
                        if( l>= 1 )
                        {
                            oJag[i][k][0] = (0);
                            oJag[i][k][l-1] = new float[l];

                        }
                    }

                }
            }
        }

        public void SetThreeDimJagVarAry( Object [][][] vJag, int iDim1, int iDim2 )
        {
            for( int i=0; i< iDim1; i++ )
            {
                vJag[i] = new Object[iDim2][];
                for(int k=0; k<iDim2; k++)
                {
                    vJag[i][k] = new Object[k];
                    for(int l = 0; l< k; l++ )
                    {
                        if( l>= 1 )
                        {
                            vJag[i][k][0] = (0);
                            vJag[i][k][l-1] = ( new double[l] );
                        }
                    }

                }
            }
        }
    }

}
