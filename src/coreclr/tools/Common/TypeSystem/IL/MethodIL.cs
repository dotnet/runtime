// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.TypeSystem;

namespace Internal.IL
{
    //
    // This duplicates types from System.Reflection.Metadata to avoid layering issues, and 
    // because of the System.Reflection.Metadata constructors are not public anyway.
    //

    public enum ILExceptionRegionKind
    {
        Catch = 0,
        Filter = 1,
        Finally = 2,
        Fault = 4,
    }

    public struct ILExceptionRegion
    {
        public readonly ILExceptionRegionKind Kind;
        public readonly int TryOffset;
        public readonly int TryLength;
        public readonly int HandlerOffset;
        public readonly int HandlerLength;
        public readonly int ClassToken;
        public readonly int FilterOffset;

        public ILExceptionRegion(
            ILExceptionRegionKind kind,
            int tryOffset,
            int tryLength,
            int handlerOffset,
            int handlerLength,
            int classToken,
            int filterOffset)
        {
            Kind = kind;
            TryOffset = tryOffset;
            TryLength = tryLength;
            HandlerOffset = handlerOffset;
            HandlerLength = handlerLength;
            ClassToken = classToken;
            FilterOffset = filterOffset;
        }
    }

    /// <summary>
    /// Represents a method body.
    /// </summary>
    [System.Diagnostics.DebuggerTypeProxy(typeof(MethodILDebugView))]
    public abstract partial class MethodIL
    {
        /// <summary>
        /// Gets the method whose body this <see cref="MethodIL"/> represents.
        /// </summary>
        public abstract MethodDesc OwningMethod { get; }

        /// <summary>
        /// Gets the maximum possible stack depth this method declares.
        /// </summary>
        public abstract int MaxStack { get; }

        /// <summary>
        /// Gets a value indicating whether the locals should be initialized to zero
        /// before first access.
        /// </summary>
        public abstract bool IsInitLocals { get; }

        /// <summary>
        /// Retrieves IL opcode bytes of this method body.
        /// </summary>
        public abstract byte[] GetILBytes();

        /// <summary>
        /// Gets the list of locals this method body defines.
        /// </summary>
        public abstract LocalVariableDefinition[] GetLocals();

        /// <summary>
        /// Resolves a token from within the method body into a type system object
        /// (typically a <see cref="MethodDesc"/>, <see cref="FieldDesc"/>, <see cref="TypeDesc"/>,
        /// or <see cref="MethodSignature"/>).
        /// </summary>
        public abstract Object GetObject(int token);

        /// <summary>
        /// Gets a list of exception regions this method body defines.
        /// </summary>
        public abstract ILExceptionRegion[] GetExceptionRegions();

        /// <summary>
        /// Gets the open (uninstantiated) version of the <see cref="MethodIL"/>.
        /// </summary>
        public virtual MethodIL GetMethodILDefinition()
        {
            return this;
        }

        public override string ToString()
        {
            return OwningMethod.ToString();
        }
    }
}
