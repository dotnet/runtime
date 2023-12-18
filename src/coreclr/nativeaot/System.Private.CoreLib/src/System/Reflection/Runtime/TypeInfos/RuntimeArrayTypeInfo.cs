// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.MethodInfos;
using System.Reflection.Runtime.TypeInfos;

using Internal.Reflection.Core;
using Internal.Reflection.Core.Execution;
using Internal.Runtime.Augments;

namespace System.Reflection.Runtime.TypeInfos
{
    //
    // The runtime's implementation of TypeInfo's for array types.
    //
    internal sealed partial class RuntimeArrayTypeInfo : RuntimeHasElementTypeInfo
    {
        private RuntimeArrayTypeInfo(UnificationKey key, bool multiDim, int rank)
            : base(key)
        {
            Debug.Assert(multiDim || rank == 1);
            _multiDim = multiDim;
            _rank = rank;
        }

        public sealed override int GetArrayRank()
        {
            return _rank;
        }

        public override bool IsArray => true;
        public override bool IsSZArray => !_multiDim;
        public override bool IsVariableBoundArray => _multiDim;

#pragma warning disable SYSLIB0050 // TypeAttributes.Serializable is obsolete
        public sealed override TypeAttributes Attributes =>
            TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Serializable;
#pragma warning restore SYSLIB0050

