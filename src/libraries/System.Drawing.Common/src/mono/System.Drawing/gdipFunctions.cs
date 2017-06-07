// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Drawing.gdipFunctions.cs
//
// Authors: 
//	Alexandre Pigolkine (pigolkine@gmx.de)
//	Jordi Mas i Hernandez (jordi@ximian.com)
//	Sanjay Gupta (gsanjay@novell.com)
//	Ravindra (rkumar@novell.com)
//	Peter Dennis Bartok (pbartok@novell.com)
//	Sebastien Pouliot <sebastien@ximian.com>
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

using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.Security;
using System.Runtime.InteropServices.ComTypes;

namespace System.Drawing
{
    /// <summary>
    /// GDI+ API Functions
    /// </summary>
    internal static class GDIPlus
    {
        public const int FACESIZE = 32;
        public const int LANG_NEUTRAL = 0;
        public static IntPtr Display = IntPtr.Zero;
        public static bool UseX11Drawable = false;
        public static bool UseCarbonDrawable = false;
        public static bool UseCocoaDrawable = false;

        private const string GdiPlus = "gdiplus";

        #region gdiplus.dll functions

        // startup / shutdown
        [DllImport(GdiPlus)]
        static internal extern Status GdiplusStartup(ref ulong token, ref GdiplusStartupInput input, ref GdiplusStartupOutput output);
        [DllImport(GdiPlus)]
        static internal extern void GdiplusShutdown(ref ulong token);

        internal static ulong GdiPlusToken = 0;

        static void ProcessExit(object sender, EventArgs e)
        {
            // Called all pending objects and claim any pending handle before
            // shutting down
            GC.Collect();
            GC.WaitForPendingFinalizers();
#if false
			GdiPlusToken = 0;

			// This causes crashes in because this call occurs before all
			// managed GDI+ objects are finalized. When they are finalized
			// they call into a shutdown GDI+ and we crash.
			GdiplusShutdown (ref GdiPlusToken);

			// This causes crashes in Mono libgdiplus because this call
			// occurs before all managed GDI objects are finalized
			// When they are finalized they use the closed display and
			// crash
			if (UseX11Drawable && Display != IntPtr.Zero) {
				XCloseDisplay (Display);
			}
#endif
        }

        static GDIPlus()
        {
#if NETSTANDARD1_6
			bool isUnix = !RuntimeInformation.IsOSPlatform (OSPlatform.Windows);
#else
            int platform = (int)Environment.OSVersion.Platform;
            bool isUnix = (platform == 4) || (platform == 6) || (platform == 128);
#endif

            if (isUnix)
            {
                if (Environment.GetEnvironmentVariable("not_supported_MONO_MWF_USE_NEW_X11_BACKEND") != null || Environment.GetEnvironmentVariable("MONO_MWF_MAC_FORCE_X11") != null)
                {
                    UseX11Drawable = true;
                }
                else
                {
                    IntPtr buf = Marshal.AllocHGlobal(8192);
                    // This is kind of a hack but gets us sysname from uname (struct utsname *name) on
                    // linux and darwin
                    if (uname(buf) != 0)
                    {
                        // WTH: We couldn't detect the OS; lets default to X11
                        UseX11Drawable = true;
                    }
                    else
                    {
                        string os = Marshal.PtrToStringAnsi(buf);
                        if (os == "Darwin")
                            UseCarbonDrawable = true;
                        else
                            UseX11Drawable = true;
                    }
                    Marshal.FreeHGlobal(buf);
                }
            }

            GdiplusStartupInput input = GdiplusStartupInput.MakeGdiplusStartupInput();
            GdiplusStartupOutput output = GdiplusStartupOutput.MakeGdiplusStartupOutput();
            try
            {
                GdiplusStartup(ref GdiPlusToken, ref input, ref output);
            }
            catch (TypeInitializationException)
            {
#if NETCORE
                throw new PlatformNotSupportedException("Could not initialize the GDI+ library.");
#else
                Console.Error.WriteLine(
                    "* ERROR: Can not initialize GDI+ library{0}{0}" +
                    "Please check http://www.mono-project.com/Problem:GDIPlusInit for details",
                    Environment.NewLine);
#endif
            }

            // under MS 1.x this event is raised only for the default application domain
#if !NETSTANDARD1_6
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(ProcessExit);
#endif
        }

        static public bool RunningOnWindows()
        {
            return !UseX11Drawable && !UseCarbonDrawable && !UseCocoaDrawable;
        }

        static public bool RunningOnUnix()
        {
            return UseX11Drawable || UseCarbonDrawable || UseCocoaDrawable;
        }

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

