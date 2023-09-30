// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Internal.TypeSystem;

namespace ILCompiler
{
    /// <summary>
    /// Represents a stack trace emission policy.
    /// </summary>
    public abstract class StackTraceEmissionPolicy
    {
        public abstract MethodStackTraceVisibilityFlags GetMethodVisibility(MethodDesc method);
    }

    public class NoStackTraceEmissionPolicy : StackTraceEmissionPolicy
    {
        public override MethodStackTraceVisibilityFlags GetMethodVisibility(MethodDesc method)
        {
            return default;
        }
    }

    /// <summary>
    /// Stack trace emission policy that ensures presence of stack trace metadata for all
    /// <see cref="Internal.TypeSystem.Ecma.EcmaMethod"/>-based methods.
    /// </summary>
    public class EcmaMethodStackTraceEmissionPolicy : StackTraceEmissionPolicy
    {
        public override MethodStackTraceVisibilityFlags GetMethodVisibility(MethodDesc method)
        {
            MethodStackTraceVisibilityFlags result = 0;

            if (method.HasCustomAttribute("System.Diagnostics", "StackTraceHiddenAttribute")
                || (method.OwningType is MetadataType mdType && mdType.HasCustomAttribute("System.Diagnostics", "StackTraceHiddenAttribute")))
            {
                result |= MethodStackTraceVisibilityFlags.IsHidden;
            }

            return method.GetTypicalMethodDefinition() is Internal.TypeSystem.Ecma.EcmaMethod
                ? result | MethodStackTraceVisibilityFlags.HasMetadata
                : result;
        }
    }

    [Flags]
    public enum MethodStackTraceVisibilityFlags
    {
        HasMetadata = 0x1,
        IsHidden = 0x2,
    }
}
