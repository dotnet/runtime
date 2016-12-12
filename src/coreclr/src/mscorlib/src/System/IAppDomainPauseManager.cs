// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
** Purpose: Interface meant for CLR to participate in framework rundown.
** AppDomainPauseManager is the class that encapsulates all Fx rundown work.
**
**
=============================================================================*/

namespace System
{
    using System;
    using System.Threading;
    using System.Security;
    using System.Diagnostics.Contracts;
    using System.Runtime.Versioning;
    using System.Runtime.CompilerServices;

    internal class AppDomainPauseManager
    {
        public AppDomainPauseManager()
        {
            isPaused = false;
        }

        static AppDomainPauseManager()
        {
        }
        
        static readonly AppDomainPauseManager instance = new AppDomainPauseManager();
        internal static AppDomainPauseManager Instance
        {
            get { return instance; }
        }

        // FAS: IAppDomainPauseConsumer interface implementation
        // currently there is nothing we do here as the implementation
        // of updating pause times have been moved to native CorHost2
        public void Pausing()
        {
        }

        public void Paused()
        {
            Debug.Assert(!isPaused);

            if(ResumeEvent == null)
                ResumeEvent = new ManualResetEvent(false);
            else
                ResumeEvent.Reset();

            Timer.Pause();

            // Set IsPaused at the last as other threads seeing it set, will expect a valid
            // reset ResumeEvent. Also the requirement here is that only after Paused
            // returns other threads should block on this event. So there is a race condition here.
            isPaused = true;
        }

        public void Resuming()
        {
            Debug.Assert(isPaused);
            isPaused = false;
            ResumeEvent.Set();
        }

        public void Resumed()
        {
            Timer.Resume();
        }
   
        private static volatile bool isPaused;

        internal static bool IsPaused
        {
            get { return isPaused; }
        }

        internal static ManualResetEvent ResumeEvent
        {
            get; 
            set;
        }
    }
}
