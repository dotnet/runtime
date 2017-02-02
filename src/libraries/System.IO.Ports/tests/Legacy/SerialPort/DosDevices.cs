// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

public class DosDevices : IEnumerable<KeyValuePair<string, string>>
{
    private Dictionary<string, string> _dosDevices;

    public DosDevices()
    {
        _dosDevices = new Dictionary<string, string>(100, StringComparer.InvariantCultureIgnoreCase);
        Initialize();
    }

    public static void Main()
    {
        DosDevices dosDevices = new DosDevices();

        foreach (KeyValuePair<string, string> keyValuePair in dosDevices)
        {
            Console.WriteLine("'{0}'='{1}'", keyValuePair.Key, keyValuePair.Value.Trim());
        }

        Console.WriteLine("CommonNameExists(\"LPT1\")={0}", dosDevices.CommonNameExists("LPT1"));
        Console.WriteLine("CommonNameExists(\"A:\")={0}", dosDevices.CommonNameExists("A:"));

        Console.WriteLine("CommonNameExists(\"LPT\")={0}", dosDevices.CommonNameExists("LPT"));

        string s = dosDevices["LPT1"];
        for (int i = 0; i < s.Length; ++i)
        {
            Console.WriteLine("{0}({1})", (int)s[i], s[i]);
        }
    }

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        return _dosDevices.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return _dosDevices.GetEnumerator();
    }

    public bool CommonNameExists(string commonName)
    {
        return _dosDevices.ContainsKey(commonName);
    }

    public bool InternalNameExists(string internalName)
    {
        return _dosDevices.ContainsValue(internalName);
    }

    public bool InternalNameContains(string internalName)
    {
        foreach (String value in _dosDevices.Values)
        {
            if (-1 != value.IndexOf(internalName))
                return true;
        }

        return false;
    }

    public string this[string commonName]
    {
        get
        {
            return _dosDevices[commonName];
        }
    }

    private void Initialize()
    {
        // Get all the MS-DOS names on the local machine 
        //(sending null for lpctstrName gets all the names)
        int dataSize;
        char[] buffer = CallQueryDosDevice(null, out dataSize);

        int i = 0;
        while (i < dataSize)
        {
            // Walk through the buffer building a name until we hit the delimiter \0
            int start = i;
            while (buffer[i] != '\0')
            {
                i++;
            }

            if (i != start)
            {
                // We now have an MS-DOS name (the common name). We call QueryDosDevice again with
                // this name to get the underlying system name mapped to the MS-DOS name. 
                string currentName = TrimTrailingNull((new String(buffer, start, i - start)).Trim());
                int nameSize;
                char[] nameBuffer = CallQueryDosDevice(currentName, out nameSize);

                // If we got a system name, see if it's a serial port name. If it is, add the common name
                // to our list
                if (nameSize > 0)
                {
                    // internalName will include the trailing null chars as well as any additional
                    // names that may get returned.  This is ok, since we are only interested in the
                    // first name and we can use StartsWith. 
                    string internalName = TrimTrailingNull((new string(nameBuffer, 0, nameSize)).Trim());
                    _dosDevices.Add(currentName, internalName);
                }
                else
                {
                    _dosDevices.Add(currentName, String.Empty);
                }
            }
            i++;
        }
    }

    private static string TrimTrailingNull(string s)
    {
        int count = s.Length;

        for (int i = s.Length - 1; 0 <= i; --i)
        {
            if ((int)s[i] != 0)
                break;
            --count;
        }

        return s.Substring(0, count);
    }

    private static unsafe char[] CallQueryDosDevice(string name, out int dataSize)
    {
        char[] buffer = new char[1024];

        fixed (char* bufferPtr = buffer)
        {
            dataSize = QueryDosDevice(name, buffer, buffer.Length);
            while (dataSize <= 0)
            {
                int lastError = Marshal.GetLastWin32Error();
                if (lastError == ERROR_INSUFFICIENT_BUFFER || lastError == ERROR_MORE_DATA)
                {
                    buffer = new char[buffer.Length * 2];
                    dataSize = QueryDosDevice(null, buffer, buffer.Length);
                }
                else
                {
                    throw new Exception("Unkown Win32 Error: " + lastError);
                }
            }
        }
        return buffer;
    }

    public const int ERROR_INSUFFICIENT_BUFFER = 122;
    public const int ERROR_MORE_DATA = 234;

    [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    internal static extern int QueryDosDevice(string lpDeviceName, char[] lpTargetPath, int ucchMax);
}
