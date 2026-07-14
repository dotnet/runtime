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
        private readonly MethodStackTraceVisibilityFlags _flags;
        private MetadataType _iAsyncStateMachineType;
        private bool _iAsyncStateMachineTypeComputed;

        public EcmaMethodStackTraceEmissionPolicy(bool includeLineNumbers)
        {
            _flags = includeLineNumbers ? MethodStackTraceVisibilityFlags.HasLineNumbers : 0;
        }

        public override MethodStackTraceVisibilityFlags GetMethodVisibility(MethodDesc method)
        {
            MethodStackTraceVisibilityFlags result = _flags;

            if (method.HasCustomAttribute("System.Diagnostics", "StackTraceHiddenAttribute")
                || (method.OwningType is MetadataType mdType && mdType.HasCustomAttribute("System.Diagnostics", "StackTraceHiddenAttribute"))
                || (method is Internal.IL.Stubs.ILStubMethod) || method.IsAsyncThunk()) // see MethodDesc::IsDiagnosticsHidden() in src/coreclr/vm/method.inl
            {
                result |= MethodStackTraceVisibilityFlags.IsHidden;
            }

            if (IsAsyncFrame(method))
            {
                result |= MethodStackTraceVisibilityFlags.IsAsync;
            }

            return (method.GetTypicalMethodDefinition() is Internal.TypeSystem.Ecma.EcmaMethod || (method.IsAsync && method.IsAsyncCall()))
                ? result | MethodStackTraceVisibilityFlags.HasMetadata
                : result;
        }

        // Determines whether a frame for this method should be treated as "async" when formatting a
        // stack trace. Async frames suppress the "--- End of stack trace from previous location ---"
        // delimiter. This covers both runtime-async (V2) methods and the MoveNext method of a
        // compiler-generated (V1) async state machine.
        private bool IsAsyncFrame(MethodDesc method)
        {
            if (method.IsAsync)
            {
                return true;
            }

            // Async state machines only expose their exception-throwing code through IAsyncStateMachine.MoveNext.
            if (method.Name != "MoveNext"u8)
            {
                return false;
            }

            if (!_iAsyncStateMachineTypeComputed)
            {
                _iAsyncStateMachineType = method.Context.SystemModule.GetType("System.Runtime.CompilerServices"u8, "IAsyncStateMachine"u8, throwIfNotFound: false);
                _iAsyncStateMachineTypeComputed = true;
            }

            if (_iAsyncStateMachineType == null)
            {
                return false;
            }

            foreach (DefType interfaceType in method.OwningType.RuntimeInterfaces)
            {
                if (interfaceType == _iAsyncStateMachineType)
                {
                    return true;
                }
            }

            return false;
        }
    }

    [Flags]
    public enum MethodStackTraceVisibilityFlags
    {
        HasMetadata = 0x1,
        IsHidden = 0x2,
        HasLineNumbers = 0x4,
        IsAsync = 0x8,
    }
}
