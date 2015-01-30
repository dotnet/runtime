// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//
/*=============================================================================
**
**
** Purpose: Interface meant for CLR to participate in framework rundown.
** AppDomainPauseManager is the class that encapsulates all Fx rundown work.
**
** Copyright (c) Microsoft
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

#if FEATURE_LEGACYNETCFFAS
    [System.Security.SecurityCritical]
    public interface IAppDomainPauseManager
    {
        void Pausing();
        void Paused();
        void Resuming();
        void Resumed();
    }
#endif

    [System.Security.SecurityCritical]
#if FEATURE_LEGACYNETCFFAS
    public class AppDomainPauseManager : IAppDomainPauseManager
#else
    internal class AppDomainPauseManager
#endif
    {
        [System.Security.SecurityCritical]
        public AppDomainPauseManager()
        {
            isPaused = false;
        }

        [System.Security.SecurityCritical]
        static AppDomainPauseManager()
        {
        }
        
        static readonly AppDomainPauseManager instance = new AppDomainPauseManager();
        internal static AppDomainPauseManager Instance
        {
            [System.Security.SecurityCritical]
            get { return instance; }
        }

        // FAS: IAppDomainPauseConsumer interface implementation
        // currently there is nothing we do here as the implementation
        // of updating pause times have been moved to native CorHost2
        [System.Security.SecurityCritical]
        public void Pausing()
        {
        }

        [System.Security.SecurityCritical]
        public void Paused()
        {
            Contract.Assert(!isPaused);

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

        [System.Security.SecurityCritical]
        public void Resuming()
        {
            Contract.Assert(isPaused);
            isPaused = false;
            ResumeEvent.Set();
        }

        [System.Security.SecurityCritical]
        public void Resumed()
        {
            Timer.Resume();
        }
   
        private static volatile bool isPaused;

        internal static bool IsPaused
        {
            [System.Security.SecurityCritical]
            get { return isPaused; }
        }

        internal static ManualResetEvent ResumeEvent
        {
            [System.Security.SecurityCritical]
            get; 
            [System.Security.SecurityCritical]
            set;
        }
    }
}
