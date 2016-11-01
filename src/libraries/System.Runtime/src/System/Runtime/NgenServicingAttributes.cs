// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Runtime
{
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    public sealed class AssemblyTargetedPatchBandAttribute : Attribute
    {
        private String m_targetedPatchBand;

        public AssemblyTargetedPatchBandAttribute(String targetedPatchBand)
        {
            m_targetedPatchBand = targetedPatchBand;
        }

        public String TargetedPatchBand
        {
            get { return m_targetedPatchBand; }
        }
    }

    //============================================================================================================
    // Sacrifices cheap servicing of a method body in order to allow unrestricted inlining.  Certain types of
    // trivial methods (e.g. simple property getters) are automatically attributed by ILCA.EXE during the build.
    // For other performance critical methods, it should be added manually.
    //============================================================================================================

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    public sealed class TargetedPatchingOptOutAttribute : Attribute
    {
        private String m_reason;

        public TargetedPatchingOptOutAttribute(String reason)
        {
            m_reason = reason;
        }

        public String Reason
        {
            get { return m_reason; }
        }

        private TargetedPatchingOptOutAttribute() { }
    }
}