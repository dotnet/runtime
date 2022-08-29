// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

[assembly: UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
    Target = "M:System.Security.Cryptography.CryptoConfigForwarder.#cctor",
    Scope = "member",
    Justification = "The cctor caches the RequiresUnreferencedCode call in a delegate, and usage of that delegate is marked with RequiresUnreferencedCode.")]

namespace System.Security.Cryptography
{
    internal static class CryptoConfigForwarder
    {
        internal const string CreateFromNameUnreferencedCodeMessage = "The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.";

        // Suppressed for the linker by the assembly-level UnconditionalSuppressMessageAttribute
        // https://github.com/dotnet/linker/issues/2648
#pragma warning disable IL2026
        private static readonly Func<string, object?> s_createFromName = BindCreateFromName();
#pragma warning restore IL2026

        [RequiresUnreferencedCode(CreateFromNameUnreferencedCodeMessage)]
        private static Func<string, object?> BindCreateFromName()
        {
            const string CryptoConfigTypeName =
                "System.Security.Cryptography.CryptoConfig, System.Security.Cryptography.Algorithms";

            const string CreateFromNameMethodName = "CreateFromName";

            Type t = Type.GetType(CryptoConfigTypeName, throwOnError: true)!;
            MethodInfo? createFromName = t.GetMethod(CreateFromNameMethodName, new[] { typeof(string) });

            if (createFromName == null)
            {
                throw new MissingMethodException(t.FullName, CreateFromNameMethodName);
            }

            return createFromName.CreateDelegate<Func<string, object?>>();
        }

        [RequiresUnreferencedCode(CreateFromNameUnreferencedCodeMessage)]
        internal static T? CreateFromName<T>(string name) where T : class
        {
            object? o = s_createFromName(name);
            try
            {
                return (T?)o;
            }
            catch
            {
                (o as IDisposable)?.Dispose();
                throw;
            }
        }

        internal static HashAlgorithm CreateDefaultHashAlgorithm() =>
            throw new PlatformNotSupportedException(SR.Cryptography_DefaultAlgorithm_NotSupported);
    }
}
