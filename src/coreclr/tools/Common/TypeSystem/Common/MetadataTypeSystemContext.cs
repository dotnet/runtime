// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem
{
    public abstract partial class MetadataTypeSystemContext : TypeSystemContext
    {
        private static readonly string[] s_wellKnownTypeNames = new string[] {
            "Void",

            "Boolean",
            "Char",
            "SByte",
            "Byte",
            "Int16",
            "UInt16",
            "Int32",
            "UInt32",
            "Int64",
            "UInt64",
            "IntPtr",
            "UIntPtr",
            "Single",
            "Double",

            "ValueType",
            "Enum",
            "Nullable`1",

            "Object",
            "String",
            "Array",
            "MulticastDelegate",

            "RuntimeTypeHandle",
            "RuntimeMethodHandle",
            "RuntimeFieldHandle",

            "Exception",

            "TypedReference",
        };

        public static IEnumerable<string> WellKnownTypeNames => s_wellKnownTypeNames;

        private MetadataType[] _wellKnownTypes;

        public MetadataTypeSystemContext()
        {
        }

        public MetadataTypeSystemContext(TargetDetails details)
            : base(details)
        {
        }

        public virtual void SetSystemModule(ModuleDesc systemModule)
        {
            InitializeSystemModule(systemModule);

            // Sanity check the name table
            Debug.Assert(s_wellKnownTypeNames[(int)WellKnownType.MulticastDelegate - 1] == "MulticastDelegate");

            _wellKnownTypes = new MetadataType[s_wellKnownTypeNames.Length];

            // Initialize all well known types - it will save us from checking the name for each loaded type
            for (int typeIndex = 0; typeIndex < _wellKnownTypes.Length; typeIndex++)
            {
                // Require System.Object to be present as a minimal sanity check.
                // The set of required well-known types is not strictly defined since different .NET profiles implement different subsets.
                MetadataType type = systemModule.GetType("System", s_wellKnownTypeNames[typeIndex], throwIfNotFound: typeIndex == (int)WellKnownType.Object);
                if (type != null)
                {
                    type.SetWellKnownType((WellKnownType)(typeIndex + 1));
                    _wellKnownTypes[typeIndex] = type;
                }
            }
        }

        public override DefType GetWellKnownType(WellKnownType wellKnownType, bool throwIfNotFound = true)
        {
            Debug.Assert(_wellKnownTypes != null, "Forgot to call SetSystemModule?");

            int typeIndex = (int)wellKnownType - 1;
            DefType type = _wellKnownTypes[typeIndex];
            if (type == null && throwIfNotFound)
                ThrowHelper.ThrowTypeLoadException("System", s_wellKnownTypeNames[typeIndex], SystemModule);

            return type;
        }

        protected internal sealed override bool ComputeHasStaticConstructor(TypeDesc type)
        {
            if (type is MetadataType)
            {
                return ((MetadataType)type).GetStaticConstructor() != null;
            }
            return false;
        }

        protected internal sealed override bool IsIDynamicInterfaceCastableInterface(DefType type)
        {
            MetadataType t = (MetadataType)type;
            return t.Module == SystemModule
                && t.Name == "IDynamicInterfaceCastable"
                && t.Namespace == "System.Runtime.InteropServices";
        }
    }
}
