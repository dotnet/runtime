// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace DefaultNamespace {
    using System;

    public class GC_Base1
    {
        internal RandomNode[] CvA_RandomNode;

        public static int Main( System.String [] Args )
        {

            int iRep = 0;
            int iObj = 0;

            Console.WriteLine("Test should return with ExitCode 100 ...");

            switch( Args.Length )
            {
               case 1:
                   if (!Int32.TryParse( Args[0], out iRep ))
                   {
                       iRep = 15;
                   }
               break;
               case 2:
                   if (!Int32.TryParse( Args[0], out iRep ))
                   {
                       iRep = 15;
                   }
                   if (!Int32.TryParse( Args[1], out iObj ))
                   {
                       iObj = 1000;
                   }
               break;
               default:
                   iRep = 15;
                   iObj = 1000;
               break;
            }

            GC_Base1 Mv_Base = new GC_Base1();

            if(Mv_Base.runTest(iRep, iObj ))
            {
                Console.WriteLine( "Test Passed" );
                return 100;
            }
            else
            {
                Console.WriteLine( "Test Failed" );
                return 1;
            }
        }


        public bool runTest( int Rep, int Obj )
        {

            for( int i = 0; i < Rep; i++ )
            {
                CvA_RandomNode = new RandomNode[ Obj ];

                for( int j = 0; j < Obj; j++ )
                {
                    CvA_RandomNode[ j ] = new RandomNode( j, j );
                    if( j == 0 )
                    {
                        CvA_RandomNode[ j ].setBigSize( 0 );
                    }
                }

                CvA_RandomNode = null;

                GC.Collect();
            }

            return true;

        }

    }

    public class BaseNode
    {
        internal int iValue = 0;
        internal int iType = 111111;

        internal static bool UseFinals = true;

        public static bool getUseFinal()
        {
            return UseFinals;
        }

        public static void setUseFinal(bool Final)
        {
            UseFinals = Final;
        }

        public virtual void setValue(int Value)
        {
            iValue = Value;
        }

        public virtual int getValue()
        {
            return iValue;
        }

        public virtual void setType(int Type)
        {
            iType = Type;
        }

        public virtual int getType()
        {
            return iType;
        }

    }

    public class RandomNode : BaseNode
    {
        internal byte[] SimpleSize;
        internal static int iSize = 0;
        internal int iiSize = 0;

        public RandomNode(int Size, int value)
        {
            setValue(value);
            setType(2);
            SimpleSize = new byte[Size];

            if (Size != 0)
            {
                SimpleSize[0] = (byte)255;
                SimpleSize[Size - 1] = (byte)255;
            }

            setSize(Size);

        }

        public virtual void setSize(int Size)
        {
            iiSize = Size;
            iSize += Size;
        }

        public virtual int getSize()
        {
            return iiSize;
        }

        public virtual int getBigSize()
        {
            return iSize;
        }

        public virtual void setBigSize(int Size)
        {
            iSize = Size;
        }



    }
}
