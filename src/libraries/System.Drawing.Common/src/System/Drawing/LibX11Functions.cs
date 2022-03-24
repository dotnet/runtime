// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Originally in System.Drawing.gdipFunctions.cs

using System.Runtime.InteropServices;

namespace System.Drawing
{
    internal static partial class LibX11Functions
    {
        // Some special X11 stuff
        [LibraryImport("libX11", EntryPoint = "XOpenDisplay")]
        internal static partial IntPtr XOpenDisplay(IntPtr display);

        [LibraryImport("libX11", EntryPoint = "XCloseDisplay")]
        internal static partial int XCloseDisplay(IntPtr display);

        [LibraryImport("libX11", EntryPoint = "XRootWindow")]
        internal static partial IntPtr XRootWindow(IntPtr display, int screen);

        [LibraryImport("libX11", EntryPoint = "XDefaultScreen")]
        internal static partial int XDefaultScreen(IntPtr display);

        [LibraryImport("libX11", EntryPoint = "XDefaultDepth")]
        internal static partial uint XDefaultDepth(IntPtr display, int screen);

        [LibraryImport("libX11", EntryPoint = "XGetImage")]
        internal static partial IntPtr XGetImage(IntPtr display, IntPtr drawable, int src_x, int src_y, int width, int height, int pane, int format);

        [LibraryImport("libX11", EntryPoint = "XGetPixel")]
        internal static partial int XGetPixel(IntPtr image, int x, int y);

        [LibraryImport("libX11", EntryPoint = "XDestroyImage")]
        internal static partial int XDestroyImage(IntPtr image);

        [LibraryImport("libX11", EntryPoint = "XDefaultVisual")]
        internal static partial IntPtr XDefaultVisual(IntPtr display, int screen);

        [LibraryImport("libX11", EntryPoint = "XGetVisualInfo")]
        internal static partial IntPtr XGetVisualInfo(IntPtr display, int vinfo_mask, ref XVisualInfo vinfo_template, ref int nitems);

        [LibraryImport("libX11", EntryPoint = "XVisualIDFromVisual")]
        internal static partial IntPtr XVisualIDFromVisual(IntPtr visual);

        [LibraryImport("libX11", EntryPoint = "XFree")]
        internal static partial void XFree(IntPtr data);
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
