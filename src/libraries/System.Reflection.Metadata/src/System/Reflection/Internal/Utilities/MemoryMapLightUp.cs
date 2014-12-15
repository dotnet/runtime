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
        private static Type s_lazyMemoryMappedFileType;
        private static Type s_lazyMemoryMappedViewAccessorType;
        private static Type s_lazyMemoryMappedFileAccessType;
        private static Type s_lazyMemoryMappedFileSecurityType;
        private static Type s_lazyHandleInheritabilityType;
        private static MethodInfo s_lazyCreateFromFile;
        private static MethodInfo s_lazyCreateViewAccessor;
        private static PropertyInfo s_lazySafeMemoryMappedViewHandle;
        private static PropertyInfo s_lazyPointerOffset;
        private static FieldInfo s_lazyInternalViewField;
        private static PropertyInfo s_lazyInternalPointerOffset;

        private static readonly object s_MemoryMappedFileAccess_Read = 1;
        private static readonly object s_HandleInheritability_None = 0;
        private static readonly object s_LongZero = (long)0;
        private static readonly object s_True = true;

        // test only:
        internal static bool Test450Compat;

        private static bool? s_lazyIsAvailable;

        internal static bool IsAvailable
        {
            get
            {
                if (!s_lazyIsAvailable.HasValue)
                {
                    s_lazyIsAvailable = TryLoadTypes();
                }

                return s_lazyIsAvailable.Value;
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
                && TryLoadType("System.IO.MemoryMappedFiles.MemoryMappedFile, " + SystemCoreRef, out s_lazyMemoryMappedFileType)
                && TryLoadType("System.IO.MemoryMappedFiles.MemoryMappedViewAccessor, " + SystemCoreRef, out s_lazyMemoryMappedViewAccessorType)
                && TryLoadType("System.IO.MemoryMappedFiles.MemoryMappedFileAccess, " + SystemCoreRef, out s_lazyMemoryMappedFileAccessType)
                && TryLoadType("System.IO.MemoryMappedFiles.MemoryMappedFileSecurity, " + SystemCoreRef, out s_lazyMemoryMappedFileSecurityType)
                && TryLoadType("System.IO.HandleInheritability, " + SystemCoreRef, out s_lazyHandleInheritabilityType)
                && TryLoadMembers();
        }

        private static bool TryLoadMembers()
        {
            s_lazyCreateFromFile =
                (from m in s_lazyMemoryMappedFileType.GetTypeInfo().GetDeclaredMethods("CreateFromFile")
                 let ps = m.GetParameters()
                 where ps.Length == 7 &&
                     ps[0].ParameterType == FileStreamReadLightUp.FileStreamType.Value &&
                     ps[1].ParameterType == typeof(string) &&
                     ps[2].ParameterType == typeof(long) &&
                     ps[3].ParameterType == s_lazyMemoryMappedFileAccessType &&
                     ps[4].ParameterType == s_lazyMemoryMappedFileSecurityType &&
                     ps[5].ParameterType == s_lazyHandleInheritabilityType &&
                     ps[6].ParameterType == typeof(bool)
                 select m).SingleOrDefault();

            if (s_lazyCreateFromFile == null)
            {
                return false;
            }

            s_lazyCreateViewAccessor =
                (from m in s_lazyMemoryMappedFileType.GetTypeInfo().GetDeclaredMethods("CreateViewAccessor")
                 let ps = m.GetParameters()
                 where ps.Length == 3 &&
                     ps[0].ParameterType == typeof(long) &&
                     ps[1].ParameterType == typeof(long) &&
                     ps[2].ParameterType == s_lazyMemoryMappedFileAccessType
                 select m).SingleOrDefault();

            if (s_lazyCreateViewAccessor == null)
            {
                return false;
            }

            s_lazySafeMemoryMappedViewHandle = s_lazyMemoryMappedViewAccessorType.GetTypeInfo().GetDeclaredProperty("SafeMemoryMappedViewHandle");
            if (s_lazySafeMemoryMappedViewHandle == null)
            {
                return false;
            }

            // Available on FW >= 4.5.1:
            s_lazyPointerOffset = Test450Compat ? null : s_lazyMemoryMappedViewAccessorType.GetTypeInfo().GetDeclaredProperty("PointerOffset");
            if (s_lazyPointerOffset == null)
            {
                // FW < 4.5.1
                s_lazyInternalViewField = s_lazyMemoryMappedViewAccessorType.GetTypeInfo().GetDeclaredField("m_view");
                if (s_lazyInternalViewField == null)
                {
                    return false;
                }

                s_lazyInternalPointerOffset = s_lazyInternalViewField.FieldType.GetTypeInfo().GetDeclaredProperty("PointerOffset");
                if (s_lazyInternalPointerOffset == null)
                {
                    return false;
                }
            }

            return true;
        }

        internal static IDisposable CreateMemoryMap(Stream stream)
        {
            Debug.Assert(s_lazyIsAvailable.GetValueOrDefault());

            try
            {
                return (IDisposable)s_lazyCreateFromFile.Invoke(null, new object[7]
                {
                    stream,                      // fileStream
                    null,                        // mapName
                    s_LongZero,                    // capacity
                    s_MemoryMappedFileAccess_Read, // access
                    null,                        // memoryMappedFileSecurity
                    s_HandleInheritability_None,   // inheritability
                    s_True,                        // leaveOpen
                });
            }
            catch (MemberAccessException)
            {
                s_lazyIsAvailable = false;
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
            Debug.Assert(s_lazyIsAvailable.GetValueOrDefault());
            try
            {
                return (IDisposable)s_lazyCreateViewAccessor.Invoke(memoryMap, new object[3]
                {
                    start,                       // start
                    (long)size,                  // size
                    s_MemoryMappedFileAccess_Read, // access
                });
            }
            catch (MemberAccessException)
            {
                s_lazyIsAvailable = false;
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
            Debug.Assert(s_lazyIsAvailable.GetValueOrDefault());

            safeBuffer = (SafeBuffer)s_lazySafeMemoryMappedViewHandle.GetValue(accessor);

            byte* ptr = null;
            safeBuffer.AcquirePointer(ref ptr);

            long offset;
            if (s_lazyPointerOffset != null)
            {
                offset = (long)s_lazyPointerOffset.GetValue(accessor);
            }
            else
            {
                object internalView = s_lazyInternalViewField.GetValue(accessor);
                offset = (long)s_lazyInternalPointerOffset.GetValue(internalView);
            }

            return ptr + offset;
        }
    }
}
