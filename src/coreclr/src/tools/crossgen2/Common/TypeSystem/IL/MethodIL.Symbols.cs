// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

using Internal.TypeSystem;

namespace Internal.IL
{
    partial class MethodIL
    {
        public virtual MethodDebugInformation GetDebugInfo()
        {
            return MethodDebugInformation.None;
        }
    }

    partial class InstantiatedMethodIL
    {
        public override MethodDebugInformation GetDebugInfo()
        {
            return _methodIL.GetDebugInfo();
        }
    }

    /// <summary>
    /// Represents debug information attached to a <see cref="MethodIL"/>.
    /// </summary>
    public class MethodDebugInformation
    {
        public static MethodDebugInformation None = new MethodDebugInformation();

        public virtual IEnumerable<ILSequencePoint> GetSequencePoints()
        {
            return null;
        }

        public virtual IEnumerable<ILLocalVariable> GetLocalVariables()
        {
            return null;
        }

        public virtual IEnumerable<string> GetParameterNames()
        {
            return null;
        }
    }

    /// <summary>
    /// Represents a sequence point within an IL method body.
    /// Sequence point describes a point in the method body at which all side effects of
    /// previous evaluations have been performed.
    /// </summary>
    public struct ILSequencePoint
    {
        public readonly int Offset;
        public readonly string Document;
        public readonly int LineNumber;
        // TODO: The remaining info

        public ILSequencePoint(int offset, string document, int lineNumber)
        {
            Offset = offset;
            Document = document;
            LineNumber = lineNumber;
        }
    }

    /// <summary>
    /// Represents information about a local variable within a method body.
    /// </summary>
    public struct ILLocalVariable
    {
        public readonly int Slot;
        public readonly string Name;
        public readonly bool CompilerGenerated;

        public ILLocalVariable(int slot, string name, bool compilerGenerated)
        {
            Slot = slot;
            Name = name;
            CompilerGenerated = compilerGenerated;
        }
    }
}
