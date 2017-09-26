// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// Authors: 
//    Alexandre Pigolkine (pigolkine@gmx.de)
//    Jordi Mas i Hernandez (jordi@ximian.com)
//    Sanjay Gupta (gsanjay@novell.com)
//    Ravindra (rkumar@novell.com)
//    Peter Dennis Bartok (pbartok@novell.com)
//    Sebastien Pouliot <sebastien@ximian.com>
//
// Copyright (C) 2004 - 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.Runtime.InteropServices;

namespace System.Drawing
{
    internal static class MarshallingHelpers
    {
        // Copies a Ptr to an array of Points and releases the memory
        static public void FromUnManagedMemoryToPointI(IntPtr prt, Point[] pts)
        {
            int nPointSize = Marshal.SizeOf(pts[0]);
            IntPtr pos = prt;
            for (int i = 0; i < pts.Length; i++, pos = new IntPtr(pos.ToInt64() + nPointSize))
                pts[i] = (Point)Marshal.PtrToStructure(pos, typeof(Point));

            Marshal.FreeHGlobal(prt);
        }

        // Copies a Ptr to an array of Points and releases the memory
        static public void FromUnManagedMemoryToPoint(IntPtr prt, PointF[] pts)
        {
            int nPointSize = Marshal.SizeOf(pts[0]);
            IntPtr pos = prt;
            for (int i = 0; i < pts.Length; i++, pos = new IntPtr(pos.ToInt64() + nPointSize))
                pts[i] = (PointF)Marshal.PtrToStructure(pos, typeof(PointF));

            Marshal.FreeHGlobal(prt);
        }

        // Copies an array of Points to unmanaged memory
        static public IntPtr FromPointToUnManagedMemoryI(Point[] pts)
        {
            int nPointSize = Marshal.SizeOf(pts[0]);
            IntPtr dest = Marshal.AllocHGlobal(nPointSize * pts.Length);
            IntPtr pos = dest;
            for (int i = 0; i < pts.Length; i++, pos = new IntPtr(pos.ToInt64() + nPointSize))
                Marshal.StructureToPtr(pts[i], pos, false);

            return dest;
        }

        // Copies an array of Points to unmanaged memory
        static public IntPtr FromPointToUnManagedMemory(PointF[] pts)
        {
            int nPointSize = Marshal.SizeOf(pts[0]);
            IntPtr dest = Marshal.AllocHGlobal(nPointSize * pts.Length);
            IntPtr pos = dest;
            for (int i = 0; i < pts.Length; i++, pos = new IntPtr(pos.ToInt64() + nPointSize))
                Marshal.StructureToPtr(pts[i], pos, false);

            return dest;
        }
    }
}
