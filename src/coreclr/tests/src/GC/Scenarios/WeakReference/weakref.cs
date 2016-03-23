// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace DefaultNamespace {
    using System;
    using System.Collections.Generic;

    internal class CreateObj
    {
        private String [] Str;
        private List<WeakReference> alWeakRef;

        public CreateObj(int iObj,int iSwitch)
        {
            Str = new String[iObj];
            alWeakRef = new List<WeakReference>();
        }

        public bool RunTest(int iObj,int iSwitch)
        {
            if (!DeleteObj(iObj,iSwitch))
            {
                return false;
            }
            return CheckResult(iObj);
        }

         public bool DeleteObj(int iObj,int iSwitch)
         {
            for( int i= 0; i< iObj; i++ )
            {
                Str[i] = ( i.ToString() );
                alWeakRef.Add( new WeakReference( Str[i], iSwitch==1) );
            }

            GC.Collect();

            for( int i=0; i< iObj; i++)
            {
                if ( alWeakRef[i].Target == null )
                {
                    //*all weakref have strong reference, so it should not return null
                    return false;
                }
            }

            for( int i=0; i< iObj; i++)
            {
                Str[i] = null;
            }
            GC.Collect();

            return true;

          }

          public bool CheckResult(int iObj)
          {
            for( int i=0; i< iObj; i++)
            {
                if ( alWeakRef[ i ].Target != null )
                {
                    //*no weakref have strong reference, so it should return null
                    return false;
                }
            }
            return true;
          }

    }


    internal class CreateObj2
    {
        public  WeakRef mv_Obj;

        public CreateObj2()
        {
            mv_Obj = new WeakRef();
        }

        public bool RunTest(int iObj,int iSwitch)
        {
            return ( mv_Obj.RealWeakRef( iObj, iSwitch ));
        }
    }


    internal class WeakRef
    {

        public bool RealWeakRef(int iObj, int iSwitch)
        {

            CreateObj temp = new CreateObj(iObj,iSwitch);
            bool result = temp.RunTest(iObj,iSwitch);
            return result;
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

    internal class BNode
    {
        public static int icCreateNode = 0;
        public static int icFinalNode = 0;
        internal int [] mem;
        internal List<BNode> ResObj = new List<BNode>();
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
}
