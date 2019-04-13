// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
** 
**
**  
**
** Purpose: Represents a Label to the ILGenerator class.
**
** 
===========================================================*/

#nullable enable
namespace System.Reflection.Emit
{
    // The Label class is an opaque representation of a label used by the 
    // ILGenerator class.  The token is used to mark where labels occur in the IL
    // stream and then the necessary offsets are put back in the code when the ILGenerator 
    // is passed to the MethodWriter.
    // Labels are created by using ILGenerator.CreateLabel and their position is set
    // by using ILGenerator.MarkLabel.
    public struct Label : IEquatable<Label>
    {
        internal int m_label;

        //public Label() {
        //    m_label=0;
        //}

        internal Label(int label)
        {
            m_label = label;
        }

        internal int GetLabelValue()
        {
            return m_label;
        }

        public override int GetHashCode()
        {
            return m_label;
        }

        public override bool Equals(object? obj)
        {
            if (obj is Label)
                return Equals((Label)obj);
            else
                return false;
        }

        public bool Equals(Label obj)
        {
            return obj.m_label == m_label;
        }

        public static bool operator ==(Label a, Label b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Label a, Label b)
        {
            return !(a == b);
        }
    }
}
