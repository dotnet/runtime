// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace ILCompiler
{
    /// <summary>
    /// Represents a stack trace emission policy.
    /// </summary>
    public abstract class StackTraceEmissionPolicy
    {
        public abstract bool ShouldIncludeMethod(MethodDesc method);
    }

    public class NoStackTraceEmissionPolicy : StackTraceEmissionPolicy
    {
        public override bool ShouldIncludeMethod(MethodDesc method)
        {
            return false;
        }
    }

    /// <summary>
    /// Stack trace emission policy that ensures presence of stack trace metadata for all
    /// <see cref="Internal.TypeSystem.Ecma.EcmaMethod"/>-based methods.
    /// </summary>
    public class EcmaMethodStackTraceEmissionPolicy : StackTraceEmissionPolicy
    {
        public override bool ShouldIncludeMethod(MethodDesc method)
        {
            return method.GetTypicalMethodDefinition() is Internal.TypeSystem.Ecma.EcmaMethod;
        }
    }
}
