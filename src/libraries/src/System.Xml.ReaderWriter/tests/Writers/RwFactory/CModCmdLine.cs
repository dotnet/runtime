// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
