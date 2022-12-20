// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;

using System.Reflection.Runtime.General;
using System.Reflection.Runtime.TypeInfos;
using System.Reflection.Runtime.Assemblies;

namespace System.Reflection.Runtime.TypeParsing
{
    //
    // The TypeName class is the base class for a family of types that represent the nodes in a parse tree for
    // assembly-qualified type names.
    //
    internal abstract class TypeName
    {
        /// <summary>
        /// Helper for the Type.GetType() family of apis. "containingAssemblyIsAny" is the assembly to search for (as determined
        /// by a qualifying assembly string in the original type string passed to Type.GetType(). If null, it means the type stream
        /// didn't specify an assembly name. How to respond to that is up to the type resolver delegate in getTypeOptions - this class
        /// is just a middleman.
        /// </summary>
        public abstract Type ResolveType(Assembly containingAssemblyIfAny, GetTypeOptions getTypeOptions);
        public abstract override string ToString();
    }

    //
    // Represents a parse of a type name qualified by an assembly name.
    //
    internal sealed class AssemblyQualifiedTypeName : TypeName
    {
        public AssemblyQualifiedTypeName(NonQualifiedTypeName nonQualifiedTypeName, RuntimeAssemblyName assemblyName)
        {
            Debug.Assert(nonQualifiedTypeName != null);
            Debug.Assert(assemblyName != null);
            _nonQualifiedTypeName = nonQualifiedTypeName;
            _assemblyName = assemblyName;
        }

        public sealed override string ToString()
        {
            return _nonQualifiedTypeName.ToString() + ", " + _assemblyName.FullName;
        }

        public sealed override Type ResolveType(Assembly containingAssemblyIfAny, GetTypeOptions getTypeOptions)
        {
            containingAssemblyIfAny = getTypeOptions.CoreResolveAssembly(_assemblyName);
            if (containingAssemblyIfAny == null)
                return null;
            return _nonQualifiedTypeName.ResolveType(containingAssemblyIfAny, getTypeOptions);
        }

        private readonly RuntimeAssemblyName _assemblyName;
        private readonly NonQualifiedTypeName _nonQualifiedTypeName;
    }

    //
    // Base class for all non-assembly-qualified type names.
    //
    internal abstract class NonQualifiedTypeName : TypeName
    {
    }

    //
    // Base class for namespace or nested type.
    //
    internal abstract class NamedTypeName : NonQualifiedTypeName
    {
    }

    //
    // Non-nested named type. The full name is the namespace-qualified name. For example, the FullName for
    // System.Collections.Generic.IList<> is "System.Collections.Generic.IList`1".
    //
    internal sealed partial class NamespaceTypeName : NamedTypeName
    {
        public NamespaceTypeName(string fullName)
        {
            _fullName = fullName;
        }

        public sealed override Type ResolveType(Assembly containingAssemblyIfAny, GetTypeOptions getTypeOptions)
        {
            return getTypeOptions.CoreResolveType(containingAssemblyIfAny, _fullName);
        }

        public sealed override string ToString()
        {
            return _fullName.EscapeTypeNameIdentifier();
        }

        private readonly string _fullName;
    }

    //
    // A nested type. The Name is the simple name of the type (not including any portion of its declaring type name.)
    //
    internal sealed class NestedTypeName : NamedTypeName
    {
        public NestedTypeName(string nestedTypeName, NamedTypeName declaringType)
        {
            _nestedTypeName = nestedTypeName;
            _declaringType = declaringType;
        }

        public sealed override string ToString()
        {
            return _declaringType + "+" + _nestedTypeName.EscapeTypeNameIdentifier();
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern",
            Justification = "Reflection implementation")]
        public sealed override Type ResolveType(Assembly containingAssemblyIfAny, GetTypeOptions getTypeOptions)
        {
            Type declaringType = _declaringType.ResolveType(containingAssemblyIfAny, getTypeOptions);
            if (declaringType == null)
                return null;

            // Desktop compat note: If there is more than one nested type that matches the name in a case-blind match,
            // we might not return the same one that the desktop returns. The actual selection method is influenced both by the type's
            // placement in the IL and the implementation details of the CLR's internal hashtables so it would be very
            // hard to replicate here.
            //
            // Desktop compat note #2: Case-insensitive lookups: If we don't find a match, we do *not* go back and search
            // other declaring types that might match the case-insensitive search and contain the nested type being sought.
            // Though this is somewhat unsatisfactory, the desktop CLR has the same limitation.

            // Don't change these flags - we may be talking to a third party type here and we need to invoke it the way CoreClr does.
            BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic;
            Type? nestedType;
            if (!getTypeOptions.IgnoreCase)
            {
                nestedType = declaringType.GetNestedType(_nestedTypeName, bf);
            }
            else
            {
                // Return the first name that matches. Which one gets returned on a multiple match is an implementation detail.
                // Unfortunately, compat prevents us from just throwing AmbiguousMatchException.
                nestedType = null;
                string lowerNestedTypeName = _nestedTypeName.ToLowerInvariant(); //@todo: Once String.Equals() works with StringComparison.InvariantIgnoreCase, it would be better to use that.
                foreach (Type nt in declaringType.GetNestedTypes(bf))
                {
                    if (nt.Name.ToLowerInvariant() == lowerNestedTypeName)
                    {
                        nestedType = nt;
                        break;
                    }
                }
            }
            if (nestedType == null && getTypeOptions.ThrowOnError)
                throw Helpers.CreateTypeLoadException(ToString(), containingAssemblyIfAny);
            return nestedType;
        }

