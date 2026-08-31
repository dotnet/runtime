// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Diagnostics
{
    /// <summary>
    /// Represents diagnostic information about a method. Information provided by this class is similar to information
    /// provided by <see cref="MethodBase"/> but it's meant for logging and tracing purposes.
    /// </summary>
    public sealed partial class DiagnosticMethodInfo
    {
#if !NATIVEAOT
        private readonly MethodBase _method;

        private DiagnosticMethodInfo(MethodBase method) => _method = method;

        /// <summary>
        /// Gets the name of the method.
        /// </summary>
        /// <remarks>Only the simple name of the method is returned, without information about generic parameters or arity.</remarks>
        public string Name => _method.Name;

        /// <summary>
        /// Gets the fully qualified name of the type that owns this method, including its namespace but not its assembly.
        /// </summary>
        public string? DeclaringTypeName
        {
            get
            {
                Type? declaringType = _method.DeclaringType;
                if (declaringType is { IsConstructedGenericType: true })
                    declaringType = declaringType.GetGenericTypeDefinition();
                return declaringType?.FullName;
            }
        }

        /// <summary>
        /// Gets the display name of the assembly that owns this method.
        /// </summary>
        public string? DeclaringAssemblyName => _method.Module.Assembly.FullName;

        /// <summary>
        /// Creates a <see cref="DiagnosticMethodInfo"/> that represents the target of the delegate.
        /// </summary>
        /// <remarks>This returns the definition of the target method, with stripped instantiation information.
        /// The return value might be null if the `StackTraceSupport` feature switch is set to false.</remarks>
        public static DiagnosticMethodInfo? Create(Delegate @delegate)
        {
            ArgumentNullException.ThrowIfNull(@delegate);

            if (!StackTrace.IsSupported)
                return null;

            return new DiagnosticMethodInfo(@delegate.Method);

        }

        /// <summary>
        /// Creates a <see cref="DiagnosticMethodInfo"/> that represents the method this stack frame is associtated with.
        /// </summary>
        /// <remarks>This returns the definition of the target method, with stripped instantiation information.
        /// The return value might be null if the `StackTraceSupport` feature switch is set to false.
        /// The return value might be null if the target method is unknown.</remarks>
        [UnconditionalSuppressMessage("Trimming", "IL2026",
            Justification = "IL-level trimming doesn't remove method name and owning type information; this implementation is not used with native AOT trimming")]
        public static DiagnosticMethodInfo? Create(StackFrame frame)
        {
            ArgumentNullException.ThrowIfNull(frame);

            if (!StackTrace.IsSupported)
                return null;

            MethodBase? method = frame.GetMethod();
            if (method != null)
                return new DiagnosticMethodInfo(method);

            return null;
        }
#endif
    }
}
