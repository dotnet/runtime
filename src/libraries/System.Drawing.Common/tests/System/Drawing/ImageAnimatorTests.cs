// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Drawing.Tests
{
    public class ImageAnimatorTests
    {
        [Fact]
        public void UpdateFrames_Succeeds_WithNothingAnimating()
        {
            ImageAnimator.UpdateFrames();
        }

        [Theory]
        [InlineData("1bit.png")]
        [InlineData("48x48_one_entry_1bit.ico")]
        [InlineData("81773-interlaced.gif")]
        public void CanAnimate_ReturnsFalse_ForNonAnimatedImages(string imageName)
        {
            using (var image = new Bitmap(Helpers.GetTestBitmapPath(imageName)))
            {
                Assert.False(ImageAnimator.CanAnimate(image));
            }
        }

        [Fact]
        public void Animate_Succeeds_ForNonAnimatedImages_WithNothingAnimating()
        {
            var image = new Bitmap(Helpers.GetTestBitmapPath("1bit.png"));
            ImageAnimator.Animate(image, (object o, EventArgs e) => { });
        }

        [Fact]
        public void Animate_Succeeds_ForNonAnimatedImages_WithCurrentAnimations()
        {
            var animatedImage = new Bitmap(Helpers.GetTestBitmapPath("animated-timer-100fps-repeat-2.gif"));
            ImageAnimator.Animate(animatedImage, (object o, EventArgs e) => { });

            var image = new Bitmap(Helpers.GetTestBitmapPath("1bit.png"));
            ImageAnimator.Animate(image, (object o, EventArgs e) => { });
        }

        [Fact]
        public void UpdateFrames_Succeeds_ForNonAnimatedImages_WithNothingAnimating()
        {
            var image = new Bitmap(Helpers.GetTestBitmapPath("1bit.png"));
            ImageAnimator.UpdateFrames(image);
        }

        [Fact]
        public void UpdateFrames_Succeeds_ForNonAnimatedImages_WithCurrentAnimations()
        {
            var animatedImage = new Bitmap(Helpers.GetTestBitmapPath("animated-timer-100fps-repeat-2.gif"));
            ImageAnimator.Animate(animatedImage, (object o, EventArgs e) => { });

            var image = new Bitmap(Helpers.GetTestBitmapPath("1bit.png"));
            ImageAnimator.UpdateFrames(image);
        }

        [Fact]
        public void StopAnimate_Succeeds_ForNonAnimatedImages_WithNothingAnimating()
        {
            var image = new Bitmap(Helpers.GetTestBitmapPath("1bit.png"));
            ImageAnimator.StopAnimate(image, (object o, EventArgs e) => { });
        }

        [Fact]
        public void StopAnimate_Succeeds_ForNonAnimatedImages_WithCurrentAnimations()
        {
            var animatedImage = new Bitmap(Helpers.GetTestBitmapPath("animated-timer-100fps-repeat-2.gif"));
            ImageAnimator.Animate(animatedImage, (object o, EventArgs e) => { });

            var image = new Bitmap(Helpers.GetTestBitmapPath("1bit.png"));
            ImageAnimator.StopAnimate(image, (object o, EventArgs e) => { });
        }

        [Theory]
        [InlineData("animated-timer-1fps-repeat-2.gif")]
        [InlineData("animated-timer-1fps-repeat-infinite.gif")]
        [InlineData("animated-timer-10fps-repeat-2.gif")]
        [InlineData("animated-timer-10fps-repeat-infinite.gif")]
        [InlineData("animated-timer-100fps-repeat-2.gif")]
        [InlineData("animated-timer-100fps-repeat-infinite.gif")]
        public void CanAnimate_ReturnsTrue_ForAnimatedImages(string imageName)
        {
            using (var image = new Bitmap(Helpers.GetTestBitmapPath(imageName)))
            {
                Assert.True(ImageAnimator.CanAnimate(image));
            }
        }

        [Fact]
        public void Animate_Succeeds_ForAnimatedImages_WithNothingAnimating()
        {
            var image = new Bitmap(Helpers.GetTestBitmapPath("animated-timer-100fps-repeat-2.gif"));
            ImageAnimator.Animate(image, (object o, EventArgs e) => { });
        }

        [Fact]
        public void Animate_Succeeds_ForAnimatedImages_WithCurrentAnimations()
        {
            var animatedImage = new Bitmap(Helpers.GetTestBitmapPath("animated-timer-100fps-repeat-2.gif"));
            ImageAnimator.Animate(animatedImage, (object o, EventArgs e) => { });

            var image = new Bitmap(Helpers.GetTestBitmapPath("animated-timer-100fps-repeat-infinite.gif"));
            ImageAnimator.Animate(image, (object o, EventArgs e) => { });
        }

        [Fact]
        public void UpdateFrames_Succeeds_ForAnimatedImages_WithNothingAnimating()
        {
            var animatedImage = new Bitmap(Helpers.GetTestBitmapPath("animated-timer-100fps-repeat-2.gif"));
            ImageAnimator.UpdateFrames(animatedImage);
        }

        [Fact]
        public void UpdateFrames_Succeeds_WithCurrentAnimations()
        {
            var animatedImage = new Bitmap(Helpers.GetTestBitmapPath("animated-timer-100fps-repeat-2.gif"));
            ImageAnimator.Animate(animatedImage, (object o, EventArgs e) => { });
            ImageAnimator.UpdateFrames();
        }

        [Fact]
        public void UpdateFrames_Succeeds_ForAnimatedImages_WithCurrentAnimations()
        {
            var animatedImage = new Bitmap(Helpers.GetTestBitmapPath("animated-timer-100fps-repeat-2.gif"));
            ImageAnimator.Animate(animatedImage, (object o, EventArgs e) => { });
            ImageAnimator.UpdateFrames(animatedImage);
        }

        [Fact]
        public void StopAnimate_Succeeds_ForAnimatedImages_WithNothingAnimating()
        {
            var image = new Bitmap(Helpers.GetTestBitmapPath("animated-timer-100fps-repeat-2.gif"));
            ImageAnimator.StopAnimate(image, (object o, EventArgs e) => { });
        }

        [Fact]
        public void StopAnimate_Succeeds_ForAnimatedImages_WithCurrentAnimations()
        {
            var animatedImage = new Bitmap(Helpers.GetTestBitmapPath("animated-timer-100fps-repeat-2.gif"));
            ImageAnimator.Animate(animatedImage, (object o, EventArgs e) => { });

            var image = new Bitmap(Helpers.GetTestBitmapPath("animated-timer-100fps-repeat-infinite.gif"));
            ImageAnimator.StopAnimate(animatedImage, (object o, EventArgs e) => { });
            ImageAnimator.StopAnimate(image, (object o, EventArgs e) => { });
        }
    }
}
