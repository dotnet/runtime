// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.CustomAttributes;

#pragma warning disable CA1067 // override Equals because it implements IEquatable<T>

namespace System.Reflection.Runtime.TypeInfos
{
    //
    // TypeInfos that represent type definitions (i.e. Foo or Foo<>, but not Foo<int> or arrays/pointers/byrefs.)
    //
    //
    internal abstract partial class RuntimeNamedTypeInfo : RuntimeTypeDefinitionTypeInfo, IEquatable<RuntimeNamedTypeInfo>
    {
        protected RuntimeNamedTypeInfo(RuntimeTypeHandle typeHandle)
        {
            _typeHandle = typeHandle;
        }

        public sealed override bool ContainsGenericParameters
        {
            get
            {
                return IsGenericTypeDefinition;
            }
        }

        public sealed override IEnumerable<CustomAttributeData> CustomAttributes
        {
            get
            {
                foreach (CustomAttributeData cad in TrueCustomAttributes)
                    yield return cad;

                TypeAttributes attributes = Attributes;
                if (0 != (attributes & TypeAttributes.Import))
                    yield return new RuntimePseudoCustomAttributeData(typeof(ComImportAttribute), null);

                if (0 != (attributes & TypeAttributes.Serializable))
                    yield return new RuntimePseudoCustomAttributeData(typeof(SerializableAttribute), null);
            }
        }

        public bool Equals(RuntimeNamedTypeInfo? other)
        {
            // RuntimeTypeInfo.Equals(object) is the one that encapsulates our unification strategy so defer to him.
            object? otherAsObject = other;
            return base.Equals(otherAsObject);
        }

        /// <summary>
        /// Override this function to read the Guid attribute from a type's metadata. If the attribute
        /// is not present, or isn't parseable, return null. Should be overridden by metadata specific logic
        /// </summary>
        protected abstract Guid? ComputeGuidFromCustomAttributes();

        public sealed override Guid GUID
        {
            get
            {
                Guid? guidFromAttributes = ComputeGuidFromCustomAttributes();
                if (guidFromAttributes.HasValue)
                    return guidFromAttributes.Value;

                //
                // If we got here, there was no [Guid] attribute.
                //
                // Ideally, we'd now compute the same GUID the desktop returns - however, that algorithm is complex and has questionable dependencies
                // (in particular, the GUID changes if the language compilers ever change the way it emits metadata tokens into certain unordered lists.
                // We don't even retain that order across the Project N toolchain.)
                //
                // For now, this is a compromise that satisfies our app-compat goals. We ensure that each unique Type receives a different GUID (at least one app
                // uses the GUID as a dictionary key to look up types.) It will not be the same GUID on multiple runs of the app but so far, there's
                // no evidence that's needed.
                //
                return s_namedTypeToGuidTable.GetOrAdd(this).Item1;
            }
        }

        public sealed override string FullName
        {
            get
            {
                Debug.Assert(!IsConstructedGenericType);
                Debug.Assert(!IsGenericParameter);
                Debug.Assert(!HasElementType);

                string name = Name;

                Type? declaringType = this.DeclaringType;
                if (declaringType is not null)
                {
                    string? declaringTypeFullName = declaringType.FullName;
                    return declaringTypeFullName + "+" + name;
                }

                string ns = Namespace;
                if (ns == null)
                    return name;
                return ns + "." + name;
            }
        }

#if DEBUG
        public sealed override bool HasSameMetadataDefinitionAs(MemberInfo other) => base.HasSameMetadataDefinitionAs(other);
#endif

        protected abstract void GetPackSizeAndSize(out int packSize, out int size);

        public sealed override StructLayoutAttribute StructLayoutAttribute
        {
            get
            {
                // Note: CoreClr checks HasElementType and IsGenericParameter in addition to IsInterface but those properties cannot be true here as this
                // RuntimeTypeInfo subclass is solely for TypeDef types.)
                if (IsInterface)
                    return null;

                TypeAttributes attributes = Attributes;

                LayoutKind layoutKind;
                switch (attributes & TypeAttributes.LayoutMask)
                {
                    case TypeAttributes.ExplicitLayout: layoutKind = LayoutKind.Explicit; break;
                    case TypeAttributes.AutoLayout: layoutKind = LayoutKind.Auto; break;
                    case TypeAttributes.SequentialLayout: layoutKind = LayoutKind.Sequential; break;
                    default: layoutKind = LayoutKind.Auto;  break;
                }

                CharSet charSet;
                switch (attributes & TypeAttributes.StringFormatMask)
                {
                    case TypeAttributes.AnsiClass: charSet = CharSet.Ansi; break;
                    case TypeAttributes.AutoClass: charSet = CharSet.Auto; break;
                    case TypeAttributes.UnicodeClass: charSet = CharSet.Unicode; break;
                    default: charSet = CharSet.None;  break;
                }

                GetPackSizeAndSize(out int pack, out int size);

                return new StructLayoutAttribute(layoutKind)
                {
                    CharSet = charSet,
                    Pack = pack,
                    Size = size,
                };
            }
        }

        protected abstract IEnumerable<CustomAttributeData> TrueCustomAttributes { get; }

        //
        // Returns the anchoring typedef that declares the members that this type wants returned by the Declared*** properties.
        // The Declared*** properties will project the anchoring typedef's members by overriding their DeclaringType property with "this"
        // and substituting the value of this.TypeContext into any generic parameters.
        //
        // Default implementation returns null which causes the Declared*** properties to return no members.
        //
        // Note that this does not apply to DeclaredNestedTypes. Nested types and their containers have completely separate generic instantiation environments
        // (despite what C# might lead you to think.) Constructed generic types return the exact same same nested types that its generic type definition does
        // - i.e. their DeclaringTypes refer back to the generic type definition, not the constructed generic type.)
        //
        // Note also that we cannot use this anchoring concept for base types because of generic parameters. Generic parameters return
        // baseclass and interfaces based on its constraints.
        //
        internal sealed override RuntimeNamedTypeInfo AnchoringTypeDefinitionForDeclaredMembers
        {
            get
            {
                return this;
            }
        }

        internal sealed override bool CanBrowseWithoutMissingMetadataExceptions => true;

        internal sealed override RuntimeTypeHandle InternalTypeHandleIfAvailable
        {
            get
            {
                return _typeHandle;
            }
        }

        //
        // Returns the generic parameter substitutions to use when enumerating declared members, base class and implemented interfaces.
        //
        internal sealed override TypeContext TypeContext
        {
            get
            {
                return new TypeContext(this.RuntimeGenericTypeParameters, null);
            }
        }

        /// <summary>
        /// QTypeDefRefOrSpec handle that can be used to re-acquire this type. Must be implemented
        /// for all metadata sourced type implementations.
        /// </summary>
        internal abstract QTypeDefRefOrSpec TypeDefinitionQHandle { get; }

        private readonly RuntimeTypeHandle _typeHandle;

        private static readonly NamedTypeToGuidTable s_namedTypeToGuidTable = new NamedTypeToGuidTable();
        private sealed class NamedTypeToGuidTable : ConcurrentUnifier<RuntimeNamedTypeInfo, Tuple<Guid>>
        {
            protected sealed override Tuple<Guid> Factory(RuntimeNamedTypeInfo key)
            {
                return new Tuple<Guid>(Guid.NewGuid());
            }
        }
    }
}
