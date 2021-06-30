// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace System.IO.Ports
{
    public partial class SerialPort : Component
    {
        public static string[] GetPortNames()
        {
#if NETCOREAPP
            return OperatingSystem.IsLinux() ? GetPortNames_Linux()
                : OperatingSystem.IsMacOS() ? GetPortNames_OSX()
                : OperatingSystem.IsFreeBSD() ? GetPortNames_FreeBSD()
#else
            return RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? GetPortNames_Linux()
                : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? GetPortNames_OSX()
                : RuntimeInformation.IsOSPlatform(OSPlatform.Create("FREEBSD")) ? GetPortNames_FreeBSD()
#endif
                : throw new PlatformNotSupportedException(SR.PlatformNotSupported_SerialPort_GetPortNames);
        }

        private static string[] GetPortNames_Linux()
        {
            const string sysTtyDir = "/sys/class/tty";
            const string sysUsbDir = "/sys/bus/usb-serial/devices/";
            const string devDir = "/dev/";

            if (Directory.Exists(sysTtyDir))
            {
                // /sys is mounted. Let's explore tty class and pick active nodes.
                List<string> ports = new List<string>();
                DirectoryInfo di = new DirectoryInfo(sysTtyDir);
                var entries = di.EnumerateFileSystemInfos(@"*", SearchOption.TopDirectoryOnly);
                foreach (var entry in entries)
                {
                    // /sys/class/tty contains some bogus entries such as console, tty
                    // and a lot of bogus ttyS* entries mixed with correct ones.
                    // console and tty can be filtered out by checking for presence of device/tty
                    // ttyS entries pass this check but those can be filtered out
                    // by checking for presence of device/id or device/of_node
                    // checking for that for non-ttyS entries is incorrect as some uart
                    // devices are incorrectly filtered out
                    bool isTtyS = entry.Name.StartsWith("ttyS", StringComparison.Ordinal);
                    bool isTtyGS = !isTtyS && entry.Name.StartsWith("ttyGS", StringComparison.Ordinal);
                    if ((isTtyS &&
                         (File.Exists(entry.FullName + "/device/id") ||
                          Directory.Exists(entry.FullName + "/device/of_node"))) ||
                        (!isTtyS && Directory.Exists(entry.FullName + "/device/tty")) ||
                        Directory.Exists(sysUsbDir + entry.Name) ||
                        (isTtyGS && (File.Exists(entry.FullName + "/dev"))))
                    {
                        string deviceName = devDir + entry.Name;
                        if (File.Exists(deviceName))
                        {
                            ports.Add(deviceName);
                        }
                    }
                }

                return ports.ToArray();
            }
            else
            {
                // Fallback to scanning /dev. That may have more devices then needed.
                // This can also miss usb or serial devices with non-standard name.
                var ports = new List<string>();
                foreach (var portName in Directory.EnumerateFiles(devDir, "tty*"))
                {
                    if (portName.StartsWith("/dev/ttyS", StringComparison.Ordinal) ||
                        portName.StartsWith("/dev/ttyUSB", StringComparison.Ordinal) ||
                        portName.StartsWith("/dev/ttyACM", StringComparison.Ordinal) ||
                        portName.StartsWith("/dev/ttyAMA", StringComparison.Ordinal) ||
                        portName.StartsWith("/dev/ttymxc", StringComparison.Ordinal))
                    {
                        ports.Add(portName);
                    }
                }

                return ports.ToArray();
            }
        }

        private static string[] GetPortNames_OSX()
        {
            List<string> ports = new List<string>();

            foreach (string name in Directory.GetFiles("/dev", "tty.*"))
            {
                // GetFiles can return unexpected results because of 8.3 matching.
                // Like /dev/tty
                if (name.StartsWith("/dev/tty.", StringComparison.Ordinal))
                {
                    ports.Add(name);
                }
            }

            foreach (string name in Directory.GetFiles("/dev", "cu.*"))
            {
                if (name.StartsWith("/dev/cu.", StringComparison.Ordinal))
                {
                    ports.Add(name);
                }
            }

            return ports.ToArray();
        }

        private static string[] GetPortNames_FreeBSD()
        {
            List<string> ports = new List<string>();

            foreach (string name in Directory.GetFiles("/dev", "ttyd*"))
            {
                if (!name.EndsWith(".init", StringComparison.Ordinal) && !name.EndsWith(".lock", StringComparison.Ordinal))
                {
                    ports.Add(name);
                }
            }

            foreach (string name in Directory.GetFiles("/dev", "cuau*"))
            {
                if (!name.EndsWith(".init", StringComparison.Ordinal) && !name.EndsWith(".lock", StringComparison.Ordinal))
                {
                    ports.Add(name);
                }
            }

            return ports.ToArray();
        }
    }
}
