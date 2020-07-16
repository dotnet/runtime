// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Internal.TypeSystem
{
    /// <summary>
    /// Base type for all type system exceptions.
    /// </summary>
    public abstract class TypeSystemException : Exception
    {
        private string[] _arguments;

        /// <summary>
        /// Gets the resource string identifier.
        /// </summary>
        public ExceptionStringID StringID { get; }

        /// <summary>
        /// Gets the formatting arguments for the exception string.
        /// </summary>
        public IReadOnlyList<string> Arguments
        {
            get
            {
                return _arguments;
            }
        }

        public override string Message
        {
            get
            {
                return GetExceptionString(StringID, _arguments);
            }
        }

        internal TypeSystemException(ExceptionStringID id, params string[] args)
        {
            StringID = id;
            _arguments = args;
        }

        private static string GetFormatString(ExceptionStringID id)
        {
            switch (id)
            {
                case ExceptionStringID.ClassLoadGeneral: return SR.ClassLoadGeneral;
                case ExceptionStringID.ClassLoadBadFormat: return SR.ClassLoadBadFormat;
                case ExceptionStringID.ClassLoadExplicitGeneric: return SR.ClassLoadExplicitGeneric;
                case ExceptionStringID.ClassLoadExplicitLayout: return SR.ClassLoadExplicitLayout;
                case ExceptionStringID.ClassLoadValueClassTooLarge: return SR.ClassLoadValueClassTooLarge;
                case ExceptionStringID.ClassLoadRankTooLarge: return SR.ClassLoadRankTooLarge;
                case ExceptionStringID.MissingMethod: return SR.MissingMethod;
                case ExceptionStringID.MissingField: return SR.MissingField;
                case ExceptionStringID.InvalidProgramDefault: return SR.InvalidProgramDefault;
                case ExceptionStringID.InvalidProgramSpecific: return SR.InvalidProgramSpecific;
                case ExceptionStringID.InvalidProgramVararg: return SR.InvalidProgramVararg;
                case ExceptionStringID.InvalidProgramCallVirtFinalize: return SR.InvalidProgramCallVirtFinalize;
                case ExceptionStringID.InvalidProgramUnmanagedCallersOnly: return SR.InvalidProgramUnmanagedCallersOnly;
                case ExceptionStringID.InvalidProgramCallAbstractMethod: return SR.InvalidProgramCallAbstractMethod;
                case ExceptionStringID.InvalidProgramCallVirtStatic: return SR.InvalidProgramCallVirtStatic;
                case ExceptionStringID.InvalidProgramNonStaticMethod: return SR.InvalidProgramNonStaticMethod;
                case ExceptionStringID.InvalidProgramGenericMethod: return SR.InvalidProgramGenericMethod;
                case ExceptionStringID.InvalidProgramNonBlittableTypes: return SR.InvalidProgramNonBlittableTypes;
                case ExceptionStringID.BadImageFormatGeneric: return SR.BadImageFormatGeneric;
                case ExceptionStringID.FileLoadErrorGeneric: return SR.FileLoadErrorGeneric;
            }
#if !DEBUG
            throw new Exception($"Unknown Exception string id {id}");
#else
            return null;
#endif
        }

        private static string GetExceptionString(ExceptionStringID id, string[] args)
        {
            string formatString = GetFormatString(id);
#if !DEBUG
            try
            {
#endif
                return String.Format(formatString, (object[])args);
#if !DEBUG
            }
            catch
            {
                return "[TEMPORARY EXCEPTION MESSAGE] " + id.ToString() + ": " + String.Join(", ", args);
            }
#endif
        }

        /// <summary>
        /// The exception that is thrown when type-loading failures occur.
        /// </summary>
        public class TypeLoadException : TypeSystemException
        {
            public string TypeName { get; }

            public string AssemblyName { get; }

            internal TypeLoadException(ExceptionStringID id, string typeName, string assemblyName, string messageArg)
                : base(id, new string[] { typeName, assemblyName, messageArg })
            {
                TypeName = typeName;
                AssemblyName = assemblyName;
            }

            internal TypeLoadException(ExceptionStringID id, string typeName, string assemblyName)
                : base(id, new string[] { typeName, assemblyName })
            {
                TypeName = typeName;
                AssemblyName = assemblyName;
            }
        }

        /// <summary>
        /// The exception that is thrown when there is an attempt to access a class member that does not exist
        /// or that is not declared as public.
        /// </summary>
        public abstract class MissingMemberException : TypeSystemException
        {
            protected internal MissingMemberException(ExceptionStringID id, params string[] args)
                : base(id, args)
            {
            }
        }

        /// <summary>
        /// The exception that is thrown when there is an attempt to access a method that does not exist.
        /// </summary>
        public class MissingMethodException : MissingMemberException
        {
            internal MissingMethodException(ExceptionStringID id, params string[] args)
                : base(id, args)
            {
            }
        }

        /// <summary>
        /// The exception that is thrown when there is an attempt to access a field that does not exist.
        /// </summary>
        public class MissingFieldException : MissingMemberException
        {
            internal MissingFieldException(ExceptionStringID id, params string[] args)
                : base(id, args)
            {
            }
        }

        /// <summary>
        /// The exception that is thrown when an attempt to access a file that does not exist on disk fails.
        /// </summary>
        public class FileNotFoundException : TypeSystemException
        {
            internal FileNotFoundException(ExceptionStringID id, string fileName)
                : base(id, fileName)
            {
            }
        }

        /// <summary>
        /// The exception that is thrown when a program contains invalid Microsoft intermediate language (MSIL) or metadata.
        /// Generally this indicates a bug in the compiler that generated the program.
        /// </summary>
        public class InvalidProgramException : TypeSystemException
        {
            internal InvalidProgramException(ExceptionStringID id, string method)
                : base(id, method)
            {
            }

            internal InvalidProgramException()
                : base(ExceptionStringID.InvalidProgramDefault)
            {
            }
        }

        public class BadImageFormatException : TypeSystemException
        {
            internal BadImageFormatException()
                : base(ExceptionStringID.BadImageFormatGeneric)
            {
            }
        }
    }
}