        private readonly string _nestedTypeName;
        private readonly NamedTypeName _declaringType;
    }

    //
    // Abstract base for array, byref and pointer type names.
    //
    internal abstract class HasElementTypeName : NonQualifiedTypeName
    {
        public HasElementTypeName(TypeName elementTypeName)
        {
            ElementTypeName = elementTypeName;
        }

        protected TypeName ElementTypeName { get; }
    }

    //
    // A single-dimensional zero-lower-bound array type name.
    //
    internal sealed class ArrayTypeName : HasElementTypeName
    {
        public ArrayTypeName(TypeName elementTypeName)
            : base(elementTypeName)
        {
        }

        public sealed override string ToString()
        {
            return ElementTypeName + "[]";
        }

        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:AotUnfriendlyApi",
            Justification = "Used to implement resolving types from strings.")]
        public sealed override Type ResolveType(Assembly containingAssemblyIfAny, GetTypeOptions getTypeOptions)
        {
            return ElementTypeName.ResolveType(containingAssemblyIfAny, getTypeOptions)?.MakeArrayType();
        }
    }

    //
    // A multidim array type name.
    //
    internal sealed class MultiDimArrayTypeName : HasElementTypeName
    {
        public MultiDimArrayTypeName(TypeName elementTypeName, int rank)
            : base(elementTypeName)
        {
            _rank = rank;
        }

        public sealed override string ToString()
        {
            return ElementTypeName + "[" + (_rank == 1 ? "*" : new string(',', _rank - 1)) + "]";
        }

        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:AotUnfriendlyApi",
            Justification = "Used to implement resolving types from strings.")]
        public sealed override Type ResolveType(Assembly containingAssemblyIfAny, GetTypeOptions getTypeOptions)
        {
            return ElementTypeName.ResolveType(containingAssemblyIfAny, getTypeOptions)?.MakeArrayType(_rank);
        }

        private readonly int _rank;
    }

    //
    // A byref type.
    //
    internal sealed class ByRefTypeName : HasElementTypeName
    {
        public ByRefTypeName(TypeName elementTypeName)
            : base(elementTypeName)
        {
        }

        public sealed override string ToString()
        {
            return ElementTypeName + "&";
        }

        public sealed override Type ResolveType(Assembly containingAssemblyIfAny, GetTypeOptions getTypeOptions)
        {
            return ElementTypeName.ResolveType(containingAssemblyIfAny, getTypeOptions)?.MakeByRefType();
        }
    }

    //
    // A pointer type.
    //
    internal sealed class PointerTypeName : HasElementTypeName
    {
        public PointerTypeName(TypeName elementTypeName)
            : base(elementTypeName)
        {
        }

        public sealed override string ToString()
        {
            return ElementTypeName + "*";
        }

        public sealed override Type ResolveType(Assembly containingAssemblyIfAny, GetTypeOptions getTypeOptions)
        {
            return ElementTypeName.ResolveType(containingAssemblyIfAny, getTypeOptions)?.MakePointerType();
        }
    }

    //
    // A constructed generic type.
    //
    internal sealed class ConstructedGenericTypeName : NonQualifiedTypeName
    {
        public ConstructedGenericTypeName(NamedTypeName genericTypeDefinition, IList<TypeName> genericTypeArguments)
        {
            _genericTypeDefinition = genericTypeDefinition;
            _genericTypeArguments = genericTypeArguments;
        }

        public sealed override string ToString()
        {
            string s = _genericTypeDefinition.ToString();
            s += "[";
            string sep = "";
            foreach (TypeName genericTypeArgument in _genericTypeArguments)
            {
                s += sep;
                sep = ",";
                if (genericTypeArgument is AssemblyQualifiedTypeName)
                    s += "[" + genericTypeArgument.ToString() + "]";
                else
                    s += genericTypeArgument.ToString();
            }
            s += "]";
            return s;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2055:RequiresUnreferencedCode",
            Justification = "Used to implement resolving types from strings.")]
        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:AotUnfriendlyApi",
            Justification = "Used to implement resolving types from strings.")]
        public sealed override Type ResolveType(Assembly containingAssemblyIfAny, GetTypeOptions getTypeOptions)
        {
            Type genericTypeDefinition = _genericTypeDefinition.ResolveType(containingAssemblyIfAny, getTypeOptions);
            if (genericTypeDefinition == null)
                return null;

            int numGenericArguments = _genericTypeArguments.Count;
            Type[] genericArgumentTypes = new Type[numGenericArguments];
            for (int i = 0; i < numGenericArguments; i++)
            {
                // Do not pass containingAssemblyIfAny down to ResolveType for the generic type arguments.
                if ((genericArgumentTypes[i] = _genericTypeArguments[i].ResolveType(null, getTypeOptions)) == null)
                    return null;
            }
            return genericTypeDefinition.MakeGenericType(genericArgumentTypes);
        }

        private readonly NamedTypeName _genericTypeDefinition;
        private readonly IList<TypeName> _genericTypeArguments;
    }
}
