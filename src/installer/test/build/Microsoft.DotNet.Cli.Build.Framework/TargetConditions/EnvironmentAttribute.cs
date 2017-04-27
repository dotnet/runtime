using System;
using System.Linq;

namespace Microsoft.DotNet.Cli.Build.Framework
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class EnvironmentAttribute : TargetConditionAttribute
    {
        private string _envVar;
        private string[] _expectedVals;

        public EnvironmentAttribute(string envVar, params string[] expectedVals)
        {
            if (string.IsNullOrEmpty(envVar))
            {
                throw new ArgumentNullException(nameof(envVar));
            }
            if (expectedVals == null)
            {
                throw new ArgumentNullException(nameof(expectedVals));
            }

            _envVar = envVar;
            _expectedVals = expectedVals;
        }

        public override bool EvaluateCondition()
        {
            var actualVal = Environment.GetEnvironmentVariable(_envVar);

            if (_expectedVals.Any())
            {
                return _expectedVals.Any(ev => string.Equals(actualVal, ev, StringComparison.Ordinal));
            }
            else
            {
                return !string.IsNullOrEmpty(actualVal);
            }
        }
    }
}