        // Copies a Ptr to an array of v and releases the memory
        static public void FromUnManagedMemoryToRectangles(IntPtr prt, RectangleF[] pts)
        {
            int nPointSize = Marshal.SizeOf(pts[0]);
            IntPtr pos = prt;
            for (int i = 0; i < pts.Length; i++, pos = new IntPtr(pos.ToInt64() + nPointSize))
                pts[i] = (RectangleF)Marshal.PtrToStructure(pos, typeof(RectangleF));

            Marshal.FreeHGlobal(prt);
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

        // Converts a status into exception
        // TODO: Add more status code mappings here
        static internal void CheckStatus(Status status)
        {
            string msg;
            switch (status)
            {
                case Status.Ok:
                    return;
                case Status.GenericError:
                    msg = string.Format("Generic Error [GDI+ status: {0}]", status);
                    throw new Exception(msg);
                case Status.InvalidParameter:
                    msg = string.Format("A null reference or invalid value was found [GDI+ status: {0}]", status);
                    throw new ArgumentException(msg);
                case Status.OutOfMemory:
                    msg = string.Format("Not enough memory to complete operation [GDI+ status: {0}]", status);
                    throw new OutOfMemoryException(msg);
                case Status.ObjectBusy:
                    msg = string.Format("Object is busy and cannot state allow this operation [GDI+ status: {0}]", status);
                    throw new MemberAccessException(msg);
                case Status.InsufficientBuffer:
                    msg = string.Format("Insufficient buffer provided to complete operation [GDI+ status: {0}]", status);
#if NETCORE
                    throw new Exception(msg);
#else
				throw new InternalBufferOverflowException (msg);
#endif
                case Status.PropertyNotSupported:
                    msg = string.Format("Property not supported [GDI+ status: {0}]", status);
                    throw new NotSupportedException(msg);
                case Status.FileNotFound:
                    msg = string.Format("Requested file was not found [GDI+ status: {0}]", status);
                    throw new FileNotFoundException(msg);
                case Status.AccessDenied:
                    msg = string.Format("Access to resource was denied [GDI+ status: {0}]", status);
                    throw new UnauthorizedAccessException(msg);
                case Status.UnknownImageFormat:
                    msg = string.Format("Either the image format is unknown or you don't have the required libraries to decode this format [GDI+ status: {0}]", status);
                    throw new NotSupportedException(msg);
                case Status.NotImplemented:
                    msg = string.Format("The requested feature is not implemented [GDI+ status: {0}]", status);
                    throw new NotImplementedException(msg);
                case Status.WrongState:
                    msg = string.Format("Object is not in a state that can allow this operation [GDI+ status: {0}]", status);
                    throw new ArgumentException(msg);
                case Status.FontFamilyNotFound:
                    msg = string.Format("The requested FontFamily could not be found [GDI+ status: {0}]", status);
                    throw new ArgumentException(msg);
                case Status.ValueOverflow:
                    msg = string.Format("Argument is out of range [GDI+ status: {0}]", status);
                    throw new OverflowException(msg);
                case Status.Win32Error:
                    msg = string.Format("The operation is invalid [GDI+ status: {0}]", status);
                    throw new InvalidOperationException(msg);
                default:
                    msg = string.Format("Unknown Error [GDI+ status: {0}]", status);
                    throw new Exception(msg);
            }
        }

        // Memory functions
        [DllImport(GdiPlus)]
        static internal extern IntPtr GdipAlloc(int size);
        [DllImport(GdiPlus)]
        static internal extern void GdipFree(IntPtr ptr);


        // Brush functions
        [DllImport(GdiPlus)]
        static internal extern Status GdipCloneBrush(IntPtr brush, out IntPtr clonedBrush);
        [DllImport(GdiPlus)]
        static internal extern Status GdipDeleteBrush(IntPtr brush);
        [DllImport(GdiPlus)]
        static internal extern Status GdipGetBrushType(IntPtr brush, out BrushType type);


        // Region functions
        [DllImport(GdiPlus)]
        static internal extern Status GdipCreateRegion(out IntPtr region);

        [DllImport(GdiPlus)]
        static internal extern Status GdipCreateRegionRgnData(byte[] data, int size, out IntPtr region);

        [DllImport(GdiPlus)]
        static internal extern Status GdipDeleteRegion(IntPtr region);

        [DllImport(GdiPlus)]
        static internal extern Status GdipCloneRegion(IntPtr region, out IntPtr cloned);

        [DllImport(GdiPlus)]
        static internal extern Status GdipCreateRegionRect(ref RectangleF rect, out IntPtr region);

        [DllImport(GdiPlus)]
        static internal extern Status GdipCreateRegionRectI(ref Rectangle rect, out IntPtr region);

        [DllImport(GdiPlus)]
        static internal extern Status GdipCreateRegionPath(IntPtr path, out IntPtr region);

        [DllImport(GdiPlus)]
        static internal extern Status GdipTranslateRegion(IntPtr region, float dx, float dy);

        [DllImport(GdiPlus)]
        static internal extern Status GdipTranslateRegionI(IntPtr region, int dx, int dy);

        [DllImport(GdiPlus)]
        static internal extern Status GdipIsVisibleRegionPoint(IntPtr region, float x, float y,
            IntPtr graphics, out bool result);

        [DllImport(GdiPlus)]
        static internal extern Status GdipIsVisibleRegionPointI(IntPtr region, int x, int y,
            IntPtr graphics, out bool result);

        [DllImport(GdiPlus)]
        static internal extern Status GdipIsVisibleRegionRect(IntPtr region, float x, float y, float width,
            float height, IntPtr graphics, out bool result);

        [DllImport(GdiPlus)]
        static internal extern Status GdipIsVisibleRegionRectI(IntPtr region, int x, int y, int width,
            int height, IntPtr graphics, out bool result);


        [DllImport(GdiPlus)]
        static internal extern Status GdipCombineRegionRect(IntPtr region, ref RectangleF rect,
            CombineMode combineMode);

        [DllImport(GdiPlus)]
        static internal extern Status GdipCombineRegionRectI(IntPtr region, ref Rectangle rect,
            CombineMode combineMode);

        [DllImport(GdiPlus)]
        static internal extern Status GdipCombineRegionPath(IntPtr region, IntPtr path, CombineMode combineMode);

        [DllImport(GdiPlus)]
        static internal extern Status GdipGetRegionBounds(IntPtr region, IntPtr graphics, ref RectangleF rect);

        [DllImport(GdiPlus)]
        static internal extern Status GdipSetInfinite(IntPtr region);

        [DllImport(GdiPlus)]
        static internal extern Status GdipSetEmpty(IntPtr region);

        [DllImport(GdiPlus)]
        static internal extern Status GdipIsEmptyRegion(IntPtr region, IntPtr graphics, out bool result);

        [DllImport(GdiPlus)]
        static internal extern Status GdipIsInfiniteRegion(IntPtr region, IntPtr graphics, out bool result);

        [DllImport(GdiPlus)]
        static internal extern Status GdipCombineRegionRegion(IntPtr region, IntPtr region2,
            CombineMode combineMode);

        [DllImport(GdiPlus)]
        static internal extern Status GdipIsEqualRegion(IntPtr region, IntPtr region2,
            IntPtr graphics, out bool result);

        [DllImport(GdiPlus)]
        static internal extern Status GdipGetRegionDataSize(IntPtr region, out int bufferSize);

        [DllImport(GdiPlus)]
        static internal extern Status GdipGetRegionData(IntPtr region, byte[] buffer, int bufferSize,
         out int sizeFilled);

        [DllImport(GdiPlus)]
        static internal extern Status GdipGetRegionScansCount(IntPtr region, out int count, IntPtr matrix);

        [DllImport(GdiPlus)]
        static internal extern Status GdipGetRegionScans(IntPtr region, IntPtr rects, out int count,
            IntPtr matrix);

        [DllImport(GdiPlus)]
        static internal extern Status GdipTransformRegion(IntPtr region, IntPtr matrix);

        [DllImport(GdiPlus)]
        static internal extern Status GdipFillRegion(IntPtr graphics, IntPtr brush, IntPtr region);

        [DllImport(GdiPlus)]
        static internal extern Status GdipGetRegionHRgn(IntPtr region, IntPtr graphics, ref IntPtr hRgn);

        [DllImport(GdiPlus)]
        static internal extern Status GdipCreateRegionHrgn(IntPtr hRgn, out IntPtr region);

        // Solid brush functions
        [DllImport(GdiPlus)]
        static internal extern Status GdipCreateSolidFill(int color, out IntPtr brush);
        [DllImport(GdiPlus)]
        static internal extern Status GdipGetSolidFillColor(IntPtr brush, out int color);
        [DllImport(GdiPlus)]
        static internal extern Status GdipSetSolidFillColor(IntPtr brush, int color);

        // Hatch Brush functions
        [DllImport(GdiPlus)]
        static internal extern Status GdipCreateHatchBrush(HatchStyle hatchstyle, int foreColor, int backColor, out IntPtr brush);
        [DllImport(GdiPlus)]
        static internal extern Status GdipGetHatchStyle(IntPtr brush, out HatchStyle hatchstyle);
        [DllImport(GdiPlus)]
        static internal extern Status GdipGetHatchForegroundColor(IntPtr brush, out int foreColor);
        [DllImport(GdiPlus)]
        static internal extern Status GdipGetHatchBackgroundColor(IntPtr brush, out int backColor);

        // Texture brush functions
        [DllImport(GdiPlus)]
        static internal extern Status GdipGetTextureImage(IntPtr texture, out IntPtr image);
        [DllImport(GdiPlus)]
        static internal extern Status GdipCreateTexture(IntPtr image, WrapMode wrapMode, out IntPtr texture);
        [DllImport(GdiPlus)]
        static internal extern Status GdipCreateTextureIAI(IntPtr image, IntPtr imageAttributes, int x, int y, int width, int height, out IntPtr texture);
        [DllImport(GdiPlus)]
        static internal extern Status GdipCreateTextureIA(IntPtr image, IntPtr imageAttributes, float x, float y, float width, float height, out IntPtr texture);
        [DllImport(GdiPlus)]
        static internal extern Status GdipCreateTexture2I(IntPtr image, WrapMode wrapMode, int x, int y, int width, int height, out IntPtr texture);
        [DllImport(GdiPlus)]
        static internal extern Status GdipCreateTexture2(IntPtr image, WrapMode wrapMode, float x, float y, float width, float height, out IntPtr texture);
        [DllImport(GdiPlus)]
        static internal extern Status GdipGetTextureTransform(IntPtr texture, IntPtr matrix);
        [DllImport(GdiPlus)]
        static internal extern Status GdipSetTextureTransform(IntPtr texture, IntPtr matrix);
        [DllImport(GdiPlus)]
        static internal extern Status GdipGetTextureWrapMode(IntPtr texture, out WrapMode wrapMode);
        [DllImport(GdiPlus)]
        static internal extern Status GdipSetTextureWrapMode(IntPtr texture, WrapMode wrapMode);
        [DllImport(GdiPlus)]
        static internal extern Status GdipMultiplyTextureTransform(IntPtr texture, IntPtr matrix, MatrixOrder order);
        [DllImport(GdiPlus)]
        static internal extern Status GdipResetTextureTransform(IntPtr texture);
        [DllImport(GdiPlus)]
        static internal extern Status GdipRotateTextureTransform(IntPtr texture, float angle, MatrixOrder order);
        [DllImport(GdiPlus)]
        static internal extern Status GdipScaleTextureTransform(IntPtr texture, float sx, float sy, MatrixOrder order);
        [DllImport(GdiPlus)]
        static internal extern Status GdipTranslateTextureTransform(IntPtr texture, float dx, float dy, MatrixOrder order);

        // PathGradientBrush functions
        [DllImport(GdiPlus)]
        static internal extern Status GdipCreatePathGradientFromPath(IntPtr path, out IntPtr brush);
        [DllImport(GdiPlus)]
        static internal extern Status GdipCreatePathGradientI(Point[] points, int count, WrapMode wrapMode, out IntPtr brush);
        [DllImport(GdiPlus)]
        static internal extern Status GdipCreatePathGradient(PointF[] points, int count, WrapMode wrapMode, out IntPtr brush);
        [DllImport(GdiPlus)]
        static internal extern Status GdipGetPathGradientBlendCount(IntPtr brush, out int count);
        [DllImport(GdiPlus)]
        static internal extern Status GdipGetPathGradientBlend(IntPtr brush, float[] blend, float[] positions, int count);
        [DllImport(GdiPlus)]
        static internal extern Status GdipSetPathGradientBlend(IntPtr brush, float[] blend, float[] positions, int count);
        [DllImport(GdiPlus)]
        static internal extern Status GdipGetPathGradientCenterColor(IntPtr brush, out int color);
        [DllImport(GdiPlus)]
        static internal extern Status GdipSetPathGradientCenterColor(IntPtr brush, int color);
        [DllImport(GdiPlus)]
        static internal extern Status GdipGetPathGradientCenterPoint(IntPtr brush, out PointF point);
        [DllImport(GdiPlus)]
        static internal extern Status GdipSetPathGradientCenterPoint(IntPtr brush, ref PointF point);
        [DllImport(GdiPlus)]
        static internal extern Status GdipGetPathGradientFocusScales(IntPtr brush, out float xScale, out float yScale);
        [DllImport(GdiPlus)]
        static internal extern Status GdipSetPathGradientFocusScales(IntPtr brush, float xScale, float yScale);
        [DllImport(GdiPlus)]
        static internal extern Status GdipGetPathGradientPresetBlendCount(IntPtr brush, out int count);
        [DllImport(GdiPlus)]
        static internal extern Status GdipGetPathGradientPresetBlend(IntPtr brush, int[] blend, float[] positions, int count);
        [DllImport(GdiPlus)]
        static internal extern Status GdipSetPathGradientPresetBlend(IntPtr brush, int[] blend, float[] positions, int count);
        [DllImport(GdiPlus)]
        static internal extern Status GdipGetPathGradientRect(IntPtr brush, out RectangleF rect);
        [DllImport(GdiPlus)]
        static internal extern Status GdipGetPathGradientSurroundColorCount(IntPtr brush, out int count);
        [DllImport(GdiPlus)]
        static internal extern Status GdipGetPathGradientSurroundColorsWithCount(IntPtr brush, int[] color, ref int count);
        [DllImport(GdiPlus)]
        static internal extern Status GdipSetPathGradientSurroundColorsWithCount(IntPtr brush, int[] color, ref int count);
        [DllImport(GdiPlus)]
        static internal extern Status GdipGetPathGradientTransform(IntPtr brush, IntPtr matrix);
        [DllImport(GdiPlus)]
        static internal extern Status GdipSetPathGradientTransform(IntPtr brush, IntPtr matrix);
        [DllImport(GdiPlus)]
        static internal extern Status GdipGetPathGradientWrapMode(IntPtr brush, out WrapMode wrapMode);
        [DllImport(GdiPlus)]
        static internal extern Status GdipSetPathGradientWrapMode(IntPtr brush, WrapMode wrapMode);
        [DllImport(GdiPlus)]
        static internal extern Status GdipSetPathGradientLinearBlend(IntPtr brush, float focus, float scale);
        [DllImport(GdiPlus)]
        static internal extern Status GdipSetPathGradientSigmaBlend(IntPtr brush, float focus, float scale);
        [DllImport(GdiPlus)]
        static internal extern Status GdipMultiplyPathGradientTransform(IntPtr texture, IntPtr matrix, MatrixOrder order);
        [DllImport(GdiPlus)]
        static internal extern Status GdipResetPathGradientTransform(IntPtr brush);
        [DllImport(GdiPlus)]
        static internal extern Status GdipRotatePathGradientTransform(IntPtr brush, float angle, MatrixOrder order);
        [DllImport(GdiPlus)]
        static internal extern Status GdipScalePathGradientTransform(IntPtr brush, float sx, float sy, MatrixOrder order);
        [DllImport(GdiPlus)]
        static internal extern Status GdipTranslatePathGradientTransform(IntPtr brush, float dx, float dy, MatrixOrder order);

        // LinearGradientBrush functions
        [DllImport(GdiPlus)]
        static internal extern Status GdipCreateLineBrushI(ref Point point1, ref Point point2, int color1, int color2, WrapMode wrapMode, out IntPtr brush);
        [DllImport(GdiPlus)]
        static internal extern Status GdipCreateLineBrush(ref PointF point1, ref PointF point2, int color1, int color2, WrapMode wrapMode, out IntPtr brush);
        [DllImport(GdiPlus)]
        static internal extern Status GdipCreateLineBrushFromRectI(ref Rectangle rect, int color1, int color2, LinearGradientMode linearGradientMode, WrapMode wrapMode, out IntPtr brush);
        [DllImport(GdiPlus)]
        static internal extern Status GdipCreateLineBrushFromRect(ref RectangleF rect, int color1, int color2, LinearGradientMode linearGradientMode, WrapMode wrapMode, out IntPtr brush);
        [DllImport(GdiPlus)]
        static internal extern Status GdipCreateLineBrushFromRectWithAngleI(ref Rectangle rect, int color1, int color2, float angle, bool isAngleScaleable, WrapMode wrapMode, out IntPtr brush);
        [DllImport(GdiPlus)]
        static internal extern Status GdipCreateLineBrushFromRectWithAngle(ref RectangleF rect, int color1, int color2, float angle, bool isAngleScaleable, WrapMode wrapMode, out IntPtr brush);
        [DllImport(GdiPlus)]
        static internal extern Status GdipGetLineBlendCount(IntPtr brush, out int count);
        [DllImport(GdiPlus)]
        static internal extern Status GdipSetLineBlend(IntPtr brush, float[] blend, float[] positions, int count);
        [DllImport(GdiPlus)]
        static internal extern Status GdipGetLineBlend(IntPtr brush, float[] blend, float[] positions, int count);
        [DllImport(GdiPlus)]
        static internal extern Status GdipSetLineGammaCorrection(IntPtr brush, bool useGammaCorrection);
        [DllImport(GdiPlus)]
        static internal extern Status GdipGetLineGammaCorrection(IntPtr brush, out bool useGammaCorrection);
        [DllImport(GdiPlus)]
        static internal extern Status GdipGetLinePresetBlendCount(IntPtr brush, out int count);
        [DllImport(GdiPlus)]
        static internal extern Status GdipSetLinePresetBlend(IntPtr brush, int[] blend, float[] positions, int count);
        [DllImport(GdiPlus)]
        static internal extern Status GdipGetLinePresetBlend(IntPtr brush, int[] blend, float[] positions, int count);
        [DllImport(GdiPlus)]
        static internal extern Status GdipSetLineColors(IntPtr brush, int color1, int color2);
        [DllImport(GdiPlus)]
        static internal extern Status GdipGetLineColors(IntPtr brush, int[] colors);
        [DllImport(GdiPlus)]
        static internal extern Status GdipGetLineRectI(IntPtr brush, out Rectangle rect);
        [DllImport(GdiPlus)]
        static internal extern Status GdipGetLineRect(IntPtr brush, out RectangleF rect);
        [DllImport(GdiPlus)]
        static internal extern Status GdipSetLineTransform(IntPtr brush, IntPtr matrix);
        [DllImport(GdiPlus)]
        static internal extern Status GdipGetLineTransform(IntPtr brush, IntPtr matrix);
        [DllImport(GdiPlus)]
        static internal extern Status GdipSetLineWrapMode(IntPtr brush, WrapMode wrapMode);
        [DllImport(GdiPlus)]
        static internal extern Status GdipGetLineWrapMode(IntPtr brush, out WrapMode wrapMode);
        [DllImport(GdiPlus)]
        static internal extern Status GdipSetLineLinearBlend(IntPtr brush, float focus, float scale);
        [DllImport(GdiPlus)]
        static internal extern Status GdipSetLineSigmaBlend(IntPtr brush, float focus, float scale);
        [DllImport(GdiPlus)]
        static internal extern Status GdipMultiplyLineTransform(IntPtr brush, IntPtr matrix, MatrixOrder order);
        [DllImport(GdiPlus)]
        static internal extern Status GdipResetLineTransform(IntPtr brush);
        [DllImport(GdiPlus)]
        static internal extern Status GdipRotateLineTransform(IntPtr brush, float angle, MatrixOrder order);
        [DllImport(GdiPlus)]
        static internal extern Status GdipScaleLineTransform(IntPtr brush, float sx, float sy, MatrixOrder order);
        [DllImport(GdiPlus)]
        static internal extern Status GdipTranslateLineTransform(IntPtr brush, float dx, float dy, MatrixOrder order);

        // Graphics functions
        [DllImport(GdiPlus)]
        static internal extern Status GdipCreateFromHDC(IntPtr hDC, out IntPtr graphics);
        [DllImport(GdiPlus)]
        static internal extern Status GdipDeleteGraphics(IntPtr graphics);
        [DllImport(GdiPlus)]
        static internal extern Status GdipRestoreGraphics(IntPtr graphics, uint graphicsState);
        [DllImport(GdiPlus)]
        static internal extern Status GdipSaveGraphics(IntPtr graphics, out uint state);
        [DllImport(GdiPlus)]
        static internal extern Status GdipMultiplyWorldTransform(IntPtr graphics, IntPtr matrix, MatrixOrder order);

        [DllImport(GdiPlus)]
        static internal extern Status GdipRotateWorldTransform(IntPtr graphics, float angle, MatrixOrder order);
        [DllImport(GdiPlus)]
        static internal extern Status GdipTranslateWorldTransform(IntPtr graphics, float dx, float dy, MatrixOrder order);
        [DllImport(GdiPlus)]
        static internal extern Status GdipDrawArc(IntPtr graphics, IntPtr pen, float x, float y, float width, float height, float startAngle, float sweepAngle);
        [DllImport(GdiPlus)]
        static internal extern Status GdipDrawArcI(IntPtr graphics, IntPtr pen, int x, int y, int width, int height, float startAngle, float sweepAngle);
        [DllImport(GdiPlus)]
        static internal extern Status GdipDrawBezier(IntPtr graphics, IntPtr pen, float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4);
        [DllImport(GdiPlus)]
        static internal extern Status GdipDrawBezierI(IntPtr graphics, IntPtr pen, int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4);
        [DllImport(GdiPlus)]
        static internal extern Status GdipDrawEllipseI(IntPtr graphics, IntPtr pen, int x, int y, int width, int height);
        [DllImport(GdiPlus)]
        static internal extern Status GdipDrawEllipse(IntPtr graphics, IntPtr pen, float x, float y, float width, float height);
        [DllImport(GdiPlus)]
        static internal extern Status GdipDrawLine(IntPtr graphics, IntPtr pen, float x1, float y1, float x2, float y2);
        [DllImport(GdiPlus)]
        static internal extern Status GdipDrawLineI(IntPtr graphics, IntPtr pen, int x1, int y1, int x2, int y2);
        [DllImport(GdiPlus)]
        static internal extern Status GdipDrawLines(IntPtr graphics, IntPtr pen, PointF[] points, int count);
        [DllImport(GdiPlus)]
        static internal extern Status GdipDrawLinesI(IntPtr graphics, IntPtr pen, Point[] points, int count);
        [DllImport(GdiPlus)]
        static internal extern Status GdipDrawPath(IntPtr graphics, IntPtr pen, IntPtr path);
        [DllImport(GdiPlus)]
        static internal extern Status GdipDrawPie(IntPtr graphics, IntPtr pen, float x, float y, float width, float height, float startAngle, float sweepAngle);
        [DllImport(GdiPlus)]
        static internal extern Status GdipDrawPieI(IntPtr graphics, IntPtr pen, int x, int y, int width, int height, float startAngle, float sweepAngle);
        [DllImport(GdiPlus)]
        static internal extern Status GdipDrawPolygon(IntPtr graphics, IntPtr pen, PointF[] points, int count);
        [DllImport(GdiPlus)]
        static internal extern Status GdipDrawPolygonI(IntPtr graphics, IntPtr pen, Point[] points, int count);
        [DllImport(GdiPlus)]
        static internal extern Status GdipDrawRectangle(IntPtr graphics, IntPtr pen, float x, float y, float width, float height);
        [DllImport(GdiPlus)]
        static internal extern Status GdipDrawRectangleI(IntPtr graphics, IntPtr pen, int x, int y, int width, int height);
        [DllImport(GdiPlus)]
        static internal extern Status GdipDrawRectangles(IntPtr graphics, IntPtr pen, RectangleF[] rects, int count);
        [DllImport(GdiPlus)]
        static internal extern Status GdipDrawRectanglesI(IntPtr graphics, IntPtr pen, Rectangle[] rects, int count);
        [DllImport(GdiPlus)]
        static internal extern Status GdipFillEllipseI(IntPtr graphics, IntPtr pen, int x, int y, int width, int height);
        [DllImport(GdiPlus)]
        static internal extern Status GdipFillEllipse(IntPtr graphics, IntPtr pen, float x, float y, float width, float height);
        [DllImport(GdiPlus)]
        static internal extern Status GdipFillPolygon(IntPtr graphics, IntPtr brush, PointF[] points, int count, FillMode fillMode);
        [DllImport(GdiPlus)]
        static internal extern Status GdipFillPolygonI(IntPtr graphics, IntPtr brush, Point[] points, int count, FillMode fillMode);
        [DllImport(GdiPlus)]
        static internal extern Status GdipFillPolygon2(IntPtr graphics, IntPtr brush, PointF[] points, int count);
        [DllImport(GdiPlus)]
        static internal extern Status GdipFillPolygon2I(IntPtr graphics, IntPtr brush, Point[] points, int count);
        [DllImport(GdiPlus)]
        static internal extern Status GdipFillRectangle(IntPtr graphics, IntPtr brush, float x1, float y1, float x2, float y2);
        [DllImport(GdiPlus)]
        static internal extern Status GdipFillRectangleI(IntPtr graphics, IntPtr brush, int x1, int y1, int x2, int y2);
        [DllImport(GdiPlus)]
        static internal extern Status GdipFillRectangles(IntPtr graphics, IntPtr brush, RectangleF[] rects, int count);
        [DllImport(GdiPlus)]
        static internal extern Status GdipFillRectanglesI(IntPtr graphics, IntPtr brush, Rectangle[] rects, int count);
        [DllImport(GdiPlus, CharSet = CharSet.Unicode)]
        static internal extern Status GdipDrawString(IntPtr graphics, string text, int len, IntPtr font, ref RectangleF rc, IntPtr format, IntPtr brush);
        [DllImport(GdiPlus)]
        static internal extern Status GdipGetDC(IntPtr graphics, out IntPtr hdc);
        [DllImport(GdiPlus)]
        static internal extern Status GdipReleaseDC(IntPtr graphics, IntPtr hdc);
        [DllImport(GdiPlus)]
        static internal extern Status GdipDrawImageRectI(IntPtr graphics, IntPtr image, int x, int y, int width, int height);
        [DllImport(GdiPlus)]
        static internal extern Status GdipGetRenderingOrigin(IntPtr graphics, out int x, out int y);
        [DllImport(GdiPlus)]
        static internal extern Status GdipSetRenderingOrigin(IntPtr graphics, int x, int y);
        [DllImport(GdiPlus)]
        internal static extern Status GdipCloneBitmapArea(float x, float y, float width, float height, PixelFormat format, IntPtr original, out IntPtr bitmap);
        [DllImport(GdiPlus)]
        internal static extern Status GdipCloneBitmapAreaI(int x, int y, int width, int height, PixelFormat format, IntPtr original, out IntPtr bitmap);
        [DllImport(GdiPlus)]
        internal static extern Status GdipResetWorldTransform(IntPtr graphics);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetWorldTransform(IntPtr graphics, IntPtr matrix);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetWorldTransform(IntPtr graphics, IntPtr matrix);
        [DllImport(GdiPlus)]
        internal static extern Status GdipScaleWorldTransform(IntPtr graphics, float sx, float sy, MatrixOrder order);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGraphicsClear(IntPtr graphics, int argb);
        [DllImport(GdiPlus)]
        internal static extern Status GdipDrawClosedCurve(IntPtr graphics, IntPtr pen, PointF[] points, int count);
        [DllImport(GdiPlus)]
        internal static extern Status GdipDrawClosedCurveI(IntPtr graphics, IntPtr pen, Point[] points, int count);
        [DllImport(GdiPlus)]
        internal static extern Status GdipDrawClosedCurve2(IntPtr graphics, IntPtr pen, PointF[] points, int count, float tension);
        [DllImport(GdiPlus)]
        internal static extern Status GdipDrawClosedCurve2I(IntPtr graphics, IntPtr pen, Point[] points, int count, float tension);
        [DllImport(GdiPlus)]
        internal static extern Status GdipDrawCurve(IntPtr graphics, IntPtr pen, PointF[] points, int count);
        [DllImport(GdiPlus)]
        internal static extern Status GdipDrawCurveI(IntPtr graphics, IntPtr pen, Point[] points, int count);
        [DllImport(GdiPlus)]
        internal static extern Status GdipDrawCurve2(IntPtr graphics, IntPtr pen, PointF[] points, int count, float tension);
        [DllImport(GdiPlus)]
        internal static extern Status GdipDrawCurve2I(IntPtr graphics, IntPtr pen, Point[] points, int count, float tension);
        [DllImport(GdiPlus)]
        internal static extern Status GdipDrawCurve3(IntPtr graphics, IntPtr pen, PointF[] points, int count, int offset, int numberOfSegments, float tension);
        [DllImport(GdiPlus)]
        internal static extern Status GdipDrawCurve3I(IntPtr graphics, IntPtr pen, Point[] points, int count, int offset, int numberOfSegments, float tension);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetClipRect(IntPtr graphics, float x, float y, float width, float height, CombineMode combineMode);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetClipRectI(IntPtr graphics, int x, int y, int width, int height, CombineMode combineMode);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetClipPath(IntPtr graphics, IntPtr path, CombineMode combineMode);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetClipRegion(IntPtr graphics, IntPtr region, CombineMode combineMode);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetClipGraphics(IntPtr graphics, IntPtr srcgraphics, CombineMode combineMode);
        [DllImport(GdiPlus)]
        internal static extern Status GdipResetClip(IntPtr graphics);
        [DllImport(GdiPlus)]
        internal static extern Status GdipEndContainer(IntPtr graphics, uint state);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetClip(IntPtr graphics, IntPtr region);

        [DllImport(GdiPlus)]
        internal static extern Status GdipFillClosedCurve(IntPtr graphics, IntPtr brush, PointF[] points, int count);

        [DllImport(GdiPlus)]
        internal static extern Status GdipFillClosedCurveI(IntPtr graphics, IntPtr brush, Point[] points, int count);

        [DllImport(GdiPlus)]
        internal static extern Status GdipFillClosedCurve2(IntPtr graphics, IntPtr brush,
                    PointF[] points, int count, float tension, FillMode fillMode);

        [DllImport(GdiPlus)]
        internal static extern Status GdipFillClosedCurve2I(IntPtr graphics, IntPtr brush,
            Point[] points, int count, float tension, FillMode fillMode);

        [DllImport(GdiPlus)]
        internal static extern Status GdipFillPie(IntPtr graphics, IntPtr brush, float x, float y,
            float width, float height, float startAngle, float sweepAngle);

        [DllImport(GdiPlus)]
        internal static extern Status GdipFillPieI(IntPtr graphics, IntPtr brush, int x, int y,
            int width, int height, float startAngle, float sweepAngle);

        [DllImport(GdiPlus)]
        internal static extern Status GdipFillPath(IntPtr graphics, IntPtr brush, IntPtr path);

        [DllImport(GdiPlus)]
        internal static extern Status GdipGetNearestColor(IntPtr graphics, out int argb);

        [DllImport(GdiPlus)]
        internal static extern Status GdipIsVisiblePoint(IntPtr graphics, float x, float y, out bool result);

        [DllImport(GdiPlus)]
        internal static extern Status GdipIsVisiblePointI(IntPtr graphics, int x, int y, out bool result);

        [DllImport(GdiPlus)]
        internal static extern Status GdipIsVisibleRect(IntPtr graphics, float x, float y,
            float width, float height, out bool result);

        [DllImport(GdiPlus)]
        internal static extern Status GdipIsVisibleRectI(IntPtr graphics, int x, int y,
            int width, int height, out bool result);

        [DllImport(GdiPlus)]
        internal static extern Status GdipTransformPoints(IntPtr graphics, CoordinateSpace destSpace,
            CoordinateSpace srcSpace, IntPtr points, int count);

        [DllImport(GdiPlus)]
        internal static extern Status GdipTransformPointsI(IntPtr graphics, CoordinateSpace destSpace,
            CoordinateSpace srcSpace, IntPtr points, int count);

        [DllImport(GdiPlus)]
        internal static extern Status GdipTranslateClip(IntPtr graphics, float dx, float dy);
        [DllImport(GdiPlus)]
        internal static extern Status GdipTranslateClipI(IntPtr graphics, int dx, int dy);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetClipBounds(IntPtr graphics, out RectangleF rect);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetCompositingMode(IntPtr graphics, CompositingMode compositingMode);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetCompositingMode(IntPtr graphics, out CompositingMode compositingMode);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetCompositingQuality(IntPtr graphics, CompositingQuality compositingQuality);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetCompositingQuality(IntPtr graphics, out CompositingQuality compositingQuality);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetInterpolationMode(IntPtr graphics, InterpolationMode interpolationMode);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetInterpolationMode(IntPtr graphics, out InterpolationMode interpolationMode);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetDpiX(IntPtr graphics, out float dpi);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetDpiY(IntPtr graphics, out float dpi);
        [DllImport(GdiPlus)]
        internal static extern Status GdipIsClipEmpty(IntPtr graphics, out bool result);
        [DllImport(GdiPlus)]
        internal static extern Status GdipIsVisibleClipEmpty(IntPtr graphics, out bool result);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetPageUnit(IntPtr graphics, out GraphicsUnit unit);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetPageScale(IntPtr graphics, out float scale);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetPageUnit(IntPtr graphics, GraphicsUnit unit);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetPageScale(IntPtr graphics, float scale);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetPixelOffsetMode(IntPtr graphics, PixelOffsetMode pixelOffsetMode);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetPixelOffsetMode(IntPtr graphics, out PixelOffsetMode pixelOffsetMode);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetSmoothingMode(IntPtr graphics, SmoothingMode smoothingMode);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetSmoothingMode(IntPtr graphics, out SmoothingMode smoothingMode);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetTextContrast(IntPtr graphics, int contrast);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetTextContrast(IntPtr graphics, out int contrast);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetTextRenderingHint(IntPtr graphics, TextRenderingHint mode);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetTextRenderingHint(IntPtr graphics, out TextRenderingHint mode);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetVisibleClipBounds(IntPtr graphics, out RectangleF rect);

        [DllImport(GdiPlus)]
        internal static extern Status GdipFlush(IntPtr graphics, FlushIntention intention);

        [DllImport(GdiPlus, CharSet = CharSet.Unicode)]
        internal static extern Status GdipAddPathString(IntPtr path, string s, int lenght, IntPtr family, int style, float emSize, ref RectangleF layoutRect, IntPtr format);
        [DllImport(GdiPlus, CharSet = CharSet.Unicode)]
        internal static extern Status GdipAddPathStringI(IntPtr path, string s, int lenght, IntPtr family, int style, float emSize, ref Rectangle layoutRect, IntPtr format);


        // Pen functions
        [DllImport(GdiPlus)]
        internal static extern Status GdipCreatePen1(int argb, float width, GraphicsUnit unit, out IntPtr pen);
        [DllImport(GdiPlus)]
        internal static extern Status GdipCreatePen2(IntPtr brush, float width, GraphicsUnit unit, out IntPtr pen);
        [DllImport(GdiPlus)]
        internal static extern Status GdipClonePen(IntPtr pen, out IntPtr clonepen);
        [DllImport(GdiPlus)]
        internal static extern Status GdipDeletePen(IntPtr pen);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetPenBrushFill(IntPtr pen, IntPtr brush);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetPenBrushFill(IntPtr pen, out IntPtr brush);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetPenFillType(IntPtr pen, out PenType type);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetPenColor(IntPtr pen, int color);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetPenColor(IntPtr pen, out int color);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetPenCompoundArray(IntPtr pen, float[] dash, int count);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetPenCompoundArray(IntPtr pen, float[] dash, int count);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetPenCompoundCount(IntPtr pen, out int count);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetPenDashCap197819(IntPtr pen, DashCap dashCap);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetPenDashCap197819(IntPtr pen, out DashCap dashCap);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetPenDashStyle(IntPtr pen, DashStyle dashStyle);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetPenDashStyle(IntPtr pen, out DashStyle dashStyle);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetPenDashOffset(IntPtr pen, float offset);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetPenDashOffset(IntPtr pen, out float offset);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetPenDashCount(IntPtr pen, out int count);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetPenDashArray(IntPtr pen, float[] dash, int count);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetPenDashArray(IntPtr pen, float[] dash, int count);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetPenMiterLimit(IntPtr pen, float miterLimit);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetPenMiterLimit(IntPtr pen, out float miterLimit);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetPenLineJoin(IntPtr pen, LineJoin lineJoin);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetPenLineJoin(IntPtr pen, out LineJoin lineJoin);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetPenLineCap197819(IntPtr pen, LineCap startCap, LineCap endCap, DashCap dashCap);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetPenMode(IntPtr pen, PenAlignment alignment);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetPenMode(IntPtr pen, out PenAlignment alignment);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetPenStartCap(IntPtr pen, LineCap startCap);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetPenStartCap(IntPtr pen, out LineCap startCap);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetPenEndCap(IntPtr pen, LineCap endCap);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetPenEndCap(IntPtr pen, out LineCap endCap);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetPenCustomStartCap(IntPtr pen, IntPtr customCap);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetPenCustomStartCap(IntPtr pen, out IntPtr customCap);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetPenCustomEndCap(IntPtr pen, IntPtr customCap);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetPenCustomEndCap(IntPtr pen, out IntPtr customCap);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetPenTransform(IntPtr pen, IntPtr matrix);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetPenTransform(IntPtr pen, IntPtr matrix);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetPenWidth(IntPtr pen, float width);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetPenWidth(IntPtr pen, out float width);
        [DllImport(GdiPlus)]
        internal static extern Status GdipResetPenTransform(IntPtr pen);
        [DllImport(GdiPlus)]
        internal static extern Status GdipMultiplyPenTransform(IntPtr pen, IntPtr matrix, MatrixOrder order);
        [DllImport(GdiPlus)]
        internal static extern Status GdipRotatePenTransform(IntPtr pen, float angle, MatrixOrder order);
        [DllImport(GdiPlus)]
        internal static extern Status GdipScalePenTransform(IntPtr pen, float sx, float sy, MatrixOrder order);
        [DllImport(GdiPlus)]
        internal static extern Status GdipTranslatePenTransform(IntPtr pen, float dx, float dy, MatrixOrder order);

        // CustomLineCap functions
        [DllImport(GdiPlus)]
        internal static extern Status GdipCreateCustomLineCap(IntPtr fillPath, IntPtr strokePath, LineCap baseCap, float baseInset, out IntPtr customCap);
        [DllImport(GdiPlus)]
        internal static extern Status GdipDeleteCustomLineCap(IntPtr customCap);
        [DllImport(GdiPlus)]
        internal static extern Status GdipCloneCustomLineCap(IntPtr customCap, out IntPtr clonedCap);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetCustomLineCapStrokeCaps(IntPtr customCap, LineCap startCap, LineCap endCap);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetCustomLineCapStrokeCaps(IntPtr customCap, out LineCap startCap, out LineCap endCap);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetCustomLineCapStrokeJoin(IntPtr customCap, LineJoin lineJoin);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetCustomLineCapStrokeJoin(IntPtr customCap, out LineJoin lineJoin);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetCustomLineCapBaseCap(IntPtr customCap, LineCap baseCap);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetCustomLineCapBaseCap(IntPtr customCap, out LineCap baseCap);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetCustomLineCapBaseInset(IntPtr customCap, float inset);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetCustomLineCapBaseInset(IntPtr customCap, out float inset);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetCustomLineCapWidthScale(IntPtr customCap, float widthScale);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetCustomLineCapWidthScale(IntPtr customCap, out float widthScale);

        // AdjustableArrowCap functions
        [DllImport(GdiPlus)]
        internal static extern Status GdipCreateAdjustableArrowCap(float height, float width, bool isFilled, out IntPtr arrowCap);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetAdjustableArrowCapHeight(IntPtr arrowCap, float height);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetAdjustableArrowCapHeight(IntPtr arrowCap, out float height);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetAdjustableArrowCapWidth(IntPtr arrowCap, float width);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetAdjustableArrowCapWidth(IntPtr arrowCap, out float width);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetAdjustableArrowCapMiddleInset(IntPtr arrowCap, float middleInset);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetAdjustableArrowCapMiddleInset(IntPtr arrowCap, out float middleInset);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetAdjustableArrowCapFillState(IntPtr arrowCap, bool isFilled);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetAdjustableArrowCapFillState(IntPtr arrowCap, out bool isFilled);


        [DllImport(GdiPlus)]
        internal static extern Status GdipCreateFromHWND(IntPtr hwnd, out IntPtr graphics);

        [DllImport(GdiPlus, CharSet = CharSet.Unicode)]
        internal unsafe static extern Status GdipMeasureString(IntPtr graphics, string str, int length,
            IntPtr font, ref RectangleF layoutRect, IntPtr stringFormat, out RectangleF boundingBox,
            int* codepointsFitted, int* linesFilled);

        [DllImport(GdiPlus, CharSet = CharSet.Unicode)]
        internal static extern Status GdipMeasureCharacterRanges(IntPtr graphics, string str, int length, IntPtr font,
            ref RectangleF layoutRect, IntPtr stringFormat, int regcount, out IntPtr regions);

        [DllImport(GdiPlus)]
        internal static extern Status GdipSetStringFormatMeasurableCharacterRanges(IntPtr native,
            int cnt, CharacterRange[] range);

        [DllImport(GdiPlus)]
        internal static extern Status GdipGetStringFormatMeasurableCharacterRangeCount(IntPtr native,
            out int cnt);

        // Bitmap functions
        [DllImport(GdiPlus)]
        internal static extern Status GdipCreateBitmapFromScan0(int width, int height, int stride, PixelFormat format, IntPtr scan0, out IntPtr bmp);

        [DllImport(GdiPlus)]
        internal static extern Status GdipCreateBitmapFromGraphics(int width, int height, IntPtr target, out IntPtr bitmap);

        [DllImport(GdiPlus)]
        internal static extern Status GdipBitmapLockBits(IntPtr bmp, ref Rectangle rc, ImageLockMode flags, PixelFormat format, [In, Out] BitmapData bmpData);

        [DllImport(GdiPlus)]
        internal static extern Status GdipBitmapSetResolution(IntPtr bmp, float xdpi, float ydpi);

        [DllImport(GdiPlus)]
        internal static extern Status GdipBitmapUnlockBits(IntPtr bmp, [In, Out] BitmapData bmpData);

        [DllImport(GdiPlus)]
        internal static extern Status GdipBitmapGetPixel(IntPtr bmp, int x, int y, out int argb);

        [DllImport(GdiPlus)]
        internal static extern Status GdipBitmapSetPixel(IntPtr bmp, int x, int y, int argb);

        // Image functions
        [DllImport(GdiPlus, CharSet = CharSet.Auto)]
        internal static extern Status GdipLoadImageFromFile([MarshalAs(UnmanagedType.LPWStr)] string filename, out IntPtr image);

#if !TEST
        // Stream functions for Win32 (original Win32 ones)
        [DllImport(GdiPlus, ExactSpelling = true, CharSet = CharSet.Unicode)]
        internal static extern Status GdipLoadImageFromStream([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ComIStreamMarshaler))] IStream stream, out IntPtr image);

