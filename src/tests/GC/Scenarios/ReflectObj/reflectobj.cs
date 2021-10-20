// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/**************************************************************/
/* TEST: ReflectObj
/* Purpose: test if GC can handle objects create by reflect
/* Coverage:    Class.CreateInstance()
/*              Class.GetField()
/*              Class.GetConstructor()
/*              ConstructorInfo.Invoke()
/*              FieldInfo.SetValue()
/*              FieldInfo.IsStatic()
/*              FieldInfo.Ispublic()
/**************************************************************/

namespace App {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    class ReflectObj
    {
        Object obj;
        public static int icCreat = 0;
        public static int icFinal = 0;
        public static List<object> al = new List<object>( );
        public ReflectObj()
        {
            obj = new long[1000];
            icCreat++;
        }

        public ReflectObj( int l )
        {
            obj = new long[l];
            icCreat++;
        }

        public Object GetObj()
        {
            return obj;
        }

        ~ReflectObj()
        {
            al.Add( GetObj() );
            icFinal++;
        }

        public static int Main( String [] str )
        {
            Console.WriteLine("Test should return with ExitCode 100 ...");
            CreateObj temp = new CreateObj();
            if (temp.RunTest())
            {
                Console.WriteLine("Test Passed");
                return 100;
            }
            Console.WriteLine("Test Failed");
            return 1;
        }

        class CreateObj
        {
            private Object[] v;
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
            private Type myClass;
            private Type [] rtype;
            private ConstructorInfo CInfo;

            public CreateObj()
            {
                myClass = Type.GetType( "App.ReflectObj" );
                v = new Object[1];
                for( int i=0; i< 2000; i++ )
                {
                    v[0] = i;
                    Activator.CreateInstance(myClass, v );
                }
            }

            public bool RunTest()
            {
                bool retVal = false;
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                Console.WriteLine("Created Objects: {0} Finalized objects: {1}",icCreat, icFinal );
                if ( icFinal != icCreat )
                {
                    return false;
                }

                FieldInfo fInfo = myClass.GetField( "icCreat", BindingFlags.IgnoreCase);
                fInfo = myClass.GetField( "icFinal", BindingFlags.IgnoreCase);

                Console.WriteLine( "Fieldinfo done" ); //debug;

                CreateMoreObj();

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                retVal = (icFinal == icCreat );

                Console.WriteLine("Living objects: "+ ReflectObj.al.Count );
                ReflectObj.al = null;

                return retVal;

            }

            [MethodImplAttribute(MethodImplOptions.NoInlining)]
            public void CreateMoreObj()
            {
                rtype = new Type[0];
                CInfo = myClass.GetConstructor(rtype );

                for( int i=0; i< 2000; i++ )
                {
                    CInfo.Invoke((Object[])null );
                }
            }


        }


    }

}
