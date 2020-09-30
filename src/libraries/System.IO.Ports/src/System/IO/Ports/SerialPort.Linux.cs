// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.IO;

namespace System.IO.Ports
{
    public partial class SerialPort : Component
    {
        public static string[] GetPortNames()
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
    }
}
