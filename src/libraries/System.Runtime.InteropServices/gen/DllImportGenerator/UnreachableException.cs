using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Interop
{
    /// <summary>
    /// An exception that should be thrown on code-paths that are unreachable.
    /// </summary>
    internal class UnreachableException : Exception
    {
    }
}