    internal sealed override IEnumerable<RuntimeConstructorInfo> SyntheticConstructors
        {
            get
            {
                bool multiDim = this.IsVariableBoundArray;
                int rank = this.GetArrayRank();

                RuntimeArrayTypeInfo arrayType = this;
                RuntimeTypeInfo countType = typeof(int).ToRuntimeTypeInfo();

                {
                    //
                    // Expose a constructor that takes n Int32's (one for each dimension) and constructs a zero lower-bounded array. For example,
                    //
                    //   String[,]
                    //
                    // exposes
                    //
                    //   .ctor(int32, int32)
                    //

                    RuntimeTypeInfo[] ctorParameters = new RuntimeTypeInfo[rank];
                    for (int i = 0; i < rank; i++)
                        ctorParameters[i] = countType;
                    yield return RuntimeSyntheticConstructorInfo.GetRuntimeSyntheticConstructorInfo(
                        SyntheticMethodId.ArrayCtor,
                        arrayType,
                        ctorParameters,
                        InvokerOptions.AllowNullThis,
                        delegate (object _this, object[] args, Type thisType)
                        {
                            int[] lengths = new int[rank];
                            for (int i = 0; i < rank; i++)
                            {
                                lengths[i] = (int)(args[i]);
                            }
                            return RuntimeAugments.NewMultiDimArray(arrayType.TypeHandle, lengths, null);
                        }
                    );
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
                    RuntimeTypeInfo elementType = this.InternalRuntimeElementType;
                    while (elementType.IsSZArray)
                    {
                        RuntimeTypeInfo[] ctorParameters = new RuntimeTypeInfo[parameterCount];
                        for (int i = 0; i < parameterCount; i++)
                            ctorParameters[i] = countType;
                        yield return RuntimeSyntheticConstructorInfo.GetRuntimeSyntheticConstructorInfo(
                            SyntheticMethodId.ArrayCtorJagged + parameterCount,
                            arrayType,
                            ctorParameters,
                            InvokerOptions.AllowNullThis,
                            delegate (object _this, object[] args, Type thisType)
                            {
                                int[] lengths = new int[args.Length];
                                for (int i = 0; i < args.Length; i++)
                                {
                                    lengths[i] = (int)(args[i]);
                                }
                                Array jaggedArray = CreateJaggedArray(arrayType, lengths, 0);
                                return jaggedArray;
                            }
                        );
                        parameterCount++;
                        elementType = elementType.InternalRuntimeElementType;
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

                    RuntimeTypeInfo[] ctorParameters = new RuntimeTypeInfo[rank * 2];
                    for (int i = 0; i < rank * 2; i++)
                        ctorParameters[i] = countType;
                    yield return RuntimeSyntheticConstructorInfo.GetRuntimeSyntheticConstructorInfo(
                        SyntheticMethodId.ArrayMultiDimCtor,
                        arrayType,
                        ctorParameters,
                        InvokerOptions.AllowNullThis,
                        delegate (object _this, object[] args, Type thisType)
                        {
                            int[] lengths = new int[rank];
                            int[] lowerBounds = new int[rank];
                            for (int i = 0; i < rank; i++)
                            {
                                lowerBounds[i] = (int)(args[i * 2]);
                                lengths[i] = (int)(args[i * 2 + 1]);
                            }
                            return RuntimeAugments.NewMultiDimArray(arrayType.TypeHandle, lengths, lowerBounds);
                        }
                    );
                }
            }
        }

        internal sealed override IEnumerable<RuntimeMethodInfo> SyntheticMethods
        {
            get
            {
                int rank = this.GetArrayRank();

                RuntimeTypeInfo indexType = typeof(int).ToRuntimeTypeInfo();
                RuntimeArrayTypeInfo arrayType = this;
                RuntimeTypeInfo elementType = arrayType.InternalRuntimeElementType;
                RuntimeTypeInfo voidType = typeof(void).ToRuntimeTypeInfo();

                {
                    RuntimeTypeInfo[] getParameters = new RuntimeTypeInfo[rank];
                    for (int i = 0; i < rank; i++)
                        getParameters[i] = indexType;
                    yield return RuntimeSyntheticMethodInfo.GetRuntimeSyntheticMethodInfo(
                        SyntheticMethodId.ArrayGet,
                        "Get",
                        arrayType,
                        getParameters,
                        elementType,
                        InvokerOptions.None,
                        delegate (object _this, object[] args, Type thisType)
                        {
                            Array array = (Array)_this;
                            int[] indices = new int[rank];
                            for (int i = 0; i < rank; i++)
                                indices[i] = (int)(args[i]);
                            return array.GetValue(indices);
                        }
                    );
                }

                {
                    RuntimeTypeInfo[] setParameters = new RuntimeTypeInfo[rank + 1];
                    for (int i = 0; i < rank; i++)
                        setParameters[i] = indexType;
                    setParameters[rank] = elementType;
                    yield return RuntimeSyntheticMethodInfo.GetRuntimeSyntheticMethodInfo(
                        SyntheticMethodId.ArraySet,
                        "Set",
                        arrayType,
                        setParameters,
                        voidType,
                        InvokerOptions.None,
                        delegate (object _this, object[] args, Type thisType)
                        {
                            Array array = (Array)_this;
                            int[] indices = new int[rank];
                            for (int i = 0; i < rank; i++)
                                indices[i] = (int)(args[i]);
                            object value = args[rank];
                            array.SetValue(value, indices);
                            return null;
                        }
                    );
                }

                {
                    RuntimeTypeInfo[] addressParameters = new RuntimeTypeInfo[rank];
                    for (int i = 0; i < rank; i++)
                        addressParameters[i] = indexType;
                    yield return RuntimeSyntheticMethodInfo.GetRuntimeSyntheticMethodInfo(
                        SyntheticMethodId.ArrayAddress,
                        "Address",
                        arrayType,
                        addressParameters,
                        elementType.GetByRefType(),
                        InvokerOptions.None,
                        delegate (object _this, object[] args, Type thisType)
                        {
                            throw new NotSupportedException();
                        }
                    );
                }
            }
        }

        //
        // Returns the base type as a typeDef, Ref, or Spec. Default behavior is to QTypeDefRefOrSpec.Null, which causes BaseType to return null.
        //
        internal sealed override QTypeDefRefOrSpec TypeRefDefOrSpecForBaseType
        {
            get
            {
                return TypeDefInfoProjectionForArrays.TypeRefDefOrSpecForBaseType;
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
                if (this.IsVariableBoundArray)
                    return Array.Empty<QTypeDefRefOrSpec>();
                else
                    return TypeDefInfoProjectionForArrays.TypeRefDefOrSpecsForDirectlyImplementedInterfaces;
            }
        }

        //
        // Returns the generic parameter substitutions to use when enumerating declared members, base class and implemented interfaces.
        //
        internal sealed override TypeContext TypeContext
        {
            get
            {
                return new TypeContext(new RuntimeTypeInfo[] { this.InternalRuntimeElementType }, null);
            }
        }

        protected sealed override string Suffix
        {
            get
            {
                if (!_multiDim)
                    return "[]";
                else if (_rank == 1)
                    return "[*]";
                else
                    return "[" + new string(',', _rank - 1) + "]";
            }
        }

        //
        // Arrays don't have a true typedef behind them but for the purpose of reporting base classes and interfaces, we can create a pretender.
        //
        private static RuntimeTypeInfo TypeDefInfoProjectionForArrays
        {
            get
            {
                RuntimeTypeHandle projectionTypeHandleForArrays = RuntimeAugments.ProjectionTypeForArrays;
                RuntimeTypeInfo projectionRuntimeTypeForArrays = projectionTypeHandleForArrays.GetRuntimeTypeInfoForRuntimeTypeHandle();
                return projectionRuntimeTypeForArrays;
            }
        }

        //
        // Helper for jagged array constructors.
        //
        private static Array CreateJaggedArray(RuntimeTypeInfo arrayType, int[] lengths, int index)
        {
            int length = lengths[index];
            Array jaggedArray = RuntimeAugments.NewArray(arrayType.TypeHandle, length);
            if (index != lengths.Length - 1)
            {
                for (int i = 0; i < length; i++)
                {
                    Array subArray = CreateJaggedArray(arrayType.InternalRuntimeElementType, lengths, index + 1);
                    jaggedArray.SetValue(subArray, i);
                }
            }
            return jaggedArray;
        }

        private readonly int _rank;
        private readonly bool _multiDim;
    }
}
