// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace System
{
    internal static partial class StartupHookProvider
    {
        private const string StartupHookTypeName = "StartupHook";
        private const string InitializeMethodName = "Initialize";
        private const string DisallowedSimpleAssemblyNameSuffix = ".dll";

#pragma warning disable IL4000
        // Suppression can be removed once we have feature switch attributes
        [FeatureCheck(typeof(RequiresUnreferencedCodeAttribute))]
        private static bool IsSupported => AppContext.TryGetSwitch("System.StartupHookProvider.IsSupported", out bool isSupported) ? isSupported : true;
#pragma warning restore IL4000

        private struct StartupHookNameOrPath
        {
            public AssemblyName AssemblyName;
            public string Path;
        }

        // Parse a string specifying a list of assemblies and types
        // containing a startup hook, and call each hook in turn.
        private static void ProcessStartupHooks(string diagnosticStartupHooks)
        {
            if (!IsSupported)
                return;

            string? startupHooksVariable = AppContext.GetData("STARTUP_HOOKS") as string;
            if (null == startupHooksVariable && string.IsNullOrEmpty(diagnosticStartupHooks))
                return;

            List<string> startupHookParts = new();

            if (!string.IsNullOrEmpty(diagnosticStartupHooks))
            {
                startupHookParts.AddRange(diagnosticStartupHooks.Split(Path.PathSeparator));
            }

            if (null != startupHooksVariable)
            {
                startupHookParts.AddRange(startupHooksVariable.Split(Path.PathSeparator));
            }

            // Parse startup hooks variable
            StartupHookNameOrPath[] startupHooks = new StartupHookNameOrPath[startupHookParts.Count];
            for (int i = 0; i < startupHookParts.Count; i++)
            {
                ParseStartupHook(ref startupHooks[i], startupHookParts[i]);
            }

            // Call each startup hook
            for (int i = 0; i < startupHooks.Length; i++)
            {
#pragma warning disable IL2026 // suppressed in ILLink.Suppressions.LibraryBuild.xml
                CallStartupHook(startupHooks[i]);
#pragma warning restore IL2026
            }
        }

        // Parse a string specifying a single entry containing a startup hook,
        // and call the hook.
        [UnconditionalSuppressMessageAttribute("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "An ILLink warning when trimming an app with System.StartupHookProvider.IsSupported=true already exists for ProcessStartupHooks.")]
        private static unsafe void CallStartupHook(char* pStartupHookPart)
        {
            if (!IsSupported)
                return;

            StartupHookNameOrPath startupHook = default(StartupHookNameOrPath);

            ParseStartupHook(ref startupHook, new string(pStartupHookPart));

#pragma warning disable IL2026 // suppressed in ILLink.Suppressions.LibraryBuild.xml
            CallStartupHook(startupHook);
#pragma warning restore IL2026
        }

        private static void ParseStartupHook(ref StartupHookNameOrPath startupHook, string startupHookPart)
        {
            ReadOnlySpan<char> disallowedSimpleAssemblyNameChars = stackalloc char[4]
            {
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar,
                ' ',
                ','
            };

            if (string.IsNullOrEmpty(startupHookPart))
            {
                return;
            }

            if (Path.IsPathFullyQualified(startupHookPart))
            {
                startupHook.Path = startupHookPart;
            }
            else
            {
                // The intent here is to only support simple assembly names, but AssemblyName .ctor accepts
                // lot of other forms (fully qualified assembly name, strings which look like relative paths and so on).
                // So add a check on top which will disallow any directory separator, space or comma in the assembly name.
                for (int j = 0; j < disallowedSimpleAssemblyNameChars.Length; j++)
                {
                    if (startupHookPart.Contains(disallowedSimpleAssemblyNameChars[j]))
                    {
                        throw new ArgumentException(SR.Format(SR.Argument_InvalidStartupHookSimpleAssemblyName, startupHookPart));
                    }
                }

                if (startupHookPart.EndsWith(DisallowedSimpleAssemblyNameSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException(SR.Format(SR.Argument_InvalidStartupHookSimpleAssemblyName, startupHookPart));
                }

                try
                {
                    // This will throw if the string is not a valid assembly name.
                    startupHook.AssemblyName = new AssemblyName(startupHookPart);
                }
                catch (Exception assemblyNameException)
                {
                    throw new ArgumentException(SR.Format(SR.Argument_InvalidStartupHookSimpleAssemblyName, startupHookPart), assemblyNameException);
                }
            }
        }

        // Load the specified assembly, and call the specified type's
        // "static void Initialize()" method.
        [RequiresUnreferencedCode("The StartupHookSupport feature switch has been enabled for this app which is being trimmed. " +
            "Startup hook code is not observable by the trimmer and so required assemblies, types and members may be removed")]
        private static void CallStartupHook(StartupHookNameOrPath startupHook)
        {
            Assembly assembly;
            try
            {
                if (startupHook.Path != null)
                {
                    Debug.Assert(Path.IsPathFullyQualified(startupHook.Path));
                    assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(startupHook.Path);
                }
                else if (startupHook.AssemblyName != null)
                {
                    Debug.Assert(startupHook.AssemblyName != null);
                    assembly = AssemblyLoadContext.Default.LoadFromAssemblyName(startupHook.AssemblyName);
                }
                else
                {
                    // Empty slot - skip it
                    return;
                }
            }
            catch (Exception assemblyLoadException)
            {
                throw new ArgumentException(
                    SR.Format(SR.Argument_StartupHookAssemblyLoadFailed, startupHook.Path ?? startupHook.AssemblyName!.ToString()),
                    assemblyLoadException);
            }

            Debug.Assert(assembly != null);
            Type type = assembly.GetType(StartupHookTypeName, throwOnError: true)!;

            // Look for a static method without any parameters
            MethodInfo? initializeMethod = type.GetMethod(InitializeMethodName,
                                                         BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                                                         null, // use default binder
                                                         Type.EmptyTypes, // parameters
                                                         null); // no parameter modifiers
            if (initializeMethod == null)
            {
                // There weren't any static methods without
                // parameters. Look for any methods with the correct
                // name, to provide precise error handling.
                try
                {
                    // This could find zero, one, or multiple methods
                    // with the correct name.
                    MethodInfo? wrongSigMethod = type.GetMethod(InitializeMethodName,
                                                      BindingFlags.Public | BindingFlags.NonPublic |
                                                      BindingFlags.Static | BindingFlags.Instance);
                    // Didn't find any
                    if (wrongSigMethod == null)
                    {
                        throw new MissingMethodException(StartupHookTypeName, InitializeMethodName);
                    }
                }
                catch (AmbiguousMatchException)
                {
                    // Found multiple. Will throw below due to initializeMethod being null.
                    Debug.Assert(initializeMethod == null);
                }
            }

            // Found Initialize method(s) with non-conforming signatures
            if (initializeMethod == null || initializeMethod.ReturnType != typeof(void))
            {
                throw new ArgumentException(SR.Format(SR.Argument_InvalidStartupHookSignature,
                                                      StartupHookTypeName + Type.Delimiter + InitializeMethodName,
                                                      startupHook.Path ?? startupHook.AssemblyName.ToString()));
            }

            Debug.Assert(initializeMethod != null &&
                         initializeMethod.IsStatic &&
                         initializeMethod.ReturnType == typeof(void) &&
                         initializeMethod.GetParametersAsSpan().Length == 0);

            initializeMethod.Invoke(null, null);
        }
    }
}
