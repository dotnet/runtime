// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

using Internal.NativeFormat;
using Internal.TypeSystem;

namespace Internal.Runtime.TypeLoader
{
    internal struct NativeLayoutInfo
    {
        public uint Offset;
        public NativeFormatModuleInfo Module;
        public NativeReader Reader;
        public NativeLayoutInfoLoadContext LoadContext;
    }

    //
    // TypeBuilder per-Type state. It is attached to each TypeDesc that gets involved in type building.
    //
    internal class TypeBuilderState
    {
        public TypeBuilderState(TypeDesc typeBeingBuilt)
        {
            TypeBeingBuilt = typeBeingBuilt;
        }

        public readonly TypeDesc TypeBeingBuilt;

        //
        // We cache and try to reuse the most recently used TypeSystemContext. The TypeSystemContext is used by not just type builder itself,
        // but also in several random other places in reflection. There can be multiple ResolutionContexts in flight at any given moment.
        // This check ensures that the RuntimeTypeHandle cache in the current resolution context is refreshed in case there were new
        // types built using a different TypeSystemContext in the meantime.
        // NOTE: For correctness, this value must be recomputed every time the context is recycled. This requires flushing the TypeBuilderState
        // from each type that has one if context is recycled.
        //
        public bool AttemptedAndFailedToRetrieveTypeHandle;

        public bool NeedsTypeHandle;
        public bool HasBeenPrepared;

        public RuntimeTypeHandle HalfBakedRuntimeTypeHandle;
        public IntPtr HalfBakedDictionary;

        private bool _templateComputed;
        private TypeDesc _templateType;

        public TypeDesc TemplateType
        {
            get
            {
                if (!_templateComputed)
                {
                    // Multidimensional arrays don't implement generic interfaces and are special cases. They use
                    // typeof(object[,]) as their template.
                    if (TypeBeingBuilt.IsMdArray)
                    {
                        _templateType = TypeBeingBuilt.Context.ResolveRuntimeTypeHandle(typeof(object[,]).TypeHandle);
                    }
                    else if (TypeBeingBuilt.IsSzArray && ((ArrayType)TypeBeingBuilt).ElementType is TypeDesc elementType &&
                        (elementType.IsPointer || elementType.IsFunctionPointer))
                    {
                        // Pointer arrays don't implement generic interfaces.
                        _templateType = TypeBeingBuilt.Context.ResolveRuntimeTypeHandle(typeof(char*[]).TypeHandle);
                    }
                    else
                    {
                        _templateType = TemplateLocator.TryGetTypeTemplate(TypeBeingBuilt, ref _nativeLayoutInfo);
                        Debug.Assert(_templateType is null || !_templateType.RuntimeTypeHandle.IsNull());
                    }
                    _templateComputed = true;
                }

                return _templateType;
            }
        }

        private bool _nativeLayoutComputed;

        private NativeLayoutInfo _nativeLayoutInfo;

        private void EnsureNativeLayoutInfoComputed()
        {
            if (!_nativeLayoutComputed)
            {
                _ = TemplateType;

                if (_nativeLayoutInfo.Module is not null)
                {
                    FinishInitNativeLayoutInfo(TypeBeingBuilt, ref _nativeLayoutInfo);
                }

                _nativeLayoutComputed = true;
            }
        }

        /// <summary>
        /// Initialize the Reader and LoadContext fields of the native layout info
        /// </summary>
        /// <param name="type"></param>
        /// <param name="nativeLayoutInfo"></param>
        private static void FinishInitNativeLayoutInfo(TypeDesc type, ref NativeLayoutInfo nativeLayoutInfo)
        {
            var nativeLayoutInfoLoadContext = new NativeLayoutInfoLoadContext();

            nativeLayoutInfoLoadContext._typeSystemContext = type.Context;
            nativeLayoutInfoLoadContext._module = nativeLayoutInfo.Module;

            if (type is DefType)
            {
                nativeLayoutInfoLoadContext._typeArgumentHandles = ((DefType)type).Instantiation;
            }
            else if (type is ArrayType)
            {
                nativeLayoutInfoLoadContext._typeArgumentHandles = new Instantiation(new TypeDesc[] { ((ArrayType)type).ElementType });
            }
            else
            {
                Debug.Assert(false);
            }

            nativeLayoutInfoLoadContext._methodArgumentHandles = new Instantiation(null);

            nativeLayoutInfo.Reader = TypeLoaderEnvironment.GetNativeLayoutInfoReader(nativeLayoutInfo.Module.Handle);
            nativeLayoutInfo.LoadContext = nativeLayoutInfoLoadContext;
        }

        public NativeLayoutInfo NativeLayoutInfo
        {
            get
            {
                EnsureNativeLayoutInfoComputed();
                return _nativeLayoutInfo;
            }
        }

        public NativeParser GetParserForNativeLayoutInfo()
        {
            EnsureNativeLayoutInfoComputed();
            if (_nativeLayoutInfo.Reader is not null)
                return new NativeParser(_nativeLayoutInfo.Reader, _nativeLayoutInfo.Offset);
            else
                return default(NativeParser);
        }

        public GenericTypeDictionary Dictionary;

        public int NonGcDataSize;

        public bool HasStaticConstructor => ClassConstructorPointer.HasValue;

        public IntPtr? ClassConstructorPointer;
        public IntPtr GcStaticDesc;
        public IntPtr ThreadStaticDesc;
        public uint ThreadStaticOffset;
    }
}
