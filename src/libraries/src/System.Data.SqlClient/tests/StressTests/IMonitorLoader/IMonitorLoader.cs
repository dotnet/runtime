// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Monitoring;

namespace Monitoring
{
    public interface IMonitorLoader
    {
        string HostMachine { get; set; }
        string AssemblyPath { get; set; }
        string TestName { get; set; }
        bool Enabled { get; set; }

        void Action(MonitorLoaderUtils.MonitorAction monitoraction);
        void AddPerfData(MonitorMetrics data);
        Dictionary<string, MonitorMetrics> GetPerfData();
    }

    public class MonitorLoaderUtils
    {
        public enum MonitorAction
        {
            Initialize,
            Start,
            Stop,
            DoNothing
        }
    }
}
