// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace TestLibrary
{
    public static partial class Env
    {
        const int MAX_PATH = 260;
        const int ERROR_ENVVAR_NOT_FOUND = 0xCB;

        public static string NewLine
        {
            get
            {
                if (Utilities.IsWindows)
                {
                    return "\r\n";
                }
                else
                {
                    return "\n";
                }
            }
        }

        public static string FileSeperator
        {
            get
            {
                if (Utilities.IsWindows)
                {
                    return "\\";
                }
                else
                {
                    return "/";
                }
            }
        }

        public static string VolumeSeperator
        {
            get
            {
                if (Utilities.IsWindows)
                {
                    return ":";
                }
                else
                {
                    return "/";
                }
            }
        }

        public static string AltFileSeperator
        {
            get
            {
                if (Utilities.IsWindows)
                {
                    return "/";
                }
                else
                {
                    return "\\";
                }
            }
        }

        public static string PathDelimiter
        {
            get
            {
                if (Utilities.IsWindows)
                {
                    return ";";
                }
                else
                {
                    return ":";
                }
            }
        }

        public static int MaxPath
        {
            get
            {
                return 248;
            }
        }

        public static int MaxFileName
        {
            get
            {
                return 260;
            }
        }

        public static String CurrentDirectory
        {
            get { return ""; }
        }
    }
}
