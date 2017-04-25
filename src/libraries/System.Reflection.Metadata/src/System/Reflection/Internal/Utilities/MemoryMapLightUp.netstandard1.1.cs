// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
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
        private static MethodInfo s_lazyCreateFromFileClassic;
        private static MethodInfo s_lazyCreateViewAccessor;
        private static PropertyInfo s_lazySafeMemoryMappedViewHandle;
        private static PropertyInfo s_lazyPointerOffset;
        private static FieldInfo s_lazyInternalViewField;
        private static PropertyInfo s_lazyInternalPointerOffset;

        private static readonly object s_MemoryMappedFileAccess_Read = 1;
        private static readonly object s_HandleInheritability_None = 0;
        private static readonly object s_LongZero = (long)0;
        private static readonly object s_True = true;

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

        private static bool TryLoadType(string typeName, string modernAssembly, string classicAssembly, out Type type)
        {
            type = LightUpHelper.GetType(typeName, modernAssembly, classicAssembly);
            return type != null;
        }

        private static bool TryLoadTypes()
        {
            const string systemIOMemoryMappedFiles = "System.IO.MemoryMappedFiles, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            const string systemRuntimeHandles = "System.Runtime.Handles, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
            const string systemCore = "System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";

            TryLoadType("System.IO.MemoryMappedFiles.MemoryMappedFileSecurity", systemIOMemoryMappedFiles, systemCore, out s_lazyMemoryMappedFileSecurityType);

            return FileStreamReadLightUp.FileStreamType.Value != null
                && TryLoadType("System.IO.MemoryMappedFiles.MemoryMappedFile", systemIOMemoryMappedFiles, systemCore, out s_lazyMemoryMappedFileType)
                && TryLoadType("System.IO.MemoryMappedFiles.MemoryMappedViewAccessor", systemIOMemoryMappedFiles, systemCore, out s_lazyMemoryMappedViewAccessorType)
                && TryLoadType("System.IO.MemoryMappedFiles.MemoryMappedFileAccess", systemIOMemoryMappedFiles, systemCore, out s_lazyMemoryMappedFileAccessType)
                && TryLoadType("System.IO.HandleInheritability", systemRuntimeHandles, systemCore, out s_lazyHandleInheritabilityType)
                && TryLoadMembers();
        }

        private static bool TryLoadMembers()
        {
            // .NET Core, .NET 4.6+ 
            s_lazyCreateFromFile = LightUpHelper.GetMethod(
                s_lazyMemoryMappedFileType,
                "CreateFromFile",
                FileStreamReadLightUp.FileStreamType.Value,
                typeof(string),
                typeof(long),
                s_lazyMemoryMappedFileAccessType,
                s_lazyHandleInheritabilityType,
                typeof(bool)
                );

            // .NET < 4.6
            if (s_lazyCreateFromFile == null)
            {
                if (s_lazyMemoryMappedFileSecurityType != null)
                {
                    s_lazyCreateFromFileClassic = LightUpHelper.GetMethod(
                        s_lazyMemoryMappedFileType,
                        "CreateFromFile",
                        FileStreamReadLightUp.FileStreamType.Value,
                        typeof(string),
                        typeof(long),
                        s_lazyMemoryMappedFileAccessType,
                        s_lazyMemoryMappedFileSecurityType,
                        s_lazyHandleInheritabilityType,
                        typeof(bool));
                }

                if (s_lazyCreateFromFileClassic == null)
                {
                    return false;
                }
            }

            s_lazyCreateViewAccessor = LightUpHelper.GetMethod(
                s_lazyMemoryMappedFileType,
                "CreateViewAccessor",
                typeof(long),
                typeof(long),
                s_lazyMemoryMappedFileAccessType);

            if (s_lazyCreateViewAccessor == null)
            {
                return false;
            }

            s_lazySafeMemoryMappedViewHandle = s_lazyMemoryMappedViewAccessorType.GetTypeInfo().GetDeclaredProperty("SafeMemoryMappedViewHandle");
            if (s_lazySafeMemoryMappedViewHandle == null)
            {
                return false;
            }

            // .NET Core, .NET 4.5.1+
            s_lazyPointerOffset = s_lazyMemoryMappedViewAccessorType.GetTypeInfo().GetDeclaredProperty("PointerOffset");

            // .NET < 4.5.1
            if (s_lazyPointerOffset == null)
            {
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
                if (s_lazyCreateFromFile != null)
                {
                    return (IDisposable)s_lazyCreateFromFile.Invoke(null, new object[6]
                    {
                        stream,                        // fileStream
                        null,                          // mapName
                        s_LongZero,                    // capacity
                        s_MemoryMappedFileAccess_Read, // access
                        s_HandleInheritability_None,   // inheritability
                        s_True,                        // leaveOpen
                    });
                }
                else
                {
                    Debug.Assert(s_lazyCreateFromFileClassic != null);
                    return (IDisposable)s_lazyCreateFromFileClassic.Invoke(null, new object[7]
                    {
                        stream,                        // fileStream
                        null,                          // mapName
                        s_LongZero,                    // capacity
                        s_MemoryMappedFileAccess_Read, // access
                        null,                          // memoryMappedFileSecurity
                        s_HandleInheritability_None,   // inheritability
                        s_True,                        // leaveOpen
                    });
                }
            }
            catch (MemberAccessException)
            {
                s_lazyIsAvailable = false;
                return null;
            }
            catch (InvalidOperationException)
            {
                // thrown when accessing unapproved API in a Windows Store app
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
            catch (InvalidOperationException)
            {
                s_lazyIsAvailable = false;
                return null;
            }
            catch (TargetInvocationException ex) when (ex.InnerException is UnauthorizedAccessException)
            {
                throw new IOException(ex.InnerException.Message, ex.InnerException);
            }
            catch (TargetInvocationException ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }

        internal static bool TryGetSafeBufferAndPointerOffset(object accessor, out SafeBuffer safeBuffer, out long offset)
        {
            Debug.Assert(s_lazyIsAvailable.GetValueOrDefault());

            safeBuffer = (SafeBuffer)s_lazySafeMemoryMappedViewHandle.GetValue(accessor);
            offset = 0;

            try
            {
                if (s_lazyPointerOffset != null)
                {
                    offset = (long)s_lazyPointerOffset.GetValue(accessor);
                }
                else
                {
                    object internalView = s_lazyInternalViewField.GetValue(accessor);
                    offset = (long)s_lazyInternalPointerOffset.GetValue(internalView);
                }

                return true;
            }
            catch (MemberAccessException)
            {
                s_lazyIsAvailable = false;
                return false;
            }
            catch (InvalidOperationException)
            {
                // thrown when accessing unapproved API in a Windows Store app
                s_lazyIsAvailable = false;
                return false;
            }
            catch (TargetInvocationException ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }
    }
}
