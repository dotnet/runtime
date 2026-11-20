// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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
            HashSet<string>? bootDevices = GetBootDevices(out string? serialConsoleDriver);

            // A serial console that isn't com(4), e.g. pluart(4) on arm64, takes over com(4)'s device nodes,
            // so /dev/tty00 is pluart0 then.
            string comDriver = serialConsoleDriver ?? "com";
            List<string> ports = [];

            foreach (string path in Directory.EnumerateFiles("/dev"))
            {
                if (GetOpenBSDSerialDevice(Path.GetFileName(path.AsSpan()), comDriver) is string device && IsAttached(device, bootDevices))
                {
                    ports.Add(path);
                }
            }

            return ports.ToArray();

            // Per MAKEDEV, e.g. tty00 and tty0a (com, hex units), ttyc0 (cy), ttyU0 (ucom) and ttyVI00 (viocon), with cua* dial-out.
            static string? GetOpenBSDSerialDevice(ReadOnlySpan<char> name, string comDriver)
            {
                bool dialIn = name.StartsWith("tty", StringComparison.Ordinal);
                if (!dialIn && !name.StartsWith("cua", StringComparison.Ordinal))
                {
                    return null;
                }

                ReadOnlySpan<char> unit = name.Slice(3);
                if (unit.Length == 2 && unit[0] is >= '0' and <= '7' && char.IsAsciiHexDigitLower(unit[1]))
                {
                    return comDriver + int.Parse(unit, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
                }

                return HasUnit(unit, "c") ? "cy"
                    : HasUnit(unit, "U", allowLetters: true) ? UsbDevice
                    : dialIn && HasUnit(unit, "VI") ? "viocon"
                    : null;
            }
        }

        private static string[] GetPortNames_NetBSD()
        {
            HashSet<string>? bootDevices = GetBootDevices(out _);
            List<string> ports = [];

            foreach (string path in Directory.EnumerateFiles("/dev"))
            {
                if (GetNetBSDSerialDevice(Path.GetFileName(path.AsSpan())) is string device && IsAttached(device, bootDevices))
                {
                    ports.Add(path);
                }
            }

            return ports.ToArray();

            // Per MAKEDEV, e.g. tty00 (com), ttyZ0 (zstty), ttyU0 (ucom) and plcom0 (arm), with dty* dial-out.
            static string? GetNetBSDSerialDevice(ReadOnlySpan<char> name)
            {
                if (HasUnit(name, "plcom"))
                {
                    return name.ToString();
                }

                if (HasUnit(name, "dplcom"))
                {
                    return name.Slice(1).ToString();
                }

                bool dialIn = name.StartsWith("tty", StringComparison.Ordinal);
                if (!dialIn && !name.StartsWith("dty", StringComparison.Ordinal))
                {
                    return null;
                }

                ReadOnlySpan<char> unit = name.Slice(3);
                if (HasUnit(unit, ""))
                {
                    return "com" + int.Parse(unit, NumberStyles.None, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
                }

                return HasUnit(unit, "Z") ? "zstty" + unit.Slice(1).ToString()
                    : HasUnit(unit, "U") || HasUnit(unit, "Y") ? UsbDevice
                    : HasUnit(unit, "CY") ? "cy"
                    : HasUnit(unit, "CZ") ? "cz"
                    : dialIn && HasUnit(unit, "VI") ? "viocon"
                    : null;
            }
        }

        // The device behind USB serial nodes, which can be plugged in after boot.
        private const string UsbDevice = "";

        // Whether the device behind a node is present: USB devices always count, and so does everything when the boot devices are unknown.
        private static bool IsAttached(string device, HashSet<string>? bootDevices) =>
            device.Length == 0 || bootDevices is null || bootDevices.Contains(device);

        // OpenBSD and NetBSD have a static /dev with nodes for every unit MAKEDEV knows about. Fixed serial ports only
        // attach at boot, so the dmesg copy saved at boot tells which of them exist (OpenBSD's fw_update reads it too).
        // Returns the attached devices, both as "com0" and as the driver "com", or null if that can't be determined,
        // plus the driver of the serial console, if there is one.
        private static HashSet<string>? GetBootDevices(out string? serialConsoleDriver)
        {
            HashSet<string> devices = [];
            bool sawBootBanner = false;
            serialConsoleDriver = null;

            try
            {
                foreach (string line in File.ReadLines("/var/run/dmesg.boot"))
                {
                    // NetBSD may prefix messages with a timestamp, e.g. "[     1.003637] com0 at isa0 port 0x3f8-0x3ff irq 4".
                    ReadOnlySpan<char> message = line.AsSpan();
                    if (message.StartsWith('['))
                    {
                        message = message.Slice(message.IndexOf(']') + 1).TrimStart();
                    }

                    // The message buffer can still hold an earlier boot, so only the last one counts.
                    if (message.StartsWith("OpenBSD ", StringComparison.Ordinal) || message.StartsWith("NetBSD ", StringComparison.Ordinal))
                    {
                        devices.Clear();
                        serialConsoleDriver = null;
                        sawBootBanner = true;
                        continue;
                    }

                    // e.g. "com0 at acpi0 COM1 addr 0x3f8/0x8 irq 4: ns16550a, 16 byte fifo"
                    int at = message.IndexOf(" at ", StringComparison.Ordinal);
                    if (at > 0 && !message.Slice(0, at).ContainsAny(' ', ':'))
                    {
                        ReadOnlySpan<char> device = message.Slice(0, at);
                        devices.Add(device.ToString());
                        devices.Add(device.TrimEnd("0123456789").ToString());
                    }

                    // e.g. "pluart0: console" or "com0: console", but not "wsdisplay0 at vga1 mux 1: console (80x25, vt100 emulation)"
                    int end = message.IndexOfAny(' ', ':');
                    if (end > 0 && message.EndsWith(": console", StringComparison.Ordinal))
                    {
                        serialConsoleDriver = message.Slice(0, end).TrimEnd("0123456789").ToString();
                    }
                }
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                return null;
            }

            // Without the boot banner the start of the boot was lost, so attach messages may be missing too.
            return sawBootBanner ? devices : null;
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
