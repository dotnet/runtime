using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.InternalAbstractions;

namespace Microsoft.DotNet.Cli.Build
{
    public partial class DotNetCli
    {
        public static readonly DotNetCli Stage1 = new DotNetCli(Dirs.Stage1);
        public static readonly DotNetCli Stage2 = new DotNetCli(Dirs.Stage2);
    }
}
