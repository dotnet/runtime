// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Originally in System.Drawing.gdipFunctions.cs

using System.Runtime.InteropServices;

namespace System.Drawing
{
    internal static class LibX11Functions
    {
        // Some special X11 stuff
        [DllImport("libX11", EntryPoint = "XOpenDisplay")]
        internal extern static IntPtr XOpenDisplay(IntPtr display);

        [DllImport("libX11", EntryPoint = "XCloseDisplay")]
        internal extern static int XCloseDisplay(IntPtr display);

        [DllImport("libX11", EntryPoint = "XRootWindow")]
        internal extern static IntPtr XRootWindow(IntPtr display, int screen);

        [DllImport("libX11", EntryPoint = "XDefaultScreen")]
        internal extern static int XDefaultScreen(IntPtr display);

        [DllImport("libX11", EntryPoint = "XDefaultDepth")]
        internal extern static uint XDefaultDepth(IntPtr display, int screen);

        [DllImport("libX11", EntryPoint = "XGetImage")]
        internal extern static IntPtr XGetImage(IntPtr display, IntPtr drawable, int src_x, int src_y, int width, int height, int pane, int format);

        [DllImport("libX11", EntryPoint = "XGetPixel")]
        internal extern static int XGetPixel(IntPtr image, int x, int y);

        [DllImport("libX11", EntryPoint = "XDestroyImage")]
        internal extern static int XDestroyImage(IntPtr image);

        [DllImport("libX11", EntryPoint = "XDefaultVisual")]
        internal extern static IntPtr XDefaultVisual(IntPtr display, int screen);

        [DllImport("libX11", EntryPoint = "XGetVisualInfo")]
        internal extern static IntPtr XGetVisualInfo(IntPtr display, int vinfo_mask, ref XVisualInfo vinfo_template, ref int nitems);

        [DllImport("libX11", EntryPoint = "XVisualIDFromVisual")]
        internal extern static IntPtr XVisualIDFromVisual(IntPtr visual);

        [DllImport("libX11", EntryPoint = "XFree")]
        internal extern static void XFree(IntPtr data);
    }
}
