// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

class Foo
{
    public Foo F1;
    public Foo F2;
    public Foo F3;
    public Foo F4;
    static int SizeOfFoo = 32;

    static void Main(string[] args)
    {
        int mem = Int32.MaxValue / 2;
        if( args.Length > 0 )
        {
            if ( !Int32.TryParse( args[0], out mem ) )
            {
                mem = Int32.MaxValue / 2;
            }
        }

        int fooCnt = mem / SizeOfFoo;
        Console.WriteLine( "Creating {0} objects of type 'Foo', each with a size of {1} bytes.", fooCnt, SizeOfFoo );

        Foo[] currFoos = new Foo[ 4 ];
        for( int i = 0; i < fooCnt; ++i )
        {
            Foo f = new Foo();
            if( i < 4 )
            {
                currFoos[i] = f;
            } 
            else 
            {
                f.F1 = currFoos[0];
                f.F2 = currFoos[1];
                f.F3 = currFoos[2];
                f.F4 = currFoos[3];

                currFoos[0] = currFoos[1];
                currFoos[1] = currFoos[2];
                currFoos[2] = currFoos[3];
                currFoos[3] = f;
            }
        }

        Random randDel = new Random( 17 );
        int delCnt = randDel.Next( 5, 100 );
        Console.WriteLine( "Deleting {0} objects.", delCnt );

        Foo currDelBlock = currFoos[0].F1;
        Foo nextDelBlock = currFoos[0].F1;
        for( int i = 0; i < delCnt; ++i )
        {
            for( int j = 0; j < 10; ++j )
            {
                nextDelBlock = nextDelBlock.F1;
            }
            for( int k = 0; k < 4; ++k )
            {
                currDelBlock.F1 = null;
                currDelBlock.F2.F4 = null;
                currDelBlock.F3.F3 = null;
                currDelBlock.F4.F2 = null;
            }
            currDelBlock = nextDelBlock;
        }
    }
}

