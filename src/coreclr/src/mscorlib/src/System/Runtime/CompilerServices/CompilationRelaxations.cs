// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////


using System;

namespace System.Runtime.CompilerServices
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module | AttributeTargets.Class | AttributeTargets.Method)]
    public class CompilationRelaxationsAttribute : Attribute
    {
        private int m_relaxations;      // The relaxations.

        public CompilationRelaxationsAttribute(
            int relaxations)
        {
            m_relaxations = relaxations;
        }

        public CompilationRelaxationsAttribute(
            CompilationRelaxations relaxations)
        {
            m_relaxations = (int)relaxations;
        }

        public int CompilationRelaxations
        {
            get
            {
                return m_relaxations;
            }
        }
    }
}
