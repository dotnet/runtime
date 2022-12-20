// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace System
{
    internal static class StartupHookProvider
    {
        private const string StartupHookTypeName = "StartupHook";
        private const string InitializeMethodName = "Initialize";
        private const string DisallowedSimpleAssemblyNameSuffix = ".dll";

        private static bool IsSupported => AppContext.TryGetSwitch("System.StartupHookProvider.IsSupported", out bool isSupported) ? isSupported : true;

        private struct StartupHookNameOrPath
        {
            public AssemblyName AssemblyName;
            public string Path;
        }

        // Parse a string specifying a list of assemblies and types
        // containing a startup hook, and call each hook in turn.
        private static void ProcessStartupHooks()
        {
            if (!IsSupported)
                return;

            // Initialize tracing before any user code can be called if EventSource is enabled.
            if (EventSource.IsSupported)
            {
                System.Diagnostics.Tracing.RuntimeEventSource.Initialize();
            }

            string? startupHooksVariable = AppContext.GetData("STARTUP_HOOKS") as string;
            if (startupHooksVariable == null)
            {
                return;
            }

            ReadOnlySpan<char> disallowedSimpleAssemblyNameChars = stackalloc char[4]
            {
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar,
                ' ',
                ','
            };

            // Parse startup hooks variable
            string[] startupHookParts = startupHooksVariable.Split(Path.PathSeparator);
            StartupHookNameOrPath[] startupHooks = new StartupHookNameOrPath[startupHookParts.Length];
            for (int i = 0; i < startupHookParts.Length; i++)
            {
                string startupHookPart = startupHookParts[i];
                if (string.IsNullOrEmpty(startupHookPart))
                {
                    // Leave the slot in startupHooks empty (nulls for everything). This is simpler than shifting and resizing the array.
                    continue;
                }

                if (Path.IsPathFullyQualified(startupHookPart))
                {
                    startupHooks[i].Path = startupHookPart;
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
                        startupHooks[i].AssemblyName = new AssemblyName(startupHookPart);
                    }
                    catch (Exception assemblyNameException)
                    {
                        throw new ArgumentException(SR.Format(SR.Argument_InvalidStartupHookSimpleAssemblyName, startupHookPart), assemblyNameException);
                    }
                }
            }

            // Call each hook in turn
            foreach (StartupHookNameOrPath startupHook in startupHooks)
            {
#pragma warning disable IL2026 // suppressed in ILLink.Suppressions.LibraryBuild.xml
                CallStartupHook(startupHook);
#pragma warning restore IL2026
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

            bool wrongSignature = false;
            if (initializeMethod == null)
            {
                // There weren't any static methods without
                // parameters. Look for any methods with the correct
                // name, to provide precise error handling.
                try
                {
                    // This could find zero, one, or multiple methods
                    // with the correct name.
                    initializeMethod = type.GetMethod(InitializeMethodName,
                                                      BindingFlags.Public | BindingFlags.NonPublic |
                                                      BindingFlags.Static | BindingFlags.Instance);
                }
                catch (AmbiguousMatchException)
                {
                    // Found multiple. Will throw below due to initializeMethod being null.
                    Debug.Assert(initializeMethod == null);
                }

                if (initializeMethod != null)
                {
                    // Found one
                    wrongSignature = true;
                }
                else
                {
                    // Didn't find any
                    throw new MissingMethodException(StartupHookTypeName, InitializeMethodName);
                }
            }
            else if (initializeMethod.ReturnType != typeof(void))
            {
                wrongSignature = true;
            }

            if (wrongSignature)
            {
                throw new ArgumentException(SR.Format(SR.Argument_InvalidStartupHookSignature,
                                                      StartupHookTypeName + Type.Delimiter + InitializeMethodName,
                                                      startupHook.Path ?? startupHook.AssemblyName.ToString()));
            }

            Debug.Assert(initializeMethod != null &&
                         initializeMethod.IsStatic &&
                         initializeMethod.ReturnType == typeof(void) &&
                         initializeMethod.GetParameters().Length == 0);

            initializeMethod.Invoke(null, null);
        }
    }
}
