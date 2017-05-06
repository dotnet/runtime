// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics
{
    using System;
    using System.IO;
    using System.Collections;
    using System.Runtime.CompilerServices;
    using Encoding = System.Text.Encoding;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;
    using System.Diagnostics.CodeAnalysis;

    using Console = Internal.Console;

    // LogSwitchLevelHandlers are triggered when the level of a LogSwitch is modified
    // NOTE: These are NOT triggered when the log switch setting is changed from the 
    // attached debugger.
    // 
    [Serializable]
    internal delegate void LogSwitchLevelHandler(LogSwitch ls, LoggingLevels newLevel);


    internal static class Log
    {
        // Switches allow relatively fine level control of which messages are
        // actually shown.  Normally most debugging messages are not shown - the
        // user will typically enable those which are relevant to what is being
        // investigated.
        // 
        // An attached debugger can enable or disable which messages will
        // actually be reported to the user through the COM+ debugger
        // services API.  This info is communicated to the runtime so only
        // desired events are actually reported to the debugger.  
        internal static Hashtable m_Hashtable;
        private static volatile bool m_fConsoleDeviceEnabled;
        private static volatile LogSwitchLevelHandler _LogSwitchLevelHandler;
        private static Object locker;

        // Constant representing the global switch
        public static readonly LogSwitch GlobalSwitch;


        static Log()
        {
            m_Hashtable = new Hashtable();
            m_fConsoleDeviceEnabled = false;
            //pConsole = null;
            //iNumOfMsgHandlers = 0;
            //iMsgHandlerArraySize = 0;
            locker = new Object();

            // allocate the GlobalSwitch object
            GlobalSwitch = new LogSwitch("Global", "Global Switch for this log");

            GlobalSwitch.MinimumLevel = LoggingLevels.ErrorLevel;
        }

        internal static void InvokeLogSwitchLevelHandlers(LogSwitch ls, LoggingLevels newLevel)
        {
            LogSwitchLevelHandler handler = _LogSwitchLevelHandler;
            if (handler != null)
                handler(ls, newLevel);
        }


        // Property to Enable/Disable ConsoleDevice. Enabling the console device 
        // adds the console device as a log output, causing any
        // log messages which make it through filters to be written to the 
        // application console.  The console device is enabled by default if the 
        // ??? registry entry or ??? environment variable is set.
        public static bool IsConsoleEnabled
        {
            get { return m_fConsoleDeviceEnabled; }
            set { m_fConsoleDeviceEnabled = value; }
        }

        // Generates a log message. If its switch (or a parent switch) allows the 
        // level for the message, it is "broadcast" to all of the log
        // devices.
        // 
        public static void LogMessage(LoggingLevels level, LogSwitch logswitch, String message)
        {
            if (logswitch == null)
                throw new ArgumentNullException("LogSwitch");

            if (level < 0)
                throw new ArgumentOutOfRangeException(nameof(level), SR.ArgumentOutOfRange_NeedNonNegNum);
            Contract.EndContractBlock();

            // Is logging for this level for this switch enabled?
            if (logswitch.CheckLevel(level) == true)
            {
                // Send message for logging

                // first send it to the debugger
                Debugger.Log((int)level, logswitch.strName, message);

                // Send to the console device
                if (m_fConsoleDeviceEnabled)
                {
                    Console.Write(message);
                }
            }
        }

        /*
        * Following are convenience entry points; all go through Log()
        * Note that the (Switch switch, String message) variations 
        * are preferred.
        */
        public static void Trace(LogSwitch logswitch, String message)
        {
            LogMessage(LoggingLevels.TraceLevel0, logswitch, message);
        }

        public static void Trace(String message)
        {
            LogMessage(LoggingLevels.TraceLevel0, GlobalSwitch, message);
        }


        // Native method to inform the EE about the creation of a new LogSwitch
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void AddLogSwitch(LogSwitch logSwitch);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void ModifyLogSwitch(int iNewLevel, String strSwitchName, String strParentName);
    }
}
