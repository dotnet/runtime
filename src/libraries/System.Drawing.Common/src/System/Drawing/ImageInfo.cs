// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Drawing.Imaging;

namespace System.Drawing
{
    /// <summary>
    /// Animates one or more images that have time-based frames. This file contains the nested ImageInfo class
    /// - See ImageAnimator.cs for the definition of the outer class.
    /// </summary>
    public sealed partial class ImageAnimator
    {
        /// <summary>
        /// ImageAnimator nested helper class used to store extra image state info.
        /// </summary>
        private sealed class ImageInfo
        {
            private const int PropertyTagFrameDelay = 0x5100;

            private readonly Image _image;
            private int _frame;
            private readonly int _frameCount;
            private bool _frameDirty;
            private readonly bool _animated;
            private EventHandler? _onFrameChangedHandler;
            private readonly int[] _frameEndTimes;
            private int _frameTimer;

            public ImageInfo(Image image)
            {
                _image = image;
                _animated = ImageAnimator.CanAnimate(image);
                _frameEndTimes = null!; // guaranteed to be initialized by the final check

                if (_animated)
                {
                    _frameCount = image.GetFrameCount(FrameDimension.Time);

                    PropertyItem? frameDelayItem = image.GetPropertyItem(PropertyTagFrameDelay);

                    // If the image does not have a frame delay, we just return 0.
                    if (frameDelayItem != null)
                    {
                        // Convert the frame delay from byte[] to int
                        byte[] values = frameDelayItem.Value!;

                        // On Windows, we get the frame delays for every frame. On Linux, we only get the first frame delay.
                        // We handle this by treating the frame delays as a repeating sequence, asserting that the sequence
                        // is fully repeatable to match the frame count.
                        Debug.Assert(FrameCount % (values.Length / 4) == 0, "PropertyItem has invalid value byte array. The FrameCount should be evenly divisible by a quarter of the byte array's length.");

                        _frameEndTimes = new int[FrameCount];

                        for (int f = 0, i = 0; f < FrameCount; ++f, i += 4)
                        {
                            if (i == values.Length)
                            {
                                i = 0;
                            }

                            // Frame delays are stored in 1/100ths of a second; convert to milliseconds while accumulating
                            _frameEndTimes[f] = (f > 0 ? _frameEndTimes[f - 1] : 0) + (BitConverter.ToInt32(values, i) * 10);
                        }
                    }
                }
                else
                {
                    _frameCount = 1;
                }
                if (_frameEndTimes == null)
                {
                    _frameEndTimes = new int[FrameCount];
                }
            }

            /// <summary>
            /// Whether the image supports animation.
            /// </summary>
            public bool Animated
            {
                get
                {
                    return _animated;
                }
            }

            /// <summary>
            /// The current frame.
            /// </summary>
            private int Frame
            {
                get
                {
                    return _frame;
                }
            }

            /// <summary>
            /// The current frame has changed but the image has not yet been updated.
            /// </summary>
            public bool FrameDirty
            {
                get
                {
                    return _frameDirty;
                }
            }

            public EventHandler? FrameChangedHandler
            {
                get
                {
                    return _onFrameChangedHandler;
                }
                set
                {
                    _onFrameChangedHandler = value;
                }
            }

            /// <summary>
            /// The number of frames in the image.
            /// </summary>
            private int FrameCount
            {
                get
                {
                    return _frameCount;
                }
            }

            /// <summary>
            /// The total animation time of the image, in milliseconds.
            /// </summary>
            private int TotalAnimationTime
            {
                get
                {
                    if (Animated)
                    {
                        return _frameEndTimes[_frameCount - 1];
                    }

                    return 0;
                }
            }

            /// <summary>
            /// Advance the animation by the specified number of milliseconds. If the advancement
            /// progresses beyond the end time of the current Frame, then <see cref="Frame"/> will
            /// be updated and the <see cref="FrameChangedHandler"/> will be called. Subscribed
            /// handlers often use that event to call <see cref="ImageAnimator.UpdateFrames(Image)"/>.
            /// <para>
            /// If the animation progresses beyond the end of the image's total animation time,
            /// the animation will loop.
            /// </para>
            /// </summary>
            /// <remarks>
            /// This animation does not respect a GIF's specified number of animation repeats;
            /// instead, animations loop indefinitely.
            /// </remarks>
            /// <param name="milliseconds">The number of milliseconds to advance the animation by</param>
            public void AdvanceAnimationBy(int milliseconds)
            {
                int oldFrame = _frame;
                _frameTimer += milliseconds;

                if (_frameTimer > TotalAnimationTime)
                {
                    _frameTimer %= TotalAnimationTime;
                }

                // If the timer is before the current frame's start time, loop
                if (_frame > 0 && _frameTimer < _frameEndTimes[_frame - 1])
                {
                    _frame = 0;
                }

                while (_frameTimer > _frameEndTimes[_frame])
                {
                    _frame++;
                }

                if (_frame != oldFrame)
                {
                    _frameDirty = true;
                    OnFrameChanged(EventArgs.Empty);
                }
            }

            /// <summary>
            /// The image this object wraps.
            /// </summary>
            internal Image Image
            {
                get
                {
                    return _image;
                }
            }

            /// <summary>
            /// Selects the current frame as the active frame in the image.
            /// </summary>
            internal void UpdateFrame()
            {
                if (_frameDirty)
                {
                    _image.SelectActiveFrame(FrameDimension.Time, Frame);
                    _frameDirty = false;
                }
            }

            /// <summary>
            /// Raises the FrameChanged event.
            /// </summary>
            private void OnFrameChanged(EventArgs e)
            {
                if (_onFrameChangedHandler != null)
                {
                    _onFrameChangedHandler(_image, e);
                }
            }
        }
    }
}
