// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace System.Drawing
{
    internal partial class SafeNativeMethods
    {
        internal unsafe partial class Gdip
        {
            private static IntPtr LoadNativeLibrary()
            {
                IntPtr lib = IntPtr.Zero;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    lib = Interop.Libdl.dlopen("libgdiplus.dylib", Interop.Libdl.RTLD_NOW);
                }
                else
                {
                    // Various Unix package managers have chosen different names for the "libgdiplus" shared library.
                    // The mono project, where libgdiplus originated, allowed both of the names below to be used, via
                    // a global configuration setting. We prefer the "unversioned" shared object name, and fallback to
                    // the name suffixed with ".0".
                    lib = Interop.Libdl.dlopen("libgdiplus.so", Interop.Libdl.RTLD_NOW);
                    if (lib == IntPtr.Zero)
                    {
                        lib = Interop.Libdl.dlopen("libgdiplus.so.0", Interop.Libdl.RTLD_NOW);
                    }
                }

                // This function may return a null handle. If it does, individual functions loaded from it will throw a DllNotFoundException,
                // but not until an attempt is made to actually use the function (rather than load it). This matches how PInvokes behave.
                return lib;
            }

            private static IntPtr LoadFunctionPointer(IntPtr nativeLibraryHandle, string functionName) => Interop.Libdl.dlsym(nativeLibraryHandle, functionName);

            internal static void CheckStatus(Status status)
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

            private static void LoadPlatformFunctionPointers()
            {
                GdiplusStartup_ptr = FunctionWrapper.Load<GdiplusStartup_delegate>(s_gdipModule, "GdiplusStartup");
                GdiplusShutdown_ptr = FunctionWrapper.Load<GdiplusShutdown_delegate>(s_gdipModule, "GdiplusShutdown");
                GdipAlloc_ptr = FunctionWrapper.Load<GdipAlloc_delegate>(s_gdipModule, "GdipAlloc");
                GdipFree_ptr = FunctionWrapper.Load<GdipFree_delegate>(s_gdipModule, "GdipFree");
                GdipDeleteBrush_ptr = FunctionWrapper.Load<GdipDeleteBrush_delegate>(s_gdipModule, "GdipDeleteBrush");
                GdipGetBrushType_ptr = FunctionWrapper.Load<GdipGetBrushType_delegate>(s_gdipModule, "GdipGetBrushType");
                GdipCreateFromHDC_ptr = FunctionWrapper.Load<GdipCreateFromHDC_delegate>(s_gdipModule, "GdipCreateFromHDC");
                GdipDeleteGraphics_ptr = FunctionWrapper.Load<GdipDeleteGraphics_delegate>(s_gdipModule, "GdipDeleteGraphics");
                GdipRestoreGraphics_ptr = FunctionWrapper.Load<GdipRestoreGraphics_delegate>(s_gdipModule, "GdipRestoreGraphics");
                GdipSaveGraphics_ptr = FunctionWrapper.Load<GdipSaveGraphics_delegate>(s_gdipModule, "GdipSaveGraphics");
                GdipDrawArc_ptr = FunctionWrapper.Load<GdipDrawArc_delegate>(s_gdipModule, "GdipDrawArc");
                GdipDrawArcI_ptr = FunctionWrapper.Load<GdipDrawArcI_delegate>(s_gdipModule, "GdipDrawArcI");
                GdipDrawBezier_ptr = FunctionWrapper.Load<GdipDrawBezier_delegate>(s_gdipModule, "GdipDrawBezier");
                GdipDrawBezierI_ptr = FunctionWrapper.Load<GdipDrawBezierI_delegate>(s_gdipModule, "GdipDrawBezierI");
                GdipDrawEllipseI_ptr = FunctionWrapper.Load<GdipDrawEllipseI_delegate>(s_gdipModule, "GdipDrawEllipseI");
                GdipDrawEllipse_ptr = FunctionWrapper.Load<GdipDrawEllipse_delegate>(s_gdipModule, "GdipDrawEllipse");
                GdipDrawLine_ptr = FunctionWrapper.Load<GdipDrawLine_delegate>(s_gdipModule, "GdipDrawLine");
                GdipDrawLineI_ptr = FunctionWrapper.Load<GdipDrawLineI_delegate>(s_gdipModule, "GdipDrawLineI");
                GdipDrawLines_ptr = FunctionWrapper.Load<GdipDrawLines_delegate>(s_gdipModule, "GdipDrawLines");
                GdipDrawLinesI_ptr = FunctionWrapper.Load<GdipDrawLinesI_delegate>(s_gdipModule, "GdipDrawLinesI");
                GdipDrawPath_ptr = FunctionWrapper.Load<GdipDrawPath_delegate>(s_gdipModule, "GdipDrawPath");
                GdipDrawPie_ptr = FunctionWrapper.Load<GdipDrawPie_delegate>(s_gdipModule, "GdipDrawPie");
                GdipDrawPieI_ptr = FunctionWrapper.Load<GdipDrawPieI_delegate>(s_gdipModule, "GdipDrawPieI");
                GdipDrawPolygon_ptr = FunctionWrapper.Load<GdipDrawPolygon_delegate>(s_gdipModule, "GdipDrawPolygon");
                GdipDrawPolygonI_ptr = FunctionWrapper.Load<GdipDrawPolygonI_delegate>(s_gdipModule, "GdipDrawPolygonI");
                GdipDrawRectangle_ptr = FunctionWrapper.Load<GdipDrawRectangle_delegate>(s_gdipModule, "GdipDrawRectangle");
                GdipDrawRectangleI_ptr = FunctionWrapper.Load<GdipDrawRectangleI_delegate>(s_gdipModule, "GdipDrawRectangleI");
                GdipDrawRectangles_ptr = FunctionWrapper.Load<GdipDrawRectangles_delegate>(s_gdipModule, "GdipDrawRectangles");
                GdipDrawRectanglesI_ptr = FunctionWrapper.Load<GdipDrawRectanglesI_delegate>(s_gdipModule, "GdipDrawRectanglesI");
                GdipFillEllipseI_ptr = FunctionWrapper.Load<GdipFillEllipseI_delegate>(s_gdipModule, "GdipFillEllipseI");
                GdipFillEllipse_ptr = FunctionWrapper.Load<GdipFillEllipse_delegate>(s_gdipModule, "GdipFillEllipse");
                GdipFillPolygon_ptr = FunctionWrapper.Load<GdipFillPolygon_delegate>(s_gdipModule, "GdipFillPolygon");
                GdipFillPolygonI_ptr = FunctionWrapper.Load<GdipFillPolygonI_delegate>(s_gdipModule, "GdipFillPolygonI");
                GdipFillPolygon2_ptr = FunctionWrapper.Load<GdipFillPolygon2_delegate>(s_gdipModule, "GdipFillPolygon2");
                GdipFillPolygon2I_ptr = FunctionWrapper.Load<GdipFillPolygon2I_delegate>(s_gdipModule, "GdipFillPolygon2I");
                GdipFillRectangle_ptr = FunctionWrapper.Load<GdipFillRectangle_delegate>(s_gdipModule, "GdipFillRectangle");
                GdipFillRectangleI_ptr = FunctionWrapper.Load<GdipFillRectangleI_delegate>(s_gdipModule, "GdipFillRectangleI");
                GdipFillRectangles_ptr = FunctionWrapper.Load<GdipFillRectangles_delegate>(s_gdipModule, "GdipFillRectangles");
                GdipFillRectanglesI_ptr = FunctionWrapper.Load<GdipFillRectanglesI_delegate>(s_gdipModule, "GdipFillRectanglesI");
                GdipDrawString_ptr = FunctionWrapper.Load<GdipDrawString_delegate>(s_gdipModule, "GdipDrawString");
                GdipGetDC_ptr = FunctionWrapper.Load<GdipGetDC_delegate>(s_gdipModule, "GdipGetDC");
                GdipReleaseDC_ptr = FunctionWrapper.Load<GdipReleaseDC_delegate>(s_gdipModule, "GdipReleaseDC");
                GdipDrawImageRectI_ptr = FunctionWrapper.Load<GdipDrawImageRectI_delegate>(s_gdipModule, "GdipDrawImageRectI");
                GdipGetRenderingOrigin_ptr = FunctionWrapper.Load<GdipGetRenderingOrigin_delegate>(s_gdipModule, "GdipGetRenderingOrigin");
                GdipSetRenderingOrigin_ptr = FunctionWrapper.Load<GdipSetRenderingOrigin_delegate>(s_gdipModule, "GdipSetRenderingOrigin");
                GdipCloneBitmapArea_ptr = FunctionWrapper.Load<GdipCloneBitmapArea_delegate>(s_gdipModule, "GdipCloneBitmapArea");
                GdipCloneBitmapAreaI_ptr = FunctionWrapper.Load<GdipCloneBitmapAreaI_delegate>(s_gdipModule, "GdipCloneBitmapAreaI");
                GdipGraphicsClear_ptr = FunctionWrapper.Load<GdipGraphicsClear_delegate>(s_gdipModule, "GdipGraphicsClear");
                GdipDrawClosedCurve_ptr = FunctionWrapper.Load<GdipDrawClosedCurve_delegate>(s_gdipModule, "GdipDrawClosedCurve");
                GdipDrawClosedCurveI_ptr = FunctionWrapper.Load<GdipDrawClosedCurveI_delegate>(s_gdipModule, "GdipDrawClosedCurveI");
                GdipDrawClosedCurve2_ptr = FunctionWrapper.Load<GdipDrawClosedCurve2_delegate>(s_gdipModule, "GdipDrawClosedCurve2");
                GdipDrawClosedCurve2I_ptr = FunctionWrapper.Load<GdipDrawClosedCurve2I_delegate>(s_gdipModule, "GdipDrawClosedCurve2I");
                GdipDrawCurve_ptr = FunctionWrapper.Load<GdipDrawCurve_delegate>(s_gdipModule, "GdipDrawCurve");
                GdipDrawCurveI_ptr = FunctionWrapper.Load<GdipDrawCurveI_delegate>(s_gdipModule, "GdipDrawCurveI");
                GdipDrawCurve2_ptr = FunctionWrapper.Load<GdipDrawCurve2_delegate>(s_gdipModule, "GdipDrawCurve2");
                GdipDrawCurve2I_ptr = FunctionWrapper.Load<GdipDrawCurve2I_delegate>(s_gdipModule, "GdipDrawCurve2I");
                GdipDrawCurve3_ptr = FunctionWrapper.Load<GdipDrawCurve3_delegate>(s_gdipModule, "GdipDrawCurve3");
                GdipDrawCurve3I_ptr = FunctionWrapper.Load<GdipDrawCurve3I_delegate>(s_gdipModule, "GdipDrawCurve3I");
                GdipFillClosedCurve_ptr = FunctionWrapper.Load<GdipFillClosedCurve_delegate>(s_gdipModule, "GdipFillClosedCurve");
                GdipFillClosedCurveI_ptr = FunctionWrapper.Load<GdipFillClosedCurveI_delegate>(s_gdipModule, "GdipFillClosedCurveI");
                GdipFillClosedCurve2_ptr = FunctionWrapper.Load<GdipFillClosedCurve2_delegate>(s_gdipModule, "GdipFillClosedCurve2");
                GdipFillClosedCurve2I_ptr = FunctionWrapper.Load<GdipFillClosedCurve2I_delegate>(s_gdipModule, "GdipFillClosedCurve2I");
                GdipFillPie_ptr = FunctionWrapper.Load<GdipFillPie_delegate>(s_gdipModule, "GdipFillPie");
                GdipFillPieI_ptr = FunctionWrapper.Load<GdipFillPieI_delegate>(s_gdipModule, "GdipFillPieI");
                GdipFillPath_ptr = FunctionWrapper.Load<GdipFillPath_delegate>(s_gdipModule, "GdipFillPath");
                GdipGetNearestColor_ptr = FunctionWrapper.Load<GdipGetNearestColor_delegate>(s_gdipModule, "GdipGetNearestColor");
                GdipTransformPoints_ptr = FunctionWrapper.Load<GdipTransformPoints_delegate>(s_gdipModule, "GdipTransformPoints");
                GdipTransformPointsI_ptr = FunctionWrapper.Load<GdipTransformPointsI_delegate>(s_gdipModule, "GdipTransformPointsI");
                GdipSetCompositingMode_ptr = FunctionWrapper.Load<GdipSetCompositingMode_delegate>(s_gdipModule, "GdipSetCompositingMode");
                GdipGetCompositingMode_ptr = FunctionWrapper.Load<GdipGetCompositingMode_delegate>(s_gdipModule, "GdipGetCompositingMode");
                GdipSetCompositingQuality_ptr = FunctionWrapper.Load<GdipSetCompositingQuality_delegate>(s_gdipModule, "GdipSetCompositingQuality");
                GdipGetCompositingQuality_ptr = FunctionWrapper.Load<GdipGetCompositingQuality_delegate>(s_gdipModule, "GdipGetCompositingQuality");
                GdipSetInterpolationMode_ptr = FunctionWrapper.Load<GdipSetInterpolationMode_delegate>(s_gdipModule, "GdipSetInterpolationMode");
                GdipGetInterpolationMode_ptr = FunctionWrapper.Load<GdipGetInterpolationMode_delegate>(s_gdipModule, "GdipGetInterpolationMode");
                GdipGetDpiX_ptr = FunctionWrapper.Load<GdipGetDpiX_delegate>(s_gdipModule, "GdipGetDpiX");
                GdipGetDpiY_ptr = FunctionWrapper.Load<GdipGetDpiY_delegate>(s_gdipModule, "GdipGetDpiY");
                GdipGetPageUnit_ptr = FunctionWrapper.Load<GdipGetPageUnit_delegate>(s_gdipModule, "GdipGetPageUnit");
                GdipGetPageScale_ptr = FunctionWrapper.Load<GdipGetPageScale_delegate>(s_gdipModule, "GdipGetPageScale");
                GdipSetPageUnit_ptr = FunctionWrapper.Load<GdipSetPageUnit_delegate>(s_gdipModule, "GdipSetPageUnit");
                GdipSetPageScale_ptr = FunctionWrapper.Load<GdipSetPageScale_delegate>(s_gdipModule, "GdipSetPageScale");
                GdipSetPixelOffsetMode_ptr = FunctionWrapper.Load<GdipSetPixelOffsetMode_delegate>(s_gdipModule, "GdipSetPixelOffsetMode");
                GdipGetPixelOffsetMode_ptr = FunctionWrapper.Load<GdipGetPixelOffsetMode_delegate>(s_gdipModule, "GdipGetPixelOffsetMode");
                GdipSetSmoothingMode_ptr = FunctionWrapper.Load<GdipSetSmoothingMode_delegate>(s_gdipModule, "GdipSetSmoothingMode");
                GdipGetSmoothingMode_ptr = FunctionWrapper.Load<GdipGetSmoothingMode_delegate>(s_gdipModule, "GdipGetSmoothingMode");
                GdipSetTextContrast_ptr = FunctionWrapper.Load<GdipSetTextContrast_delegate>(s_gdipModule, "GdipSetTextContrast");
                GdipGetTextContrast_ptr = FunctionWrapper.Load<GdipGetTextContrast_delegate>(s_gdipModule, "GdipGetTextContrast");
                GdipSetTextRenderingHint_ptr = FunctionWrapper.Load<GdipSetTextRenderingHint_delegate>(s_gdipModule, "GdipSetTextRenderingHint");
                GdipGetTextRenderingHint_ptr = FunctionWrapper.Load<GdipGetTextRenderingHint_delegate>(s_gdipModule, "GdipGetTextRenderingHint");
                GdipFlush_ptr = FunctionWrapper.Load<GdipFlush_delegate>(s_gdipModule, "GdipFlush");
                GdipAddPathString_ptr = FunctionWrapper.Load<GdipAddPathString_delegate>(s_gdipModule, "GdipAddPathString");
                GdipAddPathStringI_ptr = FunctionWrapper.Load<GdipAddPathStringI_delegate>(s_gdipModule, "GdipAddPathStringI");
                GdipCreateFromHWND_ptr = FunctionWrapper.Load<GdipCreateFromHWND_delegate>(s_gdipModule, "GdipCreateFromHWND");
                GdipMeasureString_ptr = FunctionWrapper.Load<GdipMeasureString_delegate>(s_gdipModule, "GdipMeasureString");
                GdipMeasureCharacterRanges_ptr = FunctionWrapper.Load<GdipMeasureCharacterRanges_delegate>(s_gdipModule, "GdipMeasureCharacterRanges");
                GdipCreateBitmapFromScan0_ptr = FunctionWrapper.Load<GdipCreateBitmapFromScan0_delegate>(s_gdipModule, "GdipCreateBitmapFromScan0");
                GdipCreateBitmapFromGraphics_ptr = FunctionWrapper.Load<GdipCreateBitmapFromGraphics_delegate>(s_gdipModule, "GdipCreateBitmapFromGraphics");
                GdipBitmapLockBits_ptr = FunctionWrapper.Load<GdipBitmapLockBits_delegate>(s_gdipModule, "GdipBitmapLockBits");
                GdipBitmapSetResolution_ptr = FunctionWrapper.Load<GdipBitmapSetResolution_delegate>(s_gdipModule, "GdipBitmapSetResolution");
                GdipBitmapUnlockBits_ptr = FunctionWrapper.Load<GdipBitmapUnlockBits_delegate>(s_gdipModule, "GdipBitmapUnlockBits");
                GdipBitmapGetPixel_ptr = FunctionWrapper.Load<GdipBitmapGetPixel_delegate>(s_gdipModule, "GdipBitmapGetPixel");
                GdipBitmapSetPixel_ptr = FunctionWrapper.Load<GdipBitmapSetPixel_delegate>(s_gdipModule, "GdipBitmapSetPixel");
                GdipLoadImageFromFile_ptr = FunctionWrapper.Load<GdipLoadImageFromFile_delegate>(s_gdipModule, "GdipLoadImageFromFile");
                GdipLoadImageFromStream_ptr = FunctionWrapper.Load<GdipLoadImageFromStream_delegate>(s_gdipModule, "GdipLoadImageFromStream");
                GdipSaveImageToStream_ptr = FunctionWrapper.Load<GdipSaveImageToStream_delegate>(s_gdipModule, "GdipSaveImageToStream");
                GdipCloneImage_ptr = FunctionWrapper.Load<GdipCloneImage_delegate>(s_gdipModule, "GdipCloneImage");
                GdipLoadImageFromFileICM_ptr = FunctionWrapper.Load<GdipLoadImageFromFileICM_delegate>(s_gdipModule, "GdipLoadImageFromFileICM");
                GdipCreateBitmapFromHBITMAP_ptr = FunctionWrapper.Load<GdipCreateBitmapFromHBITMAP_delegate>(s_gdipModule, "GdipCreateBitmapFromHBITMAP");
                GdipDisposeImage_ptr = FunctionWrapper.Load<GdipDisposeImage_delegate>(s_gdipModule, "GdipDisposeImage");
                GdipGetImageFlags_ptr = FunctionWrapper.Load<GdipGetImageFlags_delegate>(s_gdipModule, "GdipGetImageFlags");
                GdipGetImageType_ptr = FunctionWrapper.Load<GdipGetImageType_delegate>(s_gdipModule, "GdipGetImageType");
                GdipImageGetFrameDimensionsCount_ptr = FunctionWrapper.Load<GdipImageGetFrameDimensionsCount_delegate>(s_gdipModule, "GdipImageGetFrameDimensionsCount");
                GdipImageGetFrameDimensionsList_ptr = FunctionWrapper.Load<GdipImageGetFrameDimensionsList_delegate>(s_gdipModule, "GdipImageGetFrameDimensionsList");
                GdipGetImageHeight_ptr = FunctionWrapper.Load<GdipGetImageHeight_delegate>(s_gdipModule, "GdipGetImageHeight");
                GdipGetImageHorizontalResolution_ptr = FunctionWrapper.Load<GdipGetImageHorizontalResolution_delegate>(s_gdipModule, "GdipGetImageHorizontalResolution");
                GdipGetImagePaletteSize_ptr = FunctionWrapper.Load<GdipGetImagePaletteSize_delegate>(s_gdipModule, "GdipGetImagePaletteSize");
                GdipGetImagePalette_ptr = FunctionWrapper.Load<GdipGetImagePalette_delegate>(s_gdipModule, "GdipGetImagePalette");
                GdipSetImagePalette_ptr = FunctionWrapper.Load<GdipSetImagePalette_delegate>(s_gdipModule, "GdipSetImagePalette");
                GdipGetImageDimension_ptr = FunctionWrapper.Load<GdipGetImageDimension_delegate>(s_gdipModule, "GdipGetImageDimension");
                GdipGetImagePixelFormat_ptr = FunctionWrapper.Load<GdipGetImagePixelFormat_delegate>(s_gdipModule, "GdipGetImagePixelFormat");
                GdipGetPropertyCount_ptr = FunctionWrapper.Load<GdipGetPropertyCount_delegate>(s_gdipModule, "GdipGetPropertyCount");
                GdipGetPropertyIdList_ptr = FunctionWrapper.Load<GdipGetPropertyIdList_delegate>(s_gdipModule, "GdipGetPropertyIdList");
                GdipGetPropertySize_ptr = FunctionWrapper.Load<GdipGetPropertySize_delegate>(s_gdipModule, "GdipGetPropertySize");
                GdipGetAllPropertyItems_ptr = FunctionWrapper.Load<GdipGetAllPropertyItems_delegate>(s_gdipModule, "GdipGetAllPropertyItems");
                GdipGetImageRawFormat_ptr = FunctionWrapper.Load<GdipGetImageRawFormat_delegate>(s_gdipModule, "GdipGetImageRawFormat");
                GdipGetImageVerticalResolution_ptr = FunctionWrapper.Load<GdipGetImageVerticalResolution_delegate>(s_gdipModule, "GdipGetImageVerticalResolution");
                GdipGetImageWidth_ptr = FunctionWrapper.Load<GdipGetImageWidth_delegate>(s_gdipModule, "GdipGetImageWidth");
                GdipGetImageBounds_ptr = FunctionWrapper.Load<GdipGetImageBounds_delegate>(s_gdipModule, "GdipGetImageBounds");
                GdipGetEncoderParameterListSize_ptr = FunctionWrapper.Load<GdipGetEncoderParameterListSize_delegate>(s_gdipModule, "GdipGetEncoderParameterListSize");
                GdipGetEncoderParameterList_ptr = FunctionWrapper.Load<GdipGetEncoderParameterList_delegate>(s_gdipModule, "GdipGetEncoderParameterList");
                GdipImageGetFrameCount_ptr = FunctionWrapper.Load<GdipImageGetFrameCount_delegate>(s_gdipModule, "GdipImageGetFrameCount");
                GdipImageSelectActiveFrame_ptr = FunctionWrapper.Load<GdipImageSelectActiveFrame_delegate>(s_gdipModule, "GdipImageSelectActiveFrame");
                GdipGetPropertyItemSize_ptr = FunctionWrapper.Load<GdipGetPropertyItemSize_delegate>(s_gdipModule, "GdipGetPropertyItemSize");
                GdipGetPropertyItem_ptr = FunctionWrapper.Load<GdipGetPropertyItem_delegate>(s_gdipModule, "GdipGetPropertyItem");
                GdipRemovePropertyItem_ptr = FunctionWrapper.Load<GdipRemovePropertyItem_delegate>(s_gdipModule, "GdipRemovePropertyItem");
                GdipSetPropertyItem_ptr = FunctionWrapper.Load<GdipSetPropertyItem_delegate>(s_gdipModule, "GdipSetPropertyItem");
                GdipGetImageThumbnail_ptr = FunctionWrapper.Load<GdipGetImageThumbnail_delegate>(s_gdipModule, "GdipGetImageThumbnail");
                GdipImageRotateFlip_ptr = FunctionWrapper.Load<GdipImageRotateFlip_delegate>(s_gdipModule, "GdipImageRotateFlip");
                GdipSaveImageToFile_ptr = FunctionWrapper.Load<GdipSaveImageToFile_delegate>(s_gdipModule, "GdipSaveImageToFile");
                GdipSaveAdd_ptr = FunctionWrapper.Load<GdipSaveAdd_delegate>(s_gdipModule, "GdipSaveAdd");
                GdipSaveAddImage_ptr = FunctionWrapper.Load<GdipSaveAddImage_delegate>(s_gdipModule, "GdipSaveAddImage");
                GdipDrawImageI_ptr = FunctionWrapper.Load<GdipDrawImageI_delegate>(s_gdipModule, "GdipDrawImageI");
                GdipGetImageGraphicsContext_ptr = FunctionWrapper.Load<GdipGetImageGraphicsContext_delegate>(s_gdipModule, "GdipGetImageGraphicsContext");
                GdipDrawImage_ptr = FunctionWrapper.Load<GdipDrawImage_delegate>(s_gdipModule, "GdipDrawImage");
                GdipDrawImagePoints_ptr = FunctionWrapper.Load<GdipDrawImagePoints_delegate>(s_gdipModule, "GdipDrawImagePoints");
                GdipDrawImagePointsI_ptr = FunctionWrapper.Load<GdipDrawImagePointsI_delegate>(s_gdipModule, "GdipDrawImagePointsI");
                GdipDrawImageRectRectI_ptr = FunctionWrapper.Load<GdipDrawImageRectRectI_delegate>(s_gdipModule, "GdipDrawImageRectRectI");
                GdipDrawImageRectRect_ptr = FunctionWrapper.Load<GdipDrawImageRectRect_delegate>(s_gdipModule, "GdipDrawImageRectRect");
                GdipDrawImagePointsRectI_ptr = FunctionWrapper.Load<GdipDrawImagePointsRectI_delegate>(s_gdipModule, "GdipDrawImagePointsRectI");
                GdipDrawImagePointsRect_ptr = FunctionWrapper.Load<GdipDrawImagePointsRect_delegate>(s_gdipModule, "GdipDrawImagePointsRect");
                GdipDrawImageRect_ptr = FunctionWrapper.Load<GdipDrawImageRect_delegate>(s_gdipModule, "GdipDrawImageRect");
                GdipDrawImagePointRect_ptr = FunctionWrapper.Load<GdipDrawImagePointRect_delegate>(s_gdipModule, "GdipDrawImagePointRect");
                GdipDrawImagePointRectI_ptr = FunctionWrapper.Load<GdipDrawImagePointRectI_delegate>(s_gdipModule, "GdipDrawImagePointRectI");
                GdipCreateHBITMAPFromBitmap_ptr = FunctionWrapper.Load<GdipCreateHBITMAPFromBitmap_delegate>(s_gdipModule, "GdipCreateHBITMAPFromBitmap");
                GdipCreateBitmapFromFile_ptr = FunctionWrapper.Load<GdipCreateBitmapFromFile_delegate>(s_gdipModule, "GdipCreateBitmapFromFile");
                GdipCreateBitmapFromFileICM_ptr = FunctionWrapper.Load<GdipCreateBitmapFromFileICM_delegate>(s_gdipModule, "GdipCreateBitmapFromFileICM");
                GdipCreateHICONFromBitmap_ptr = FunctionWrapper.Load<GdipCreateHICONFromBitmap_delegate>(s_gdipModule, "GdipCreateHICONFromBitmap");
                GdipCreateBitmapFromHICON_ptr = FunctionWrapper.Load<GdipCreateBitmapFromHICON_delegate>(s_gdipModule, "GdipCreateBitmapFromHICON");
                GdipCreateBitmapFromResource_ptr = FunctionWrapper.Load<GdipCreateBitmapFromResource_delegate>(s_gdipModule, "GdipCreateBitmapFromResource");
                GdipCreatePath_ptr = FunctionWrapper.Load<GdipCreatePath_delegate>(s_gdipModule, "GdipCreatePath");
                GdipCreatePath2_ptr = FunctionWrapper.Load<GdipCreatePath2_delegate>(s_gdipModule, "GdipCreatePath2");
                GdipCreatePath2I_ptr = FunctionWrapper.Load<GdipCreatePath2I_delegate>(s_gdipModule, "GdipCreatePath2I");
                GdipClonePath_ptr = FunctionWrapper.Load<GdipClonePath_delegate>(s_gdipModule, "GdipClonePath");
                GdipDeletePath_ptr = FunctionWrapper.Load<GdipDeletePath_delegate>(s_gdipModule, "GdipDeletePath");
                GdipResetPath_ptr = FunctionWrapper.Load<GdipResetPath_delegate>(s_gdipModule, "GdipResetPath");
                GdipGetPointCount_ptr = FunctionWrapper.Load<GdipGetPointCount_delegate>(s_gdipModule, "GdipGetPointCount");
                GdipGetPathTypes_ptr = FunctionWrapper.Load<GdipGetPathTypes_delegate>(s_gdipModule, "GdipGetPathTypes");
                GdipGetPathPoints_ptr = FunctionWrapper.Load<GdipGetPathPoints_delegate>(s_gdipModule, "GdipGetPathPoints");
                GdipGetPathPointsI_ptr = FunctionWrapper.Load<GdipGetPathPointsI_delegate>(s_gdipModule, "GdipGetPathPointsI");
                GdipGetPathFillMode_ptr = FunctionWrapper.Load<GdipGetPathFillMode_delegate>(s_gdipModule, "GdipGetPathFillMode");
                GdipSetPathFillMode_ptr = FunctionWrapper.Load<GdipSetPathFillMode_delegate>(s_gdipModule, "GdipSetPathFillMode");
                GdipStartPathFigure_ptr = FunctionWrapper.Load<GdipStartPathFigure_delegate>(s_gdipModule, "GdipStartPathFigure");
                GdipClosePathFigure_ptr = FunctionWrapper.Load<GdipClosePathFigure_delegate>(s_gdipModule, "GdipClosePathFigure");
                GdipClosePathFigures_ptr = FunctionWrapper.Load<GdipClosePathFigures_delegate>(s_gdipModule, "GdipClosePathFigures");
                GdipSetPathMarker_ptr = FunctionWrapper.Load<GdipSetPathMarker_delegate>(s_gdipModule, "GdipSetPathMarker");
                GdipClearPathMarkers_ptr = FunctionWrapper.Load<GdipClearPathMarkers_delegate>(s_gdipModule, "GdipClearPathMarkers");
                GdipReversePath_ptr = FunctionWrapper.Load<GdipReversePath_delegate>(s_gdipModule, "GdipReversePath");
                GdipGetPathLastPoint_ptr = FunctionWrapper.Load<GdipGetPathLastPoint_delegate>(s_gdipModule, "GdipGetPathLastPoint");
                GdipAddPathLine_ptr = FunctionWrapper.Load<GdipAddPathLine_delegate>(s_gdipModule, "GdipAddPathLine");
                GdipAddPathLine2_ptr = FunctionWrapper.Load<GdipAddPathLine2_delegate>(s_gdipModule, "GdipAddPathLine2");
                GdipAddPathLine2I_ptr = FunctionWrapper.Load<GdipAddPathLine2I_delegate>(s_gdipModule, "GdipAddPathLine2I");
                GdipAddPathArc_ptr = FunctionWrapper.Load<GdipAddPathArc_delegate>(s_gdipModule, "GdipAddPathArc");
                GdipAddPathBezier_ptr = FunctionWrapper.Load<GdipAddPathBezier_delegate>(s_gdipModule, "GdipAddPathBezier");
                GdipAddPathBeziers_ptr = FunctionWrapper.Load<GdipAddPathBeziers_delegate>(s_gdipModule, "GdipAddPathBeziers");
                GdipAddPathCurve_ptr = FunctionWrapper.Load<GdipAddPathCurve_delegate>(s_gdipModule, "GdipAddPathCurve");
                GdipAddPathCurveI_ptr = FunctionWrapper.Load<GdipAddPathCurveI_delegate>(s_gdipModule, "GdipAddPathCurveI");
                GdipAddPathCurve2_ptr = FunctionWrapper.Load<GdipAddPathCurve2_delegate>(s_gdipModule, "GdipAddPathCurve2");
                GdipAddPathCurve2I_ptr = FunctionWrapper.Load<GdipAddPathCurve2I_delegate>(s_gdipModule, "GdipAddPathCurve2I");
                GdipAddPathCurve3_ptr = FunctionWrapper.Load<GdipAddPathCurve3_delegate>(s_gdipModule, "GdipAddPathCurve3");
                GdipAddPathCurve3I_ptr = FunctionWrapper.Load<GdipAddPathCurve3I_delegate>(s_gdipModule, "GdipAddPathCurve3I");
                GdipAddPathClosedCurve_ptr = FunctionWrapper.Load<GdipAddPathClosedCurve_delegate>(s_gdipModule, "GdipAddPathClosedCurve");
                GdipAddPathClosedCurveI_ptr = FunctionWrapper.Load<GdipAddPathClosedCurveI_delegate>(s_gdipModule, "GdipAddPathClosedCurveI");
                GdipAddPathClosedCurve2_ptr = FunctionWrapper.Load<GdipAddPathClosedCurve2_delegate>(s_gdipModule, "GdipAddPathClosedCurve2");
                GdipAddPathClosedCurve2I_ptr = FunctionWrapper.Load<GdipAddPathClosedCurve2I_delegate>(s_gdipModule, "GdipAddPathClosedCurve2I");
                GdipAddPathRectangle_ptr = FunctionWrapper.Load<GdipAddPathRectangle_delegate>(s_gdipModule, "GdipAddPathRectangle");
                GdipAddPathRectangles_ptr = FunctionWrapper.Load<GdipAddPathRectangles_delegate>(s_gdipModule, "GdipAddPathRectangles");
                GdipAddPathEllipse_ptr = FunctionWrapper.Load<GdipAddPathEllipse_delegate>(s_gdipModule, "GdipAddPathEllipse");
                GdipAddPathEllipseI_ptr = FunctionWrapper.Load<GdipAddPathEllipseI_delegate>(s_gdipModule, "GdipAddPathEllipseI");
                GdipAddPathPie_ptr = FunctionWrapper.Load<GdipAddPathPie_delegate>(s_gdipModule, "GdipAddPathPie");
                GdipAddPathPieI_ptr = FunctionWrapper.Load<GdipAddPathPieI_delegate>(s_gdipModule, "GdipAddPathPieI");
                GdipAddPathPolygon_ptr = FunctionWrapper.Load<GdipAddPathPolygon_delegate>(s_gdipModule, "GdipAddPathPolygon");
                GdipAddPathPath_ptr = FunctionWrapper.Load<GdipAddPathPath_delegate>(s_gdipModule, "GdipAddPathPath");
                GdipAddPathLineI_ptr = FunctionWrapper.Load<GdipAddPathLineI_delegate>(s_gdipModule, "GdipAddPathLineI");
                GdipAddPathArcI_ptr = FunctionWrapper.Load<GdipAddPathArcI_delegate>(s_gdipModule, "GdipAddPathArcI");
                GdipAddPathBezierI_ptr = FunctionWrapper.Load<GdipAddPathBezierI_delegate>(s_gdipModule, "GdipAddPathBezierI");
                GdipAddPathBeziersI_ptr = FunctionWrapper.Load<GdipAddPathBeziersI_delegate>(s_gdipModule, "GdipAddPathBeziersI");
                GdipAddPathPolygonI_ptr = FunctionWrapper.Load<GdipAddPathPolygonI_delegate>(s_gdipModule, "GdipAddPathPolygonI");
                GdipAddPathRectangleI_ptr = FunctionWrapper.Load<GdipAddPathRectangleI_delegate>(s_gdipModule, "GdipAddPathRectangleI");
                GdipAddPathRectanglesI_ptr = FunctionWrapper.Load<GdipAddPathRectanglesI_delegate>(s_gdipModule, "GdipAddPathRectanglesI");
                GdipFlattenPath_ptr = FunctionWrapper.Load<GdipFlattenPath_delegate>(s_gdipModule, "GdipFlattenPath");
                GdipTransformPath_ptr = FunctionWrapper.Load<GdipTransformPath_delegate>(s_gdipModule, "GdipTransformPath");
                GdipWarpPath_ptr = FunctionWrapper.Load<GdipWarpPath_delegate>(s_gdipModule, "GdipWarpPath");
                GdipWidenPath_ptr = FunctionWrapper.Load<GdipWidenPath_delegate>(s_gdipModule, "GdipWidenPath");
                GdipGetPathWorldBounds_ptr = FunctionWrapper.Load<GdipGetPathWorldBounds_delegate>(s_gdipModule, "GdipGetPathWorldBounds");
                GdipGetPathWorldBoundsI_ptr = FunctionWrapper.Load<GdipGetPathWorldBoundsI_delegate>(s_gdipModule, "GdipGetPathWorldBoundsI");
                GdipIsVisiblePathPoint_ptr = FunctionWrapper.Load<GdipIsVisiblePathPoint_delegate>(s_gdipModule, "GdipIsVisiblePathPoint");
                GdipIsVisiblePathPointI_ptr = FunctionWrapper.Load<GdipIsVisiblePathPointI_delegate>(s_gdipModule, "GdipIsVisiblePathPointI");
                GdipIsOutlineVisiblePathPoint_ptr = FunctionWrapper.Load<GdipIsOutlineVisiblePathPoint_delegate>(s_gdipModule, "GdipIsOutlineVisiblePathPoint");
                GdipIsOutlineVisiblePathPointI_ptr = FunctionWrapper.Load<GdipIsOutlineVisiblePathPointI_delegate>(s_gdipModule, "GdipIsOutlineVisiblePathPointI");
                GdipCreateFont_ptr = FunctionWrapper.Load<GdipCreateFont_delegate>(s_gdipModule, "GdipCreateFont");
                GdipDeleteFont_ptr = FunctionWrapper.Load<GdipDeleteFont_delegate>(s_gdipModule, "GdipDeleteFont");
                GdipGetLogFont_ptr = FunctionWrapper.Load<GdipGetLogFont_delegate>(s_gdipModule, "GdipGetLogFontW");
                GdipCreateFontFromDC_ptr = FunctionWrapper.Load<GdipCreateFontFromDC_delegate>(s_gdipModule, "GdipCreateFontFromDC");
                GdipCreateFontFromLogfont_ptr = FunctionWrapper.Load<GdipCreateFontFromLogfont_delegate>(s_gdipModule, "GdipCreateFontFromLogfontW");
                GdipCreateFontFromHfont_ptr = FunctionWrapper.Load<GdipCreateFontFromHfont_delegate>(s_gdipModule, "GdipCreateFontFromHfontA");
                GdipGetFontSize_ptr = FunctionWrapper.Load<GdipGetFontSize_delegate>(s_gdipModule, "GdipGetFontSize");
                GdipGetFontHeight_ptr = FunctionWrapper.Load<GdipGetFontHeight_delegate>(s_gdipModule, "GdipGetFontHeight");
                GdipGetFontHeightGivenDPI_ptr = FunctionWrapper.Load<GdipGetFontHeightGivenDPI_delegate>(s_gdipModule, "GdipGetFontHeightGivenDPI");
                GdipCreateMetafileFromFile_ptr = FunctionWrapper.Load<GdipCreateMetafileFromFile_delegate>(s_gdipModule, "GdipCreateMetafileFromFile");
                GdipCreateMetafileFromEmf_ptr = FunctionWrapper.Load<GdipCreateMetafileFromEmf_delegate>(s_gdipModule, "GdipCreateMetafileFromEmf");
                GdipCreateMetafileFromWmf_ptr = FunctionWrapper.Load<GdipCreateMetafileFromWmf_delegate>(s_gdipModule, "GdipCreateMetafileFromWmf");
                GdipGetMetafileHeaderFromFile_ptr = FunctionWrapper.Load<GdipGetMetafileHeaderFromFile_delegate>(s_gdipModule, "GdipGetMetafileHeaderFromFile");
                GdipGetMetafileHeaderFromMetafile_ptr = FunctionWrapper.Load<GdipGetMetafileHeaderFromMetafile_delegate>(s_gdipModule, "GdipGetMetafileHeaderFromMetafile");
                GdipGetMetafileHeaderFromEmf_ptr = FunctionWrapper.Load<GdipGetMetafileHeaderFromEmf_delegate>(s_gdipModule, "GdipGetMetafileHeaderFromEmf");
                GdipGetMetafileHeaderFromWmf_ptr = FunctionWrapper.Load<GdipGetMetafileHeaderFromWmf_delegate>(s_gdipModule, "GdipGetMetafileHeaderFromWmf");
                GdipGetHemfFromMetafile_ptr = FunctionWrapper.Load<GdipGetHemfFromMetafile_delegate>(s_gdipModule, "GdipGetHemfFromMetafile");
                GdipGetMetafileDownLevelRasterizationLimit_ptr = FunctionWrapper.Load<GdipGetMetafileDownLevelRasterizationLimit_delegate>(s_gdipModule, "GdipGetMetafileDownLevelRasterizationLimit");
                GdipSetMetafileDownLevelRasterizationLimit_ptr = FunctionWrapper.Load<GdipSetMetafileDownLevelRasterizationLimit_delegate>(s_gdipModule, "GdipSetMetafileDownLevelRasterizationLimit");
                GdipPlayMetafileRecord_ptr = FunctionWrapper.Load<GdipPlayMetafileRecord_delegate>(s_gdipModule, "GdipPlayMetafileRecord");
                GdipRecordMetafile_ptr = FunctionWrapper.Load<GdipRecordMetafile_delegate>(s_gdipModule, "GdipRecordMetafile");
                GdipRecordMetafileI_ptr = FunctionWrapper.Load<GdipRecordMetafileI_delegate>(s_gdipModule, "GdipRecordMetafileI");
                GdipRecordMetafileFileName_ptr = FunctionWrapper.Load<GdipRecordMetafileFileName_delegate>(s_gdipModule, "GdipRecordMetafileFileName");
                GdipRecordMetafileFileNameI_ptr = FunctionWrapper.Load<GdipRecordMetafileFileNameI_delegate>(s_gdipModule, "GdipRecordMetafileFileNameI");
                GdipCreateMetafileFromStream_ptr = FunctionWrapper.Load<GdipCreateMetafileFromStream_delegate>(s_gdipModule, "GdipCreateMetafileFromStream");
                GdipGetMetafileHeaderFromStream_ptr = FunctionWrapper.Load<GdipGetMetafileHeaderFromStream_delegate>(s_gdipModule, "GdipGetMetafileHeaderFromStream");
                GdipRecordMetafileStream_ptr = FunctionWrapper.Load<GdipRecordMetafileStream_delegate>(s_gdipModule, "GdipRecordMetafileStream");
                GdipRecordMetafileStreamI_ptr = FunctionWrapper.Load<GdipRecordMetafileStreamI_delegate>(s_gdipModule, "GdipRecordMetafileStreamI");
                GdipCreateFromContext_macosx_ptr = FunctionWrapper.Load<GdipCreateFromContext_macosx_delegate>(s_gdipModule, "GdipCreateFromContext_macosx");
                GdipSetVisibleClip_linux_ptr = FunctionWrapper.Load<GdipSetVisibleClip_linux_delegate>(s_gdipModule, "GdipSetVisibleClip_linux");
                GdipCreateFromXDrawable_linux_ptr = FunctionWrapper.Load<GdipCreateFromXDrawable_linux_delegate>(s_gdipModule, "GdipCreateFromXDrawable_linux");
                GdipLoadImageFromDelegate_linux_ptr = FunctionWrapper.Load<GdipLoadImageFromDelegate_linux_delegate>(s_gdipModule, "GdipLoadImageFromDelegate_linux");
                GdipSaveImageToDelegate_linux_ptr = FunctionWrapper.Load<GdipSaveImageToDelegate_linux_delegate>(s_gdipModule, "GdipSaveImageToDelegate_linux");
                GdipCreateMetafileFromDelegate_linux_ptr = FunctionWrapper.Load<GdipCreateMetafileFromDelegate_linux_delegate>(s_gdipModule, "GdipCreateMetafileFromDelegate_linux");
                GdipGetMetafileHeaderFromDelegate_linux_ptr = FunctionWrapper.Load<GdipGetMetafileHeaderFromDelegate_linux_delegate>(s_gdipModule, "GdipGetMetafileHeaderFromDelegate_linux");
                GdipRecordMetafileFromDelegate_linux_ptr = FunctionWrapper.Load<GdipRecordMetafileFromDelegate_linux_delegate>(s_gdipModule, "GdipRecordMetafileFromDelegate_linux");
                GdipRecordMetafileFromDelegateI_linux_ptr = FunctionWrapper.Load<GdipRecordMetafileFromDelegateI_linux_delegate>(s_gdipModule, "GdipRecordMetafileFromDelegateI_linux");
                GdipGetPostScriptSavePage_ptr = FunctionWrapper.Load<GdipGetPostScriptSavePage_delegate>(s_gdipModule, "GdipGetPostScriptSavePage");
                GdipGetPostScriptGraphicsContext_ptr = FunctionWrapper.Load<GdipGetPostScriptGraphicsContext_delegate>(s_gdipModule, "GdipGetPostScriptGraphicsContext");
            }

            // Imported functions

            private delegate Status GdiplusStartup_delegate(out IntPtr token, ref StartupInput input, out StartupOutput output);
            private static FunctionWrapper<GdiplusStartup_delegate> GdiplusStartup_ptr;
            internal static int GdiplusStartup(out IntPtr token, ref StartupInput input, out StartupOutput output) => (int)GdiplusStartup_ptr.Delegate(out token, ref input, out output);

            private delegate void GdiplusShutdown_delegate(ref ulong token);
            private static FunctionWrapper<GdiplusShutdown_delegate> GdiplusShutdown_ptr;
            internal static void GdiplusShutdown(ref ulong token) => GdiplusShutdown_ptr.Delegate(ref token);

            private delegate IntPtr GdipAlloc_delegate(int size);
            private static FunctionWrapper<GdipAlloc_delegate> GdipAlloc_ptr;
            internal static IntPtr GdipAlloc(int size) => GdipAlloc_ptr.Delegate(size);

            private delegate void GdipFree_delegate(IntPtr ptr);
            private static FunctionWrapper<GdipFree_delegate> GdipFree_ptr;
            internal static void GdipFree(IntPtr ptr) => GdipFree_ptr.Delegate(ptr);

            private delegate Status GdipDeleteBrush_delegate(IntPtr brush);
            private static FunctionWrapper<GdipDeleteBrush_delegate> GdipDeleteBrush_ptr;
            internal static Status GdipDeleteBrush(IntPtr brush) => GdipDeleteBrush_ptr.Delegate(brush);
            internal static int IntGdipDeleteBrush(HandleRef brush) => (int)GdipDeleteBrush_ptr.Delegate(brush.Handle);

            private delegate Status GdipGetBrushType_delegate(IntPtr brush, out BrushType type);
            private static FunctionWrapper<GdipGetBrushType_delegate> GdipGetBrushType_ptr;
            internal static Status GdipGetBrushType(IntPtr brush, out BrushType type) => GdipGetBrushType_ptr.Delegate(brush, out type);

            private delegate Status GdipCreateFromHDC_delegate(IntPtr hDC, out IntPtr graphics);
            private static FunctionWrapper<GdipCreateFromHDC_delegate> GdipCreateFromHDC_ptr;
            internal static Status GdipCreateFromHDC(IntPtr hDC, out IntPtr graphics) => GdipCreateFromHDC_ptr.Delegate(hDC, out graphics);

            private delegate Status GdipDeleteGraphics_delegate(IntPtr graphics);
            private static FunctionWrapper<GdipDeleteGraphics_delegate> GdipDeleteGraphics_ptr;
            internal static Status GdipDeleteGraphics(IntPtr graphics) => GdipDeleteGraphics_ptr.Delegate(graphics);
            internal static int IntGdipDeleteGraphics(HandleRef graphics) => (int)GdipDeleteGraphics_ptr.Delegate(graphics.Handle);

            private delegate Status GdipRestoreGraphics_delegate(IntPtr graphics, uint graphicsState);
            private static FunctionWrapper<GdipRestoreGraphics_delegate> GdipRestoreGraphics_ptr;
            internal static Status GdipRestoreGraphics(IntPtr graphics, uint graphicsState) => GdipRestoreGraphics_ptr.Delegate(graphics, graphicsState);

            private delegate Status GdipSaveGraphics_delegate(IntPtr graphics, out uint state);
            private static FunctionWrapper<GdipSaveGraphics_delegate> GdipSaveGraphics_ptr;
            internal static Status GdipSaveGraphics(IntPtr graphics, out uint state) => GdipSaveGraphics_ptr.Delegate(graphics, out state);

            private delegate Status GdipDrawArc_delegate(IntPtr graphics, IntPtr pen, float x, float y, float width, float height, float startAngle, float sweepAngle);
            private static FunctionWrapper<GdipDrawArc_delegate> GdipDrawArc_ptr;
            internal static Status GdipDrawArc(IntPtr graphics, IntPtr pen, float x, float y, float width, float height, float startAngle, float sweepAngle) => GdipDrawArc_ptr.Delegate(graphics, pen, x, y, width, height, startAngle, sweepAngle);

            private delegate Status GdipDrawArcI_delegate(IntPtr graphics, IntPtr pen, int x, int y, int width, int height, float startAngle, float sweepAngle);
            private static FunctionWrapper<GdipDrawArcI_delegate> GdipDrawArcI_ptr;
            internal static Status GdipDrawArcI(IntPtr graphics, IntPtr pen, int x, int y, int width, int height, float startAngle, float sweepAngle) => GdipDrawArcI_ptr.Delegate(graphics, pen, x, y, width, height, startAngle, sweepAngle);

            private delegate Status GdipDrawBezier_delegate(IntPtr graphics, IntPtr pen, float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4);
            private static FunctionWrapper<GdipDrawBezier_delegate> GdipDrawBezier_ptr;
            internal static Status GdipDrawBezier(IntPtr graphics, IntPtr pen, float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4) => GdipDrawBezier_ptr.Delegate(graphics, pen, x1, y1, x2, y2, x3, y3, x4, y4);

            private delegate Status GdipDrawBezierI_delegate(IntPtr graphics, IntPtr pen, int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4);
            private static FunctionWrapper<GdipDrawBezierI_delegate> GdipDrawBezierI_ptr;
            internal static Status GdipDrawBezierI(IntPtr graphics, IntPtr pen, int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4) => GdipDrawBezierI_ptr.Delegate(graphics, pen, x1, y1, x2, y2, x3, y3, x4, y4);

            private delegate Status GdipDrawEllipseI_delegate(IntPtr graphics, IntPtr pen, int x, int y, int width, int height);
            private static FunctionWrapper<GdipDrawEllipseI_delegate> GdipDrawEllipseI_ptr;
            internal static Status GdipDrawEllipseI(IntPtr graphics, IntPtr pen, int x, int y, int width, int height) => GdipDrawEllipseI_ptr.Delegate(graphics, pen, x, y, width, height);

            private delegate Status GdipDrawEllipse_delegate(IntPtr graphics, IntPtr pen, float x, float y, float width, float height);
            private static FunctionWrapper<GdipDrawEllipse_delegate> GdipDrawEllipse_ptr;
            internal static Status GdipDrawEllipse(IntPtr graphics, IntPtr pen, float x, float y, float width, float height) => GdipDrawEllipse_ptr.Delegate(graphics, pen, x, y, width, height);

            private delegate Status GdipDrawLine_delegate(IntPtr graphics, IntPtr pen, float x1, float y1, float x2, float y2);
            private static FunctionWrapper<GdipDrawLine_delegate> GdipDrawLine_ptr;
            internal static Status GdipDrawLine(IntPtr graphics, IntPtr pen, float x1, float y1, float x2, float y2) => GdipDrawLine_ptr.Delegate(graphics, pen, x1, y1, x2, y2);

            private delegate Status GdipDrawLineI_delegate(IntPtr graphics, IntPtr pen, int x1, int y1, int x2, int y2);
            private static FunctionWrapper<GdipDrawLineI_delegate> GdipDrawLineI_ptr;
            internal static Status GdipDrawLineI(IntPtr graphics, IntPtr pen, int x1, int y1, int x2, int y2) => GdipDrawLineI_ptr.Delegate(graphics, pen, x1, y1, x2, y2);

            private delegate Status GdipDrawLines_delegate(IntPtr graphics, IntPtr pen, PointF[] points, int count);
            private static FunctionWrapper<GdipDrawLines_delegate> GdipDrawLines_ptr;
            internal static Status GdipDrawLines(IntPtr graphics, IntPtr pen, PointF[] points, int count) => GdipDrawLines_ptr.Delegate(graphics, pen, points, count);

            private delegate Status GdipDrawLinesI_delegate(IntPtr graphics, IntPtr pen, Point[] points, int count);
            private static FunctionWrapper<GdipDrawLinesI_delegate> GdipDrawLinesI_ptr;
            internal static Status GdipDrawLinesI(IntPtr graphics, IntPtr pen, Point[] points, int count) => GdipDrawLinesI_ptr.Delegate(graphics, pen, points, count);

            private delegate Status GdipDrawPath_delegate(IntPtr graphics, IntPtr pen, IntPtr path);
            private static FunctionWrapper<GdipDrawPath_delegate> GdipDrawPath_ptr;
            internal static Status GdipDrawPath(IntPtr graphics, IntPtr pen, IntPtr path) => GdipDrawPath_ptr.Delegate(graphics, pen, path);

            private delegate Status GdipDrawPie_delegate(IntPtr graphics, IntPtr pen, float x, float y, float width, float height, float startAngle, float sweepAngle);
            private static FunctionWrapper<GdipDrawPie_delegate> GdipDrawPie_ptr;
            internal static Status GdipDrawPie(IntPtr graphics, IntPtr pen, float x, float y, float width, float height, float startAngle, float sweepAngle) => GdipDrawPie_ptr.Delegate(graphics, pen, x, y, width, height, startAngle, sweepAngle);

            private delegate Status GdipDrawPieI_delegate(IntPtr graphics, IntPtr pen, int x, int y, int width, int height, float startAngle, float sweepAngle);
            private static FunctionWrapper<GdipDrawPieI_delegate> GdipDrawPieI_ptr;
            internal static Status GdipDrawPieI(IntPtr graphics, IntPtr pen, int x, int y, int width, int height, float startAngle, float sweepAngle) => GdipDrawPieI_ptr.Delegate(graphics, pen, x, y, width, height, startAngle, sweepAngle);

            private delegate Status GdipDrawPolygon_delegate(IntPtr graphics, IntPtr pen, PointF[] points, int count);
            private static FunctionWrapper<GdipDrawPolygon_delegate> GdipDrawPolygon_ptr;
            internal static Status GdipDrawPolygon(IntPtr graphics, IntPtr pen, PointF[] points, int count) => GdipDrawPolygon_ptr.Delegate(graphics, pen, points, count);

            private delegate Status GdipDrawPolygonI_delegate(IntPtr graphics, IntPtr pen, Point[] points, int count);
            private static FunctionWrapper<GdipDrawPolygonI_delegate> GdipDrawPolygonI_ptr;
            internal static Status GdipDrawPolygonI(IntPtr graphics, IntPtr pen, Point[] points, int count) => GdipDrawPolygonI_ptr.Delegate(graphics, pen, points, count);

            private delegate Status GdipDrawRectangle_delegate(IntPtr graphics, IntPtr pen, float x, float y, float width, float height);
            private static FunctionWrapper<GdipDrawRectangle_delegate> GdipDrawRectangle_ptr;
            internal static Status GdipDrawRectangle(IntPtr graphics, IntPtr pen, float x, float y, float width, float height) => GdipDrawRectangle_ptr.Delegate(graphics, pen, x, y, width, height);

            private delegate Status GdipDrawRectangleI_delegate(IntPtr graphics, IntPtr pen, int x, int y, int width, int height);
            private static FunctionWrapper<GdipDrawRectangleI_delegate> GdipDrawRectangleI_ptr;
            internal static Status GdipDrawRectangleI(IntPtr graphics, IntPtr pen, int x, int y, int width, int height) => GdipDrawRectangleI_ptr.Delegate(graphics, pen, x, y, width, height);

            private delegate Status GdipDrawRectangles_delegate(IntPtr graphics, IntPtr pen, RectangleF[] rects, int count);
            private static FunctionWrapper<GdipDrawRectangles_delegate> GdipDrawRectangles_ptr;
            internal static Status GdipDrawRectangles(IntPtr graphics, IntPtr pen, RectangleF[] rects, int count) => GdipDrawRectangles_ptr.Delegate(graphics, pen, rects, count);

            private delegate Status GdipDrawRectanglesI_delegate(IntPtr graphics, IntPtr pen, Rectangle[] rects, int count);
            private static FunctionWrapper<GdipDrawRectanglesI_delegate> GdipDrawRectanglesI_ptr;
            internal static Status GdipDrawRectanglesI(IntPtr graphics, IntPtr pen, Rectangle[] rects, int count) => GdipDrawRectanglesI_ptr.Delegate(graphics, pen, rects, count);

            private delegate Status GdipFillEllipseI_delegate(IntPtr graphics, IntPtr pen, int x, int y, int width, int height);
            private static FunctionWrapper<GdipFillEllipseI_delegate> GdipFillEllipseI_ptr;
            internal static Status GdipFillEllipseI(IntPtr graphics, IntPtr pen, int x, int y, int width, int height) => GdipFillEllipseI_ptr.Delegate(graphics, pen, x, y, width, height);

            private delegate Status GdipFillEllipse_delegate(IntPtr graphics, IntPtr pen, float x, float y, float width, float height);
            private static FunctionWrapper<GdipFillEllipse_delegate> GdipFillEllipse_ptr;
            internal static Status GdipFillEllipse(IntPtr graphics, IntPtr pen, float x, float y, float width, float height) => GdipFillEllipse_ptr.Delegate(graphics, pen, x, y, width, height);

            private delegate Status GdipFillPolygon_delegate(IntPtr graphics, IntPtr brush, PointF[] points, int count, FillMode fillMode);
            private static FunctionWrapper<GdipFillPolygon_delegate> GdipFillPolygon_ptr;
            internal static Status GdipFillPolygon(IntPtr graphics, IntPtr brush, PointF[] points, int count, FillMode fillMode) => GdipFillPolygon_ptr.Delegate(graphics, brush, points, count, fillMode);

            private delegate Status GdipFillPolygonI_delegate(IntPtr graphics, IntPtr brush, Point[] points, int count, FillMode fillMode);
            private static FunctionWrapper<GdipFillPolygonI_delegate> GdipFillPolygonI_ptr;
            internal static Status GdipFillPolygonI(IntPtr graphics, IntPtr brush, Point[] points, int count, FillMode fillMode) => GdipFillPolygonI_ptr.Delegate(graphics, brush, points, count, fillMode);

            private delegate Status GdipFillPolygon2_delegate(IntPtr graphics, IntPtr brush, PointF[] points, int count);
            private static FunctionWrapper<GdipFillPolygon2_delegate> GdipFillPolygon2_ptr;
            internal static Status GdipFillPolygon2(IntPtr graphics, IntPtr brush, PointF[] points, int count) => GdipFillPolygon2_ptr.Delegate(graphics, brush, points, count);

            private delegate Status GdipFillPolygon2I_delegate(IntPtr graphics, IntPtr brush, Point[] points, int count);
            private static FunctionWrapper<GdipFillPolygon2I_delegate> GdipFillPolygon2I_ptr;
            internal static Status GdipFillPolygon2I(IntPtr graphics, IntPtr brush, Point[] points, int count) => GdipFillPolygon2I_ptr.Delegate(graphics, brush, points, count);

            private delegate Status GdipFillRectangle_delegate(IntPtr graphics, IntPtr brush, float x1, float y1, float x2, float y2);
            private static FunctionWrapper<GdipFillRectangle_delegate> GdipFillRectangle_ptr;
            internal static Status GdipFillRectangle(IntPtr graphics, IntPtr brush, float x1, float y1, float x2, float y2) => GdipFillRectangle_ptr.Delegate(graphics, brush, x1, y1, x2, y2);

            private delegate Status GdipFillRectangleI_delegate(IntPtr graphics, IntPtr brush, int x1, int y1, int x2, int y2);
            private static FunctionWrapper<GdipFillRectangleI_delegate> GdipFillRectangleI_ptr;
            internal static Status GdipFillRectangleI(IntPtr graphics, IntPtr brush, int x1, int y1, int x2, int y2) => GdipFillRectangleI_ptr.Delegate(graphics, brush, x1, y1, x2, y2);

            private delegate Status GdipFillRectangles_delegate(IntPtr graphics, IntPtr brush, RectangleF[] rects, int count);
            private static FunctionWrapper<GdipFillRectangles_delegate> GdipFillRectangles_ptr;
            internal static Status GdipFillRectangles(IntPtr graphics, IntPtr brush, RectangleF[] rects, int count) => GdipFillRectangles_ptr.Delegate(graphics, brush, rects, count);

            private delegate Status GdipFillRectanglesI_delegate(IntPtr graphics, IntPtr brush, Rectangle[] rects, int count);
            private static FunctionWrapper<GdipFillRectanglesI_delegate> GdipFillRectanglesI_ptr;
            internal static Status GdipFillRectanglesI(IntPtr graphics, IntPtr brush, Rectangle[] rects, int count) => GdipFillRectanglesI_ptr.Delegate(graphics, brush, rects, count);

            private delegate Status GdipDrawString_delegate(IntPtr graphics, [MarshalAs(UnmanagedType.LPWStr)]string text, int len, IntPtr font, ref RectangleF rc, IntPtr format, IntPtr brush);
            private static FunctionWrapper<GdipDrawString_delegate> GdipDrawString_ptr;
            internal static Status GdipDrawString(IntPtr graphics, string text, int len, IntPtr font, ref RectangleF rc, IntPtr format, IntPtr brush) => GdipDrawString_ptr.Delegate(graphics, text, len, font, ref rc, format, brush);

            private delegate Status GdipGetDC_delegate(IntPtr graphics, out IntPtr hdc);
            private static FunctionWrapper<GdipGetDC_delegate> GdipGetDC_ptr;
            internal static Status GdipGetDC(IntPtr graphics, out IntPtr hdc) => GdipGetDC_ptr.Delegate(graphics, out hdc);

            private delegate Status GdipReleaseDC_delegate(IntPtr graphics, IntPtr hdc);
            private static FunctionWrapper<GdipReleaseDC_delegate> GdipReleaseDC_ptr;
            internal static Status GdipReleaseDC(IntPtr graphics, IntPtr hdc) => GdipReleaseDC_ptr.Delegate(graphics, hdc);
            internal static int IntGdipReleaseDC(HandleRef graphics, HandleRef hdc) => (int)GdipReleaseDC_ptr.Delegate(graphics.Handle, hdc.Handle);

            private delegate Status GdipDrawImageRectI_delegate(IntPtr graphics, IntPtr image, int x, int y, int width, int height);
            private static FunctionWrapper<GdipDrawImageRectI_delegate> GdipDrawImageRectI_ptr;
            internal static Status GdipDrawImageRectI(IntPtr graphics, IntPtr image, int x, int y, int width, int height) => GdipDrawImageRectI_ptr.Delegate(graphics, image, x, y, width, height);

            private delegate Status GdipGetRenderingOrigin_delegate(IntPtr graphics, out int x, out int y);
            private static FunctionWrapper<GdipGetRenderingOrigin_delegate> GdipGetRenderingOrigin_ptr;
            internal static Status GdipGetRenderingOrigin(IntPtr graphics, out int x, out int y) => GdipGetRenderingOrigin_ptr.Delegate(graphics, out x, out y);

            private delegate Status GdipSetRenderingOrigin_delegate(IntPtr graphics, int x, int y);
            private static FunctionWrapper<GdipSetRenderingOrigin_delegate> GdipSetRenderingOrigin_ptr;
            internal static Status GdipSetRenderingOrigin(IntPtr graphics, int x, int y) => GdipSetRenderingOrigin_ptr.Delegate(graphics, x, y);

            private delegate Status GdipCloneBitmapArea_delegate(float x, float y, float width, float height, PixelFormat format, IntPtr original, out IntPtr bitmap);
            private static FunctionWrapper<GdipCloneBitmapArea_delegate> GdipCloneBitmapArea_ptr;
            internal static Status GdipCloneBitmapArea(float x, float y, float width, float height, PixelFormat format, IntPtr original, out IntPtr bitmap) => GdipCloneBitmapArea_ptr.Delegate(x, y, width, height, format, original, out bitmap);

            private delegate Status GdipCloneBitmapAreaI_delegate(int x, int y, int width, int height, PixelFormat format, IntPtr original, out IntPtr bitmap);
            private static FunctionWrapper<GdipCloneBitmapAreaI_delegate> GdipCloneBitmapAreaI_ptr;
            internal static Status GdipCloneBitmapAreaI(int x, int y, int width, int height, PixelFormat format, IntPtr original, out IntPtr bitmap) => GdipCloneBitmapAreaI_ptr.Delegate(x, y, width, height, format, original, out bitmap);

            private delegate Status GdipGraphicsClear_delegate(IntPtr graphics, int argb);
            private static FunctionWrapper<GdipGraphicsClear_delegate> GdipGraphicsClear_ptr;
            internal static Status GdipGraphicsClear(IntPtr graphics, int argb) => GdipGraphicsClear_ptr.Delegate(graphics, argb);

            private delegate Status GdipDrawClosedCurve_delegate(IntPtr graphics, IntPtr pen, PointF[] points, int count);
            private static FunctionWrapper<GdipDrawClosedCurve_delegate> GdipDrawClosedCurve_ptr;
            internal static Status GdipDrawClosedCurve(IntPtr graphics, IntPtr pen, PointF[] points, int count) => GdipDrawClosedCurve_ptr.Delegate(graphics, pen, points, count);

            private delegate Status GdipDrawClosedCurveI_delegate(IntPtr graphics, IntPtr pen, Point[] points, int count);
            private static FunctionWrapper<GdipDrawClosedCurveI_delegate> GdipDrawClosedCurveI_ptr;
            internal static Status GdipDrawClosedCurveI(IntPtr graphics, IntPtr pen, Point[] points, int count) => GdipDrawClosedCurveI_ptr.Delegate(graphics, pen, points, count);

            private delegate Status GdipDrawClosedCurve2_delegate(IntPtr graphics, IntPtr pen, PointF[] points, int count, float tension);
            private static FunctionWrapper<GdipDrawClosedCurve2_delegate> GdipDrawClosedCurve2_ptr;
            internal static Status GdipDrawClosedCurve2(IntPtr graphics, IntPtr pen, PointF[] points, int count, float tension) => GdipDrawClosedCurve2_ptr.Delegate(graphics, pen, points, count, tension);

            private delegate Status GdipDrawClosedCurve2I_delegate(IntPtr graphics, IntPtr pen, Point[] points, int count, float tension);
            private static FunctionWrapper<GdipDrawClosedCurve2I_delegate> GdipDrawClosedCurve2I_ptr;
            internal static Status GdipDrawClosedCurve2I(IntPtr graphics, IntPtr pen, Point[] points, int count, float tension) => GdipDrawClosedCurve2I_ptr.Delegate(graphics, pen, points, count, tension);

            private delegate Status GdipDrawCurve_delegate(IntPtr graphics, IntPtr pen, PointF[] points, int count);
            private static FunctionWrapper<GdipDrawCurve_delegate> GdipDrawCurve_ptr;
            internal static Status GdipDrawCurve(IntPtr graphics, IntPtr pen, PointF[] points, int count) => GdipDrawCurve_ptr.Delegate(graphics, pen, points, count);

            private delegate Status GdipDrawCurveI_delegate(IntPtr graphics, IntPtr pen, Point[] points, int count);
            private static FunctionWrapper<GdipDrawCurveI_delegate> GdipDrawCurveI_ptr;
            internal static Status GdipDrawCurveI(IntPtr graphics, IntPtr pen, Point[] points, int count) => GdipDrawCurveI_ptr.Delegate(graphics, pen, points, count);

            private delegate Status GdipDrawCurve2_delegate(IntPtr graphics, IntPtr pen, PointF[] points, int count, float tension);
            private static FunctionWrapper<GdipDrawCurve2_delegate> GdipDrawCurve2_ptr;
            internal static Status GdipDrawCurve2(IntPtr graphics, IntPtr pen, PointF[] points, int count, float tension) => GdipDrawCurve2_ptr.Delegate(graphics, pen, points, count, tension);

            private delegate Status GdipDrawCurve2I_delegate(IntPtr graphics, IntPtr pen, Point[] points, int count, float tension);
            private static FunctionWrapper<GdipDrawCurve2I_delegate> GdipDrawCurve2I_ptr;
            internal static Status GdipDrawCurve2I(IntPtr graphics, IntPtr pen, Point[] points, int count, float tension) => GdipDrawCurve2I_ptr.Delegate(graphics, pen, points, count, tension);

            private delegate Status GdipDrawCurve3_delegate(IntPtr graphics, IntPtr pen, PointF[] points, int count, int offset, int numberOfSegments, float tension);
            private static FunctionWrapper<GdipDrawCurve3_delegate> GdipDrawCurve3_ptr;
            internal static Status GdipDrawCurve3(IntPtr graphics, IntPtr pen, PointF[] points, int count, int offset, int numberOfSegments, float tension) => GdipDrawCurve3_ptr.Delegate(graphics, pen, points, count, offset, numberOfSegments, tension);

            private delegate Status GdipDrawCurve3I_delegate(IntPtr graphics, IntPtr pen, Point[] points, int count, int offset, int numberOfSegments, float tension);
            private static FunctionWrapper<GdipDrawCurve3I_delegate> GdipDrawCurve3I_ptr;
            internal static Status GdipDrawCurve3I(IntPtr graphics, IntPtr pen, Point[] points, int count, int offset, int numberOfSegments, float tension) => GdipDrawCurve3I_ptr.Delegate(graphics, pen, points, count, offset, numberOfSegments, tension);

            private delegate Status GdipFillClosedCurve_delegate(IntPtr graphics, IntPtr brush, PointF[] points, int count);
            private static FunctionWrapper<GdipFillClosedCurve_delegate> GdipFillClosedCurve_ptr;
            internal static Status GdipFillClosedCurve(IntPtr graphics, IntPtr brush, PointF[] points, int count) => GdipFillClosedCurve_ptr.Delegate(graphics, brush, points, count);

            private delegate Status GdipFillClosedCurveI_delegate(IntPtr graphics, IntPtr brush, Point[] points, int count);
            private static FunctionWrapper<GdipFillClosedCurveI_delegate> GdipFillClosedCurveI_ptr;
            internal static Status GdipFillClosedCurveI(IntPtr graphics, IntPtr brush, Point[] points, int count) => GdipFillClosedCurveI_ptr.Delegate(graphics, brush, points, count);

            private delegate Status GdipFillClosedCurve2_delegate(IntPtr graphics, IntPtr brush, PointF[] points, int count, float tension, FillMode fillMode);
            private static FunctionWrapper<GdipFillClosedCurve2_delegate> GdipFillClosedCurve2_ptr;
            internal static Status GdipFillClosedCurve2(IntPtr graphics, IntPtr brush, PointF[] points, int count, float tension, FillMode fillMode) => GdipFillClosedCurve2_ptr.Delegate(graphics, brush, points, count, tension, fillMode);

            private delegate Status GdipFillClosedCurve2I_delegate(IntPtr graphics, IntPtr brush, Point[] points, int count, float tension, FillMode fillMode);
            private static FunctionWrapper<GdipFillClosedCurve2I_delegate> GdipFillClosedCurve2I_ptr;
            internal static Status GdipFillClosedCurve2I(IntPtr graphics, IntPtr brush, Point[] points, int count, float tension, FillMode fillMode) => GdipFillClosedCurve2I_ptr.Delegate(graphics, brush, points, count, tension, fillMode);

            private delegate Status GdipFillPie_delegate(IntPtr graphics, IntPtr brush, float x, float y, float width, float height, float startAngle, float sweepAngle);
            private static FunctionWrapper<GdipFillPie_delegate> GdipFillPie_ptr;
            internal static Status GdipFillPie(IntPtr graphics, IntPtr brush, float x, float y, float width, float height, float startAngle, float sweepAngle) => GdipFillPie_ptr.Delegate(graphics, brush, x, y, width, height, startAngle, sweepAngle);

            private delegate Status GdipFillPieI_delegate(IntPtr graphics, IntPtr brush, int x, int y, int width, int height, float startAngle, float sweepAngle);
            private static FunctionWrapper<GdipFillPieI_delegate> GdipFillPieI_ptr;
            internal static Status GdipFillPieI(IntPtr graphics, IntPtr brush, int x, int y, int width, int height, float startAngle, float sweepAngle) => GdipFillPieI_ptr.Delegate(graphics, brush, x, y, width, height, startAngle, sweepAngle);

            private delegate Status GdipFillPath_delegate(IntPtr graphics, IntPtr brush, IntPtr path);
            private static FunctionWrapper<GdipFillPath_delegate> GdipFillPath_ptr;
            internal static Status GdipFillPath(IntPtr graphics, IntPtr brush, IntPtr path) => GdipFillPath_ptr.Delegate(graphics, brush, path);

            private delegate Status GdipGetNearestColor_delegate(IntPtr graphics, out int argb);
            private static FunctionWrapper<GdipGetNearestColor_delegate> GdipGetNearestColor_ptr;
            internal static Status GdipGetNearestColor(IntPtr graphics, out int argb) => GdipGetNearestColor_ptr.Delegate(graphics, out argb);

            private delegate Status GdipTransformPoints_delegate(IntPtr graphics, CoordinateSpace destSpace, CoordinateSpace srcSpace, IntPtr points, int count);
            private static FunctionWrapper<GdipTransformPoints_delegate> GdipTransformPoints_ptr;
            internal static Status GdipTransformPoints(IntPtr graphics, CoordinateSpace destSpace, CoordinateSpace srcSpace, IntPtr points, int count) => GdipTransformPoints_ptr.Delegate(graphics, destSpace, srcSpace, points, count);

            private delegate Status GdipTransformPointsI_delegate(IntPtr graphics, CoordinateSpace destSpace, CoordinateSpace srcSpace, IntPtr points, int count);
            private static FunctionWrapper<GdipTransformPointsI_delegate> GdipTransformPointsI_ptr;
            internal static Status GdipTransformPointsI(IntPtr graphics, CoordinateSpace destSpace, CoordinateSpace srcSpace, IntPtr points, int count) => GdipTransformPointsI_ptr.Delegate(graphics, destSpace, srcSpace, points, count);

            private delegate Status GdipSetCompositingMode_delegate(IntPtr graphics, CompositingMode compositingMode);
            private static FunctionWrapper<GdipSetCompositingMode_delegate> GdipSetCompositingMode_ptr;
            internal static Status GdipSetCompositingMode(IntPtr graphics, CompositingMode compositingMode) => GdipSetCompositingMode_ptr.Delegate(graphics, compositingMode);

            private delegate Status GdipGetCompositingMode_delegate(IntPtr graphics, out CompositingMode compositingMode);
            private static FunctionWrapper<GdipGetCompositingMode_delegate> GdipGetCompositingMode_ptr;
            internal static Status GdipGetCompositingMode(IntPtr graphics, out CompositingMode compositingMode) => GdipGetCompositingMode_ptr.Delegate(graphics, out compositingMode);

            private delegate Status GdipSetCompositingQuality_delegate(IntPtr graphics, CompositingQuality compositingQuality);
            private static FunctionWrapper<GdipSetCompositingQuality_delegate> GdipSetCompositingQuality_ptr;
            internal static Status GdipSetCompositingQuality(IntPtr graphics, CompositingQuality compositingQuality) => GdipSetCompositingQuality_ptr.Delegate(graphics, compositingQuality);

            private delegate Status GdipGetCompositingQuality_delegate(IntPtr graphics, out CompositingQuality compositingQuality);
            private static FunctionWrapper<GdipGetCompositingQuality_delegate> GdipGetCompositingQuality_ptr;
            internal static Status GdipGetCompositingQuality(IntPtr graphics, out CompositingQuality compositingQuality) => GdipGetCompositingQuality_ptr.Delegate(graphics, out compositingQuality);

            private delegate Status GdipSetInterpolationMode_delegate(IntPtr graphics, InterpolationMode interpolationMode);
            private static FunctionWrapper<GdipSetInterpolationMode_delegate> GdipSetInterpolationMode_ptr;
            internal static Status GdipSetInterpolationMode(IntPtr graphics, InterpolationMode interpolationMode) => GdipSetInterpolationMode_ptr.Delegate(graphics, interpolationMode);

            private delegate Status GdipGetInterpolationMode_delegate(IntPtr graphics, out InterpolationMode interpolationMode);
            private static FunctionWrapper<GdipGetInterpolationMode_delegate> GdipGetInterpolationMode_ptr;
            internal static Status GdipGetInterpolationMode(IntPtr graphics, out InterpolationMode interpolationMode) => GdipGetInterpolationMode_ptr.Delegate(graphics, out interpolationMode);

            private delegate Status GdipGetDpiX_delegate(IntPtr graphics, out float dpi);
            private static FunctionWrapper<GdipGetDpiX_delegate> GdipGetDpiX_ptr;
            internal static Status GdipGetDpiX(IntPtr graphics, out float dpi) => GdipGetDpiX_ptr.Delegate(graphics, out dpi);

            private delegate Status GdipGetDpiY_delegate(IntPtr graphics, out float dpi);
            private static FunctionWrapper<GdipGetDpiY_delegate> GdipGetDpiY_ptr;
            internal static Status GdipGetDpiY(IntPtr graphics, out float dpi) => GdipGetDpiY_ptr.Delegate(graphics, out dpi);

            private delegate Status GdipGetPageUnit_delegate(IntPtr graphics, out GraphicsUnit unit);
            private static FunctionWrapper<GdipGetPageUnit_delegate> GdipGetPageUnit_ptr;
            internal static Status GdipGetPageUnit(IntPtr graphics, out GraphicsUnit unit) => GdipGetPageUnit_ptr.Delegate(graphics, out unit);

            private delegate Status GdipGetPageScale_delegate(IntPtr graphics, out float scale);
            private static FunctionWrapper<GdipGetPageScale_delegate> GdipGetPageScale_ptr;
            internal static Status GdipGetPageScale(IntPtr graphics, out float scale) => GdipGetPageScale_ptr.Delegate(graphics, out scale);

            private delegate Status GdipSetPageUnit_delegate(IntPtr graphics, GraphicsUnit unit);
            private static FunctionWrapper<GdipSetPageUnit_delegate> GdipSetPageUnit_ptr;
            internal static Status GdipSetPageUnit(IntPtr graphics, GraphicsUnit unit) => GdipSetPageUnit_ptr.Delegate(graphics, unit);

            private delegate Status GdipSetPageScale_delegate(IntPtr graphics, float scale);
            private static FunctionWrapper<GdipSetPageScale_delegate> GdipSetPageScale_ptr;
            internal static Status GdipSetPageScale(IntPtr graphics, float scale) => GdipSetPageScale_ptr.Delegate(graphics, scale);

            private delegate Status GdipSetPixelOffsetMode_delegate(IntPtr graphics, PixelOffsetMode pixelOffsetMode);
            private static FunctionWrapper<GdipSetPixelOffsetMode_delegate> GdipSetPixelOffsetMode_ptr;
            internal static Status GdipSetPixelOffsetMode(IntPtr graphics, PixelOffsetMode pixelOffsetMode) => GdipSetPixelOffsetMode_ptr.Delegate(graphics, pixelOffsetMode);

            private delegate Status GdipGetPixelOffsetMode_delegate(IntPtr graphics, out PixelOffsetMode pixelOffsetMode);
            private static FunctionWrapper<GdipGetPixelOffsetMode_delegate> GdipGetPixelOffsetMode_ptr;
            internal static Status GdipGetPixelOffsetMode(IntPtr graphics, out PixelOffsetMode pixelOffsetMode) => GdipGetPixelOffsetMode_ptr.Delegate(graphics, out pixelOffsetMode);

            private delegate Status GdipSetSmoothingMode_delegate(IntPtr graphics, SmoothingMode smoothingMode);
            private static FunctionWrapper<GdipSetSmoothingMode_delegate> GdipSetSmoothingMode_ptr;
            internal static Status GdipSetSmoothingMode(IntPtr graphics, SmoothingMode smoothingMode) => GdipSetSmoothingMode_ptr.Delegate(graphics, smoothingMode);

            private delegate Status GdipGetSmoothingMode_delegate(IntPtr graphics, out SmoothingMode smoothingMode);
            private static FunctionWrapper<GdipGetSmoothingMode_delegate> GdipGetSmoothingMode_ptr;
            internal static Status GdipGetSmoothingMode(IntPtr graphics, out SmoothingMode smoothingMode) => GdipGetSmoothingMode_ptr.Delegate(graphics, out smoothingMode);

            private delegate Status GdipSetTextContrast_delegate(IntPtr graphics, int contrast);
            private static FunctionWrapper<GdipSetTextContrast_delegate> GdipSetTextContrast_ptr;
            internal static Status GdipSetTextContrast(IntPtr graphics, int contrast) => GdipSetTextContrast_ptr.Delegate(graphics, contrast);

            private delegate Status GdipGetTextContrast_delegate(IntPtr graphics, out int contrast);
            private static FunctionWrapper<GdipGetTextContrast_delegate> GdipGetTextContrast_ptr;
            internal static Status GdipGetTextContrast(IntPtr graphics, out int contrast) => GdipGetTextContrast_ptr.Delegate(graphics, out contrast);

            private delegate Status GdipSetTextRenderingHint_delegate(IntPtr graphics, TextRenderingHint mode);
            private static FunctionWrapper<GdipSetTextRenderingHint_delegate> GdipSetTextRenderingHint_ptr;
            internal static Status GdipSetTextRenderingHint(IntPtr graphics, TextRenderingHint mode) => GdipSetTextRenderingHint_ptr.Delegate(graphics, mode);

            private delegate Status GdipGetTextRenderingHint_delegate(IntPtr graphics, out TextRenderingHint mode);
            private static FunctionWrapper<GdipGetTextRenderingHint_delegate> GdipGetTextRenderingHint_ptr;
            internal static Status GdipGetTextRenderingHint(IntPtr graphics, out TextRenderingHint mode) => GdipGetTextRenderingHint_ptr.Delegate(graphics, out mode);

            private delegate Status GdipFlush_delegate(IntPtr graphics, FlushIntention intention);
            private static FunctionWrapper<GdipFlush_delegate> GdipFlush_ptr;
            internal static Status GdipFlush(IntPtr graphics, FlushIntention intention) => GdipFlush_ptr.Delegate(graphics, intention);

            private delegate Status GdipAddPathString_delegate(IntPtr path, [MarshalAs(UnmanagedType.LPWStr)]string s, int lenght, IntPtr family, int style, float emSize, ref RectangleF layoutRect, IntPtr format);
            private static FunctionWrapper<GdipAddPathString_delegate> GdipAddPathString_ptr;
            internal static Status GdipAddPathString(IntPtr path, string s, int lenght, IntPtr family, int style, float emSize, ref RectangleF layoutRect, IntPtr format) => GdipAddPathString_ptr.Delegate(path, s, lenght, family, style, emSize, ref layoutRect, format);

            private delegate Status GdipAddPathStringI_delegate(IntPtr path, [MarshalAs(UnmanagedType.LPWStr)]string s, int lenght, IntPtr family, int style, float emSize, ref Rectangle layoutRect, IntPtr format);
            private static FunctionWrapper<GdipAddPathStringI_delegate> GdipAddPathStringI_ptr;
            internal static Status GdipAddPathStringI(IntPtr path, string s, int lenght, IntPtr family, int style, float emSize, ref Rectangle layoutRect, IntPtr format) => GdipAddPathStringI_ptr.Delegate(path, s, lenght, family, style, emSize, ref layoutRect, format);

            private delegate Status GdipCreateFromHWND_delegate(IntPtr hwnd, out IntPtr graphics);
            private static FunctionWrapper<GdipCreateFromHWND_delegate> GdipCreateFromHWND_ptr;
            internal static Status GdipCreateFromHWND(IntPtr hwnd, out IntPtr graphics) => GdipCreateFromHWND_ptr.Delegate(hwnd, out graphics);

            private delegate Status GdipMeasureString_delegate(IntPtr graphics, [MarshalAs(UnmanagedType.LPWStr)]string str, int length, IntPtr font, ref RectangleF layoutRect, IntPtr stringFormat, out RectangleF boundingBox, int* codepointsFitted, int* linesFilled);
            private static FunctionWrapper<GdipMeasureString_delegate> GdipMeasureString_ptr;
            internal static Status GdipMeasureString(IntPtr graphics, string str, int length, IntPtr font, ref RectangleF layoutRect, IntPtr stringFormat, out RectangleF boundingBox, int* codepointsFitted, int* linesFilled) => GdipMeasureString_ptr.Delegate(graphics, str, length, font, ref layoutRect, stringFormat, out boundingBox, codepointsFitted, linesFilled);

            private delegate Status GdipMeasureCharacterRanges_delegate(IntPtr graphics, [MarshalAs(UnmanagedType.LPWStr)]string str, int length, IntPtr font, ref RectangleF layoutRect, IntPtr stringFormat, int regcount, out IntPtr regions);
            private static FunctionWrapper<GdipMeasureCharacterRanges_delegate> GdipMeasureCharacterRanges_ptr;
            internal static Status GdipMeasureCharacterRanges(IntPtr graphics, string str, int length, IntPtr font, ref RectangleF layoutRect, IntPtr stringFormat, int regcount, out IntPtr regions) => GdipMeasureCharacterRanges_ptr.Delegate(graphics, str, length, font, ref layoutRect, stringFormat, regcount, out regions);

            private delegate Status GdipCreateBitmapFromScan0_delegate(int width, int height, int stride, PixelFormat format, IntPtr scan0, out IntPtr bmp);
            private static FunctionWrapper<GdipCreateBitmapFromScan0_delegate> GdipCreateBitmapFromScan0_ptr;
            internal static Status GdipCreateBitmapFromScan0(int width, int height, int stride, PixelFormat format, IntPtr scan0, out IntPtr bmp) => GdipCreateBitmapFromScan0_ptr.Delegate(width, height, stride, format, scan0, out bmp);

            private delegate Status GdipCreateBitmapFromGraphics_delegate(int width, int height, IntPtr target, out IntPtr bitmap);
            private static FunctionWrapper<GdipCreateBitmapFromGraphics_delegate> GdipCreateBitmapFromGraphics_ptr;
            internal static Status GdipCreateBitmapFromGraphics(int width, int height, IntPtr target, out IntPtr bitmap) => GdipCreateBitmapFromGraphics_ptr.Delegate(width, height, target, out bitmap);

            private delegate Status GdipBitmapLockBits_delegate(IntPtr bmp, ref Rectangle rc, ImageLockMode flags, PixelFormat format, [In] [Out] BitmapData bmpData);
            private static FunctionWrapper<GdipBitmapLockBits_delegate> GdipBitmapLockBits_ptr;
            internal static Status GdipBitmapLockBits(IntPtr bmp, ref Rectangle rc, ImageLockMode flags, PixelFormat format, [In] [Out] BitmapData bmpData) => GdipBitmapLockBits_ptr.Delegate(bmp, ref rc, flags, format, bmpData);

            private delegate Status GdipBitmapSetResolution_delegate(IntPtr bmp, float xdpi, float ydpi);
            private static FunctionWrapper<GdipBitmapSetResolution_delegate> GdipBitmapSetResolution_ptr;
            internal static Status GdipBitmapSetResolution(IntPtr bmp, float xdpi, float ydpi) => GdipBitmapSetResolution_ptr.Delegate(bmp, xdpi, ydpi);

            private delegate Status GdipBitmapUnlockBits_delegate(IntPtr bmp, [In] [Out] BitmapData bmpData);
            private static FunctionWrapper<GdipBitmapUnlockBits_delegate> GdipBitmapUnlockBits_ptr;
            internal static Status GdipBitmapUnlockBits(IntPtr bmp, [In] [Out] BitmapData bmpData) => GdipBitmapUnlockBits_ptr.Delegate(bmp, bmpData);

            private delegate Status GdipBitmapGetPixel_delegate(IntPtr bmp, int x, int y, out int argb);
            private static FunctionWrapper<GdipBitmapGetPixel_delegate> GdipBitmapGetPixel_ptr;
            internal static Status GdipBitmapGetPixel(IntPtr bmp, int x, int y, out int argb) => GdipBitmapGetPixel_ptr.Delegate(bmp, x, y, out argb);

            private delegate Status GdipBitmapSetPixel_delegate(IntPtr bmp, int x, int y, int argb);
            private static FunctionWrapper<GdipBitmapSetPixel_delegate> GdipBitmapSetPixel_ptr;
            internal static Status GdipBitmapSetPixel(IntPtr bmp, int x, int y, int argb) => GdipBitmapSetPixel_ptr.Delegate(bmp, x, y, argb);

            private delegate Status GdipLoadImageFromFile_delegate([MarshalAs(UnmanagedType.LPWStr)]string filename, out IntPtr image);
            private static FunctionWrapper<GdipLoadImageFromFile_delegate> GdipLoadImageFromFile_ptr;
            internal static Status GdipLoadImageFromFile(string filename, out IntPtr image) => GdipLoadImageFromFile_ptr.Delegate(filename, out image);

            private delegate Status GdipLoadImageFromStream_delegate([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ComIStreamMarshaler))]IStream stream, out IntPtr image);
            private static FunctionWrapper<GdipLoadImageFromStream_delegate> GdipLoadImageFromStream_ptr;
            internal static Status GdipLoadImageFromStream(IStream stream, out IntPtr image) => GdipLoadImageFromStream_ptr.Delegate(stream, out image);

            private delegate Status GdipSaveImageToStream_delegate(HandleRef image, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ComIStreamMarshaler))]IStream stream, ref Guid clsidEncoder, HandleRef encoderParams);
            private static FunctionWrapper<GdipSaveImageToStream_delegate> GdipSaveImageToStream_ptr;
            internal static Status GdipSaveImageToStream(HandleRef image, IStream stream, ref Guid clsidEncoder, HandleRef encoderParams) => GdipSaveImageToStream_ptr.Delegate(image, stream, ref clsidEncoder, encoderParams);

            private delegate Status GdipCloneImage_delegate(IntPtr image, out IntPtr imageclone);
            private static FunctionWrapper<GdipCloneImage_delegate> GdipCloneImage_ptr;
            internal static Status GdipCloneImage(IntPtr image, out IntPtr imageclone) => GdipCloneImage_ptr.Delegate(image, out imageclone);

            private delegate Status GdipLoadImageFromFileICM_delegate([MarshalAs(UnmanagedType.LPWStr)]string filename, out IntPtr image);
            private static FunctionWrapper<GdipLoadImageFromFileICM_delegate> GdipLoadImageFromFileICM_ptr;
            internal static Status GdipLoadImageFromFileICM(string filename, out IntPtr image) => GdipLoadImageFromFileICM_ptr.Delegate(filename, out image);

            private delegate Status GdipCreateBitmapFromHBITMAP_delegate(IntPtr hBitMap, IntPtr gdiPalette, out IntPtr image);
            private static FunctionWrapper<GdipCreateBitmapFromHBITMAP_delegate> GdipCreateBitmapFromHBITMAP_ptr;
            internal static Status GdipCreateBitmapFromHBITMAP(IntPtr hBitMap, IntPtr gdiPalette, out IntPtr image) => GdipCreateBitmapFromHBITMAP_ptr.Delegate(hBitMap, gdiPalette, out image);

            private delegate Status GdipDisposeImage_delegate(IntPtr image);
            private static FunctionWrapper<GdipDisposeImage_delegate> GdipDisposeImage_ptr;
            internal static Status GdipDisposeImage(IntPtr image) => GdipDisposeImage_ptr.Delegate(image);
            internal static int IntGdipDisposeImage(HandleRef image) => (int)GdipDisposeImage_ptr.Delegate(image.Handle);

            private delegate Status GdipGetImageFlags_delegate(IntPtr image, out int flag);
            private static FunctionWrapper<GdipGetImageFlags_delegate> GdipGetImageFlags_ptr;
            internal static Status GdipGetImageFlags(IntPtr image, out int flag) => GdipGetImageFlags_ptr.Delegate(image, out flag);

            private delegate Status GdipGetImageType_delegate(IntPtr image, out ImageType type);
            private static FunctionWrapper<GdipGetImageType_delegate> GdipGetImageType_ptr;
            internal static Status GdipGetImageType(IntPtr image, out ImageType type) => GdipGetImageType_ptr.Delegate(image, out type);

            private delegate Status GdipImageGetFrameDimensionsCount_delegate(IntPtr image, out uint count);
            private static FunctionWrapper<GdipImageGetFrameDimensionsCount_delegate> GdipImageGetFrameDimensionsCount_ptr;
            internal static Status GdipImageGetFrameDimensionsCount(IntPtr image, out uint count) => GdipImageGetFrameDimensionsCount_ptr.Delegate(image, out count);

            private delegate Status GdipImageGetFrameDimensionsList_delegate(IntPtr image, [Out] Guid[] dimensionIDs, uint count);
            private static FunctionWrapper<GdipImageGetFrameDimensionsList_delegate> GdipImageGetFrameDimensionsList_ptr;
            internal static Status GdipImageGetFrameDimensionsList(IntPtr image, [Out] Guid[] dimensionIDs, uint count) => GdipImageGetFrameDimensionsList_ptr.Delegate(image, dimensionIDs, count);

            private delegate Status GdipGetImageHeight_delegate(IntPtr image, out uint height);
            private static FunctionWrapper<GdipGetImageHeight_delegate> GdipGetImageHeight_ptr;
            internal static Status GdipGetImageHeight(IntPtr image, out uint height) => GdipGetImageHeight_ptr.Delegate(image, out height);

            private delegate Status GdipGetImageHorizontalResolution_delegate(IntPtr image, out float resolution);
            private static FunctionWrapper<GdipGetImageHorizontalResolution_delegate> GdipGetImageHorizontalResolution_ptr;
            internal static Status GdipGetImageHorizontalResolution(IntPtr image, out float resolution) => GdipGetImageHorizontalResolution_ptr.Delegate(image, out resolution);

            private delegate Status GdipGetImagePaletteSize_delegate(IntPtr image, out int size);
            private static FunctionWrapper<GdipGetImagePaletteSize_delegate> GdipGetImagePaletteSize_ptr;
            internal static Status GdipGetImagePaletteSize(IntPtr image, out int size) => GdipGetImagePaletteSize_ptr.Delegate(image, out size);

            private delegate Status GdipGetImagePalette_delegate(IntPtr image, IntPtr palette, int size);
            private static FunctionWrapper<GdipGetImagePalette_delegate> GdipGetImagePalette_ptr;
            internal static Status GdipGetImagePalette(IntPtr image, IntPtr palette, int size) => GdipGetImagePalette_ptr.Delegate(image, palette, size);

            private delegate Status GdipSetImagePalette_delegate(IntPtr image, IntPtr palette);
            private static FunctionWrapper<GdipSetImagePalette_delegate> GdipSetImagePalette_ptr;
            internal static Status GdipSetImagePalette(IntPtr image, IntPtr palette) => GdipSetImagePalette_ptr.Delegate(image, palette);

            private delegate Status GdipGetImageDimension_delegate(IntPtr image, out float width, out float height);
            private static FunctionWrapper<GdipGetImageDimension_delegate> GdipGetImageDimension_ptr;
            internal static Status GdipGetImageDimension(IntPtr image, out float width, out float height) => GdipGetImageDimension_ptr.Delegate(image, out width, out height);

            private delegate Status GdipGetImagePixelFormat_delegate(IntPtr image, out PixelFormat format);
            private static FunctionWrapper<GdipGetImagePixelFormat_delegate> GdipGetImagePixelFormat_ptr;
            internal static Status GdipGetImagePixelFormat(IntPtr image, out PixelFormat format) => GdipGetImagePixelFormat_ptr.Delegate(image, out format);

            private delegate Status GdipGetPropertyCount_delegate(IntPtr image, out uint propNumbers);
            private static FunctionWrapper<GdipGetPropertyCount_delegate> GdipGetPropertyCount_ptr;
            internal static Status GdipGetPropertyCount(IntPtr image, out uint propNumbers) => GdipGetPropertyCount_ptr.Delegate(image, out propNumbers);

            private delegate Status GdipGetPropertyIdList_delegate(IntPtr image, uint propNumbers, [Out] int[] list);
            private static FunctionWrapper<GdipGetPropertyIdList_delegate> GdipGetPropertyIdList_ptr;
            internal static Status GdipGetPropertyIdList(IntPtr image, uint propNumbers, [Out] int[] list) => GdipGetPropertyIdList_ptr.Delegate(image, propNumbers, list);

            private delegate Status GdipGetPropertySize_delegate(IntPtr image, out int bufferSize, out int propNumbers);
            private static FunctionWrapper<GdipGetPropertySize_delegate> GdipGetPropertySize_ptr;
            internal static Status GdipGetPropertySize(IntPtr image, out int bufferSize, out int propNumbers) => GdipGetPropertySize_ptr.Delegate(image, out bufferSize, out propNumbers);

            private delegate Status GdipGetAllPropertyItems_delegate(IntPtr image, int bufferSize, int propNumbers, IntPtr items);
            private static FunctionWrapper<GdipGetAllPropertyItems_delegate> GdipGetAllPropertyItems_ptr;
            internal static Status GdipGetAllPropertyItems(IntPtr image, int bufferSize, int propNumbers, IntPtr items) => GdipGetAllPropertyItems_ptr.Delegate(image, bufferSize, propNumbers, items);

            private delegate Status GdipGetImageRawFormat_delegate(IntPtr image, out Guid format);
            private static FunctionWrapper<GdipGetImageRawFormat_delegate> GdipGetImageRawFormat_ptr;
            internal static Status GdipGetImageRawFormat(IntPtr image, out Guid format) => GdipGetImageRawFormat_ptr.Delegate(image, out format);

            private delegate Status GdipGetImageVerticalResolution_delegate(IntPtr image, out float resolution);
            private static FunctionWrapper<GdipGetImageVerticalResolution_delegate> GdipGetImageVerticalResolution_ptr;
            internal static Status GdipGetImageVerticalResolution(IntPtr image, out float resolution) => GdipGetImageVerticalResolution_ptr.Delegate(image, out resolution);

            private delegate Status GdipGetImageWidth_delegate(IntPtr image, out uint width);
            private static FunctionWrapper<GdipGetImageWidth_delegate> GdipGetImageWidth_ptr;
            internal static Status GdipGetImageWidth(IntPtr image, out uint width) => GdipGetImageWidth_ptr.Delegate(image, out width);

            private delegate Status GdipGetImageBounds_delegate(IntPtr image, out RectangleF source, ref GraphicsUnit unit);
            private static FunctionWrapper<GdipGetImageBounds_delegate> GdipGetImageBounds_ptr;
            internal static Status GdipGetImageBounds(IntPtr image, out RectangleF source, ref GraphicsUnit unit) => GdipGetImageBounds_ptr.Delegate(image, out source, ref unit);

            private delegate Status GdipGetEncoderParameterListSize_delegate(IntPtr image, ref Guid encoder, out uint size);
            private static FunctionWrapper<GdipGetEncoderParameterListSize_delegate> GdipGetEncoderParameterListSize_ptr;
            internal static Status GdipGetEncoderParameterListSize(IntPtr image, ref Guid encoder, out uint size) => GdipGetEncoderParameterListSize_ptr.Delegate(image, ref encoder, out size);

            private delegate Status GdipGetEncoderParameterList_delegate(IntPtr image, ref Guid encoder, uint size, IntPtr buffer);
            private static FunctionWrapper<GdipGetEncoderParameterList_delegate> GdipGetEncoderParameterList_ptr;
            internal static Status GdipGetEncoderParameterList(IntPtr image, ref Guid encoder, uint size, IntPtr buffer) => GdipGetEncoderParameterList_ptr.Delegate(image, ref encoder, size, buffer);

            private delegate Status GdipImageGetFrameCount_delegate(IntPtr image, ref Guid guidDimension, out uint count);
            private static FunctionWrapper<GdipImageGetFrameCount_delegate> GdipImageGetFrameCount_ptr;
            internal static Status GdipImageGetFrameCount(IntPtr image, ref Guid guidDimension, out uint count) => GdipImageGetFrameCount_ptr.Delegate(image, ref guidDimension, out count);

            private delegate Status GdipImageSelectActiveFrame_delegate(IntPtr image, ref Guid guidDimension, int frameIndex);
            private static FunctionWrapper<GdipImageSelectActiveFrame_delegate> GdipImageSelectActiveFrame_ptr;
            internal static Status GdipImageSelectActiveFrame(IntPtr image, ref Guid guidDimension, int frameIndex) => GdipImageSelectActiveFrame_ptr.Delegate(image, ref guidDimension, frameIndex);

            private delegate Status GdipGetPropertyItemSize_delegate(IntPtr image, int propertyID, out int propertySize);
            private static FunctionWrapper<GdipGetPropertyItemSize_delegate> GdipGetPropertyItemSize_ptr;
            internal static Status GdipGetPropertyItemSize(IntPtr image, int propertyID, out int propertySize) => GdipGetPropertyItemSize_ptr.Delegate(image, propertyID, out propertySize);

            private delegate Status GdipGetPropertyItem_delegate(IntPtr image, int propertyID, int propertySize, IntPtr buffer);
            private static FunctionWrapper<GdipGetPropertyItem_delegate> GdipGetPropertyItem_ptr;
            internal static Status GdipGetPropertyItem(IntPtr image, int propertyID, int propertySize, IntPtr buffer) => GdipGetPropertyItem_ptr.Delegate(image, propertyID, propertySize, buffer);

            private delegate Status GdipRemovePropertyItem_delegate(IntPtr image, int propertyId);
            private static FunctionWrapper<GdipRemovePropertyItem_delegate> GdipRemovePropertyItem_ptr;
            internal static Status GdipRemovePropertyItem(IntPtr image, int propertyId) => GdipRemovePropertyItem_ptr.Delegate(image, propertyId);

            private delegate Status GdipSetPropertyItem_delegate(IntPtr image, GdipPropertyItem* propertyItem);
            private static FunctionWrapper<GdipSetPropertyItem_delegate> GdipSetPropertyItem_ptr;
            internal static Status GdipSetPropertyItem(IntPtr image, GdipPropertyItem* propertyItem) => GdipSetPropertyItem_ptr.Delegate(image, propertyItem);

            private delegate Status GdipGetImageThumbnail_delegate(IntPtr image, uint width, uint height, out IntPtr thumbImage, IntPtr callback, IntPtr callBackData);
            private static FunctionWrapper<GdipGetImageThumbnail_delegate> GdipGetImageThumbnail_ptr;
            internal static Status GdipGetImageThumbnail(IntPtr image, uint width, uint height, out IntPtr thumbImage, IntPtr callback, IntPtr callBackData) => GdipGetImageThumbnail_ptr.Delegate(image, width, height, out thumbImage, callback, callBackData);

            private delegate Status GdipImageRotateFlip_delegate(IntPtr image, RotateFlipType rotateFlipType);
            private static FunctionWrapper<GdipImageRotateFlip_delegate> GdipImageRotateFlip_ptr;
            internal static Status GdipImageRotateFlip(IntPtr image, RotateFlipType rotateFlipType) => GdipImageRotateFlip_ptr.Delegate(image, rotateFlipType);

            private delegate Status GdipSaveImageToFile_delegate(IntPtr image, [MarshalAs(UnmanagedType.LPWStr)]string filename, ref Guid encoderClsID, IntPtr encoderParameters);
            private static FunctionWrapper<GdipSaveImageToFile_delegate> GdipSaveImageToFile_ptr;
            internal static Status GdipSaveImageToFile(IntPtr image, string filename, ref Guid encoderClsID, IntPtr encoderParameters) => GdipSaveImageToFile_ptr.Delegate(image, filename, ref encoderClsID, encoderParameters);

            private delegate Status GdipSaveAdd_delegate(IntPtr image, IntPtr encoderParameters);
            private static FunctionWrapper<GdipSaveAdd_delegate> GdipSaveAdd_ptr;
            internal static Status GdipSaveAdd(IntPtr image, IntPtr encoderParameters) => GdipSaveAdd_ptr.Delegate(image, encoderParameters);

            private delegate Status GdipSaveAddImage_delegate(IntPtr image, IntPtr imagenew, IntPtr encoderParameters);
            private static FunctionWrapper<GdipSaveAddImage_delegate> GdipSaveAddImage_ptr;
            internal static Status GdipSaveAddImage(IntPtr image, IntPtr imagenew, IntPtr encoderParameters) => GdipSaveAddImage_ptr.Delegate(image, imagenew, encoderParameters);

            private delegate Status GdipDrawImageI_delegate(IntPtr graphics, IntPtr image, int x, int y);
            private static FunctionWrapper<GdipDrawImageI_delegate> GdipDrawImageI_ptr;
            internal static Status GdipDrawImageI(IntPtr graphics, IntPtr image, int x, int y) => GdipDrawImageI_ptr.Delegate(graphics, image, x, y);

            private delegate Status GdipGetImageGraphicsContext_delegate(IntPtr image, out IntPtr graphics);
            private static FunctionWrapper<GdipGetImageGraphicsContext_delegate> GdipGetImageGraphicsContext_ptr;
            internal static Status GdipGetImageGraphicsContext(IntPtr image, out IntPtr graphics) => GdipGetImageGraphicsContext_ptr.Delegate(image, out graphics);

            private delegate Status GdipDrawImage_delegate(IntPtr graphics, IntPtr image, float x, float y);
            private static FunctionWrapper<GdipDrawImage_delegate> GdipDrawImage_ptr;
            internal static Status GdipDrawImage(IntPtr graphics, IntPtr image, float x, float y) => GdipDrawImage_ptr.Delegate(graphics, image, x, y);

            private delegate Status GdipDrawImagePoints_delegate(IntPtr graphics, IntPtr image, PointF[] destPoints, int count);
            private static FunctionWrapper<GdipDrawImagePoints_delegate> GdipDrawImagePoints_ptr;
            internal static Status GdipDrawImagePoints(IntPtr graphics, IntPtr image, PointF[] destPoints, int count) => GdipDrawImagePoints_ptr.Delegate(graphics, image, destPoints, count);

            private delegate Status GdipDrawImagePointsI_delegate(IntPtr graphics, IntPtr image, Point[] destPoints, int count);
            private static FunctionWrapper<GdipDrawImagePointsI_delegate> GdipDrawImagePointsI_ptr;
            internal static Status GdipDrawImagePointsI(IntPtr graphics, IntPtr image, Point[] destPoints, int count) => GdipDrawImagePointsI_ptr.Delegate(graphics, image, destPoints, count);

            private delegate Status GdipDrawImageRectRectI_delegate(IntPtr graphics, IntPtr image, int dstx, int dsty, int dstwidth, int dstheight, int srcx, int srcy, int srcwidth, int srcheight, GraphicsUnit srcUnit, IntPtr imageattr, Graphics.DrawImageAbort callback, IntPtr callbackData);
            private static FunctionWrapper<GdipDrawImageRectRectI_delegate> GdipDrawImageRectRectI_ptr;
            internal static Status GdipDrawImageRectRectI(IntPtr graphics, IntPtr image, int dstx, int dsty, int dstwidth, int dstheight, int srcx, int srcy, int srcwidth, int srcheight, GraphicsUnit srcUnit, IntPtr imageattr, Graphics.DrawImageAbort callback, IntPtr callbackData) => GdipDrawImageRectRectI_ptr.Delegate(graphics, image, dstx, dsty, dstwidth, dstheight, srcx, srcy, srcwidth, srcheight, srcUnit, imageattr, callback, callbackData);

            private delegate Status GdipDrawImageRectRect_delegate(IntPtr graphics, IntPtr image, float dstx, float dsty, float dstwidth, float dstheight, float srcx, float srcy, float srcwidth, float srcheight, GraphicsUnit srcUnit, IntPtr imageattr, Graphics.DrawImageAbort callback, IntPtr callbackData);
            private static FunctionWrapper<GdipDrawImageRectRect_delegate> GdipDrawImageRectRect_ptr;
            internal static Status GdipDrawImageRectRect(IntPtr graphics, IntPtr image, float dstx, float dsty, float dstwidth, float dstheight, float srcx, float srcy, float srcwidth, float srcheight, GraphicsUnit srcUnit, IntPtr imageattr, Graphics.DrawImageAbort callback, IntPtr callbackData) => GdipDrawImageRectRect_ptr.Delegate(graphics, image, dstx, dsty, dstwidth, dstheight, srcx, srcy, srcwidth, srcheight, srcUnit, imageattr, callback, callbackData);

            private delegate Status GdipDrawImagePointsRectI_delegate(IntPtr graphics, IntPtr image, Point[] destPoints, int count, int srcx, int srcy, int srcwidth, int srcheight, GraphicsUnit srcUnit, IntPtr imageattr, Graphics.DrawImageAbort callback, IntPtr callbackData);
            private static FunctionWrapper<GdipDrawImagePointsRectI_delegate> GdipDrawImagePointsRectI_ptr;
            internal static Status GdipDrawImagePointsRectI(IntPtr graphics, IntPtr image, Point[] destPoints, int count, int srcx, int srcy, int srcwidth, int srcheight, GraphicsUnit srcUnit, IntPtr imageattr, Graphics.DrawImageAbort callback, IntPtr callbackData) => GdipDrawImagePointsRectI_ptr.Delegate(graphics, image, destPoints, count, srcx, srcy, srcwidth, srcheight, srcUnit, imageattr, callback, callbackData);

            private delegate Status GdipDrawImagePointsRect_delegate(IntPtr graphics, IntPtr image, PointF[] destPoints, int count, float srcx, float srcy, float srcwidth, float srcheight, GraphicsUnit srcUnit, IntPtr imageattr, Graphics.DrawImageAbort callback, IntPtr callbackData);
            private static FunctionWrapper<GdipDrawImagePointsRect_delegate> GdipDrawImagePointsRect_ptr;
            internal static Status GdipDrawImagePointsRect(IntPtr graphics, IntPtr image, PointF[] destPoints, int count, float srcx, float srcy, float srcwidth, float srcheight, GraphicsUnit srcUnit, IntPtr imageattr, Graphics.DrawImageAbort callback, IntPtr callbackData) => GdipDrawImagePointsRect_ptr.Delegate(graphics, image, destPoints, count, srcx, srcy, srcwidth, srcheight, srcUnit, imageattr, callback, callbackData);

            private delegate Status GdipDrawImageRect_delegate(IntPtr graphics, IntPtr image, float x, float y, float width, float height);
            private static FunctionWrapper<GdipDrawImageRect_delegate> GdipDrawImageRect_ptr;
            internal static Status GdipDrawImageRect(IntPtr graphics, IntPtr image, float x, float y, float width, float height) => GdipDrawImageRect_ptr.Delegate(graphics, image, x, y, width, height);

            private delegate Status GdipDrawImagePointRect_delegate(IntPtr graphics, IntPtr image, float x, float y, float srcx, float srcy, float srcwidth, float srcheight, GraphicsUnit srcUnit);
            private static FunctionWrapper<GdipDrawImagePointRect_delegate> GdipDrawImagePointRect_ptr;
            internal static Status GdipDrawImagePointRect(IntPtr graphics, IntPtr image, float x, float y, float srcx, float srcy, float srcwidth, float srcheight, GraphicsUnit srcUnit) => GdipDrawImagePointRect_ptr.Delegate(graphics, image, x, y, srcx, srcy, srcwidth, srcheight, srcUnit);

            private delegate Status GdipDrawImagePointRectI_delegate(IntPtr graphics, IntPtr image, int x, int y, int srcx, int srcy, int srcwidth, int srcheight, GraphicsUnit srcUnit);
            private static FunctionWrapper<GdipDrawImagePointRectI_delegate> GdipDrawImagePointRectI_ptr;
            internal static Status GdipDrawImagePointRectI(IntPtr graphics, IntPtr image, int x, int y, int srcx, int srcy, int srcwidth, int srcheight, GraphicsUnit srcUnit) => GdipDrawImagePointRectI_ptr.Delegate(graphics, image, x, y, srcx, srcy, srcwidth, srcheight, srcUnit);

            private delegate Status GdipCreateHBITMAPFromBitmap_delegate(IntPtr bmp, out IntPtr HandleBmp, int clrbackground);
            private static FunctionWrapper<GdipCreateHBITMAPFromBitmap_delegate> GdipCreateHBITMAPFromBitmap_ptr;
            internal static Status GdipCreateHBITMAPFromBitmap(IntPtr bmp, out IntPtr HandleBmp, int clrbackground) => GdipCreateHBITMAPFromBitmap_ptr.Delegate(bmp, out HandleBmp, clrbackground);

            private delegate Status GdipCreateBitmapFromFile_delegate([MarshalAs(UnmanagedType.LPWStr)]string filename, out IntPtr bitmap);
            private static FunctionWrapper<GdipCreateBitmapFromFile_delegate> GdipCreateBitmapFromFile_ptr;
            internal static Status GdipCreateBitmapFromFile(string filename, out IntPtr bitmap) => GdipCreateBitmapFromFile_ptr.Delegate(filename, out bitmap);

            private delegate Status GdipCreateBitmapFromFileICM_delegate([MarshalAs(UnmanagedType.LPWStr)]string filename, out IntPtr bitmap);
            private static FunctionWrapper<GdipCreateBitmapFromFileICM_delegate> GdipCreateBitmapFromFileICM_ptr;
            internal static Status GdipCreateBitmapFromFileICM(string filename, out IntPtr bitmap) => GdipCreateBitmapFromFileICM_ptr.Delegate(filename, out bitmap);

            private delegate Status GdipCreateHICONFromBitmap_delegate(IntPtr bmp, out IntPtr HandleIcon);
            private static FunctionWrapper<GdipCreateHICONFromBitmap_delegate> GdipCreateHICONFromBitmap_ptr;
            internal static Status GdipCreateHICONFromBitmap(IntPtr bmp, out IntPtr HandleIcon) => GdipCreateHICONFromBitmap_ptr.Delegate(bmp, out HandleIcon);

            private delegate Status GdipCreateBitmapFromHICON_delegate(IntPtr hicon, out IntPtr bitmap);
            private static FunctionWrapper<GdipCreateBitmapFromHICON_delegate> GdipCreateBitmapFromHICON_ptr;
            internal static Status GdipCreateBitmapFromHICON(IntPtr hicon, out IntPtr bitmap) => GdipCreateBitmapFromHICON_ptr.Delegate(hicon, out bitmap);

            private delegate Status GdipCreateBitmapFromResource_delegate(IntPtr hInstance, [MarshalAs(UnmanagedType.LPWStr)]string lpBitmapName, out IntPtr bitmap);
            private static FunctionWrapper<GdipCreateBitmapFromResource_delegate> GdipCreateBitmapFromResource_ptr;
            internal static Status GdipCreateBitmapFromResource(IntPtr hInstance, string lpBitmapName, out IntPtr bitmap) => GdipCreateBitmapFromResource_ptr.Delegate(hInstance, lpBitmapName, out bitmap);

            private delegate Status GdipCreatePath_delegate(FillMode brushMode, out IntPtr path);
            private static FunctionWrapper<GdipCreatePath_delegate> GdipCreatePath_ptr;
            internal static Status GdipCreatePath(FillMode brushMode, out IntPtr path) => GdipCreatePath_ptr.Delegate(brushMode, out path);

            private delegate Status GdipCreatePath2_delegate(PointF[] points, byte[] types, int count, FillMode brushMode, out IntPtr path);
            private static FunctionWrapper<GdipCreatePath2_delegate> GdipCreatePath2_ptr;
            internal static Status GdipCreatePath2(PointF[] points, byte[] types, int count, FillMode brushMode, out IntPtr path) => GdipCreatePath2_ptr.Delegate(points, types, count, brushMode, out path);

            private delegate Status GdipCreatePath2I_delegate(Point[] points, byte[] types, int count, FillMode brushMode, out IntPtr path);
            private static FunctionWrapper<GdipCreatePath2I_delegate> GdipCreatePath2I_ptr;
            internal static Status GdipCreatePath2I(Point[] points, byte[] types, int count, FillMode brushMode, out IntPtr path) => GdipCreatePath2I_ptr.Delegate(points, types, count, brushMode, out path);

            private delegate Status GdipClonePath_delegate(IntPtr path, out IntPtr clonePath);
            private static FunctionWrapper<GdipClonePath_delegate> GdipClonePath_ptr;
            internal static Status GdipClonePath(IntPtr path, out IntPtr clonePath) => GdipClonePath_ptr.Delegate(path, out clonePath);

            private delegate Status GdipDeletePath_delegate(IntPtr path);
            private static FunctionWrapper<GdipDeletePath_delegate> GdipDeletePath_ptr;
            internal static Status GdipDeletePath(IntPtr path) => GdipDeletePath_ptr.Delegate(path);
            internal static int IntGdipDeletePath(HandleRef path) => (int)GdipDeletePath_ptr.Delegate(path.Handle);

            private delegate Status GdipResetPath_delegate(IntPtr path);
            private static FunctionWrapper<GdipResetPath_delegate> GdipResetPath_ptr;
            internal static Status GdipResetPath(IntPtr path) => GdipResetPath_ptr.Delegate(path);

            private delegate Status GdipGetPointCount_delegate(IntPtr path, out int count);
            private static FunctionWrapper<GdipGetPointCount_delegate> GdipGetPointCount_ptr;
            internal static Status GdipGetPointCount(IntPtr path, out int count) => GdipGetPointCount_ptr.Delegate(path, out count);

            private delegate Status GdipGetPathTypes_delegate(IntPtr path, [Out] byte[] types, int count);
            private static FunctionWrapper<GdipGetPathTypes_delegate> GdipGetPathTypes_ptr;
            internal static Status GdipGetPathTypes(IntPtr path, [Out] byte[] types, int count) => GdipGetPathTypes_ptr.Delegate(path, types, count);

            private delegate Status GdipGetPathPoints_delegate(IntPtr path, [Out] PointF[] points, int count);
            private static FunctionWrapper<GdipGetPathPoints_delegate> GdipGetPathPoints_ptr;
            internal static Status GdipGetPathPoints(IntPtr path, [Out] PointF[] points, int count) => GdipGetPathPoints_ptr.Delegate(path, points, count);

            private delegate Status GdipGetPathPointsI_delegate(IntPtr path, [Out] Point[] points, int count);
            private static FunctionWrapper<GdipGetPathPointsI_delegate> GdipGetPathPointsI_ptr;
            internal static Status GdipGetPathPointsI(IntPtr path, [Out] Point[] points, int count) => GdipGetPathPointsI_ptr.Delegate(path, points, count);

            private delegate Status GdipGetPathFillMode_delegate(IntPtr path, out FillMode fillMode);
            private static FunctionWrapper<GdipGetPathFillMode_delegate> GdipGetPathFillMode_ptr;
            internal static Status GdipGetPathFillMode(IntPtr path, out FillMode fillMode) => GdipGetPathFillMode_ptr.Delegate(path, out fillMode);

            private delegate Status GdipSetPathFillMode_delegate(IntPtr path, FillMode fillMode);
            private static FunctionWrapper<GdipSetPathFillMode_delegate> GdipSetPathFillMode_ptr;
            internal static Status GdipSetPathFillMode(IntPtr path, FillMode fillMode) => GdipSetPathFillMode_ptr.Delegate(path, fillMode);

            private delegate Status GdipStartPathFigure_delegate(IntPtr path);
            private static FunctionWrapper<GdipStartPathFigure_delegate> GdipStartPathFigure_ptr;
            internal static Status GdipStartPathFigure(IntPtr path) => GdipStartPathFigure_ptr.Delegate(path);

            private delegate Status GdipClosePathFigure_delegate(IntPtr path);
            private static FunctionWrapper<GdipClosePathFigure_delegate> GdipClosePathFigure_ptr;
            internal static Status GdipClosePathFigure(IntPtr path) => GdipClosePathFigure_ptr.Delegate(path);

            private delegate Status GdipClosePathFigures_delegate(IntPtr path);
            private static FunctionWrapper<GdipClosePathFigures_delegate> GdipClosePathFigures_ptr;
            internal static Status GdipClosePathFigures(IntPtr path) => GdipClosePathFigures_ptr.Delegate(path);

            private delegate Status GdipSetPathMarker_delegate(IntPtr path);
            private static FunctionWrapper<GdipSetPathMarker_delegate> GdipSetPathMarker_ptr;
            internal static Status GdipSetPathMarker(IntPtr path) => GdipSetPathMarker_ptr.Delegate(path);

            private delegate Status GdipClearPathMarkers_delegate(IntPtr path);
            private static FunctionWrapper<GdipClearPathMarkers_delegate> GdipClearPathMarkers_ptr;
            internal static Status GdipClearPathMarkers(IntPtr path) => GdipClearPathMarkers_ptr.Delegate(path);

            private delegate Status GdipReversePath_delegate(IntPtr path);
            private static FunctionWrapper<GdipReversePath_delegate> GdipReversePath_ptr;
            internal static Status GdipReversePath(IntPtr path) => GdipReversePath_ptr.Delegate(path);

            private delegate Status GdipGetPathLastPoint_delegate(IntPtr path, out PointF lastPoint);
            private static FunctionWrapper<GdipGetPathLastPoint_delegate> GdipGetPathLastPoint_ptr;
            internal static Status GdipGetPathLastPoint(IntPtr path, out PointF lastPoint) => GdipGetPathLastPoint_ptr.Delegate(path, out lastPoint);

            private delegate Status GdipAddPathLine_delegate(IntPtr path, float x1, float y1, float x2, float y2);
            private static FunctionWrapper<GdipAddPathLine_delegate> GdipAddPathLine_ptr;
            internal static Status GdipAddPathLine(IntPtr path, float x1, float y1, float x2, float y2) => GdipAddPathLine_ptr.Delegate(path, x1, y1, x2, y2);

            private delegate Status GdipAddPathLine2_delegate(IntPtr path, PointF[] points, int count);
            private static FunctionWrapper<GdipAddPathLine2_delegate> GdipAddPathLine2_ptr;
            internal static Status GdipAddPathLine2(IntPtr path, PointF[] points, int count) => GdipAddPathLine2_ptr.Delegate(path, points, count);

            private delegate Status GdipAddPathLine2I_delegate(IntPtr path, Point[] points, int count);
            private static FunctionWrapper<GdipAddPathLine2I_delegate> GdipAddPathLine2I_ptr;
            internal static Status GdipAddPathLine2I(IntPtr path, Point[] points, int count) => GdipAddPathLine2I_ptr.Delegate(path, points, count);

            private delegate Status GdipAddPathArc_delegate(IntPtr path, float x, float y, float width, float height, float startAngle, float sweepAngle);
            private static FunctionWrapper<GdipAddPathArc_delegate> GdipAddPathArc_ptr;
            internal static Status GdipAddPathArc(IntPtr path, float x, float y, float width, float height, float startAngle, float sweepAngle) => GdipAddPathArc_ptr.Delegate(path, x, y, width, height, startAngle, sweepAngle);

            private delegate Status GdipAddPathBezier_delegate(IntPtr path, float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4);
            private static FunctionWrapper<GdipAddPathBezier_delegate> GdipAddPathBezier_ptr;
            internal static Status GdipAddPathBezier(IntPtr path, float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4) => GdipAddPathBezier_ptr.Delegate(path, x1, y1, x2, y2, x3, y3, x4, y4);

            private delegate Status GdipAddPathBeziers_delegate(IntPtr path, PointF[] points, int count);
            private static FunctionWrapper<GdipAddPathBeziers_delegate> GdipAddPathBeziers_ptr;
            internal static Status GdipAddPathBeziers(IntPtr path, PointF[] points, int count) => GdipAddPathBeziers_ptr.Delegate(path, points, count);

            private delegate Status GdipAddPathCurve_delegate(IntPtr path, PointF[] points, int count);
            private static FunctionWrapper<GdipAddPathCurve_delegate> GdipAddPathCurve_ptr;
            internal static Status GdipAddPathCurve(IntPtr path, PointF[] points, int count) => GdipAddPathCurve_ptr.Delegate(path, points, count);

            private delegate Status GdipAddPathCurveI_delegate(IntPtr path, Point[] points, int count);
            private static FunctionWrapper<GdipAddPathCurveI_delegate> GdipAddPathCurveI_ptr;
            internal static Status GdipAddPathCurveI(IntPtr path, Point[] points, int count) => GdipAddPathCurveI_ptr.Delegate(path, points, count);

            private delegate Status GdipAddPathCurve2_delegate(IntPtr path, PointF[] points, int count, float tension);
            private static FunctionWrapper<GdipAddPathCurve2_delegate> GdipAddPathCurve2_ptr;
            internal static Status GdipAddPathCurve2(IntPtr path, PointF[] points, int count, float tension) => GdipAddPathCurve2_ptr.Delegate(path, points, count, tension);

            private delegate Status GdipAddPathCurve2I_delegate(IntPtr path, Point[] points, int count, float tension);
            private static FunctionWrapper<GdipAddPathCurve2I_delegate> GdipAddPathCurve2I_ptr;
            internal static Status GdipAddPathCurve2I(IntPtr path, Point[] points, int count, float tension) => GdipAddPathCurve2I_ptr.Delegate(path, points, count, tension);

            private delegate Status GdipAddPathCurve3_delegate(IntPtr path, PointF[] points, int count, int offset, int numberOfSegments, float tension);
            private static FunctionWrapper<GdipAddPathCurve3_delegate> GdipAddPathCurve3_ptr;
            internal static Status GdipAddPathCurve3(IntPtr path, PointF[] points, int count, int offset, int numberOfSegments, float tension) => GdipAddPathCurve3_ptr.Delegate(path, points, count, offset, numberOfSegments, tension);

            private delegate Status GdipAddPathCurve3I_delegate(IntPtr path, Point[] points, int count, int offset, int numberOfSegments, float tension);
            private static FunctionWrapper<GdipAddPathCurve3I_delegate> GdipAddPathCurve3I_ptr;
            internal static Status GdipAddPathCurve3I(IntPtr path, Point[] points, int count, int offset, int numberOfSegments, float tension) => GdipAddPathCurve3I_ptr.Delegate(path, points, count, offset, numberOfSegments, tension);

            private delegate Status GdipAddPathClosedCurve_delegate(IntPtr path, PointF[] points, int count);
            private static FunctionWrapper<GdipAddPathClosedCurve_delegate> GdipAddPathClosedCurve_ptr;
            internal static Status GdipAddPathClosedCurve(IntPtr path, PointF[] points, int count) => GdipAddPathClosedCurve_ptr.Delegate(path, points, count);

            private delegate Status GdipAddPathClosedCurveI_delegate(IntPtr path, Point[] points, int count);
            private static FunctionWrapper<GdipAddPathClosedCurveI_delegate> GdipAddPathClosedCurveI_ptr;
            internal static Status GdipAddPathClosedCurveI(IntPtr path, Point[] points, int count) => GdipAddPathClosedCurveI_ptr.Delegate(path, points, count);

            private delegate Status GdipAddPathClosedCurve2_delegate(IntPtr path, PointF[] points, int count, float tension);
            private static FunctionWrapper<GdipAddPathClosedCurve2_delegate> GdipAddPathClosedCurve2_ptr;
            internal static Status GdipAddPathClosedCurve2(IntPtr path, PointF[] points, int count, float tension) => GdipAddPathClosedCurve2_ptr.Delegate(path, points, count, tension);

            private delegate Status GdipAddPathClosedCurve2I_delegate(IntPtr path, Point[] points, int count, float tension);
            private static FunctionWrapper<GdipAddPathClosedCurve2I_delegate> GdipAddPathClosedCurve2I_ptr;
            internal static Status GdipAddPathClosedCurve2I(IntPtr path, Point[] points, int count, float tension) => GdipAddPathClosedCurve2I_ptr.Delegate(path, points, count, tension);

            private delegate Status GdipAddPathRectangle_delegate(IntPtr path, float x, float y, float width, float height);
            private static FunctionWrapper<GdipAddPathRectangle_delegate> GdipAddPathRectangle_ptr;
            internal static Status GdipAddPathRectangle(IntPtr path, float x, float y, float width, float height) => GdipAddPathRectangle_ptr.Delegate(path, x, y, width, height);

            private delegate Status GdipAddPathRectangles_delegate(IntPtr path, RectangleF[] rects, int count);
            private static FunctionWrapper<GdipAddPathRectangles_delegate> GdipAddPathRectangles_ptr;
            internal static Status GdipAddPathRectangles(IntPtr path, RectangleF[] rects, int count) => GdipAddPathRectangles_ptr.Delegate(path, rects, count);

            private delegate Status GdipAddPathEllipse_delegate(IntPtr path, float x, float y, float width, float height);
            private static FunctionWrapper<GdipAddPathEllipse_delegate> GdipAddPathEllipse_ptr;
            internal static Status GdipAddPathEllipse(IntPtr path, float x, float y, float width, float height) => GdipAddPathEllipse_ptr.Delegate(path, x, y, width, height);

            private delegate Status GdipAddPathEllipseI_delegate(IntPtr path, int x, int y, int width, int height);
            private static FunctionWrapper<GdipAddPathEllipseI_delegate> GdipAddPathEllipseI_ptr;
            internal static Status GdipAddPathEllipseI(IntPtr path, int x, int y, int width, int height) => GdipAddPathEllipseI_ptr.Delegate(path, x, y, width, height);

            private delegate Status GdipAddPathPie_delegate(IntPtr path, float x, float y, float width, float height, float startAngle, float sweepAngle);
            private static FunctionWrapper<GdipAddPathPie_delegate> GdipAddPathPie_ptr;
            internal static Status GdipAddPathPie(IntPtr path, float x, float y, float width, float height, float startAngle, float sweepAngle) => GdipAddPathPie_ptr.Delegate(path, x, y, width, height, startAngle, sweepAngle);

            private delegate Status GdipAddPathPieI_delegate(IntPtr path, int x, int y, int width, int height, float startAngle, float sweepAngle);
            private static FunctionWrapper<GdipAddPathPieI_delegate> GdipAddPathPieI_ptr;
            internal static Status GdipAddPathPieI(IntPtr path, int x, int y, int width, int height, float startAngle, float sweepAngle) => GdipAddPathPieI_ptr.Delegate(path, x, y, width, height, startAngle, sweepAngle);

            private delegate Status GdipAddPathPolygon_delegate(IntPtr path, PointF[] points, int count);
            private static FunctionWrapper<GdipAddPathPolygon_delegate> GdipAddPathPolygon_ptr;
            internal static Status GdipAddPathPolygon(IntPtr path, PointF[] points, int count) => GdipAddPathPolygon_ptr.Delegate(path, points, count);

            private delegate Status GdipAddPathPath_delegate(IntPtr path, IntPtr addingPath, bool connect);
            private static FunctionWrapper<GdipAddPathPath_delegate> GdipAddPathPath_ptr;
            internal static Status GdipAddPathPath(IntPtr path, IntPtr addingPath, bool connect) => GdipAddPathPath_ptr.Delegate(path, addingPath, connect);

            private delegate Status GdipAddPathLineI_delegate(IntPtr path, int x1, int y1, int x2, int y2);
            private static FunctionWrapper<GdipAddPathLineI_delegate> GdipAddPathLineI_ptr;
            internal static Status GdipAddPathLineI(IntPtr path, int x1, int y1, int x2, int y2) => GdipAddPathLineI_ptr.Delegate(path, x1, y1, x2, y2);

            private delegate Status GdipAddPathArcI_delegate(IntPtr path, int x, int y, int width, int height, float startAngle, float sweepAngle);
            private static FunctionWrapper<GdipAddPathArcI_delegate> GdipAddPathArcI_ptr;
            internal static Status GdipAddPathArcI(IntPtr path, int x, int y, int width, int height, float startAngle, float sweepAngle) => GdipAddPathArcI_ptr.Delegate(path, x, y, width, height, startAngle, sweepAngle);

            private delegate Status GdipAddPathBezierI_delegate(IntPtr path, int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4);
            private static FunctionWrapper<GdipAddPathBezierI_delegate> GdipAddPathBezierI_ptr;
            internal static Status GdipAddPathBezierI(IntPtr path, int x1, int y1, int x2, int y2, int x3, int y3, int x4, int y4) => GdipAddPathBezierI_ptr.Delegate(path, x1, y1, x2, y2, x3, y3, x4, y4);

            private delegate Status GdipAddPathBeziersI_delegate(IntPtr path, Point[] points, int count);
            private static FunctionWrapper<GdipAddPathBeziersI_delegate> GdipAddPathBeziersI_ptr;
            internal static Status GdipAddPathBeziersI(IntPtr path, Point[] points, int count) => GdipAddPathBeziersI_ptr.Delegate(path, points, count);

            private delegate Status GdipAddPathPolygonI_delegate(IntPtr path, Point[] points, int count);
            private static FunctionWrapper<GdipAddPathPolygonI_delegate> GdipAddPathPolygonI_ptr;
            internal static Status GdipAddPathPolygonI(IntPtr path, Point[] points, int count) => GdipAddPathPolygonI_ptr.Delegate(path, points, count);

            private delegate Status GdipAddPathRectangleI_delegate(IntPtr path, int x, int y, int width, int height);
            private static FunctionWrapper<GdipAddPathRectangleI_delegate> GdipAddPathRectangleI_ptr;
            internal static Status GdipAddPathRectangleI(IntPtr path, int x, int y, int width, int height) => GdipAddPathRectangleI_ptr.Delegate(path, x, y, width, height);

            private delegate Status GdipAddPathRectanglesI_delegate(IntPtr path, Rectangle[] rects, int count);
            private static FunctionWrapper<GdipAddPathRectanglesI_delegate> GdipAddPathRectanglesI_ptr;
            internal static Status GdipAddPathRectanglesI(IntPtr path, Rectangle[] rects, int count) => GdipAddPathRectanglesI_ptr.Delegate(path, rects, count);

            private delegate Status GdipFlattenPath_delegate(IntPtr path, IntPtr matrix, float floatness);
            private static FunctionWrapper<GdipFlattenPath_delegate> GdipFlattenPath_ptr;
            internal static Status GdipFlattenPath(IntPtr path, IntPtr matrix, float floatness) => GdipFlattenPath_ptr.Delegate(path, matrix, floatness);

            private delegate Status GdipTransformPath_delegate(IntPtr path, IntPtr matrix);
            private static FunctionWrapper<GdipTransformPath_delegate> GdipTransformPath_ptr;
            internal static Status GdipTransformPath(IntPtr path, IntPtr matrix) => GdipTransformPath_ptr.Delegate(path, matrix);

            private delegate Status GdipWarpPath_delegate(IntPtr path, IntPtr matrix, PointF[] points, int count, float srcx, float srcy, float srcwidth, float srcheight, WarpMode mode, float flatness);
            private static FunctionWrapper<GdipWarpPath_delegate> GdipWarpPath_ptr;
            internal static Status GdipWarpPath(IntPtr path, IntPtr matrix, PointF[] points, int count, float srcx, float srcy, float srcwidth, float srcheight, WarpMode mode, float flatness) => GdipWarpPath_ptr.Delegate(path, matrix, points, count, srcx, srcy, srcwidth, srcheight, mode, flatness);

            private delegate Status GdipWidenPath_delegate(IntPtr path, IntPtr pen, IntPtr matrix, float flatness);
            private static FunctionWrapper<GdipWidenPath_delegate> GdipWidenPath_ptr;
            internal static Status GdipWidenPath(IntPtr path, IntPtr pen, IntPtr matrix, float flatness) => GdipWidenPath_ptr.Delegate(path, pen, matrix, flatness);

            private delegate Status GdipGetPathWorldBounds_delegate(IntPtr path, out RectangleF bounds, IntPtr matrix, IntPtr pen);
            private static FunctionWrapper<GdipGetPathWorldBounds_delegate> GdipGetPathWorldBounds_ptr;
            internal static Status GdipGetPathWorldBounds(IntPtr path, out RectangleF bounds, IntPtr matrix, IntPtr pen) => GdipGetPathWorldBounds_ptr.Delegate(path, out bounds, matrix, pen);

            private delegate Status GdipGetPathWorldBoundsI_delegate(IntPtr path, out Rectangle bounds, IntPtr matrix, IntPtr pen);
            private static FunctionWrapper<GdipGetPathWorldBoundsI_delegate> GdipGetPathWorldBoundsI_ptr;
            internal static Status GdipGetPathWorldBoundsI(IntPtr path, out Rectangle bounds, IntPtr matrix, IntPtr pen) => GdipGetPathWorldBoundsI_ptr.Delegate(path, out bounds, matrix, pen);

            private delegate Status GdipIsVisiblePathPoint_delegate(IntPtr path, float x, float y, IntPtr graphics, out bool result);
            private static FunctionWrapper<GdipIsVisiblePathPoint_delegate> GdipIsVisiblePathPoint_ptr;
            internal static Status GdipIsVisiblePathPoint(IntPtr path, float x, float y, IntPtr graphics, out bool result) => GdipIsVisiblePathPoint_ptr.Delegate(path, x, y, graphics, out result);

            private delegate Status GdipIsVisiblePathPointI_delegate(IntPtr path, int x, int y, IntPtr graphics, out bool result);
            private static FunctionWrapper<GdipIsVisiblePathPointI_delegate> GdipIsVisiblePathPointI_ptr;
            internal static Status GdipIsVisiblePathPointI(IntPtr path, int x, int y, IntPtr graphics, out bool result) => GdipIsVisiblePathPointI_ptr.Delegate(path, x, y, graphics, out result);

            private delegate Status GdipIsOutlineVisiblePathPoint_delegate(IntPtr path, float x, float y, IntPtr pen, IntPtr graphics, out bool result);
            private static FunctionWrapper<GdipIsOutlineVisiblePathPoint_delegate> GdipIsOutlineVisiblePathPoint_ptr;
            internal static Status GdipIsOutlineVisiblePathPoint(IntPtr path, float x, float y, IntPtr pen, IntPtr graphics, out bool result) => GdipIsOutlineVisiblePathPoint_ptr.Delegate(path, x, y, pen, graphics, out result);

            private delegate Status GdipIsOutlineVisiblePathPointI_delegate(IntPtr path, int x, int y, IntPtr pen, IntPtr graphics, out bool result);
            private static FunctionWrapper<GdipIsOutlineVisiblePathPointI_delegate> GdipIsOutlineVisiblePathPointI_ptr;
            internal static Status GdipIsOutlineVisiblePathPointI(IntPtr path, int x, int y, IntPtr pen, IntPtr graphics, out bool result) => GdipIsOutlineVisiblePathPointI_ptr.Delegate(path, x, y, pen, graphics, out result);

            private delegate Status GdipCreateFont_delegate(IntPtr fontFamily, float emSize, FontStyle style, GraphicsUnit unit, out IntPtr font);
            private static FunctionWrapper<GdipCreateFont_delegate> GdipCreateFont_ptr;
            internal static Status GdipCreateFont(IntPtr fontFamily, float emSize, FontStyle style, GraphicsUnit unit, out IntPtr font) => GdipCreateFont_ptr.Delegate(fontFamily, emSize, style, unit, out font);

            private delegate Status GdipDeleteFont_delegate(IntPtr font);
            private static FunctionWrapper<GdipDeleteFont_delegate> GdipDeleteFont_ptr;
            internal static Status GdipDeleteFont(IntPtr font) => GdipDeleteFont_ptr.Delegate(font);
            internal static int IntGdipDeleteFont(HandleRef font) => (int)GdipDeleteFont_ptr.Delegate(font.Handle);

