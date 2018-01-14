// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;

namespace System.Runtime.Versioning
{
 
    internal static class MultitargetingHelpers
    {
        // This method gets assembly info for the corresponding type. If the typeConverter
        // is provided it is used to get this information.
        internal static string GetAssemblyQualifiedName(Type type, Func<Type, string> converter)
        {
            string assemblyFullName = null;
 
            if (type != null)
            {
                if (converter != null)
                {
                    try
                    {
                        assemblyFullName = converter(type);
                        // TODO: validate that type and assembly names are well constructed - throw if not.
                    }
                    catch (Exception e)
                    {
                        if (IsSecurityOrCriticalException(e))
                        {
                            throw;
                        }
                    }
                }
 
                if (assemblyFullName == null)
                {
                    assemblyFullName = type.AssemblyQualifiedName;
                }
            }
 
            return assemblyFullName;
        }
 
        private static bool IsCriticalException(Exception ex)
        {
            return ex is NullReferenceException
                    || ex is StackOverflowException
                    || ex is OutOfMemoryException
                    //|| ex is System.Threading.ThreadAbortException
                    || ex is IndexOutOfRangeException
                    || ex is AccessViolationException;
        }
 
        private static bool IsSecurityOrCriticalException(Exception ex)
        {
            return (ex is System.Security.SecurityException) || IsCriticalException(ex);
        }
 
    }
}
