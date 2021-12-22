// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing.Drawing2D
{
    public sealed class HatchBrush : Brush
    {
        public HatchBrush(HatchStyle hatchstyle, Color foreColor) : this(hatchstyle, foreColor, Color.FromArgb(unchecked((int)0xff000000)))
        {
        }

        public HatchBrush(HatchStyle hatchstyle, Color foreColor, Color backColor)
        {
            if (hatchstyle < HatchStyle.Min || hatchstyle > HatchStyle.SolidDiamond)
            {
                throw new ArgumentException(SR.Format(SR.InvalidEnumArgument, nameof(hatchstyle), hatchstyle, nameof(HatchStyle)), nameof(hatchstyle));
            }

            SafeBrushHandle nativeBrush;
            int status = Gdip.GdipCreateHatchBrush(unchecked((int)hatchstyle), foreColor.ToArgb(), backColor.ToArgb(), out nativeBrush);
            Gdip.CheckStatus(status);

            SetNativeBrushInternal(nativeBrush);
        }

        internal HatchBrush(SafeBrushHandle nativeBrush) : base(nativeBrush)
        {
        }

        public override object Clone()
        {
            SafeBrushHandle clonedBrush;
            int status = Gdip.GdipCloneBrush(SafeNativeBrush, out clonedBrush);
            Gdip.CheckStatus(status);

            return new HatchBrush(clonedBrush);
        }

        public HatchStyle HatchStyle
        {
            get
            {
                int hatchStyle;
                int status = Gdip.GdipGetHatchStyle(SafeNativeBrush, out hatchStyle);
                Gdip.CheckStatus(status);

                return (HatchStyle)hatchStyle;
            }
        }

        public Color ForegroundColor
        {
            get
            {
                int foregroundArgb;
                int status = Gdip.GdipGetHatchForegroundColor(SafeNativeBrush, out foregroundArgb);
                Gdip.CheckStatus(status);

                return Color.FromArgb(foregroundArgb);
            }
        }

        public Color BackgroundColor
        {
            get
            {
                int backgroundArgb;
                int status = Gdip.GdipGetHatchBackgroundColor(SafeNativeBrush, out backgroundArgb);
                Gdip.CheckStatus(status);

                return Color.FromArgb(backgroundArgb);
            }
        }
    }
}
