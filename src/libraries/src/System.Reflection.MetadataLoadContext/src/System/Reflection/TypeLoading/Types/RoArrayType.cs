// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Reflection.TypeLoading
{
    /// <summary>
    /// All RoTypes that return true for IsArray. This includes both SZArrays and multi-dim arrays.
    /// </summary>
    internal sealed partial class RoArrayType : RoHasElementType
    {
        private readonly bool _multiDim;
        private readonly int _rank;

        internal RoArrayType(RoType elementType, bool multiDim, int rank)
            : base(elementType)
        {
            Debug.Assert(elementType != null);
            Debug.Assert(multiDim || rank == 1);

            _multiDim = multiDim;
            _rank = rank;
        }

        protected sealed override bool IsArrayImpl() => true;
        public sealed override bool IsSZArray => !_multiDim;
        public sealed override bool IsVariableBoundArray => _multiDim;
        protected sealed override bool IsByRefImpl() => false;
        protected sealed override bool IsPointerImpl() => false;

        protected sealed override string Suffix => Helpers.ComputeArraySuffix(_rank, _multiDim);

        public sealed override int GetArrayRank() => _rank;

        protected sealed override RoType ComputeBaseTypeWithoutDesktopQuirk() => Loader.GetCoreType(CoreType.Array);

        protected sealed override IEnumerable<RoType> ComputeDirectlyImplementedInterfaces()
        {
            if (_multiDim)
                yield break;

            RoType[] typeArguments = { GetRoElementType() };
            foreach (CoreType coreType in s_typesImplementedByArray)
            {
                RoType ifc = Loader.TryGetCoreType(coreType);
                if (ifc != null)
                {
                    // All of our types are from a fixed list so we know they're supposed be generic interfaces taking one type parameter.
                    // But since we're loading them from a core assembly that the user supplied us, we should verify and skip if 
                    // this is not the case.
                    if (ifc is RoDefinitionType roDefinitionType && roDefinitionType.GetGenericParameterCount() == 1)
                    {
                        yield return roDefinitionType.GetUniqueConstructedGenericType(typeArguments);
                    }
                }
            }
        }

        private static readonly CoreType[] s_typesImplementedByArray =
        {
            CoreType.IEnumerableT,
            CoreType.ICollectionT,
            CoreType.IListT,
            CoreType.IReadOnlyListT,
        };

        protected sealed override TypeAttributes ComputeAttributeFlags() => TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Serializable;

        internal sealed override IEnumerable<ConstructorInfo> GetConstructorsCore(NameFilter filter)
        {
            if (filter == null || filter.Matches(ConstructorInfo.ConstructorName))
            {
                int rank = _rank;
                bool multiDim = _multiDim;
                RoType systemInt32 = Loader.GetCoreType(CoreType.Int32);

                int uniquifier = 0;

                //
                // Expose a constructor that takes n Int32's (one for each dimension) and constructs a zero lower-bounded array. For example,
                //
                //   String[,]
                //
                // exposes
                //
                //   .ctor(int32, int32)
                //
                {
                    RoType[] parameterTypes = new RoType[rank];
                    for (int i = 0; i < rank; i++)
                    {
                        parameterTypes[i] = systemInt32;
                    }
                    yield return new RoSyntheticConstructor(this, uniquifier++, parameterTypes);
                }

                if (!multiDim)
                {
                    //
                    // Jagged arrays also expose constructors that take multiple indices and construct a jagged matrix. For example,
                    //
                    //   String[][][][]
                    //
                    // also exposes:
                    //
                    //   .ctor(int32, int32)
                    //   .ctor(int32, int32, int32)
                    //   .ctor(int32, int32, int32, int32)
                    //

                    int parameterCount = 2;
                    RoType elementType = GetRoElementType();
                    while (elementType.IsSZArray)
                    {
                        RoType[] parameterTypes = new RoType[parameterCount];
                        for (int i = 0; i < parameterCount; i++)
                        {
                            parameterTypes[i] = systemInt32;
                        }
                        yield return new RoSyntheticConstructor(this, uniquifier++, parameterTypes);
                        parameterCount++;
                        elementType = elementType.GetRoElementType();
                    }
                }

                if (multiDim)
                {
                    //
                    // Expose a constructor that takes n*2 Int32's (two for each dimension) and constructs a arbitrarily lower-bounded array. For example,
                    //
                    //   String[,]
                    //
                    // exposes
                    //
                    //   .ctor(int32, int32, int32, int32)
                    //

                    RoType[] parameterTypes = new RoType[rank * 2];
                    for (int i = 0; i < rank * 2; i++)
                    {
                        parameterTypes[i] = systemInt32;
                    }
                    yield return new RoSyntheticConstructor(this, uniquifier++, parameterTypes);
                }
            }
        }

        internal sealed override IEnumerable<MethodInfo> GetMethodsCore(NameFilter filter, Type reflectedType)
        {
            int rank =_rank;

            int uniquifier = 0;
            RoType systemInt32 = Loader.GetCoreType(CoreType.Int32);
            RoArrayType arrayType = this;
            RoType elementType = GetRoElementType();
            RoType systemVoid = Loader.GetCoreType(CoreType.Void);

            if (filter == null || filter.Matches("Get"))
            {
                RoType[] getParameters = new RoType[rank];
                for (int i = 0; i < rank; i++)
                {
                    getParameters[i] = systemInt32;
                }
                yield return new RoSyntheticMethod(this, uniquifier++, "Get", elementType, getParameters);
            }

            if (filter == null || filter.Matches("Set"))
            {
                RoType[] setParameters = new RoType[rank + 1];
                for (int i = 0; i < rank; i++)
                {
                    setParameters[i] = systemInt32;
                }
                setParameters[rank] = elementType;
                yield return new RoSyntheticMethod(this, uniquifier++, "Set", systemVoid, setParameters);
            }

            if (filter == null || filter.Matches("Address"))
            {
                RoType[] addressParameters = new RoType[rank];
                for (int i = 0; i < rank; i++)
                {
                    addressParameters[i] = systemInt32;
                }
                yield return new RoSyntheticMethod(this, uniquifier++, "Address", elementType.GetUniqueByRefType(), addressParameters);
            }
        }
    }
}