#pragma warning disable CS0618 // Legacy code: We don't care about using obsolete API's.
            private delegate Status GdipGetLogFont_delegate(IntPtr font, IntPtr graphics, [MarshalAs(UnmanagedType.AsAny), Out] object logfontA);
#pragma warning restore CS0618
            private static FunctionWrapper<GdipGetLogFont_delegate> GdipGetLogFont_ptr;
            internal static Status GdipGetLogFont(IntPtr font, IntPtr graphics, [Out] object logfontA) => GdipGetLogFont_ptr.Delegate(font, graphics, logfontA);

            private delegate Status GdipCreateFontFromDC_delegate(IntPtr hdc, out IntPtr font);
            private static FunctionWrapper<GdipCreateFontFromDC_delegate> GdipCreateFontFromDC_ptr;
            internal static Status GdipCreateFontFromDC(IntPtr hdc, out IntPtr font) => GdipCreateFontFromDC_ptr.Delegate(hdc, out font);

            private delegate Status GdipCreateFontFromLogfont_delegate(IntPtr hdc, ref LOGFONT lf, out IntPtr ptr);
            private static FunctionWrapper<GdipCreateFontFromLogfont_delegate> GdipCreateFontFromLogfont_ptr;
            internal static Status GdipCreateFontFromLogfont(IntPtr hdc, ref LOGFONT lf, out IntPtr ptr) => GdipCreateFontFromLogfont_ptr.Delegate(hdc, ref lf, out ptr);

            private delegate Status GdipCreateFontFromHfont_delegate(IntPtr hdc, out IntPtr font, ref LOGFONT lf);
            private static FunctionWrapper<GdipCreateFontFromHfont_delegate> GdipCreateFontFromHfont_ptr;
            internal static Status GdipCreateFontFromHfont(IntPtr hdc, out IntPtr font, ref LOGFONT lf) => GdipCreateFontFromHfont_ptr.Delegate(hdc, out font, ref lf);

            private delegate Status GdipGetFontSize_delegate(IntPtr font, out float size);
            private static FunctionWrapper<GdipGetFontSize_delegate> GdipGetFontSize_ptr;
            internal static Status GdipGetFontSize(IntPtr font, out float size) => GdipGetFontSize_ptr.Delegate(font, out size);

            private delegate Status GdipGetFontHeight_delegate(IntPtr font, IntPtr graphics, out float height);
            private static FunctionWrapper<GdipGetFontHeight_delegate> GdipGetFontHeight_ptr;
            internal static Status GdipGetFontHeight(IntPtr font, IntPtr graphics, out float height) => GdipGetFontHeight_ptr.Delegate(font, graphics, out height);

            private delegate Status GdipGetFontHeightGivenDPI_delegate(IntPtr font, float dpi, out float height);
            private static FunctionWrapper<GdipGetFontHeightGivenDPI_delegate> GdipGetFontHeightGivenDPI_ptr;
            internal static Status GdipGetFontHeightGivenDPI(IntPtr font, float dpi, out float height) => GdipGetFontHeightGivenDPI_ptr.Delegate(font, dpi, out height);

            private delegate Status GdipCreateMetafileFromFile_delegate([MarshalAs(UnmanagedType.LPWStr)]string filename, out IntPtr metafile);
            private static FunctionWrapper<GdipCreateMetafileFromFile_delegate> GdipCreateMetafileFromFile_ptr;
            internal static Status GdipCreateMetafileFromFile(string filename, out IntPtr metafile) => GdipCreateMetafileFromFile_ptr.Delegate(filename, out metafile);

            private delegate Status GdipCreateMetafileFromEmf_delegate(IntPtr hEmf, bool deleteEmf, out IntPtr metafile);
            private static FunctionWrapper<GdipCreateMetafileFromEmf_delegate> GdipCreateMetafileFromEmf_ptr;
            internal static Status GdipCreateMetafileFromEmf(IntPtr hEmf, bool deleteEmf, out IntPtr metafile) => GdipCreateMetafileFromEmf_ptr.Delegate(hEmf, deleteEmf, out metafile);

            private delegate Status GdipCreateMetafileFromWmf_delegate(IntPtr hWmf, bool deleteWmf, WmfPlaceableFileHeader wmfPlaceableFileHeader, out IntPtr metafile);
            private static FunctionWrapper<GdipCreateMetafileFromWmf_delegate> GdipCreateMetafileFromWmf_ptr;
            internal static Status GdipCreateMetafileFromWmf(IntPtr hWmf, bool deleteWmf, WmfPlaceableFileHeader wmfPlaceableFileHeader, out IntPtr metafile) => GdipCreateMetafileFromWmf_ptr.Delegate(hWmf, deleteWmf, wmfPlaceableFileHeader, out metafile);

            private delegate Status GdipGetMetafileHeaderFromFile_delegate([MarshalAs(UnmanagedType.LPWStr)]string filename, IntPtr header);
            private static FunctionWrapper<GdipGetMetafileHeaderFromFile_delegate> GdipGetMetafileHeaderFromFile_ptr;
            internal static Status GdipGetMetafileHeaderFromFile(string filename, IntPtr header) => GdipGetMetafileHeaderFromFile_ptr.Delegate(filename, header);

            private delegate Status GdipGetMetafileHeaderFromMetafile_delegate(IntPtr metafile, IntPtr header);
            private static FunctionWrapper<GdipGetMetafileHeaderFromMetafile_delegate> GdipGetMetafileHeaderFromMetafile_ptr;
            internal static Status GdipGetMetafileHeaderFromMetafile(IntPtr metafile, IntPtr header) => GdipGetMetafileHeaderFromMetafile_ptr.Delegate(metafile, header);

            private delegate Status GdipGetMetafileHeaderFromEmf_delegate(IntPtr hEmf, IntPtr header);
            private static FunctionWrapper<GdipGetMetafileHeaderFromEmf_delegate> GdipGetMetafileHeaderFromEmf_ptr;
            internal static Status GdipGetMetafileHeaderFromEmf(IntPtr hEmf, IntPtr header) => GdipGetMetafileHeaderFromEmf_ptr.Delegate(hEmf, header);

            private delegate Status GdipGetMetafileHeaderFromWmf_delegate(IntPtr hWmf, IntPtr wmfPlaceableFileHeader, IntPtr header);
            private static FunctionWrapper<GdipGetMetafileHeaderFromWmf_delegate> GdipGetMetafileHeaderFromWmf_ptr;
            internal static Status GdipGetMetafileHeaderFromWmf(IntPtr hWmf, IntPtr wmfPlaceableFileHeader, IntPtr header) => GdipGetMetafileHeaderFromWmf_ptr.Delegate(hWmf, wmfPlaceableFileHeader, header);

            private delegate Status GdipGetHemfFromMetafile_delegate(IntPtr metafile, out IntPtr hEmf);
            private static FunctionWrapper<GdipGetHemfFromMetafile_delegate> GdipGetHemfFromMetafile_ptr;
            internal static Status GdipGetHemfFromMetafile(IntPtr metafile, out IntPtr hEmf) => GdipGetHemfFromMetafile_ptr.Delegate(metafile, out hEmf);

            private delegate Status GdipGetMetafileDownLevelRasterizationLimit_delegate(IntPtr metafile, ref uint metafileRasterizationLimitDpi);
            private static FunctionWrapper<GdipGetMetafileDownLevelRasterizationLimit_delegate> GdipGetMetafileDownLevelRasterizationLimit_ptr;
            internal static Status GdipGetMetafileDownLevelRasterizationLimit(IntPtr metafile, ref uint metafileRasterizationLimitDpi) => GdipGetMetafileDownLevelRasterizationLimit_ptr.Delegate(metafile, ref metafileRasterizationLimitDpi);

            private delegate Status GdipSetMetafileDownLevelRasterizationLimit_delegate(IntPtr metafile, uint metafileRasterizationLimitDpi);
            private static FunctionWrapper<GdipSetMetafileDownLevelRasterizationLimit_delegate> GdipSetMetafileDownLevelRasterizationLimit_ptr;
            internal static Status GdipSetMetafileDownLevelRasterizationLimit(IntPtr metafile, uint metafileRasterizationLimitDpi) => GdipSetMetafileDownLevelRasterizationLimit_ptr.Delegate(metafile, metafileRasterizationLimitDpi);

            private delegate Status GdipPlayMetafileRecord_delegate(IntPtr metafile, EmfPlusRecordType recordType, int flags, int dataSize, byte[] data);
            private static FunctionWrapper<GdipPlayMetafileRecord_delegate> GdipPlayMetafileRecord_ptr;
            internal static Status GdipPlayMetafileRecord(IntPtr metafile, EmfPlusRecordType recordType, int flags, int dataSize, byte[] data) => GdipPlayMetafileRecord_ptr.Delegate(metafile, recordType, flags, dataSize, data);

            private delegate Status GdipRecordMetafile_delegate(IntPtr hdc, EmfType type, ref RectangleF frameRect, MetafileFrameUnit frameUnit, [MarshalAs(UnmanagedType.LPWStr)]string description, out IntPtr metafile);
            private static FunctionWrapper<GdipRecordMetafile_delegate> GdipRecordMetafile_ptr;
            internal static Status GdipRecordMetafile(IntPtr hdc, EmfType type, ref RectangleF frameRect, MetafileFrameUnit frameUnit, string description, out IntPtr metafile) => GdipRecordMetafile_ptr.Delegate(hdc, type, ref frameRect, frameUnit, description, out metafile);

            private delegate Status GdipRecordMetafileI_delegate(IntPtr hdc, EmfType type, ref Rectangle frameRect, MetafileFrameUnit frameUnit, [MarshalAs(UnmanagedType.LPWStr)]string description, out IntPtr metafile);
            private static FunctionWrapper<GdipRecordMetafileI_delegate> GdipRecordMetafileI_ptr;
            internal static Status GdipRecordMetafileI(IntPtr hdc, EmfType type, ref Rectangle frameRect, MetafileFrameUnit frameUnit, string description, out IntPtr metafile) => GdipRecordMetafileI_ptr.Delegate(hdc, type, ref frameRect, frameUnit, description, out metafile);

            private delegate Status GdipRecordMetafileFileName_delegate([MarshalAs(UnmanagedType.LPWStr)]string filename, IntPtr hdc, EmfType type, ref RectangleF frameRect, MetafileFrameUnit frameUnit, [MarshalAs(UnmanagedType.LPWStr)]string description, out IntPtr metafile);
            private static FunctionWrapper<GdipRecordMetafileFileName_delegate> GdipRecordMetafileFileName_ptr;
            internal static Status GdipRecordMetafileFileName(string filename, IntPtr hdc, EmfType type, ref RectangleF frameRect, MetafileFrameUnit frameUnit, string description, out IntPtr metafile) => GdipRecordMetafileFileName_ptr.Delegate(filename, hdc, type, ref frameRect, frameUnit, description, out metafile);

            private delegate Status GdipRecordMetafileFileNameI_delegate([MarshalAs(UnmanagedType.LPWStr)]string filename, IntPtr hdc, EmfType type, ref Rectangle frameRect, MetafileFrameUnit frameUnit, [MarshalAs(UnmanagedType.LPWStr)]string description, out IntPtr metafile);
            private static FunctionWrapper<GdipRecordMetafileFileNameI_delegate> GdipRecordMetafileFileNameI_ptr;
            internal static Status GdipRecordMetafileFileNameI(string filename, IntPtr hdc, EmfType type, ref Rectangle frameRect, MetafileFrameUnit frameUnit, string description, out IntPtr metafile) => GdipRecordMetafileFileNameI_ptr.Delegate(filename, hdc, type, ref frameRect, frameUnit, description, out metafile);

            private delegate Status GdipCreateMetafileFromStream_delegate([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ComIStreamMarshaler))]IStream stream, out IntPtr metafile);
            private static FunctionWrapper<GdipCreateMetafileFromStream_delegate> GdipCreateMetafileFromStream_ptr;
            internal static Status GdipCreateMetafileFromStream(IStream stream, out IntPtr metafile) => GdipCreateMetafileFromStream_ptr.Delegate(stream, out metafile);

            private delegate Status GdipGetMetafileHeaderFromStream_delegate([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ComIStreamMarshaler))]IStream stream, IntPtr header);
            private static FunctionWrapper<GdipGetMetafileHeaderFromStream_delegate> GdipGetMetafileHeaderFromStream_ptr;
            internal static Status GdipGetMetafileHeaderFromStream(IStream stream, IntPtr header) => GdipGetMetafileHeaderFromStream_ptr.Delegate(stream, header);

            private delegate Status GdipRecordMetafileStream_delegate([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ComIStreamMarshaler))]IStream stream, IntPtr hdc, EmfType type, ref RectangleF frameRect, MetafileFrameUnit frameUnit, [MarshalAs(UnmanagedType.LPWStr)]string description, out IntPtr metafile);
            private static FunctionWrapper<GdipRecordMetafileStream_delegate> GdipRecordMetafileStream_ptr;
            internal static Status GdipRecordMetafileStream(IStream stream, IntPtr hdc, EmfType type, ref RectangleF frameRect, MetafileFrameUnit frameUnit, string description, out IntPtr metafile) => GdipRecordMetafileStream_ptr.Delegate(stream, hdc, type, ref frameRect, frameUnit, description, out metafile);

            private delegate Status GdipRecordMetafileStreamI_delegate([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ComIStreamMarshaler))]IStream stream, IntPtr hdc, EmfType type, ref Rectangle frameRect, MetafileFrameUnit frameUnit, [MarshalAs(UnmanagedType.LPWStr)]string description, out IntPtr metafile);
            private static FunctionWrapper<GdipRecordMetafileStreamI_delegate> GdipRecordMetafileStreamI_ptr;
            internal static Status GdipRecordMetafileStreamI(IStream stream, IntPtr hdc, EmfType type, ref Rectangle frameRect, MetafileFrameUnit frameUnit, string description, out IntPtr metafile) => GdipRecordMetafileStreamI_ptr.Delegate(stream, hdc, type, ref frameRect, frameUnit, description, out metafile);

            private delegate Status GdipCreateFromContext_macosx_delegate(IntPtr cgref, int width, int height, out IntPtr graphics);
            private static FunctionWrapper<GdipCreateFromContext_macosx_delegate> GdipCreateFromContext_macosx_ptr;
            internal static Status GdipCreateFromContext_macosx(IntPtr cgref, int width, int height, out IntPtr graphics) => GdipCreateFromContext_macosx_ptr.Delegate(cgref, width, height, out graphics);

            private delegate Status GdipSetVisibleClip_linux_delegate(IntPtr graphics, ref Rectangle rect);
            private static FunctionWrapper<GdipSetVisibleClip_linux_delegate> GdipSetVisibleClip_linux_ptr;
            internal static Status GdipSetVisibleClip_linux(IntPtr graphics, ref Rectangle rect) => GdipSetVisibleClip_linux_ptr.Delegate(graphics, ref rect);

            private delegate Status GdipCreateFromXDrawable_linux_delegate(IntPtr drawable, IntPtr display, out IntPtr graphics);
            private static FunctionWrapper<GdipCreateFromXDrawable_linux_delegate> GdipCreateFromXDrawable_linux_ptr;
            internal static Status GdipCreateFromXDrawable_linux(IntPtr drawable, IntPtr display, out IntPtr graphics) => GdipCreateFromXDrawable_linux_ptr.Delegate(drawable, display, out graphics);

            // Stream functions for non-Win32 (libgdiplus specific)
            private delegate Status GdipLoadImageFromDelegate_linux_delegate(StreamGetHeaderDelegate getHeader,
                StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
                StreamCloseDelegate close, StreamSizeDelegate size, out IntPtr image);
            private static FunctionWrapper<GdipLoadImageFromDelegate_linux_delegate> GdipLoadImageFromDelegate_linux_ptr;
            internal static Status GdipLoadImageFromDelegate_linux(StreamGetHeaderDelegate getHeader,
                StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
                StreamCloseDelegate close, StreamSizeDelegate size, out IntPtr image)
                => GdipLoadImageFromDelegate_linux_ptr.Delegate(getHeader, getBytes, putBytes, doSeek, close, size, out image);

            private delegate Status GdipSaveImageToDelegate_linux_delegate(IntPtr image, StreamGetBytesDelegate getBytes,
                StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek, StreamCloseDelegate close,
                StreamSizeDelegate size, ref Guid encoderClsID, IntPtr encoderParameters);
            private static FunctionWrapper<GdipSaveImageToDelegate_linux_delegate> GdipSaveImageToDelegate_linux_ptr;
            internal static Status GdipSaveImageToDelegate_linux(IntPtr image, StreamGetBytesDelegate getBytes,
                StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek, StreamCloseDelegate close,
                StreamSizeDelegate size, ref Guid encoderClsID, IntPtr encoderParameters)
                => GdipSaveImageToDelegate_linux_ptr.Delegate(image, getBytes, putBytes, doSeek, close, size, ref encoderClsID, encoderParameters);

            private delegate Status GdipCreateMetafileFromDelegate_linux_delegate(StreamGetHeaderDelegate getHeader,
                StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
                StreamCloseDelegate close, StreamSizeDelegate size, out IntPtr metafile);
            private static FunctionWrapper<GdipCreateMetafileFromDelegate_linux_delegate> GdipCreateMetafileFromDelegate_linux_ptr;
            internal static Status GdipCreateMetafileFromDelegate_linux(StreamGetHeaderDelegate getHeader,
                StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
                StreamCloseDelegate close, StreamSizeDelegate size, out IntPtr metafile)
                => GdipCreateMetafileFromDelegate_linux_ptr.Delegate(getHeader, getBytes, putBytes, doSeek, close, size, out metafile);

            private delegate Status GdipGetMetafileHeaderFromDelegate_linux_delegate(StreamGetHeaderDelegate getHeader,
                StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
                StreamCloseDelegate close, StreamSizeDelegate size, IntPtr header);
            private static FunctionWrapper<GdipGetMetafileHeaderFromDelegate_linux_delegate> GdipGetMetafileHeaderFromDelegate_linux_ptr;
            internal static Status GdipGetMetafileHeaderFromDelegate_linux(StreamGetHeaderDelegate getHeader,
                StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
                StreamCloseDelegate close, StreamSizeDelegate size, IntPtr header)
                => GdipGetMetafileHeaderFromDelegate_linux_ptr.Delegate(getHeader, getBytes, putBytes, doSeek, close, size, header);

            private delegate Status GdipRecordMetafileFromDelegate_linux_delegate(StreamGetHeaderDelegate getHeader,
                StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
                StreamCloseDelegate close, StreamSizeDelegate size, IntPtr hdc, EmfType type, ref RectangleF frameRect,
                MetafileFrameUnit frameUnit, [MarshalAs(UnmanagedType.LPWStr)] string description, out IntPtr metafile);
            private static FunctionWrapper<GdipRecordMetafileFromDelegate_linux_delegate> GdipRecordMetafileFromDelegate_linux_ptr;
            internal static Status GdipRecordMetafileFromDelegate_linux(StreamGetHeaderDelegate getHeader,
                StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
                StreamCloseDelegate close, StreamSizeDelegate size, IntPtr hdc, EmfType type, ref RectangleF frameRect,
                MetafileFrameUnit frameUnit, string description, out IntPtr metafile)
                => GdipRecordMetafileFromDelegate_linux_ptr.Delegate(getHeader, getBytes, putBytes, doSeek, close, size, hdc, type, ref frameRect, frameUnit, description, out metafile);

            private delegate Status GdipRecordMetafileFromDelegateI_linux_delegate(StreamGetHeaderDelegate getHeader,
                StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
                StreamCloseDelegate close, StreamSizeDelegate size, IntPtr hdc, EmfType type, ref Rectangle frameRect,
                MetafileFrameUnit frameUnit, [MarshalAs(UnmanagedType.LPWStr)] string description, out IntPtr metafile);
            private static FunctionWrapper<GdipRecordMetafileFromDelegateI_linux_delegate> GdipRecordMetafileFromDelegateI_linux_ptr;
            internal static Status GdipRecordMetafileFromDelegateI_linux(StreamGetHeaderDelegate getHeader,
                StreamGetBytesDelegate getBytes, StreamPutBytesDelegate putBytes, StreamSeekDelegate doSeek,
                StreamCloseDelegate close, StreamSizeDelegate size, IntPtr hdc, EmfType type, ref Rectangle frameRect,
                MetafileFrameUnit frameUnit, string description, out IntPtr metafile)
                => GdipRecordMetafileFromDelegateI_linux_ptr.Delegate(getHeader, getBytes, putBytes, doSeek, close, size, hdc, type, ref frameRect, frameUnit, description, out metafile);

            private delegate int GdipGetPostScriptGraphicsContext_delegate(string filename, int width, int height, double dpix, double dpiy, ref IntPtr graphics);
            private static FunctionWrapper<GdipGetPostScriptGraphicsContext_delegate> GdipGetPostScriptGraphicsContext_ptr;
            internal static int GdipGetPostScriptGraphicsContext([MarshalAs(UnmanagedType.LPStr)]string filename, int width, int height, double dpix, double dpiy, ref IntPtr graphics)
                => GdipGetPostScriptGraphicsContext_ptr.Delegate(filename, width, height, dpix, dpiy, ref graphics);

            private delegate int GdipGetPostScriptSavePage_delegate(IntPtr graphics);
            private static FunctionWrapper<GdipGetPostScriptSavePage_delegate> GdipGetPostScriptSavePage_ptr;
            internal static int GdipGetPostScriptSavePage(IntPtr graphics) => GdipGetPostScriptSavePage_ptr.Delegate(graphics);
        }
    }

    // These are unix-only
    internal delegate int StreamGetHeaderDelegate(IntPtr buf, int bufsz);
    internal delegate int StreamGetBytesDelegate(IntPtr buf, int bufsz, bool peek);
    internal delegate long StreamSeekDelegate(int offset, int whence);
    internal delegate int StreamPutBytesDelegate(IntPtr buf, int bufsz);
    internal delegate void StreamCloseDelegate();
    internal delegate long StreamSizeDelegate();
}
