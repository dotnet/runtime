// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


//======================================================
//
//  ThdChaos -- caues multiple asserts
//
//======================================================

namespace DefaultNamespace {
    using System.Threading;
    using System;
    using System.IO;

    public class ThdChaos
    {
        internal static int iThrd = 0;
        public static int Main( System.String [] Args )
        {
            Console.Out.WriteLine("Test should return with ExitCode 100 ...");
            // console synchronization Console.SetOut(TextWriter.Synchronized(Console.Out));
            Console.Out.WriteLine("Args.Length="+Args.Length );
            if(Args.Length >=1 )
            {
                if (!Int32.TryParse( Args[0], out iThrd ))
                {
                    iThrd = 20;
                }
            }
            else
            {
                iThrd = 20;
            }

            ThdChaos Mv_ThdChaos = new ThdChaos();
            MasterThread Mv_Thread = new MasterThread( iThrd );
            return 100;

        }

    }
}
