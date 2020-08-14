// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal enum SysLogPriority : int
        {
            // Priorities
            LOG_EMERG       = 0,        /* system is unusable */
            LOG_ALERT       = 1,        /* action must be taken immediately */
            LOG_CRIT        = 2,        /* critical conditions */
            LOG_ERR         = 3,        /* error conditions */
            LOG_WARNING     = 4,        /* warning conditions */
            LOG_NOTICE      = 5,        /* normal but significant condition */
            LOG_INFO        = 6,        /* informational */
            LOG_DEBUG       = 7,        /* debug-level messages */
        }

        /// <summary>
        /// Write a message to the system logger, which in turn writes the message to the system console, log files, etc.
        /// See man 3 syslog for more info
        /// </summary>
        /// <param name="priority">
        /// The OR of a priority and facility in the SysLogPriority enum to declare the priority and facility of the log entry
        /// </param>
        /// <param name="message">The message to put in the log entry</param>
        /// <param name="arg1">Like printf, the argument is passed to the variadic part of the C++ function to wildcards in the message</param>
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_SysLog")]
        internal static extern void SysLog(SysLogPriority priority, string message, string arg1);
    }
}
