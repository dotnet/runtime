// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace System
{
    internal static class StartupHookProvider
    {
        private const string StartupHookTypeName = "StartupHook";
        private const string InitializeMethodName = "Initialize";

        private struct StartupHookNameOrPath
        {
            public AssemblyName AssemblyName;
            public string Path;
        }

        // Parse a string specifying a list of assemblies and types
        // containing a startup hook, and call each hook in turn.
        private static void ProcessStartupHooks()
        {
            // Initialize tracing before any user code can be called.
            System.Diagnostics.Tracing.EventPipeController.Initialize();

            string startupHooksVariable = (string)AppContext.GetData("STARTUP_HOOKS");
            if (startupHooksVariable == null)
            {
                return;
            }

            // Parse startup hooks variable
            string[] startupHookParts = startupHooksVariable.Split(Path.PathSeparator);
            StartupHookNameOrPath[] startupHooks = new StartupHookNameOrPath[startupHookParts.Length];
            for (int i = 0; i < startupHookParts.Length; i++)
            {
                string startupHookPart = startupHookParts[i];
                if (string.IsNullOrEmpty(startupHookPart))
                {
                    throw new ArgumentException(SR.Argument_InvalidStartupHookSyntax);
                }

                if (Path.IsPathFullyQualified(startupHookPart))
                {
                    startupHooks[i].Path = startupHookPart;
                }
                else
                {
                    // This will throw if the string is not a valid assembly name.
                    startupHooks[i].AssemblyName = new AssemblyName(startupHookPart);
                }
            }

            // Call each hook in turn
            foreach (StartupHookNameOrPath startupHook in startupHooks)
            {
                CallStartupHook(startupHook);
            }
        }

        // Load the specified assembly, and call the specified type's
        // "static void Initialize()" method.
        private static void CallStartupHook(StartupHookNameOrPath startupHook)
        {
            Debug.Assert(startupHook.Path != null || startupHook.AssemblyName != null);

            Assembly assembly;
            if (startupHook.Path != null)
            {
                Debug.Assert(Path.IsPathFullyQualified(startupHook.Path));
                assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(startupHook.Path);
            }
            else
            {
                Debug.Assert(startupHook.AssemblyName != null);
                assembly = AssemblyLoadContext.Default.LoadFromAssemblyName(startupHook.AssemblyName);
            }

            Debug.Assert(assembly != null);
            Type type = assembly.GetType(StartupHookTypeName, throwOnError: true);

            // Look for a static method without any parameters
            MethodInfo initializeMethod = type.GetMethod(InitializeMethodName,
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
