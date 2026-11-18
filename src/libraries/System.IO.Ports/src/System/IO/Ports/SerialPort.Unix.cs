// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace System.IO.Ports
{
    public partial class SerialPort : Component
    {
        public static string[] GetPortNames()
        {
            return OperatingSystem.IsLinux() ? GetPortNames_Linux()
                : OperatingSystem.IsAndroid() ? GetPortNames_Linux()
                : OperatingSystem.IsMacOS() ? GetPortNames_OSX()
                : OperatingSystem.IsMacCatalyst() ? GetPortNames_OSX()
                : OperatingSystem.IsFreeBSD() ? GetPortNames_FreeBSD()
                : OperatingSystem.IsOSPlatform("OPENBSD") ? GetPortNames_OpenBSD()
                : OperatingSystem.IsOSPlatform("NETBSD") ? GetPortNames_NetBSD()
                : OperatingSystem.IsOSPlatform("ILLUMOS") ? GetPortNames_SunOS()
                : OperatingSystem.IsOSPlatform("SOLARIS") ? GetPortNames_SunOS()
                : OperatingSystem.IsOSPlatform("HAIKU") ? GetPortNames_Haiku()
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
                List<string> ports = [];
                DirectoryInfo di = new(sysTtyDir);
                IEnumerable<FileSystemInfo> entries = di.EnumerateFileSystemInfos(@"*", SearchOption.TopDirectoryOnly);
                foreach (FileSystemInfo entry in entries)
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
                List<string> ports = [];
                foreach (string portName in Directory.EnumerateFiles(devDir, "tty*"))
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
            List<string> ports = [];

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
            List<string> ports = [];

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

        private static string[] GetPortNames_OpenBSD()
        {
            List<string> ports = [];

            foreach (string path in Directory.EnumerateFiles("/dev"))
            {
                if (IsOpenBSDSerialPortName(Path.GetFileName(path.AsSpan())))
                {
                    ports.Add(path);
                }
            }

            return ports.ToArray();

            // Per MAKEDEV, e.g. tty00 and tty0a (com), ttyc0 (cy), ttyU0 (ucom) and ttyVI00 (viocon), with cua* dial-out.
            static bool IsOpenBSDSerialPortName(ReadOnlySpan<char> name)
            {
                bool dialIn = name.StartsWith("tty", StringComparison.Ordinal);
                if (!dialIn && !name.StartsWith("cua", StringComparison.Ordinal))
                {
                    return false;
                }

                ReadOnlySpan<char> unit = name.Slice(3);
                return (unit.Length == 2 && unit[0] is >= '0' and <= '7' && char.IsAsciiHexDigitLower(unit[1]))
                    || HasUnit(unit, "c")
                    || HasUnit(unit, "U", allowLetters: true)
                    || (dialIn && HasUnit(unit, "VI"));
            }
        }

        private static string[] GetPortNames_NetBSD()
        {
            List<string> ports = [];

            foreach (string path in Directory.EnumerateFiles("/dev"))
            {
                if (IsNetBSDSerialPortName(Path.GetFileName(path.AsSpan())))
                {
                    ports.Add(path);
                }
            }

            return ports.ToArray();

            // Per MAKEDEV, e.g. tty00 (com), ttyU0 (ucom) and plcom0 (arm), with dty* dial-out.
            static bool IsNetBSDSerialPortName(ReadOnlySpan<char> name)
            {
                if (HasUnit(name, "plcom") || HasUnit(name, "dplcom"))
                {
                    return true;
                }

                bool dialIn = name.StartsWith("tty", StringComparison.Ordinal);
                if (!dialIn && !name.StartsWith("dty", StringComparison.Ordinal))
                {
                    return false;
                }

                ReadOnlySpan<char> unit = name.Slice(3);
                return HasUnit(unit, "")
                    || HasUnit(unit, "U")
                    || HasUnit(unit, "Y")
                    || HasUnit(unit, "CY")
                    || HasUnit(unit, "CZ")
                    || HasUnit(unit, "Z")
                    || (dialIn && HasUnit(unit, "VI"));
            }
        }

        // Whether name is prefix followed by a non-empty unit of ASCII digits (or ASCII letters and digits).
        private static bool HasUnit(ReadOnlySpan<char> name, string prefix, bool allowLetters = false)
        {
            if (!name.StartsWith(prefix, StringComparison.Ordinal) || name.Length == prefix.Length)
            {
                return false;
            }

            foreach (char c in name.Slice(prefix.Length))
            {
                if (allowLetters ? !char.IsAsciiLetterOrDigit(c) : !char.IsAsciiDigit(c))
                {
                    return false;
                }
            }

            return true;
        }

        private static string[] GetPortNames_SunOS()
        {
            List<string> ports = [];

            // Dial-in ports are /dev/term/<x> and dial-out ports /dev/cua/<x>, e.g. /dev/term/a and /dev/cua/a.
            foreach (string dir in (ReadOnlySpan<string>)["/dev/term", "/dev/cua"])
            {
                if (Directory.Exists(dir))
                {
                    ports.AddRange(Directory.GetFiles(dir));
                }
            }

            return ports.ToArray();
        }

        private static string[] GetPortNames_Haiku()
        {
            // Serial drivers publish their ports under /dev/ports, e.g. /dev/ports/pc_serial0 and /dev/ports/usb0.
            const string portsDir = "/dev/ports";
            return Directory.Exists(portsDir) ? Directory.GetFiles(portsDir) : [];
        }
    }
}
