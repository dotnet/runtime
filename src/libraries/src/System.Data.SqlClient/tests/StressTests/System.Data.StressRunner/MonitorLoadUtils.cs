// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Monitoring;
using System.Reflection;

namespace DPStressHarness
{
    public static class MonitorLoader
    {
        public static IMonitorLoader LoadMonitorLoaderAssembly()

        {
            IMonitorLoader monitorloader = null;
            const string classname = "Monitoring.MonitorLoader";
            const string interfacename = "IMonitorLoader";
            Assembly mainAssembly = typeof(Monitoring.IMonitorLoader).GetTypeInfo().Assembly;

            Type t = mainAssembly.GetType(classname);
            //make sure the type is derived from IMonitorLoader
            Type[] interfaces = t.GetInterfaces();
            bool derivedFromIMonitorLoader = false;
            if (interfaces != null)
            {
                foreach (Type intrface in interfaces)
                {
                    if (intrface.Name == interfacename)
                    {
                        derivedFromIMonitorLoader = true;
                    }
                }
            }
            if (derivedFromIMonitorLoader)

            {
                monitorloader = (IMonitorLoader)Activator.CreateInstance(t);

                monitorloader.AssemblyPath = mainAssembly.FullName;
            }
            else
            {
                throw new Exception("The specified assembly does not implement " + interfacename + "!!");
            }
            return monitorloader;
        }
    }
}
