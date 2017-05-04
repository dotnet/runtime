using System;

namespace Microsoft.DotNet.Cli.Build.Framework
{
    public class UndefinedTargetException : Exception 
    { 
        public UndefinedTargetException(string message) : base(message) { }
    }
}