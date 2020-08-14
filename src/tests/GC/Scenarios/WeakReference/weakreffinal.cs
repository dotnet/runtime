// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace DefaultNamespace {
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    internal class CreateObj
    {
        public BNode []rgNode;
        public List<WeakReference> alWeakRef;
        public bool bret;

        public CreateObj(int iObj)
        {
            rgNode = new BNode[iObj];
            alWeakRef = new List<WeakReference>();
            bret = true;
        }

        public bool RunTest(int iObj,int iSwitch)
        {
            DeleteObj(iObj,iSwitch);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            bool result = CheckResult(iObj,iSwitch);
            return result;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public void DeleteObj(int iObj,int iSwitch)
        {
            for( int i= 0; i< iObj; i++ )
            {
                rgNode[i] = new BNode( i+1 );
                alWeakRef.Add( new WeakReference( rgNode[i], (iSwitch == 1) ) );
            }

            GC.Collect();
            for( int i=0; i< iObj; i++)
            {
                if ( (alWeakRef[ i ]).Target == null )
                {
                    //*all weakref have strong reference, so it should not return null
                    bret = false;
                }
            }

            for( int i=0; i< iObj; i++)
            {
                rgNode[i] = null;
            }
        }

        public bool CheckResult(int iObj,int iSwitch)
        {
            for( int i=0; i< iObj; i++)
            {
                if(iSwitch == 1)
                {
                    if ( (alWeakRef[ i ]).Target  == null )
                    {
                        //*weakrefs have strong reference, so it should not return null
                        bret = false;
                    }
                }
                else
                {
                    if ( (alWeakRef[ i ]).Target  != null )
                    {
                        //*no weakref have strong reference, so it should return null
                        bret = false;
                    }
                }
            }
            for(int i=0; i< iObj; i++ )
            {
                rgNode[i] = (BNode)BNode.ResObj[ i ];
            }
            return bret;
        }

    }

    internal class WeakRefFinal
    {

        public bool RealWeakRef(int iObj, int iSwitch)
        {
            CreateObj temp = new CreateObj(iObj);
            bool result = temp.RunTest(iObj,iSwitch);
            return result;
        }
    }


    internal class BNode
    {
        public static int icCreateNode = 0;
        public static int icFinalNode = 0;
        internal int [] mem;
        public static List<BNode> ResObj = new List<BNode>();
        public BNode( int i )
        {
            icCreateNode++;
            mem = new int[i];
            mem[0] = 0;
            if(i > 1 )
            {
                mem[mem.Length-1] = mem.Length-1;
            }

        }

    ~BNode()
        {
            icFinalNode++;
            ResObj.Add( this );
        }
    }

    internal class CreateObj2
    {
        public WeakRefFinal mv_Obj;

        public CreateObj2()
        {
            mv_Obj = new WeakRefFinal();
        }

        public bool RunTest(int iObj,int iSwitch)
        {
            return ( mv_Obj.RealWeakRef( iObj, iSwitch ));
        }

   }

    internal class Test
    {
        public static int Main(String [] Args)
        {
            int iObj = 0;
            int iSwitch = 0;

            Console.WriteLine("Test should return with ExitCode 100 ...");

            if (Args.Length >=2)
            {
                if (!Int32.TryParse( Args[0], out iObj ))
                {
                    iObj = 10;
                }
                if (!Int32.TryParse( Args[1], out iSwitch ))
                {
                    iSwitch = 1;
                }
            }
            else
            {
                iObj = 10;
                iSwitch = 1;
            }

            CreateObj2 temp = new CreateObj2();
            if (temp.RunTest(iObj,iSwitch))
            {
                Console.WriteLine("Test Passed");
                return 100;
            }
            Console.WriteLine("Test Failed");
            return 1;



        }

   }

}
