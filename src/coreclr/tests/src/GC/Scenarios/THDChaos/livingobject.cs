// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace DefaultNamespace {
    using System.Threading;
    using System;
    using System.IO;

    public class LivingObject
    {

        internal byte [ ]GlobalContainer;
        internal bool Switch = true;
        internal static int iCounter = 0;

        public LivingObject( )
        {
            Thread Mv_Thread = new Thread( new ThreadStart(this.ThreadStart) );
            Mv_Thread.Start( );
        }


        public void ThreadStart( )
        {
            // console synchronization Console.SetOut(TextWriter.Synchronized(Console.Out));

            if( iCounter%100 == 0)
            {
                Console.Out.WriteLine( iCounter + " number of threads has been started" );
            }

            byte [ ]MethodContainer = new byte[ 1024 ]; // 1K

            if( Switch )
            {
                GlobalContainer = new byte[ 1024 ]; // 1K
            }
            Switch = !Switch;

            GlobalContainer[ 0 ] = ( byte ) 1;
            GlobalContainer[ GlobalContainer.Length - 1 ] = ( byte ) 1;

            MethodContainer[ 0 ] = ( byte ) 1;
            MethodContainer[ MethodContainer.Length - 1 ] = ( byte ) 1;

            IncreatCount( );

            if( LivingObject.iCounter < ThdChaos.iThrd )
            {
                Thread Mv_Thread = new Thread( new ThreadStart (this.ThreadStart) );
                Mv_Thread.Start( );
            }

        }


        public void IncreatCount()
        {
            lock(this)
            {
                iCounter += 1;
            }
        }

    }
}
