// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.TypeInfos;
using System.Text;

using Internal.Reflection.Core.Execution;

using StructLayoutAttribute = System.Runtime.InteropServices.StructLayoutAttribute;

namespace System.Reflection.Runtime.TypeInfos
{
    //
    // TypeInfos that represent constructed generic types.
    //
    //
    internal sealed partial class RuntimeConstructedGenericTypeInfo : RuntimeTypeInfo, IKeyedItem<RuntimeConstructedGenericTypeInfo.UnificationKey>
    {
        private RuntimeConstructedGenericTypeInfo(UnificationKey key)
        {
            _key = key;
        }

        public override bool IsConstructedGenericType => true;
        public override bool IsByRefLike => GenericTypeDefinitionTypeInfo.IsByRefLike;

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


        public sealed override IEnumerable<CustomAttributeData> CustomAttributes
        {
            get
            {
                return GenericTypeDefinitionTypeInfo.CustomAttributes;
            }
        }

        public sealed override string FullName
        {
            get
            {
                // Desktop quirk: open constructions don't have "fullNames".
                if (ContainsGenericParameters)
                    return null;

                StringBuilder fullName = new StringBuilder();
                fullName.Append(GenericTypeDefinitionTypeInfo.FullName);
                fullName.Append('[');

                RuntimeTypeInfo[] genericTypeArguments = _key.GenericTypeArguments;
                for (int i = 0; i < genericTypeArguments.Length; i++)
                {
                    if (i != 0)
                        fullName.Append(',');

                    fullName.Append('[');
                    fullName.Append(genericTypeArguments[i].AssemblyQualifiedName);
                    fullName.Append(']');
                }
                fullName.Append(']');
                return fullName.ToString();
            }
        }

        public sealed override Type GetGenericTypeDefinition()
        {
            return GenericTypeDefinitionTypeInfo.ToType();
        }

        public sealed override Guid GUID
        {
            get
            {
                return GenericTypeDefinitionTypeInfo.GUID;
            }
        }

        public sealed override Assembly Assembly
        {
            get
            {
                return GenericTypeDefinitionTypeInfo.Assembly;
            }
        }

        public sealed override bool ContainsGenericParameters
        {
            get
            {
                foreach (RuntimeTypeInfo typeArgument in _key.GenericTypeArguments)
                {
                    if (typeArgument.ContainsGenericParameters)
                        return true;
                }
                return false;
            }
        }

        public sealed override bool HasSameMetadataDefinitionAs(MemberInfo other)
        {
            return GenericTypeDefinitionTypeInfo.HasSameMetadataDefinitionAs(other);
        }

        public sealed override string Namespace
        {
            get
            {
                return GenericTypeDefinitionTypeInfo.Namespace;
            }
        }

        public sealed override StructLayoutAttribute StructLayoutAttribute
        {
            get
            {
                return GenericTypeDefinitionTypeInfo.StructLayoutAttribute;
            }
        }

        public sealed override int MetadataToken
        {
            get
            {
                return GenericTypeDefinitionTypeInfo.MetadataToken;
            }
        }

        public sealed override string ToString()
        {
            // Now, append the generic type arguments.
            StringBuilder sb = new StringBuilder();
            sb.Append(GenericTypeDefinitionTypeInfo.FullName);
            sb.Append('[');
            RuntimeTypeInfo[] genericTypeArguments = _key.GenericTypeArguments;
            for (int i = 0; i < genericTypeArguments.Length; i++)
            {
                if (i != 0)
                    sb.Append(',');
                sb.Append(genericTypeArguments[i].ToString());
            }
            sb.Append(']');
            return sb.ToString();
        }

        public sealed override TypeAttributes Attributes => GenericTypeDefinitionTypeInfo.Attributes;

        public sealed override int GetHashCode()
        {
            return _key.GetHashCode();
        }

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
                return (RuntimeNamedTypeInfo)GenericTypeDefinitionTypeInfo;
            }
        }

        internal sealed override RuntimeTypeInfo InternalDeclaringType
        {
            get
            {
                return GenericTypeDefinitionTypeInfo.InternalDeclaringType;
            }
        }

        internal sealed override string InternalFullNameOfAssembly
        {
            get
            {
                return GenericTypeDefinitionTypeInfo.InternalFullNameOfAssembly;
            }
        }

        public sealed override string Name
        {
            get
            {
                return GenericTypeDefinitionTypeInfo.Name;
            }
        }

        internal sealed override RuntimeTypeInfo[] InternalRuntimeGenericTypeArguments
        {
            get
            {
                return _key.GenericTypeArguments;
            }
        }

        internal sealed override RuntimeTypeHandle InternalTypeHandleIfAvailable
        {
            get
            {
                return _key.TypeHandle;
            }
        }

        //
        // Returns the base type as a typeDef, Ref, or Spec. Default behavior is to QTypeDefRefOrSpec.Null, which causes BaseType to return null.
        //
        internal sealed override QTypeDefRefOrSpec TypeRefDefOrSpecForBaseType
        {
            get
            {
                return this.GenericTypeDefinitionTypeInfo.TypeRefDefOrSpecForBaseType;
            }
        }

        //
        // Returns the *directly implemented* interfaces as typedefs, specs or refs. ImplementedInterfaces will take care of the transitive closure and
        // insertion of the TypeContext.
        //
        internal sealed override QTypeDefRefOrSpec[] TypeRefDefOrSpecsForDirectlyImplementedInterfaces
        {
            get
            {
                return this.GenericTypeDefinitionTypeInfo.TypeRefDefOrSpecsForDirectlyImplementedInterfaces;
            }
        }

        //
        // Returns the generic parameter substitutions to use when enumerating declared members, base class and implemented interfaces.
        //
        internal sealed override TypeContext TypeContext
        {
            get
            {
                return new TypeContext(this.InternalRuntimeGenericTypeArguments, null);
            }
        }

        private RuntimeTypeInfo GenericTypeDefinitionTypeInfo
        {
            get
            {
                return _key.GenericTypeDefinition;
            }
        }

        private readonly UnificationKey _key;
    }
}
