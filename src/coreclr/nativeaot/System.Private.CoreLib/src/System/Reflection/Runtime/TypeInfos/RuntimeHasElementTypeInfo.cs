// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.TypeInfos;

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

        public sealed override bool IsTypeDefinition => false;
        public sealed override bool IsGenericTypeDefinition => false;
        protected sealed override bool HasElementTypeImpl() => true;
        protected abstract override bool IsArrayImpl();
        public abstract override bool IsSZArray { get; }
        public abstract override bool IsVariableBoundArray { get; }
        protected abstract override bool IsByRefImpl();
        protected abstract override bool IsPointerImpl();
        public sealed override bool IsConstructedGenericType => false;
        public sealed override bool IsGenericParameter => false;
        public sealed override bool IsGenericTypeParameter => false;
        public sealed override bool IsGenericMethodParameter => false;
        public sealed override bool IsByRefLike => false;

        //
        // Implements IKeyedItem.PrepareKey.
        //
        // This method is the keyed item's chance to do any lazy evaluation needed to produce the key quickly.
        // Concurrent unifiers are guaranteed to invoke this method at least once and wait for it
        // to complete before invoking the Key property. The unifier lock is NOT held across the call.
        //
        // PrepareKey() must be idempodent and thread-safe. It may be invoked multiple times and concurrently.
        //
        public void PrepareKey()
        {
        }

        //
        // Implements IKeyedItem.Key.
        //
        // Produce the key. This is a high-traffic property and is called while the hash table's lock is held. Thus, it should
        // return a precomputed stored value and refrain from invoking other methods. If the keyed item wishes to
        // do lazy evaluation of the key, it should do so in the PrepareKey() method.
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
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            // This logic is written to match CoreCLR's behavior.
            return other is Type && other is IRuntimeMemberInfoWithNoMetadataDefinition;
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
        protected override TypeAttributes GetAttributeFlagsImpl()
        {
            Debug.Assert(IsByRef || IsPointer);
            return TypeAttributes.Public;
        }

        protected sealed override int InternalGetHashCode()
        {
            return _key.ElementType.GetHashCode();
        }

        internal sealed override bool CanBrowseWithoutMissingMetadataExceptions => true;

        internal sealed override Type InternalDeclaringType
        {
            get
            {
                return null;
            }
        }

        internal sealed override string? InternalGetNameIfAvailable(ref Type? rootCauseForFailure)
        {
            string? elementTypeName = _key.ElementType.InternalGetNameIfAvailable(ref rootCauseForFailure);
            if (elementTypeName == null)
            {
                rootCauseForFailure = _key.ElementType;
                return null;
            }
            return elementTypeName + Suffix;
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
