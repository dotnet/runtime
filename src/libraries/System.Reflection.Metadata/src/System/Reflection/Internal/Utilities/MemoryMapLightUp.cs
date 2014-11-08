// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace System.Reflection.Internal
{
    internal static class MemoryMapLightUp
    {
        private static Type lazyMemoryMappedFileType;
        private static Type lazyMemoryMappedViewAccessorType;
        private static Type lazyMemoryMappedFileAccessType;
        private static Type lazyMemoryMappedFileSecurityType;
        private static Type lazyHandleInheritabilityType;
        private static MethodInfo lazyCreateFromFile;
        private static MethodInfo lazyCreateViewAccessor;
        private static PropertyInfo lazySafeMemoryMappedViewHandle;
        private static PropertyInfo lazyPointerOffset;
        private static FieldInfo lazyInternalViewField;
        private static PropertyInfo lazyInternalPointerOffset;

        private static readonly object MemoryMappedFileAccess_Read = 1;
        private static readonly object HandleInheritability_None = 0;
        private static readonly object LongZero = (long)0;
        private static readonly object True = true;

        // test only:
        internal static bool Test450Compat;

        private static bool? lazyIsAvailable;

        internal static bool IsAvailable
        {
            get
            {
                if (!lazyIsAvailable.HasValue)
                {
                    lazyIsAvailable = TryLoadTypes();
                }

                return lazyIsAvailable.Value;
            }
        }

        private static bool TryLoadType(string assemblyQualifiedName, out Type type)
        {
            try
            {
                type = Type.GetType(assemblyQualifiedName, throwOnError: false);
            }
            catch
            {
                type = null;
            }

            return type != null;
        }

        private static bool TryLoadTypes()
        {
            const string SystemCoreRef = "System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";

            return FileStreamReadLightUp.FileStreamType.Value != null
                && TryLoadType("System.IO.MemoryMappedFiles.MemoryMappedFile, " + SystemCoreRef, out lazyMemoryMappedFileType)
                && TryLoadType("System.IO.MemoryMappedFiles.MemoryMappedViewAccessor, " + SystemCoreRef, out lazyMemoryMappedViewAccessorType)
                && TryLoadType("System.IO.MemoryMappedFiles.MemoryMappedFileAccess, " + SystemCoreRef, out lazyMemoryMappedFileAccessType)
                && TryLoadType("System.IO.MemoryMappedFiles.MemoryMappedFileSecurity, " + SystemCoreRef, out lazyMemoryMappedFileSecurityType)
                && TryLoadType("System.IO.HandleInheritability, " + SystemCoreRef, out lazyHandleInheritabilityType)
                && TryLoadMembers();
        }

        private static bool TryLoadMembers()
        {
            lazyCreateFromFile =
                (from m in lazyMemoryMappedFileType.GetTypeInfo().GetDeclaredMethods("CreateFromFile")
                 let ps = m.GetParameters()
                 where ps.Length == 7 &&
                     ps[0].ParameterType == FileStreamReadLightUp.FileStreamType.Value &&
                     ps[1].ParameterType == typeof(string) &&
                     ps[2].ParameterType == typeof(long) &&
                     ps[3].ParameterType == lazyMemoryMappedFileAccessType &&
                     ps[4].ParameterType == lazyMemoryMappedFileSecurityType &&
                     ps[5].ParameterType == lazyHandleInheritabilityType &&
                     ps[6].ParameterType == typeof(bool)
                 select m).SingleOrDefault();

            if (lazyCreateFromFile == null)
            {
                return false;
            }

            lazyCreateViewAccessor =
                (from m in lazyMemoryMappedFileType.GetTypeInfo().GetDeclaredMethods("CreateViewAccessor")
                 let ps = m.GetParameters()
                 where ps.Length == 3 &&
                     ps[0].ParameterType == typeof(long) &&
                     ps[1].ParameterType == typeof(long) &&
                     ps[2].ParameterType == lazyMemoryMappedFileAccessType
                 select m).SingleOrDefault();

            if (lazyCreateViewAccessor == null)
            {
                return false;
            }

            lazySafeMemoryMappedViewHandle = lazyMemoryMappedViewAccessorType.GetTypeInfo().GetDeclaredProperty("SafeMemoryMappedViewHandle");
            if (lazySafeMemoryMappedViewHandle == null)
            {
                return false;
            }

            // Available on FW >= 4.5.1:
            lazyPointerOffset = Test450Compat ? null : lazyMemoryMappedViewAccessorType.GetTypeInfo().GetDeclaredProperty("PointerOffset");
            if (lazyPointerOffset == null)
            {
                // FW < 4.5.1
                lazyInternalViewField = lazyMemoryMappedViewAccessorType.GetTypeInfo().GetDeclaredField("m_view");
                if (lazyInternalViewField == null)
                {
                    return false;
                }

                lazyInternalPointerOffset = lazyInternalViewField.FieldType.GetTypeInfo().GetDeclaredProperty("PointerOffset");
                if (lazyInternalPointerOffset == null)
                {
                    return false;
                }
            }

            return true;
        }

        internal static IDisposable CreateMemoryMap(Stream stream)
        {
            Debug.Assert(lazyIsAvailable.GetValueOrDefault());

            try
            {
                return (IDisposable)lazyCreateFromFile.Invoke(null, new object[7]
                {
                    stream,                      // fileStream
                    null,                        // mapName
                    LongZero,                    // capacity
                    MemoryMappedFileAccess_Read, // access
                    null,                        // memoryMappedFileSecurity
                    HandleInheritability_None,   // inheritability
                    True,                        // leaveOpen
                });
            }
            catch (MemberAccessException)
            {
                lazyIsAvailable = false;
                return null;
            }
            catch (TargetInvocationException ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }

        internal static IDisposable CreateViewAccessor(object memoryMap, long start, int size)
        {
            Debug.Assert(lazyIsAvailable.GetValueOrDefault());
            try
            {
                return (IDisposable)lazyCreateViewAccessor.Invoke(memoryMap, new object[3]
                {
                    start,                       // start
                    (long)size,                  // size
                    MemoryMappedFileAccess_Read, // access
                });
            }
            catch (MemberAccessException)
            {
                lazyIsAvailable = false;
                return null;
            }
            catch (TargetInvocationException ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }

        internal unsafe static byte* AcquirePointer(object accessor, out SafeBuffer safeBuffer)
        {
            Debug.Assert(lazyIsAvailable.GetValueOrDefault());

            safeBuffer = (SafeBuffer)lazySafeMemoryMappedViewHandle.GetValue(accessor);

            byte* ptr = null;
            safeBuffer.AcquirePointer(ref ptr);

            long offset;
            if (lazyPointerOffset != null)
            {
                offset = (long)lazyPointerOffset.GetValue(accessor);
            }
            else
            {
                object internalView = lazyInternalViewField.GetValue(accessor);
                offset = (long)lazyInternalPointerOffset.GetValue(internalView);
            }

            return ptr + offset;
        }
    }
}
