/*++

Copyright (c) 2004  Microsoft Corporation

Module Name:

    GlobalDebug.cs

Abstract:

    Implements app-wide debug settings

History:

    23-Aug-2004    MattRim     Created

--*/

using System;
using System.Diagnostics;
using System.Globalization;


namespace System.DirectoryServices.AccountManagement
{

    internal enum DebugLevel
    {
        None = 0,
        Info,
        Warn,
        Error
    }


    internal static class GlobalDebug
    {
        // <SecurityKernel Critical="True" Ring="0">
        // <SatisfiesLinkDemand Name="Debug.get_Listeners():System.Diagnostics.TraceListenerCollection" />
        // </SecurityKernel>
        [System.Security.SecurityCritical]
        static GlobalDebug()
        {
            GlobalDebug.debugLevel = GlobalConfig.DebugLevel;
#if DEBUG        
            string debugLogFile = GlobalConfig.DebugLogFile;

            if (debugLogFile != null)
            {
                foreach (TraceListener listener in Debug.Listeners)
                {
                    if (listener is DefaultTraceListener)
                        ((DefaultTraceListener)listener).LogFileName = debugLogFile;
                }

                //
                Debug.WriteLine(
                            String.Format(
                                System.Globalization.CultureInfo.InvariantCulture,
                                "Principal API Debug Log - AppDomain {0} with ID {1} - {2} (UTC)",
                                System.Threading.Thread.GetDomain().FriendlyName,
                                System.Threading.Thread.GetDomainID(),
                                DateTime.UtcNow));
            }
#endif
        }

        static public bool Error
        {
            get {return DebugLevel.Error >= GlobalDebug.debugLevel ;}
        }

        static public bool Warn
        {
            get {return DebugLevel.Warn >= GlobalDebug.debugLevel ;}
        }

        static public bool Info
        {
            get {return DebugLevel.Info >= GlobalDebug.debugLevel ;}
        }

        // <SecurityKernel TreatAsSafe="Directly applied from MetaData" Critical="True" Ring="0">
        // <CallsSuppressUnmanagedCode Name="SafeNativeMethods.GetCurrentThreadId():System.Int32" />
        // </SecurityKernel>
        [System.Security.SecuritySafeCritical]
        [ConditionalAttribute("DEBUG")]
        static public void WriteLineIf(bool f, string category, string message, params object[] args)
        {
            message = "[" + SafeNativeMethods.GetCurrentThreadId().ToString("x", CultureInfo.InvariantCulture) + "] " + message;
        
            Debug.WriteLineIf(
                            f,
                            String.Format(
                                CultureInfo.InvariantCulture,
                                message,
                                args),
                            category);
        }

        // <SecurityKernel TreatAsSafe="Directly applied from MetaData" Critical="True" Ring="0">
        // <CallsSuppressUnmanagedCode Name="SafeNativeMethods.GetCurrentThreadId():System.Int32" />
        // </SecurityKernel>
        [System.Security.SecuritySafeCritical]
        [ConditionalAttribute("DEBUG")]
        static public void WriteLineIf(bool f, string category, string message)
        {
            message = "[" + SafeNativeMethods.GetCurrentThreadId().ToString("x", CultureInfo.InvariantCulture) + "] " + message;
        
            Debug.WriteLineIf(
                            f,
                            message,
                            category);
        }


        static DebugLevel debugLevel;
    }


}