        [DllImport(GdiPlus, ExactSpelling = true, CharSet = CharSet.Unicode)]
        internal static extern Status GdipSaveImageToStream(HandleRef image, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ComIStreamMarshaler))] IStream stream, [In()] ref Guid clsidEncoder, HandleRef encoderParams);
#endif

        [DllImport(GdiPlus)]
        internal static extern Status GdipCloneImage(IntPtr image, out IntPtr imageclone);

        [DllImport(GdiPlus, CharSet = CharSet.Auto)]
        internal static extern Status GdipLoadImageFromFileICM([MarshalAs(UnmanagedType.LPWStr)] string filename, out IntPtr image);

        [DllImport(GdiPlus)]
        internal static extern Status GdipCreateBitmapFromHBITMAP(IntPtr hBitMap, IntPtr gdiPalette, out IntPtr image);

        [DllImport(GdiPlus)]
        internal static extern Status GdipDisposeImage(IntPtr image);

        [DllImport(GdiPlus)]
        internal static extern Status GdipGetImageFlags(IntPtr image, out int flag);

        [DllImport(GdiPlus)]
        internal static extern Status GdipGetImageType(IntPtr image, out ImageType type);

        [DllImport(GdiPlus)]
        internal static extern Status GdipImageGetFrameDimensionsCount(IntPtr image, out uint count);

        [DllImport(GdiPlus)]
        internal static extern Status GdipImageGetFrameDimensionsList(IntPtr image, [Out] Guid[] dimensionIDs, uint count);

        [DllImport(GdiPlus)]
        internal static extern Status GdipGetImageHeight(IntPtr image, out uint height);

        [DllImport(GdiPlus)]
        internal static extern Status GdipGetImageHorizontalResolution(IntPtr image, out float resolution);

        [DllImport(GdiPlus)]
        internal static extern Status GdipGetImagePaletteSize(IntPtr image, out int size);

        [DllImport(GdiPlus)]
        internal static extern Status GdipGetImagePalette(IntPtr image, IntPtr palette, int size);

        [DllImport(GdiPlus)]
        internal static extern Status GdipSetImagePalette(IntPtr image, IntPtr palette);

        [DllImport(GdiPlus)]
        internal static extern Status GdipGetImageDimension(IntPtr image, out float width, out float height);

        [DllImport(GdiPlus)]
        internal static extern Status GdipGetImagePixelFormat(IntPtr image, out PixelFormat format);

        [DllImport(GdiPlus)]
        internal static extern Status GdipGetPropertyCount(IntPtr image, out uint propNumbers);

        [DllImport(GdiPlus)]
        internal static extern Status GdipGetPropertyIdList(IntPtr image, uint propNumbers, [Out] int[] list);

        [DllImport(GdiPlus)]
        internal static extern Status GdipGetPropertySize(IntPtr image, out int bufferSize, out int propNumbers);

        [DllImport(GdiPlus)]
        internal static extern Status GdipGetAllPropertyItems(IntPtr image, int bufferSize, int propNumbers, IntPtr items);

        [DllImport(GdiPlus)]
        internal static extern Status GdipGetImageRawFormat(IntPtr image, out Guid format);

        [DllImport(GdiPlus)]
        internal static extern Status GdipGetImageVerticalResolution(IntPtr image, out float resolution);

        [DllImport(GdiPlus)]
        internal static extern Status GdipGetImageWidth(IntPtr image, out uint width);

        [DllImport(GdiPlus)]
        internal static extern Status GdipGetImageBounds(IntPtr image, out RectangleF source, ref GraphicsUnit unit);

        [DllImport(GdiPlus)]
        internal static extern Status GdipGetEncoderParameterListSize(IntPtr image, ref Guid encoder, out uint size);

        [DllImport(GdiPlus)]
        internal static extern Status GdipGetEncoderParameterList(IntPtr image, ref Guid encoder, uint size, IntPtr buffer);

        [DllImport(GdiPlus)]
        internal static extern Status GdipImageGetFrameCount(IntPtr image, ref Guid guidDimension, out uint count);

        [DllImport(GdiPlus)]
        internal static extern Status GdipImageSelectActiveFrame(IntPtr image, ref Guid guidDimension, int frameIndex);

        [DllImport(GdiPlus)]
        internal static extern Status GdipGetPropertyItemSize(IntPtr image, int propertyID, out int propertySize);

        [DllImport(GdiPlus)]
        internal static extern Status GdipGetPropertyItem(IntPtr image, int propertyID, int propertySize, IntPtr buffer);

        [DllImport(GdiPlus)]
        internal static extern Status GdipRemovePropertyItem(IntPtr image, int propertyId);

        [DllImport(GdiPlus)]
        internal unsafe static extern Status GdipSetPropertyItem(IntPtr image, GdipPropertyItem* propertyItem);

        [DllImport(GdiPlus)]
        internal static extern Status GdipGetImageThumbnail(IntPtr image, uint width, uint height, out IntPtr thumbImage, IntPtr callback, IntPtr callBackData);

        [DllImport(GdiPlus)]
        internal static extern Status GdipImageRotateFlip(IntPtr image, RotateFlipType rotateFlipType);

        [DllImport(GdiPlus, CharSet = CharSet.Unicode)]
        internal static extern Status GdipSaveImageToFile(IntPtr image, string filename, ref Guid encoderClsID, IntPtr encoderParameters);

        [DllImport(GdiPlus)]
        internal static extern Status GdipSaveAdd(IntPtr image, IntPtr encoderParameters);

        [DllImport(GdiPlus)]
        internal static extern Status GdipSaveAddImage(IntPtr image, IntPtr imagenew, IntPtr encoderParameters);

        [DllImport(GdiPlus)]
        internal static extern Status GdipDrawImageI(IntPtr graphics, IntPtr image, int x, int y);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetImageGraphicsContext(IntPtr image, out IntPtr graphics);
        [DllImport(GdiPlus)]
        internal static extern Status GdipDrawImage(IntPtr graphics, IntPtr image, float x, float y);

        [DllImport(GdiPlus)]
        internal static extern Status GdipBeginContainer(IntPtr graphics, ref RectangleF dstrect, ref RectangleF srcrect, GraphicsUnit unit, out uint state);
        [DllImport(GdiPlus)]
        internal static extern Status GdipBeginContainerI(IntPtr graphics, ref Rectangle dstrect, ref Rectangle srcrect, GraphicsUnit unit, out uint state);
        [DllImport(GdiPlus)]
        internal static extern Status GdipBeginContainer2(IntPtr graphics, out uint state);

        [DllImport(GdiPlus)]
        internal static extern Status GdipDrawImagePoints(IntPtr graphics, IntPtr image, PointF[] destPoints, int count);


        [DllImport(GdiPlus)]
        internal static extern Status GdipDrawImagePointsI(IntPtr graphics, IntPtr image, Point[] destPoints, int count);

        [DllImport(GdiPlus)]
        internal static extern Status GdipDrawImageRectRectI(IntPtr graphics, IntPtr image,
            int dstx, int dsty, int dstwidth, int dstheight,
            int srcx, int srcy, int srcwidth, int srcheight,
            GraphicsUnit srcUnit, IntPtr imageattr, Graphics.DrawImageAbort callback, IntPtr callbackData);

        [DllImport(GdiPlus)]
        internal static extern Status GdipDrawImageRectRect(IntPtr graphics, IntPtr image,
            float dstx, float dsty, float dstwidth, float dstheight,
            float srcx, float srcy, float srcwidth, float srcheight,
            GraphicsUnit srcUnit, IntPtr imageattr, Graphics.DrawImageAbort callback, IntPtr callbackData);

        [DllImport(GdiPlus)]
        internal static extern Status GdipDrawImagePointsRectI(IntPtr graphics, IntPtr image,
             Point[] destPoints, int count, int srcx, int srcy, int srcwidth, int srcheight,
             GraphicsUnit srcUnit, IntPtr imageattr, Graphics.DrawImageAbort callback, IntPtr callbackData);

        [DllImport(GdiPlus)]
        internal static extern Status GdipDrawImagePointsRect(IntPtr graphics, IntPtr image,
             PointF[] destPoints, int count, float srcx, float srcy, float srcwidth, float srcheight,
             GraphicsUnit srcUnit, IntPtr imageattr, Graphics.DrawImageAbort callback, IntPtr callbackData);

        [DllImport(GdiPlus)]
        internal static extern Status GdipDrawImageRect(IntPtr graphics, IntPtr image, float x, float y, float width, float height);
        [DllImport(GdiPlus)]
        internal static extern Status GdipDrawImagePointRect(IntPtr graphics, IntPtr image, float x,
                float y, float srcx, float srcy, float srcwidth, float srcheight, GraphicsUnit srcUnit);

        [DllImport(GdiPlus)]
        internal static extern Status GdipDrawImagePointRectI(IntPtr graphics, IntPtr image, int x,
                int y, int srcx, int srcy, int srcwidth,
                int srcheight, GraphicsUnit srcUnit);

        [DllImport(GdiPlus)]
        internal static extern Status GdipCreateStringFormat(StringFormatFlags formatAttributes, int language, out IntPtr native);

        [DllImport(GdiPlus)]
        internal static extern Status GdipCreateHBITMAPFromBitmap(IntPtr bmp, out IntPtr HandleBmp, int clrbackground);

        [DllImport(GdiPlus, CharSet = CharSet.Auto)]
        internal static extern Status GdipCreateBitmapFromFile([MarshalAs(UnmanagedType.LPWStr)] string filename, out IntPtr bitmap);

        [DllImport(GdiPlus, CharSet = CharSet.Auto)]
        internal static extern Status GdipCreateBitmapFromFileICM([MarshalAs(UnmanagedType.LPWStr)] string filename, out IntPtr bitmap);

        [DllImport(GdiPlus)]
        internal static extern Status GdipCreateHICONFromBitmap(IntPtr bmp, out IntPtr HandleIcon);

        [DllImport(GdiPlus)]
        internal static extern Status GdipCreateBitmapFromHICON(IntPtr hicon, out IntPtr bitmap);

        [DllImport(GdiPlus)]
        internal static extern Status GdipCreateBitmapFromResource(IntPtr hInstance,
                string lpBitmapName, out IntPtr bitmap);

        // Matrix functions
        [DllImport(GdiPlus)]
        internal static extern Status GdipCreateMatrix(out IntPtr matrix);
        [DllImport(GdiPlus)]
        internal static extern Status GdipCreateMatrix2(float m11, float m12, float m21, float m22, float dx, float dy, out IntPtr matrix);
        [DllImport(GdiPlus)]
        internal static extern Status GdipCreateMatrix3(ref RectangleF rect, PointF[] dstplg, out IntPtr matrix);
        [DllImport(GdiPlus)]
        internal static extern Status GdipCreateMatrix3I(ref Rectangle rect, Point[] dstplg, out IntPtr matrix);
        [DllImport(GdiPlus)]
        internal static extern Status GdipDeleteMatrix(IntPtr matrix);

        [DllImport(GdiPlus)]
        internal static extern Status GdipCloneMatrix(IntPtr matrix, out IntPtr cloneMatrix);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetMatrixElements(IntPtr matrix, float m11, float m12, float m21, float m22, float dx, float dy);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetMatrixElements(IntPtr matrix, IntPtr matrixOut);
        [DllImport(GdiPlus)]
        internal static extern Status GdipMultiplyMatrix(IntPtr matrix, IntPtr matrix2, MatrixOrder order);
        [DllImport(GdiPlus)]
        internal static extern Status GdipTranslateMatrix(IntPtr matrix, float offsetX, float offsetY, MatrixOrder order);
        [DllImport(GdiPlus)]
        internal static extern Status GdipScaleMatrix(IntPtr matrix, float scaleX, float scaleY, MatrixOrder order);
        [DllImport(GdiPlus)]
        internal static extern Status GdipRotateMatrix(IntPtr matrix, float angle, MatrixOrder order);

        [DllImport(GdiPlus)]
        internal static extern Status GdipShearMatrix(IntPtr matrix, float shearX, float shearY, MatrixOrder order);

        [DllImport(GdiPlus)]
        internal static extern Status GdipInvertMatrix(IntPtr matrix);
        [DllImport(GdiPlus)]
        internal static extern Status GdipTransformMatrixPoints(IntPtr matrix, [In, Out] PointF[] pts, int count);
        [DllImport(GdiPlus)]
        internal static extern Status GdipTransformMatrixPointsI(IntPtr matrix, [In, Out] Point[] pts, int count);
        [DllImport(GdiPlus)]
        internal static extern Status GdipVectorTransformMatrixPoints(IntPtr matrix, [In, Out] PointF[] pts, int count);
        [DllImport(GdiPlus)]
        internal static extern Status GdipVectorTransformMatrixPointsI(IntPtr matrix, [In, Out] Point[] pts, int count);
        [DllImport(GdiPlus)]
        internal static extern Status GdipIsMatrixInvertible(IntPtr matrix, out bool result);

        [DllImport(GdiPlus)]
        internal static extern Status GdipIsMatrixIdentity(IntPtr matrix, out bool result);
        [DllImport(GdiPlus)]
        internal static extern Status GdipIsMatrixEqual(IntPtr matrix, IntPtr matrix2, out bool result);

        // GraphicsPath functions
        [DllImport(GdiPlus)]
        internal static extern Status GdipCreatePath(FillMode brushMode, out IntPtr path);
        [DllImport(GdiPlus)]
        internal static extern Status GdipCreatePath2(PointF[] points, byte[] types, int count, FillMode brushMode, out IntPtr path);
        [DllImport(GdiPlus)]
        internal static extern Status GdipCreatePath2I(Point[] points, byte[] types, int count, FillMode brushMode, out IntPtr path);
        [DllImport(GdiPlus)]
        internal static extern Status GdipClonePath(IntPtr path, out IntPtr clonePath);
        [DllImport(GdiPlus)]
        internal static extern Status GdipDeletePath(IntPtr path);
        [DllImport(GdiPlus)]
        internal static extern Status GdipResetPath(IntPtr path);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetPointCount(IntPtr path, out int count);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetPathTypes(IntPtr path, [Out] byte[] types, int count);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetPathPoints(IntPtr path, [Out] PointF[] points, int count);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetPathPointsI(IntPtr path, [Out] Point[] points, int count);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetPathFillMode(IntPtr path, out FillMode fillMode);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetPathFillMode(IntPtr path, FillMode fillMode);
        [DllImport(GdiPlus)]
        internal static extern Status GdipStartPathFigure(IntPtr path);
        [DllImport(GdiPlus)]
        internal static extern Status GdipClosePathFigure(IntPtr path);
        [DllImport(GdiPlus)]
        internal static extern Status GdipClosePathFigures(IntPtr path);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetPathMarker(IntPtr path);
        [DllImport(GdiPlus)]
        internal static extern Status GdipClearPathMarkers(IntPtr path);
        [DllImport(GdiPlus)]
        internal static extern Status GdipReversePath(IntPtr path);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetPathLastPoint(IntPtr path, out PointF lastPoint);
        [DllImport(GdiPlus)]
        internal static extern Status GdipAddPathLine(IntPtr path, float x1, float y1, float x2, float y2);
        [DllImport(GdiPlus)]
        internal static extern Status GdipAddPathLine2(IntPtr path, PointF[] points, int count);
        [DllImport(GdiPlus)]
        internal static extern Status GdipAddPathLine2I(IntPtr path, Point[] points, int count);
        [DllImport(GdiPlus)]
        internal static extern Status GdipAddPathArc(IntPtr path, float x, float y, float width, float height, float startAngle, float sweepAngle);
        [DllImport(GdiPlus)]
        internal static extern Status GdipAddPathBezier(IntPtr path, float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4);
        [DllImport(GdiPlus)]
        internal static extern Status GdipAddPathBeziers(IntPtr path, PointF[] points, int count);
        [DllImport(GdiPlus)]
        internal static extern Status GdipAddPathCurve(IntPtr path, PointF[] points, int count);
        [DllImport(GdiPlus)]
        internal static extern Status GdipAddPathCurveI(IntPtr path, Point[] points, int count);
        [DllImport(GdiPlus)]
        internal static extern Status GdipAddPathCurve2(IntPtr path, PointF[] points, int count, float tension);
        [DllImport(GdiPlus)]
        internal static extern Status GdipAddPathCurve2I(IntPtr path, Point[] points, int count, float tension);
        [DllImport(GdiPlus)]
        internal static extern Status GdipAddPathCurve3(IntPtr path, PointF[] points, int count, int offset, int numberOfSegments, float tension);
        [DllImport(GdiPlus)]
        internal static extern Status GdipAddPathCurve3I(IntPtr path, Point[] points, int count, int offset, int numberOfSegments, float tension);
        [DllImport(GdiPlus)]
        internal static extern Status GdipAddPathClosedCurve(IntPtr path, PointF[] points, int count);
        [DllImport(GdiPlus)]
        internal static extern Status GdipAddPathClosedCurveI(IntPtr path, Point[] points, int count);
        [DllImport(GdiPlus)]
        internal static extern Status GdipAddPathClosedCurve2(IntPtr path, PointF[] points, int count, float tension);
        [DllImport(GdiPlus)]
        internal static extern Status GdipAddPathClosedCurve2I(IntPtr path, Point[] points, int count, float tension);
        [DllImport(GdiPlus)]
        internal static extern Status GdipAddPathRectangle(IntPtr path, float x, float y, float width, float height);
        [DllImport(GdiPlus)]
        internal static extern Status GdipAddPathRectangles(IntPtr path, RectangleF[] rects, int count);
        [DllImport(GdiPlus)]
        internal static extern Status GdipAddPathEllipse(IntPtr path, float x, float y, float width, float height);
        [DllImport(GdiPlus)]
        internal static extern Status GdipAddPathEllipseI(IntPtr path, int x, int y, int width, int height);
        [DllImport(GdiPlus)]
        internal static extern Status GdipAddPathPie(IntPtr path, float x, float y, float width, float height, float startAngle, float sweepAngle);
        [DllImport(GdiPlus)]
        internal static extern Status GdipAddPathPieI(IntPtr path, int x, int y, int width, int height, float startAngle, float sweepAngle);
        [DllImport(GdiPlus)]
        internal static extern Status GdipAddPathPolygon(IntPtr path, PointF[] points, int count);
        [DllImport(GdiPlus)]
        internal static extern Status GdipAddPathPath(IntPtr path, IntPtr addingPath, bool connect);
        [DllImport(GdiPlus)]
        internal static extern Status GdipAddPathLineI(IntPtr path, int x1, int y1, int x2, int y2);
        [DllImport(GdiPlus)]
        internal static extern Status GdipAddPathArcI(IntPtr path, int x, int y, int width, int height, float startAngle, float sweepAngle);

        [DllImport(GdiPlus)]
        internal static extern Status GdipAddPathBezierI(IntPtr path, int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4);
        [DllImport(GdiPlus)]
        internal static extern Status GdipAddPathBeziersI(IntPtr path, Point[] points, int count);

        [DllImport(GdiPlus)]
        internal static extern Status GdipAddPathPolygonI(IntPtr path, Point[] points, int count);
        [DllImport(GdiPlus)]
        internal static extern Status GdipAddPathRectangleI(IntPtr path, int x, int y, int width, int height);
        [DllImport(GdiPlus)]
        internal static extern Status GdipAddPathRectanglesI(IntPtr path, Rectangle[] rects, int count);
        [DllImport(GdiPlus)]
        internal static extern Status GdipFlattenPath(IntPtr path, IntPtr matrix, float floatness);
        [DllImport(GdiPlus)]
        internal static extern Status GdipTransformPath(IntPtr path, IntPtr matrix);
        [DllImport(GdiPlus)]
        internal static extern Status GdipWarpPath(IntPtr path, IntPtr matrix,
            PointF[] points, int count,
            float srcx, float srcy, float srcwidth, float srcheight,
            WarpMode mode, float flatness);
        [DllImport(GdiPlus)]
        internal static extern Status GdipWidenPath(IntPtr path, IntPtr pen, IntPtr matrix, float flatness);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetPathWorldBounds(IntPtr path, out RectangleF bounds, IntPtr matrix, IntPtr pen);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetPathWorldBoundsI(IntPtr path, out Rectangle bounds, IntPtr matrix, IntPtr pen);
        [DllImport(GdiPlus)]
        internal static extern Status GdipIsVisiblePathPoint(IntPtr path, float x, float y, IntPtr graphics, out bool result);
        [DllImport(GdiPlus)]
        internal static extern Status GdipIsVisiblePathPointI(IntPtr path, int x, int y, IntPtr graphics, out bool result);
        [DllImport(GdiPlus)]
        internal static extern Status GdipIsOutlineVisiblePathPoint(IntPtr path, float x, float y, IntPtr pen, IntPtr graphics, out bool result);
        [DllImport(GdiPlus)]
        internal static extern Status GdipIsOutlineVisiblePathPointI(IntPtr path, int x, int y, IntPtr pen, IntPtr graphics, out bool result);

        // GraphicsPathIterator
        [DllImport(GdiPlus)]
        internal static extern Status GdipCreatePathIter(out IntPtr iterator, IntPtr path);
        [DllImport(GdiPlus)]
        internal static extern Status GdipPathIterGetCount(IntPtr iterator, out int count);
        [DllImport(GdiPlus)]
        internal static extern Status GdipPathIterGetSubpathCount(IntPtr iterator, out int count);
        [DllImport(GdiPlus)]
        internal static extern Status GdipDeletePathIter(IntPtr iterator);
        [DllImport(GdiPlus)]
        internal static extern Status GdipPathIterCopyData(IntPtr iterator, out int resultCount, PointF[] points, byte[] types, int startIndex, int endIndex);
        [DllImport(GdiPlus)]
        internal static extern Status GdipPathIterEnumerate(IntPtr iterator, out int resultCount, PointF[] points, byte[] types, int count);
        [DllImport(GdiPlus)]
        internal static extern Status GdipPathIterHasCurve(IntPtr iterator, out bool curve);
        [DllImport(GdiPlus)]
        internal static extern Status GdipPathIterNextMarkerPath(IntPtr iterator, out int resultCount, IntPtr path);
        [DllImport(GdiPlus)]
        internal static extern Status GdipPathIterNextMarker(IntPtr iterator, out int resultCount, out int startIndex, out int endIndex);
        [DllImport(GdiPlus)]
        internal static extern Status GdipPathIterNextPathType(IntPtr iterator, out int resultCount, out byte pathType, out int startIndex, out int endIndex);
        [DllImport(GdiPlus)]
        internal static extern Status GdipPathIterNextSubpathPath(IntPtr iterator, out int resultCount, IntPtr path, out bool isClosed);
        [DllImport(GdiPlus)]
        internal static extern Status GdipPathIterNextSubpath(IntPtr iterator, out int resultCount, out int startIndex, out int endIndex, out bool isClosed);
        [DllImport(GdiPlus)]
        internal static extern Status GdipPathIterRewind(IntPtr iterator);

        // ImageAttributes
        [DllImport(GdiPlus)]
        internal static extern Status GdipCreateImageAttributes(out IntPtr imageattr);

        [DllImport(GdiPlus)]
        internal static extern Status GdipSetImageAttributesColorKeys(IntPtr imageattr,
                ColorAdjustType type, bool enableFlag, int colorLow, int colorHigh);

        [DllImport(GdiPlus)]
        internal static extern Status GdipDisposeImageAttributes(IntPtr imageattr);

        [DllImport(GdiPlus)]
        internal static extern Status GdipSetImageAttributesColorMatrix(IntPtr imageattr,
                ColorAdjustType type, bool enableFlag, IntPtr colorMatrix,
                IntPtr grayMatrix, ColorMatrixFlag flags);

        [DllImport(GdiPlus)]
        internal static extern Status GdipSetImageAttributesGamma(IntPtr imageattr,
            ColorAdjustType type, bool enableFlag,
                                                                            float gamma);

        [DllImport(GdiPlus)]
        internal static extern Status GdipSetImageAttributesNoOp(IntPtr imageattr,
            ColorAdjustType type, bool enableFlag);

        [DllImport(GdiPlus)]
        internal static extern Status GdipSetImageAttributesOutputChannel(IntPtr imageattr,
            ColorAdjustType type, bool enableFlag, ColorChannelFlag channelFlags);

        [DllImport(GdiPlus, CharSet = CharSet.Auto)]
        internal static extern Status GdipSetImageAttributesOutputChannelColorProfile(IntPtr imageattr,
            ColorAdjustType type, bool enableFlag, [MarshalAs(UnmanagedType.LPWStr)] string profileName);

        [DllImport(GdiPlus)]
        internal static extern Status GdipSetImageAttributesRemapTable(IntPtr imageattr,
            ColorAdjustType type, bool enableFlag, uint mapSize, IntPtr colorMap);

        [DllImport(GdiPlus)]
        internal static extern Status GdipSetImageAttributesThreshold(IntPtr imageattr,
            ColorAdjustType type, bool enableFlag, float thresHold);

        [DllImport(GdiPlus)]
        internal static extern Status GdipCloneImageAttributes(IntPtr imageattr, out IntPtr cloneImageattr);

        [DllImport(GdiPlus)]
        internal static extern Status GdipGetImageAttributesAdjustedPalette(IntPtr imageattr, IntPtr colorPalette,
            ColorAdjustType colorAdjustType);

        [DllImport(GdiPlus)]
        internal static extern Status GdipSetImageAttributesWrapMode(IntPtr imageattr, WrapMode wrap,
            int argb, bool clamp);


        // Font
        [DllImport(GdiPlus)]
        internal static extern Status GdipCreateFont(IntPtr fontFamily, float emSize, FontStyle style, GraphicsUnit unit, out IntPtr font);
        [DllImport(GdiPlus)]
        internal static extern Status GdipDeleteFont(IntPtr font);
        [DllImport(GdiPlus, CharSet = CharSet.Auto)]
        internal static extern Status GdipGetLogFont(IntPtr font, IntPtr graphics, [MarshalAs(UnmanagedType.AsAny), Out] object logfontA);

        [DllImport(GdiPlus)]
        internal static extern Status GdipCreateFontFromDC(IntPtr hdc, out IntPtr font);
        [DllImport(GdiPlus, SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern Status GdipCreateFontFromLogfont(IntPtr hdc, ref LOGFONT lf, out IntPtr ptr);

        // These are our private functions, they exists in our own libgdiplus library, this way we
        // avoid relying on wine in System.Drawing
        [DllImport(GdiPlus, CharSet = CharSet.Ansi)]
        internal static extern Status GdipCreateFontFromHfont(IntPtr hdc, out IntPtr font, ref LOGFONT lf);

        // This is win32/gdi, not gdiplus, but it's easier to keep in here, also see above comment
        [DllImport("gdi32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto)]
        internal static extern IntPtr CreateFontIndirect(ref LOGFONT logfont);
        [DllImport("user32.dll", EntryPoint = "GetDC", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        internal static extern IntPtr GetDC(IntPtr hwnd);
        [DllImport("user32.dll", EntryPoint = "ReleaseDC", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        internal static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        [DllImport("gdi32.dll", EntryPoint = "SelectObject", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        internal static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool GetIconInfo(IntPtr hIcon, out IconInfo iconinfo);
        [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal static extern IntPtr CreateIconIndirect([In] ref IconInfo piconinfo);
        [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        internal static extern bool DestroyIcon(IntPtr hIcon);
        [DllImport("gdi32.dll")]
        internal static extern bool DeleteObject(IntPtr hObject);
        [DllImport("user32.dll")]
        internal static extern IntPtr GetDesktopWindow();

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern int BitBlt(IntPtr hdcDest, int nXDest, int nYDest,
            int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

        [DllImport("user32.dll", EntryPoint = "GetSysColor", CallingConvention = CallingConvention.StdCall)]
        public static extern uint Win32GetSysColor(GetSysColorIndex index);


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

        // FontCollection
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetFontCollectionFamilyCount(IntPtr collection, out int found);

        [DllImport(GdiPlus)]
        internal static extern Status GdipGetFontCollectionFamilyList(IntPtr collection, int getCount, IntPtr[] dest, out int retCount);
        //internal static extern Status GdipGetFontCollectionFamilyList( IntPtr collection, int getCount, [Out] FontFamily [] familyList, out int retCount );

        [DllImport(GdiPlus)]
        internal static extern Status GdipNewInstalledFontCollection(out IntPtr collection);

        [DllImport(GdiPlus)]
        internal static extern Status GdipNewPrivateFontCollection(out IntPtr collection);

        [DllImport(GdiPlus)]
        internal static extern Status GdipDeletePrivateFontCollection(ref IntPtr collection);

        [DllImport(GdiPlus, CharSet = CharSet.Auto)]
        internal static extern Status GdipPrivateAddFontFile(IntPtr collection,
                [MarshalAs(UnmanagedType.LPWStr)] string fileName);

        [DllImport(GdiPlus)]
        internal static extern Status GdipPrivateAddMemoryFont(IntPtr collection, IntPtr mem, int length);

        //FontFamily
        [DllImport(GdiPlus, CharSet = CharSet.Auto)]
        internal static extern Status GdipCreateFontFamilyFromName(
            [MarshalAs(UnmanagedType.LPWStr)] string fName, IntPtr collection, out IntPtr fontFamily);

        [DllImport(GdiPlus, CharSet = CharSet.Unicode)]
        internal static extern Status GdipGetFamilyName(IntPtr family, StringBuilder name, int language);

        [DllImport(GdiPlus)]
        internal static extern Status GdipGetGenericFontFamilySansSerif(out IntPtr fontFamily);

        [DllImport(GdiPlus)]
        internal static extern Status GdipGetGenericFontFamilySerif(out IntPtr fontFamily);

        [DllImport(GdiPlus)]
        internal static extern Status GdipGetGenericFontFamilyMonospace(out IntPtr fontFamily);

        [DllImport(GdiPlus)]
        internal static extern Status GdipGetCellAscent(IntPtr fontFamily, int style, out short ascent);

        [DllImport(GdiPlus)]
        internal static extern Status GdipGetCellDescent(IntPtr fontFamily, int style, out short descent);

        [DllImport(GdiPlus)]
        internal static extern Status GdipGetLineSpacing(IntPtr fontFamily, int style, out short spacing);

        [DllImport(GdiPlus)]
        internal static extern Status GdipGetEmHeight(IntPtr fontFamily, int style, out short emHeight);

        [DllImport(GdiPlus)]
        internal static extern Status GdipIsStyleAvailable(IntPtr fontFamily, int style, out bool styleAvailable);

        [DllImport(GdiPlus)]
        internal static extern Status GdipDeleteFontFamily(IntPtr fontFamily);

        [DllImport(GdiPlus)]
        internal static extern Status GdipGetFontSize(IntPtr font, out float size);

        [DllImport(GdiPlus)]
        internal static extern Status GdipGetFontHeight(IntPtr font, IntPtr graphics, out float height);

        [DllImport(GdiPlus)]
        internal static extern Status GdipGetFontHeightGivenDPI(IntPtr font, float dpi, out float height);

        [DllImport(GdiPlus)]
        internal static extern Status GdipCloneFontFamily(IntPtr fontFamily, out IntPtr clone);


        // String Format
        [DllImport(GdiPlus)]
        internal static extern Status GdipCreateStringFormat(int formatAttributes, int language, out IntPtr format);
        [DllImport(GdiPlus)]
        internal static extern Status GdipStringFormatGetGenericDefault(out IntPtr format);
        [DllImport(GdiPlus)]
        internal static extern Status GdipStringFormatGetGenericTypographic(out IntPtr format);
        [DllImport(GdiPlus)]
        internal static extern Status GdipDeleteStringFormat(IntPtr format);
        [DllImport(GdiPlus)]
        internal static extern Status GdipCloneStringFormat(IntPtr srcformat, out IntPtr format);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetStringFormatFlags(IntPtr format, StringFormatFlags flags);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetStringFormatFlags(IntPtr format, out StringFormatFlags flags);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetStringFormatAlign(IntPtr format, StringAlignment align);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetStringFormatAlign(IntPtr format, out StringAlignment align);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetStringFormatLineAlign(IntPtr format, StringAlignment align);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetStringFormatLineAlign(IntPtr format, out StringAlignment align);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetStringFormatTrimming(IntPtr format, StringTrimming trimming);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetStringFormatTrimming(IntPtr format, out StringTrimming trimming);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetStringFormatHotkeyPrefix(IntPtr format, HotkeyPrefix hotkeyPrefix);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetStringFormatHotkeyPrefix(IntPtr format, out HotkeyPrefix hotkeyPrefix);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetStringFormatTabStops(IntPtr format, float firstTabOffset, int count, float[] tabStops);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetStringFormatDigitSubstitution(IntPtr format, int language, out StringDigitSubstitute substitute);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetStringFormatDigitSubstitution(IntPtr format, int language, StringDigitSubstitute substitute);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetStringFormatTabStopCount(IntPtr format, out int count);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetStringFormatTabStops(IntPtr format, int count, out float firstTabOffset, [In, Out] float[] tabStops);

        // metafile
        [DllImport(GdiPlus, CharSet = CharSet.Auto)]
        internal static extern Status GdipCreateMetafileFromFile([MarshalAs(UnmanagedType.LPWStr)] string filename, out IntPtr metafile);
        [DllImport(GdiPlus)]
        internal static extern Status GdipCreateMetafileFromEmf(IntPtr hEmf, bool deleteEmf, out IntPtr metafile);
        [DllImport(GdiPlus)]
        internal static extern Status GdipCreateMetafileFromWmf(IntPtr hWmf, bool deleteWmf, WmfPlaceableFileHeader wmfPlaceableFileHeader, out IntPtr metafile);
        [DllImport(GdiPlus, CharSet = CharSet.Auto)]
        internal static extern Status GdipGetMetafileHeaderFromFile([MarshalAs(UnmanagedType.LPWStr)] string filename, IntPtr header);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetMetafileHeaderFromMetafile(IntPtr metafile, IntPtr header);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetMetafileHeaderFromEmf(IntPtr hEmf, IntPtr header);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetMetafileHeaderFromWmf(IntPtr hWmf, IntPtr wmfPlaceableFileHeader, IntPtr header);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetHemfFromMetafile(IntPtr metafile, out IntPtr hEmf);
        [DllImport(GdiPlus)]
        internal static extern Status GdipGetMetafileDownLevelRasterizationLimit(IntPtr metafile, ref uint metafileRasterizationLimitDpi);
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetMetafileDownLevelRasterizationLimit(IntPtr metafile, uint metafileRasterizationLimitDpi);
        [DllImport(GdiPlus)]
        internal static extern Status GdipPlayMetafileRecord(IntPtr metafile, EmfPlusRecordType recordType, int flags, int dataSize, byte[] data);

        [DllImport(GdiPlus)]
        internal static extern Status GdipRecordMetafile(IntPtr hdc, EmfType type, ref RectangleF frameRect,
            MetafileFrameUnit frameUnit, [MarshalAs(UnmanagedType.LPWStr)] string description, out IntPtr metafile);
        [DllImport(GdiPlus)]
        internal static extern Status GdipRecordMetafileI(IntPtr hdc, EmfType type, ref Rectangle frameRect,
            MetafileFrameUnit frameUnit, [MarshalAs(UnmanagedType.LPWStr)] string description, out IntPtr metafile);
        [DllImport(GdiPlus)]
        internal static extern Status GdipRecordMetafileFileName([MarshalAs(UnmanagedType.LPWStr)] string filename, IntPtr hdc, EmfType type,
            ref RectangleF frameRect, MetafileFrameUnit frameUnit, [MarshalAs(UnmanagedType.LPWStr)] string description, out IntPtr metafile);
        [DllImport(GdiPlus)]
        internal static extern Status GdipRecordMetafileFileNameI([MarshalAs(UnmanagedType.LPWStr)] string filename, IntPtr hdc, EmfType type,
            ref Rectangle frameRect, MetafileFrameUnit frameUnit, [MarshalAs(UnmanagedType.LPWStr)] string description, out IntPtr metafile);
#if !TEST
        [DllImport(GdiPlus, ExactSpelling = true, CharSet = CharSet.Unicode)]
        internal static extern Status GdipCreateMetafileFromStream([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ComIStreamMarshaler))] IStream stream, out IntPtr metafile);
        [DllImport(GdiPlus, ExactSpelling = true, CharSet = CharSet.Unicode)]
        internal static extern Status GdipGetMetafileHeaderFromStream([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ComIStreamMarshaler))] IStream stream, IntPtr header);

        [DllImport(GdiPlus)]
        internal static extern Status GdipRecordMetafileStream([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ComIStreamMarshaler))] IStream stream, IntPtr hdc,
            EmfType type, ref RectangleF frameRect, MetafileFrameUnit frameUnit, [MarshalAs(UnmanagedType.LPWStr)] string description, out IntPtr metafile);
        [DllImport(GdiPlus)]
        internal static extern Status GdipRecordMetafileStreamI([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ComIStreamMarshaler))] IStream stream, IntPtr hdc,
            EmfType type, ref Rectangle frameRect, MetafileFrameUnit frameUnit, [MarshalAs(UnmanagedType.LPWStr)] string description, out IntPtr metafile);
#endif
        //ImageCodecInfo functions
        [DllImport(GdiPlus)]
        static internal extern Status GdipGetImageDecodersSize(out int decoderNums, out int arraySize);
        [DllImport(GdiPlus)]
        static internal extern Status GdipGetImageDecoders(int decoderNums, int arraySize, IntPtr decoders);
        [DllImport(GdiPlus)]
        static internal extern Status GdipGetImageEncodersSize(out int encoderNums, out int arraySize);
        [DllImport(GdiPlus)]
        static internal extern Status GdipGetImageEncoders(int encoderNums, int arraySize, IntPtr encoders);

        //
        // These are stuff that is unix-only
        //
        public delegate int StreamGetHeaderDelegate(IntPtr buf, int bufsz);
        public delegate int StreamGetBytesDelegate(IntPtr buf, int bufsz, bool peek);
        public delegate long StreamSeekDelegate(int offset, int whence);
        public delegate int StreamPutBytesDelegate(IntPtr buf, int bufsz);
        public delegate void StreamCloseDelegate();
        public delegate long StreamSizeDelegate();

        internal sealed class GdiPlusStreamHelper
        {
            public Stream stream;

            private StreamGetHeaderDelegate sghd = null;
            private StreamGetBytesDelegate sgbd = null;
            private StreamSeekDelegate skd = null;
            private StreamPutBytesDelegate spbd = null;
            private StreamCloseDelegate scd = null;
            private StreamSizeDelegate ssd = null;
            private byte[] start_buf;
            private int start_buf_pos;
            private int start_buf_len;
            private byte[] managedBuf;
            private const int default_bufsize = 4096;

            public GdiPlusStreamHelper(Stream s, bool seekToOrigin)
            {
                managedBuf = new byte[default_bufsize];

                stream = s;
                if (stream != null && stream.CanSeek && seekToOrigin)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                }
            }

            public int StreamGetHeaderImpl(IntPtr buf, int bufsz)
            {
                int bytesRead;

                start_buf = new byte[bufsz];

                try
                {
                    bytesRead = stream.Read(start_buf, 0, bufsz);
                }
                catch (IOException)
                {
                    return -1;
                }

                if (bytesRead > 0 && buf != IntPtr.Zero)
                {
                    Marshal.Copy(start_buf, 0, (IntPtr)(buf.ToInt64()), bytesRead);
                }

                start_buf_pos = 0;
                start_buf_len = bytesRead;

                return bytesRead;
            }

            public StreamGetHeaderDelegate GetHeaderDelegate
            {
                get
                {
                    if (stream != null && stream.CanRead)
                    {
                        if (sghd == null)
                        {
                            sghd = new StreamGetHeaderDelegate(StreamGetHeaderImpl);
                        }
                        return sghd;
                    }
                    return null;
                }
            }

            public int StreamGetBytesImpl(IntPtr buf, int bufsz, bool peek)
            {
                if (buf == IntPtr.Zero && peek)
                {
                    return -1;
                }

                if (bufsz > managedBuf.Length)
                    managedBuf = new byte[bufsz];
                int bytesRead = 0;
                long streamPosition = 0;

                if (bufsz > 0)
                {
                    if (stream.CanSeek)
                    {
                        streamPosition = stream.Position;
                    }
                    if (start_buf_len > 0)
                    {
                        if (start_buf_len > bufsz)
                        {
                            Array.Copy(start_buf, start_buf_pos, managedBuf, 0, bufsz);
                            start_buf_pos += bufsz;
                            start_buf_len -= bufsz;
                            bytesRead = bufsz;
                            bufsz = 0;
                        }
                        else
                        {
                            // this is easy
                            Array.Copy(start_buf, start_buf_pos, managedBuf, 0, start_buf_len);
                            bufsz -= start_buf_len;
                            bytesRead = start_buf_len;
                            start_buf_len = 0;
                        }
                    }

                    if (bufsz > 0)
                    {
                        try
                        {
                            bytesRead += stream.Read(managedBuf, bytesRead, bufsz);
                        }
                        catch (IOException)
                        {
                            return -1;
                        }
                    }

                    if (bytesRead > 0 && buf != IntPtr.Zero)
                    {
                        Marshal.Copy(managedBuf, 0, (IntPtr)(buf.ToInt64()), bytesRead);
                    }

                    if (!stream.CanSeek && (bufsz == 10) && peek)
                    {
                        // Special 'hack' to support peeking of the type for gdi+ on non-seekable streams
                    }

                    if (peek)
                    {
                        if (stream.CanSeek)
                        {
                            // If we are peeking bytes, then go back to original position before peeking
                            stream.Seek(streamPosition, SeekOrigin.Begin);
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }
                    }
                }

                return bytesRead;
            }

            public StreamGetBytesDelegate GetBytesDelegate
            {
                get
                {
                    if (stream != null && stream.CanRead)
                    {
                        if (sgbd == null)
                        {
                            sgbd = new StreamGetBytesDelegate(StreamGetBytesImpl);
                        }
                        return sgbd;
                    }
                    return null;
                }
            }

            public long StreamSeekImpl(int offset, int whence)
            {
                // Make sure we have a valid 'whence'.
                if ((whence < 0) || (whence > 2))
                    return -1;

                // Invalidate the start_buf if we're actually going to call a Seek method.
                start_buf_pos += start_buf_len;
                start_buf_len = 0;

                SeekOrigin origin;

                // Translate 'whence' into a SeekOrigin enum member.
                switch (whence)
                {
                    case 0:
                        origin = SeekOrigin.Begin;
                        break;
                    case 1:
                        origin = SeekOrigin.Current;
                        break;
                    case 2:
                        origin = SeekOrigin.End;
                        break;

                    // The following line is redundant but necessary to avoid a
                    // "Use of unassigned local variable" error without actually
                    // initializing 'origin' to a dummy value.
                    default:
                        return -1;
                }

                // Do the actual seek operation and return its result.
                return stream.Seek((long)offset, origin);
            }

            public StreamSeekDelegate SeekDelegate
            {
                get
                {
                    if (stream != null && stream.CanSeek)
                    {
                        if (skd == null)
                        {
                            skd = new StreamSeekDelegate(StreamSeekImpl);
                        }
                        return skd;
                    }
                    return null;
                }
            }

            public int StreamPutBytesImpl(IntPtr buf, int bufsz)
            {
                if (bufsz > managedBuf.Length)
                    managedBuf = new byte[bufsz];
                Marshal.Copy(buf, managedBuf, 0, bufsz);
                stream.Write(managedBuf, 0, bufsz);
                return bufsz;
            }

            public StreamPutBytesDelegate PutBytesDelegate
            {
                get
                {
                    if (stream != null && stream.CanWrite)
                    {
                        if (spbd == null)
                        {
                            spbd = new StreamPutBytesDelegate(StreamPutBytesImpl);
                        }
                        return spbd;
                    }
                    return null;
                }
            }

            public void StreamCloseImpl()
            {
                stream.Dispose();
            }

            public StreamCloseDelegate CloseDelegate
            {
                get
                {
                    if (stream != null)
                    {
                        if (scd == null)
                        {
                            scd = new StreamCloseDelegate(StreamCloseImpl);
                        }
                        return scd;
                    }
                    return null;
                }
            }

            public long StreamSizeImpl()
            {
                try
                {
                    return stream.Length;
                }
                catch
                {
                    return -1;
                }
            }

            public StreamSizeDelegate SizeDelegate
            {
                get
                {
                    if (stream != null)
                    {
                        if (ssd == null)
                        {
                            ssd = new StreamSizeDelegate(StreamSizeImpl);
                        }
                        return ssd;
                    }
                    return null;
                }
            }

        }

        /* Mac only function calls */
        [DllImport(GdiPlus)]
        internal static extern Status GdipCreateFromContext_macosx(IntPtr cgref, int width, int height, out IntPtr graphics);

        /* Linux only function calls*/
        [DllImport(GdiPlus)]
        internal static extern Status GdipSetVisibleClip_linux(IntPtr graphics, ref Rectangle rect);

        [DllImport(GdiPlus)]
        internal static extern Status GdipCreateFromXDrawable_linux(IntPtr drawable, IntPtr display, out IntPtr graphics);

        // Stream functions for non-Win32 (libgdiplus specific)
        [DllImport(GdiPlus)]
        static internal extern Status GdipLoadImageFromDelegate_linux(StreamGetHeaderDelegate getHeader,
            StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
            StreamCloseDelegate close, StreamSizeDelegate size, out IntPtr image);

        [DllImport(GdiPlus)]
        static internal extern Status GdipSaveImageToDelegate_linux(IntPtr image, StreamGetBytesDelegate getBytes,
            StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek, StreamCloseDelegate close,
            StreamSizeDelegate size, ref Guid encoderClsID, IntPtr encoderParameters);

        [DllImport(GdiPlus)]
        static internal extern Status GdipCreateMetafileFromDelegate_linux(StreamGetHeaderDelegate getHeader,
            StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
            StreamCloseDelegate close, StreamSizeDelegate size, out IntPtr metafile);

        [DllImport(GdiPlus)]
        static internal extern Status GdipGetMetafileHeaderFromDelegate_linux(StreamGetHeaderDelegate getHeader,
            StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
            StreamCloseDelegate close, StreamSizeDelegate size, IntPtr header);

        [DllImport(GdiPlus)]
        static internal extern Status GdipRecordMetafileFromDelegate_linux(StreamGetHeaderDelegate getHeader,
            StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
            StreamCloseDelegate close, StreamSizeDelegate size, IntPtr hdc, EmfType type, ref RectangleF frameRect,
            MetafileFrameUnit frameUnit, [MarshalAs(UnmanagedType.LPWStr)] string description, out IntPtr metafile);

        [DllImport(GdiPlus)]
        static internal extern Status GdipRecordMetafileFromDelegateI_linux(StreamGetHeaderDelegate getHeader,
            StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
            StreamCloseDelegate close, StreamSizeDelegate size, IntPtr hdc, EmfType type, ref Rectangle frameRect,
            MetafileFrameUnit frameUnit, [MarshalAs(UnmanagedType.LPWStr)] string description, out IntPtr metafile);

        [DllImport("libc")]
        static extern int uname(IntPtr buf);
        #endregion
    }
}
