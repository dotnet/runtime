// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime;

using Internal.Runtime.Augments;

namespace Internal.DeveloperExperience
{
    internal class DeveloperExperience
    {
        public virtual string CreateStackTraceString(IntPtr ip, bool includeFileInfo, out bool isStackTraceHidden)
        {
            string methodName = GetMethodName(ip, out IntPtr methodStart, out isStackTraceHidden);
            if (methodName != null)
            {
                if (ip != methodStart)
                {
                    methodName = $"{methodName} + 0x{(ip - methodStart):x}";
                }
                return methodName;
            }

            // If we don't have precise information, try to map it at least back to the right module.
            string moduleFullFileName = RuntimeAugments.TryGetFullPathToApplicationModule(ip, out IntPtr moduleBase);

            // Without any callbacks or the ability to map ip correctly we better admit that we don't know
            if (string.IsNullOrEmpty(moduleFullFileName))
            {
                return "<unknown>";
            }

            ReadOnlySpan<char> fileNameWithoutExtension = Path.GetFileNameWithoutExtension(moduleFullFileName.AsSpan());
            int rva = (int)(ip - moduleBase);
            return $"{fileNameWithoutExtension}!<BaseAddress>+0x{rva:x}";
        }

        internal static string GetMethodName(IntPtr ip, out IntPtr methodStart, out bool isStackTraceHidden)
        {
            methodStart = IntPtr.Zero;
            StackTraceMetadataCallbacks stackTraceCallbacks = RuntimeAugments.StackTraceCallbacksIfAvailable;
            if (stackTraceCallbacks != null)
            {
                methodStart = RuntimeImports.RhFindMethodStartAddress(ip);
                if (methodStart != IntPtr.Zero)
                {
                    return stackTraceCallbacks.TryGetMethodNameFromStartAddress(methodStart, out isStackTraceHidden);
                }
            }
            isStackTraceHidden = false;
            return null;
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

        private static DeveloperExperience s_developerExperience;
    }
}
