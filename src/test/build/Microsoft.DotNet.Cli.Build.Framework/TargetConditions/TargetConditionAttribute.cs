using System;

namespace Microsoft.DotNet.Cli.Build.Framework
{
    public abstract class TargetConditionAttribute : Attribute
    {
        public abstract bool EvaluateCondition();
    }
}