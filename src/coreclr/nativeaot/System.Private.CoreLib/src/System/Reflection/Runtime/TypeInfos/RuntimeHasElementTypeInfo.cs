// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.TypeInfos;
using System.Runtime.InteropServices;

namespace System.Reflection.Runtime.TypeInfos
{
    //
    // The runtime's implementation of TypeInfo's for the "HasElement" subclass of types.
    //
    internal abstract partial class RuntimeHasElementTypeInfo : RuntimeTypeInfo, IKeyedItem<RuntimeHasElementTypeInfo.UnificationKey>, IRuntimeMemberInfoWithNoMetadataDefinition
    {
        protected RuntimeHasElementTypeInfo(UnificationKey key)
            : base()
        {
            _key = key;
        }

        public sealed override bool HasElementType => true;

        //
        // Implements IKeyedItem.Key.
        //
        // Produce the key. This is a high-traffic property and is called while the hash table's lock is held. Thus, it should
        // return a precomputed stored value and refrain from invoking other methods.
        //
        public UnificationKey Key
        {
            get
            {
                return _key;
            }
        }

        public sealed override Assembly Assembly
        {
            get
            {
                return _key.ElementType.Assembly;
            }
        }

        public sealed override IEnumerable<CustomAttributeData> CustomAttributes
        {
            get
            {
                return Array.Empty<CustomAttributeData>();
            }
        }

        public sealed override bool ContainsGenericParameters
        {
            get
            {
                return _key.ElementType.ContainsGenericParameters;
            }
        }

        public sealed override string FullName
        {
            get
            {
                string elementFullName = _key.ElementType.FullName;
                if (elementFullName == null)
                    return null;
                return elementFullName + Suffix;
            }
        }

        public sealed override bool HasSameMetadataDefinitionAs(MemberInfo other)
        {
            ArgumentNullException.ThrowIfNull(other);

            // This logic is written to match CoreCLR's behavior.
            return other is RuntimeType runtimeType && runtimeType.GetRuntimeTypeInfo() is IRuntimeMemberInfoWithNoMetadataDefinition;
        }

        public sealed override string Namespace
        {
            get
            {
                return _key.ElementType.Namespace;
            }
        }

        public sealed override StructLayoutAttribute StructLayoutAttribute
        {
            get
            {
                return null;
            }
        }

        public sealed override string ToString()
        {
            return _key.ElementType.ToString() + Suffix;
        }

        public sealed override int MetadataToken
        {
            get
            {
                return 0x02000000; // nil TypeDef token
            }
        }

        //
        // Left unsealed because this implementation is correct for ByRefs and Pointers but not Arrays.
        //
        public override TypeAttributes Attributes
        {
            get
            {
                Debug.Assert(IsByRef || IsPointer);
                return TypeAttributes.Public;
            }
        }

        public sealed override int GetHashCode()
        {
            return _key.ElementType.GetHashCode();
        }

        internal sealed override RuntimeTypeInfo InternalDeclaringType
        {
            get
            {
                return null;
            }
        }

        public sealed override string Name
        {
            get
            {
                return _key.ElementType.Name + Suffix;
            }
        }

        internal sealed override string InternalFullNameOfAssembly
        {
            get
            {
                return _key.ElementType.InternalFullNameOfAssembly;
            }
        }

        internal sealed override RuntimeTypeInfo InternalRuntimeElementType
        {
            get
            {
                return _key.ElementType;
            }
        }

        internal sealed override RuntimeTypeHandle InternalTypeHandleIfAvailable
        {
            get
            {
                return _key.TypeHandle;
            }
        }

        protected abstract string Suffix { get; }

        private readonly UnificationKey _key;
    }
}
