// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem
{
    public abstract partial class MetadataTypeSystemContext : TypeSystemContext
    {
        private static readonly (string Namespace, string TypeName)[] s_wellKnownTypeNames =[
            ("System", "Void"),

            ("System", "Boolean"),
            ("System", "Char"),
            ("System", "SByte"),
            ("System", "Byte"),
            ("System", "Int16"),
            ("System", "UInt16"),
            ("System", "Int32"),
            ("System", "UInt32"),
            ("System", "Int64"),
            ("System", "UInt64"),
            ("System", "IntPtr"),
            ("System", "UIntPtr"),
            ("System", "Single"),
            ("System", "Double"),

            ("System", "ValueType"),
            ("System", "Enum"),
            ("System", "Nullable`1"),

            ("System", "Object"),
            ("System", "String"),
            ("System", "Array"),
            ("System", "MulticastDelegate"),

            ("System", "RuntimeTypeHandle"),
            ("System", "RuntimeMethodHandle"),
            ("System", "RuntimeFieldHandle"),

            ("System", "Exception"),

            ("System", "TypedReference"),

            ("System", "SZArrayHelper"),
            ("System.Collections.Generic", "IEnumerable`1"),
            ("System.Collections.Generic", "IList`1"),
            ("System.Collections.Generic", "ICollection`1"),
            ("System.Collections.Generic", "IReadOnlyList`1"),
            ("System.Collections.Generic", "IReadOnlyCollection`1"),
        ];

        public static IEnumerable<(string Namespace, string TypeName)> WellKnownTypeNames => s_wellKnownTypeNames;

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
            Debug.Assert(s_wellKnownTypeNames[(int)WellKnownType.MulticastDelegate - 1] == ("System", "MulticastDelegate"));

            _wellKnownTypes = new MetadataType[s_wellKnownTypeNames.Length];

            // Initialize all well known types - it will save us from checking the name for each loaded type
            for (int typeIndex = 0; typeIndex < _wellKnownTypes.Length; typeIndex++)
            {
                // Require System.Object to be present as a minimal sanity check.
                // The set of required well-known types is not strictly defined since different .NET profiles implement different subsets.
                MetadataType type = systemModule.GetType(
                    System.Text.Encoding.UTF8.GetBytes(s_wellKnownTypeNames[typeIndex].Namespace),
                    System.Text.Encoding.UTF8.GetBytes(s_wellKnownTypeNames[typeIndex].TypeName),
                    throwIfNotFound: typeIndex == (int)WellKnownType.Object);
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
                ThrowHelper.ThrowTypeLoadException(s_wellKnownTypeNames[typeIndex].Namespace, s_wellKnownTypeNames[typeIndex].TypeName, SystemModule);

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
                && t.Name == "IDynamicInterfaceCastable"u8
                && t.Namespace == "System.Runtime.InteropServices"u8;
        }
    }
}
