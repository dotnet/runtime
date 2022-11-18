// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// We want the Debug.WriteLine statements below to actually do something.
#define DEBUG

using System;
using System.Text;
using System.Runtime;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Reflection;

using Internal.Runtime.Augments;

namespace Internal.DeveloperExperience
{
    [System.Runtime.CompilerServices.ReflectionBlocked]
    public class DeveloperExperience
    {
        /// <summary>
        /// Check the AppCompat switch 'Diagnostics.DisableMetadataStackTraceResolution'.
        /// Some customers use DIA-based tooling to translate stack traces in the raw format
        /// (module)+RVA - for them, stack trace and reflection metadata-based resolution
        /// constitutes technically a regression because these two resolution methods today cannot
        /// provide file name and line number information; PDB-based tooling can easily do that
        /// based on the RVA information.
        /// </summary>
        private static bool IsMetadataStackTraceResolutionDisabled()
        {
            AppContext.TryGetSwitch("Diagnostics.DisableMetadataStackTraceResolution", out bool disableMetadata);
            return disableMetadata;
        }

        public virtual string CreateStackTraceString(IntPtr ip, bool includeFileInfo)
        {
            if (!IsMetadataStackTraceResolutionDisabled())
            {
                StackTraceMetadataCallbacks stackTraceCallbacks = RuntimeAugments.StackTraceCallbacksIfAvailable;
                if (stackTraceCallbacks != null)
                {
                    IntPtr methodStart = RuntimeImports.RhFindMethodStartAddress(ip);
                    if (methodStart != IntPtr.Zero)
                    {
                        string methodName = stackTraceCallbacks.TryGetMethodNameFromStartAddress(methodStart);
                        if (methodName != null)
                        {
                            if (ip != methodStart)
                            {
                                methodName += " + 0x" + (ip.ToInt64() - methodStart.ToInt64()).ToString("x");
                            }
                            return methodName;
                        }
                    }
                }
            }

            // If we don't have precise information, try to map it at least back to the right module.
            string moduleFullFileName = RuntimeAugments.TryGetFullPathToApplicationModule(ip, out IntPtr moduleBase);

            // Without any callbacks or the ability to map ip correctly we better admit that we don't know
            if (string.IsNullOrEmpty(moduleFullFileName))
            {
                return "<unknown>";
            }

            StringBuilder sb = new StringBuilder();
            string fileNameWithoutExtension = GetFileNameWithoutExtension(moduleFullFileName);
            int rva = (int)(ip.ToInt64() - moduleBase.ToInt64());
            sb.Append(fileNameWithoutExtension);
            sb.Append("!<BaseAddress>+0x");
            sb.Append(rva.ToString("x"));
            return sb.ToString();
        }

        public virtual void TryGetSourceLineInfo(IntPtr ip, out string fileName, out int lineNumber, out int columnNumber)
        {
            fileName = null;
            lineNumber = 0;
            columnNumber = 0;
        }

        public virtual void TryGetILOffsetWithinMethod(IntPtr ip, out int ilOffset)
        {
            ilOffset = StackFrame.OFFSET_UNKNOWN;
        }

        /// <summary>
        /// Makes reasonable effort to get the MethodBase reflection info. Returns null if it can't.
        /// </summary>
        public virtual void TryGetMethodBase(IntPtr methodStartAddress, out MethodBase method)
        {
            ReflectionExecutionDomainCallbacks reflectionCallbacks = RuntimeAugments.CallbacksIfAvailable;
            method = null;
            if (reflectionCallbacks != null)
            {
                method = reflectionCallbacks.GetMethodBaseFromStartAddressIfAvailable(methodStartAddress);
            }
        }

        public virtual bool OnContractFailure(string? stackTrace, ContractFailureKind contractFailureKind, string? displayMessage, string userMessage, string conditionText, Exception innerException)
        {
            Debug.WriteLine("Assertion failed: " + (displayMessage ?? ""));
            if (Debugger.IsAttached)
                Debugger.Break();
            return false;
        }

        public static DeveloperExperience Default
        {
            get
            {
                DeveloperExperience result = s_developerExperience;
                if (result == null)
                    return new DeveloperExperience(); // Provide the bare-bones default if a custom one hasn't been supplied.
                return result;
            }

            set
            {
                s_developerExperience = value;
            }
        }

        private static string GetFileNameWithoutExtension(string path)
        {
            path = GetFileName(path);
            int i;
            if ((i = path.LastIndexOf('.')) == -1)
                return path; // No path extension found
            else
                return path.Substring(0, i);
        }

        private static string GetFileName(string path)
        {
            int length = path.Length;
            for (int i = length; --i >= 0;)
            {
                char ch = path[i];
                if (ch == '/' || ch == '\\' || ch == ':')
                    return path.Substring(i + 1, length - i - 1);
            }
            return path;
        }

        private static DeveloperExperience s_developerExperience;
    }
}
