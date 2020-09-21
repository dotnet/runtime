// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    /// <summary>
    /// CModCmdLine
    /// </summary>
    public class CModCmdLine
    {
        // obtain command line arguments from system command line
        private static MyDict<string, string> s_cmdList = null;

        public static MyDict<string, string> CmdLine
        {
            get
            {
                if (s_cmdList == null)
                {
                    s_cmdList = CModInfo.Options;
                }
                return s_cmdList;
            }
        }
    }
}
