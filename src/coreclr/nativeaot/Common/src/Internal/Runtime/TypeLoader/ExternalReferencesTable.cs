// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Internal.Runtime.Augments;

namespace Internal.Runtime.TypeLoader
{
    partial struct ExternalReferencesTable
    {
        private IntPtr _elements;
        private uint _elementsCount;

        private unsafe bool Initialize(TypeManagerHandle typeManager, ReflectionMapBlob blobId)
        {
            byte* pBlob;
            uint cbBlob;

            if (!RuntimeAugments.FindBlob(typeManager, (int)blobId, (IntPtr)(void*)&pBlob, (IntPtr)(void*)&cbBlob))
            {
                _elements = IntPtr.Zero;
                _elementsCount = 0;
                return false;
            }

            _elements = (IntPtr)pBlob;
            _elementsCount = (uint)(cbBlob / sizeof(uint));

            return true;
        }

        public bool IsInitialized() { return _elements != IntPtr.Zero; }

        /// <summary>
        /// Initialize ExternalReferencesTable using the CommonFixupsTable metadata blob on a given module.
        /// </summary>
        /// <param name="module">Module handle is used to locate the CommonFixupsTable blob</param>
        /// <returns>true when the CommonFixupsTable blob was found in the given module, false when not</returns>
        public bool InitializeCommonFixupsTable(TypeManagerHandle module)
        {
            return Initialize(module, ReflectionMapBlob.CommonFixupsTable);
        }

        public unsafe IntPtr GetIntPtrFromIndex(uint index)
        {
            return GetAddressFromIndex(index);
        }

        public unsafe IntPtr GetFunctionPointerFromIndex(uint index)
        {
            return GetAddressFromIndex(index);
        }

        public RuntimeTypeHandle GetRuntimeTypeHandleFromIndex(uint index)
        {
            return RuntimeAugments.CreateRuntimeTypeHandle(GetIntPtrFromIndex(index));
        }

        public unsafe IntPtr GetAddressFromIndex(uint index)
        {
            if (index >= _elementsCount)
                throw new BadImageFormatException();

            if (MethodTable.SupportsRelativePointers)
            {
                int* pRelPtr32 = &((int*)_elements)[index];
                return (IntPtr)((byte*)pRelPtr32 + *pRelPtr32);
            }

            return (IntPtr)(((void**)_elements)[index]);
        }
    }
}
