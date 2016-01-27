// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if _DEBUG
// This class writes to wherever OutputDebugString writes to.  If you don't have
// a Windows app (ie, something hosted in IE), you can use this to redirect Console
// output for some good old-fashioned console spew in MSDEV's debug output window.

using System;
using System.IO;
using System.Text;
using System.Security;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;
using System.Globalization;

namespace System.IO {
    internal class __DebugOutputTextWriter : TextWriter {
        private readonly String _consoleType;

        internal __DebugOutputTextWriter(String consoleType): base(CultureInfo.InvariantCulture)
        {
            _consoleType = consoleType;
        }

        public override Encoding Encoding {
#if FEATURE_CORECLR
            [System.Security.SecuritySafeCritical] 
#endif
            get {
                if (Marshal.SystemDefaultCharSize == 1)
                    return Encoding.Default;
                else
                    return new UnicodeEncoding(false, false);
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override void Write(char c)
        {
            OutputDebugString(c.ToString());
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override void Write(String str)
        {
            OutputDebugString(str);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override void Write(char[] array)
        {
            if (array != null) 
                OutputDebugString(new String(array));
        }
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        public override void WriteLine(String str)
        {
            if (str != null)
                OutputDebugString(_consoleType + str);
            else
                OutputDebugString("<null>");
            OutputDebugString(new String(CoreNewLine));
        }

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(Win32Native.KERNEL32, CharSet=CharSet.Auto)]
        [SuppressUnmanagedCodeSecurityAttribute()]
        private static extern void OutputDebugString(String output);
    }
}
       
#endif // _DEBUG
