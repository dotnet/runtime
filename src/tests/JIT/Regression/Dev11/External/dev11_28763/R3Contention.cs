// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Runtime.CompilerServices;
using Xunit;


namespace R3Contention
{
    public struct Size
    {
        public Int32 width;
        public Int32 height;

        public Size(Int32 width, Int32 height)
        {
            this.width = width;
            this.height = height;
            return;
        }

        public static readonly Size Empty = new Size();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Size Subtract(Size sz1, Size sz2)
        {
            return Size.Empty;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Size Add(Size sz1, Size sz2)
        {
            return Size.Empty;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool AreEqual(Size sz1, Size sz2)
        {
            return ((sz1.width == sz2.width) && (sz1.height == sz2.height));
        }
    }




    public class LayoutOptions
    {
        public string text;
        public Int32 borderSize;
        public Int32 paddingSize;
        public Int32 checkSize;
        public Int32 checkPaddingSize;
        public Int32 textImageInset;
        public bool growBorderBy1PxWhenDefault;
        public bool disableWordWrapping;
        public Size imageSize;


        public int FullCheckSize { get { return (this.checkSize + this.checkPaddingSize); } }


        [MethodImpl(MethodImplOptions.NoInlining)]
        public Size Compose(Size checkSize, Size imageSize, Size textSize)
        {
            return Size.Empty;
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        public Size Decompose(Size checkSize, Size requiredImageSize, Size proposedSize)
        {
            return Size.Empty;
        }


        public virtual Size GetTextSize(Size proposedSize)
        {
            return Size.Empty;
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        public
        Size GetPreferredSizeCore(
            Size proposedSize
            )
        {
            int linearBorderAndPadding = ((this.borderSize * 2) + (this.paddingSize * 2));

            if (this.growBorderBy1PxWhenDefault)
            {
                linearBorderAndPadding += 2;
            }



            Size bordersAndPadding = new Size(linearBorderAndPadding, linearBorderAndPadding);

            proposedSize = Size.Subtract(proposedSize, bordersAndPadding);



            int checkSizeLinear = this.FullCheckSize;

            Size checkSize =
                (checkSizeLinear > 0) ?
                    new Size(checkSizeLinear + 1, checkSizeLinear) :
                    Size.Empty;




            Size textImageInsetSize = new Size(this.textImageInset * 2, this.textImageInset * 2);

            Size requiredImageSize =
                (!Size.AreEqual(this.imageSize, Size.Empty)) ?
                    Size.Add(this.imageSize, textImageInsetSize) :
                    Size.Empty;



            proposedSize = Size.Subtract(proposedSize, textImageInsetSize);

            proposedSize = this.Decompose(checkSize, requiredImageSize, proposedSize);



            Size textSize = Size.Empty;


            if (!string.IsNullOrEmpty(this.text))
            {
                try
                {
                    this.disableWordWrapping = true;
                    textSize = Size.Add(this.GetTextSize(proposedSize), textImageInsetSize);
                }
                finally
                {
                    this.disableWordWrapping = false;
                }
            }



            Size requiredSize = this.Compose(checkSize, this.imageSize, textSize);

            requiredSize = Size.Add(requiredSize, bordersAndPadding);



            return requiredSize;
        }
    }


    public static class App
    {
        [Fact]
        public static int TestEntryPoint()
        {
            var layoutOptions = new LayoutOptions();

            layoutOptions.text = "Some text.";

            layoutOptions.GetPreferredSizeCore(Size.Empty);
            return 100;
        }
    }
}
