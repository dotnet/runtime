// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;
using System;
using System.Collections;
using System.IO;
using System.Threading;
using System.Text;
using System.Runtime.InteropServices;
using System.Globalization;

namespace System.ServiceProcess.Tests
{
    public class TestService : ServiceBase
    {
        public TestService(string serviceName)
        {
            this.ServiceName = serviceName;

            // Enable all the events
            this.CanPauseAndContinue = true;
            this.CanStop = true;
            this.CanShutdown = true;

            // We cannot easily test these so disable the events
            this.CanHandleSessionChangeEvent = false;
            this.CanHandlePowerEvent = false;
        }

        protected override void OnContinue()
        {
            WriteLog(nameof(OnContinue));
            base.OnContinue();
        }

        protected override void OnCustomCommand(int command)
        {
            WriteLog(nameof(OnCustomCommand) + " command=" + command);
            base.OnCustomCommand(command);
        }

        protected override void OnPause()
        {
            WriteLog(nameof(OnPause));
            base.OnPause();
        }

        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            WriteLog(nameof(OnSessionChange) + " change=" + changeDescription.ToString());
            base.OnSessionChange(changeDescription);
        }

        protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            WriteLog(nameof(OnPowerEvent) + " status=" + powerStatus.ToString());
            return base.OnPowerEvent(powerStatus);
        }

        protected override void OnShutdown()
        {
            WriteLog(nameof(OnShutdown));
            base.OnShutdown();
        }

        protected override void OnStart(string[] args)
        {
            WriteLog(nameof(OnStart) + " args=" + string.Join(",", args));
            base.OnStart(args);
        }

        protected override void OnStop()
        {
            WriteLog(nameof(OnStop));
            base.OnStop();

            if (_log != null)
            {
                _log.Dispose();
                _log = null;
            }
        }

        private StreamWriter _log;

        private void WriteLog(string msg)
        {
            if (_log == null)
            {
                string path = System.Reflection.Assembly.GetEntryAssembly().Location + "." + ServiceName + ".log";
                _log = new StreamWriter(path);
                _log.AutoFlush = true;
;            }

            _log.WriteLine(msg);
        }
    }
}
