using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
    [AttributeUsage (AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class LogContainsAttribute : EnableLoggerAttribute
    {
        public LogContainsAttribute (string message)
        {
            if (string.IsNullOrEmpty (message))
                throw new ArgumentException ("Value cannot be null or empty.", nameof (message));
        }
    }
}