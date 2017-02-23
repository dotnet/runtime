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

using System;
using System.Threading;
using System.Security;
using System.Diagnostics.Contracts;
using System.Runtime.Versioning;
using System.Runtime.CompilerServices;

namespace System
{
    internal class AppDomainPauseManager
    {
        public AppDomainPauseManager()
        {
            isPaused = false;
        }

        static AppDomainPauseManager()
        {
        }

        private static readonly AppDomainPauseManager instance = new AppDomainPauseManager();

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
