// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace ILCompiler.DependencyAnalysis
{
    public enum SectionType
    {
        ReadOnly,
        Writeable,
        Executable
    }

    /// <summary>
    /// Specifies the object file section a node will be placed in; ie "text" or "data"
    /// </summary>
    public class ObjectNodeSection
    {
        public string Name { get; }
        public SectionType Type { get; }
        public string ComdatName { get; }

        public ObjectNodeSection(string name, SectionType type, string comdatName)
        {
            Name = name;
            Type = type;
            ComdatName = comdatName;
        }

        public ObjectNodeSection(string name, SectionType type) : this(name, type, null)
        { }

        /// <summary>
        /// Returns true if the section is a standard one (defined as text, data, or rdata currently)
        /// </summary>
        public bool IsStandardSection
        {
            get
            {
                return this == DataSection || this == ReadOnlyDataSection || this == FoldableReadOnlyDataSection || this == TextSection || this == XDataSection;
            }
        }
               
        public static readonly ObjectNodeSection XDataSection = new ObjectNodeSection("xdata", SectionType.ReadOnly);
        public static readonly ObjectNodeSection DataSection = new ObjectNodeSection("data", SectionType.Writeable);
        public static readonly ObjectNodeSection ReadOnlyDataSection = new ObjectNodeSection("rdata", SectionType.ReadOnly);
        public static readonly ObjectNodeSection FoldableReadOnlyDataSection = new ObjectNodeSection("rdata$F", SectionType.ReadOnly);
        public static readonly ObjectNodeSection TextSection = new ObjectNodeSection("text", SectionType.Executable);
        public static readonly ObjectNodeSection TLSSection = new ObjectNodeSection("TLS", SectionType.Writeable);
        public static readonly ObjectNodeSection ManagedCodeWindowsContentSection = new ObjectNodeSection(".managedcode$I", SectionType.Executable);
        public static readonly ObjectNodeSection FoldableManagedCodeWindowsContentSection = new ObjectNodeSection(".managedcode$I", SectionType.Executable);
        public static readonly ObjectNodeSection ManagedCodeUnixContentSection = new ObjectNodeSection("__managedcode", SectionType.Executable);
        public static readonly ObjectNodeSection FoldableManagedCodeUnixContentSection = new ObjectNodeSection("__managedcode", SectionType.Executable);
    }
}
