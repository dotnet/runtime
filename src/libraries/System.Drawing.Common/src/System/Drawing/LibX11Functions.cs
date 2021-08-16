// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Originally in System.Drawing.gdipFunctions.cs

using System.Runtime.InteropServices;

namespace System.Drawing
{
    internal static class LibX11Functions
    {
        // Some special X11 stuff
        [DllImport("libX11", EntryPoint = "XOpenDisplay")]
        internal static extern IntPtr XOpenDisplay(IntPtr display);

        [DllImport("libX11", EntryPoint = "XCloseDisplay")]
        internal static extern int XCloseDisplay(IntPtr display);

        [DllImport("libX11", EntryPoint = "XRootWindow")]
        internal static extern IntPtr XRootWindow(IntPtr display, int screen);

        [DllImport("libX11", EntryPoint = "XDefaultScreen")]
        internal static extern int XDefaultScreen(IntPtr display);

        [DllImport("libX11", EntryPoint = "XDefaultDepth")]
        internal static extern uint XDefaultDepth(IntPtr display, int screen);

        [DllImport("libX11", EntryPoint = "XGetImage")]
        internal static extern IntPtr XGetImage(IntPtr display, IntPtr drawable, int src_x, int src_y, int width, int height, int pane, int format);

        [DllImport("libX11", EntryPoint = "XGetPixel")]
        internal static extern int XGetPixel(IntPtr image, int x, int y);

        [DllImport("libX11", EntryPoint = "XDestroyImage")]
        internal static extern int XDestroyImage(IntPtr image);

        [DllImport("libX11", EntryPoint = "XDefaultVisual")]
        internal static extern IntPtr XDefaultVisual(IntPtr display, int screen);

        [DllImport("libX11", EntryPoint = "XGetVisualInfo")]
        internal static extern IntPtr XGetVisualInfo(IntPtr display, int vinfo_mask, ref XVisualInfo vinfo_template, ref int nitems);

        [DllImport("libX11", EntryPoint = "XVisualIDFromVisual")]
        internal static extern IntPtr XVisualIDFromVisual(IntPtr visual);

        [DllImport("libX11", EntryPoint = "XFree")]
        internal static extern void XFree(IntPtr data);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct XVisualInfo
    {
        internal IntPtr visual;
        internal IntPtr visualid;
        internal int screen;
        internal uint depth;
        internal int klass;
        internal IntPtr red_mask;
        internal IntPtr green_mask;
        internal IntPtr blue_mask;
        internal int colormap_size;
        internal int bits_per_rgb;
    }
}
