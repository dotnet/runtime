// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.MethodInfos;
using System.Runtime.InteropServices;

using Internal.Reflection.Core.Execution;

namespace System.Reflection.Runtime.TypeInfos
{
    //
    // TypeInfos returned by the Type.GetTypeFromCLSID() api. These "types" are little more than mules that hold a CLSID
    // and optional remote server name. The only useful thing to do with them is to pass them to Activator.CreateInstance().
    //
    internal sealed partial class RuntimeCLSIDTypeInfo : RuntimeTypeDefinitionTypeInfo, IKeyedItem<RuntimeCLSIDTypeInfo.UnificationKey>
    {
        private RuntimeCLSIDTypeInfo(Guid clsid, string server)
        {
            _key = new UnificationKey(clsid, server);
            _constructors = new RuntimeConstructorInfo[] { RuntimeCLSIDNullaryConstructorInfo.GetRuntimeCLSIDNullaryConstructorInfo(this) };
        }

        public sealed override Assembly Assembly => BaseType.Assembly;
        public sealed override bool ContainsGenericParameters => false;
        public sealed override string FullName => BaseType.FullName;
        public sealed override Guid GUID => _key.ClsId;
        internal sealed override string? InternalGetNameIfAvailable(ref Type? rootCauseForFailure) => BaseType.InternalGetNameIfAvailable(ref rootCauseForFailure);
        public sealed override bool IsGenericTypeDefinition => false;
        public sealed override int MetadataToken => BaseType.MetadataToken;
        public sealed override string Namespace => BaseType.Namespace;
        public sealed override StructLayoutAttribute StructLayoutAttribute => BaseType.StructLayoutAttribute;
        public sealed override string ToString() => BaseType.ToString();

        public sealed override IEnumerable<CustomAttributeData> CustomAttributes
        {
            get
            {
                return Array.Empty<CustomAttributeData>();
            }
        }

        public sealed override bool HasSameMetadataDefinitionAs(MemberInfo other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            // This logic is written to match CoreCLR's behavior.
            return other is RuntimeCLSIDTypeInfo;
        }

        protected sealed override TypeAttributes GetAttributeFlagsImpl() => TypeAttributes.Public;
        protected sealed override int InternalGetHashCode() => _key.GetHashCode();

        internal sealed override Type BaseTypeWithoutTheGenericParameterQuirk => typeof(object);
        internal sealed override bool CanBrowseWithoutMissingMetadataExceptions => BaseType.CastToRuntimeTypeInfo().CanBrowseWithoutMissingMetadataExceptions;
        internal sealed override Type InternalDeclaringType => null;
        internal sealed override string InternalFullNameOfAssembly => BaseType.Assembly.FullName;
        internal sealed override IEnumerable<RuntimeConstructorInfo> SyntheticConstructors => _constructors;

        // No RuntimeTypeHandle for this flavor of type. This does lead to the oddity that Activator.CreateInstance() returns an object whose GetType()
        // returns __ComObject rather than this specific type. But this has happened for years on the full framework without incident.
        internal sealed override RuntimeTypeHandle InternalTypeHandleIfAvailable => default(RuntimeTypeHandle);

        internal string Server => _key.Server;

        //
        // Implements IKeyedItem.PrepareKey.
        //
        // This method is the keyed item's chance to do any lazy evaluation needed to produce the key quickly.
        // Concurrent unifiers are guaranteed to invoke this method at least once and wait for it
        // to complete before invoking the Key property. The unifier lock is NOT held across the call.
        //
        // PrepareKey() must be idempodent and thread-safe. It may be invoked multiple times and concurrently.
        //
        void IKeyedItem<UnificationKey>.PrepareKey() { }

        //
        // Implements IKeyedItem.Key.
        //
        // Produce the key. This is a high-traffic property and is called while the hash table's lock is held. Thus, it should
        // return a precomputed stored value and refrain from invoking other methods. If the keyed item wishes to
        // do lazy evaluation of the key, it should do so in the PrepareKey() method.
        //
        UnificationKey IKeyedItem<UnificationKey>.Key => _key;

        private readonly UnificationKey _key;
        private readonly RuntimeConstructorInfo[] _constructors;
    }
}
