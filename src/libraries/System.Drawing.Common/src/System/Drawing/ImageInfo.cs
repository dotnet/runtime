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
            private const int PropertyTagLoopCount = 0x5101;

            private readonly Image _image;
            private int _frame;
            private short _loop;
            private readonly int _frameCount;
            private readonly short _loopCount;
            private bool _frameDirty;
            private readonly bool _animated;
            private EventHandler? _onFrameChangedHandler;
            private readonly long[]? _frameEndTimes;
            private long _totalAnimationTime;
            private long _frameTimer;

            public ImageInfo(Image image)
            {
                _image = image;
                _animated = ImageAnimator.CanAnimate(image);
                _frameEndTimes = null;

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
                        Debug.Assert(values.Length % 4 == 0, "PropertyItem has an invalid value byte array. It should have a length evenly divisible by 4 to represent ints.");
                        Debug.Assert(_frameCount % (values.Length / 4) == 0, "PropertyItem has invalid value byte array. The FrameCount should be evenly divisible by a quarter of the byte array's length.");

                        _frameEndTimes = new long[_frameCount];
                        long lastEndTime = 0;

                        for (int f = 0, i = 0; f < _frameCount; ++f, i += 4)
                        {
                            if (i >= values.Length)
                            {
                                i = 0;
                            }

                            // Frame delays are stored in 1/100ths of a second; convert to milliseconds while accumulating
                            // Per spec, a frame delay can be 0 which is treated as a single animation tick
                            int delay = BitConverter.ToInt32(values, i) * 10;
                            lastEndTime += delay > 0 ? delay : ImageAnimator.AnimationDelayMS;

                            // Guard against overflows
                            if (lastEndTime < _totalAnimationTime)
                            {
                                lastEndTime = _totalAnimationTime;
                            }
                            else
                            {
                                _totalAnimationTime = lastEndTime;
                            }

                            _frameEndTimes[f] = lastEndTime;
                        }
                    }

                    PropertyItem? loopCountItem = image.GetPropertyItem(PropertyTagLoopCount);

                    if (loopCountItem != null)
                    {
                        // The loop count is a short where 0 = infinite, and a positive value indicates the
                        // number of times to loop. The animation will be shown 1 time more than the loop count.
                        byte[] values = loopCountItem.Value!;

                        Debug.Assert(values.Length == sizeof(short), "PropertyItem has an invalid byte array. It should represent a single short value.");
                        _loopCount = BitConverter.ToInt16(values);
                    }
                    else
                    {
                        _loopCount = 0;
                    }
                }
                else
                {
                    _frameCount = 1;
                }
            }

            /// <summary>
            /// Whether the image supports animation.
            /// </summary>
            public bool Animated => _animated;

            /// <summary>
            /// The current frame has changed but the image has not yet been updated.
            /// </summary>
            public bool FrameDirty => _frameDirty;

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
            /// The total animation time of the image in milliseconds, or <value>0</value> if not animated.
            /// </summary>
            private long TotalAnimationTime => Animated ? _totalAnimationTime : 0;

            /// <summary>
            /// Whether animation should progress, respecting the image's animation support
            /// and if there are animation frames or loops remaining.
            /// </summary>
            private bool ShouldAnimate => TotalAnimationTime > 0 ? (_loopCount == 0 || _loop <= _loopCount) : false;

            /// <summary>
            /// Advance the animation by the specified number of milliseconds. If the advancement
            /// progresses beyond the end time of the current Frame, <see cref="FrameChangedHandler"/>
            /// will be called. Subscribed handlers often use that event to call
            /// <see cref="ImageAnimator.UpdateFrames(Image)"/>.
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
            public void AdvanceAnimationBy(long milliseconds)
            {
                if (ShouldAnimate)
                {
                    int oldFrame = _frame;
                    _frameTimer += milliseconds;

                    if (_frameTimer > TotalAnimationTime)
                    {
                        _loop += (short)Math.DivRem(_frameTimer, TotalAnimationTime, out long newTimer);
                        _frameTimer = newTimer;

                        if (!ShouldAnimate)
                        {
                            // If we've finished looping, then freeze onto the last frame
                            _frame = _frameCount - 1;
                            _frameTimer = TotalAnimationTime;
                        }
                        else if (_frame > 0 && _frameTimer < _frameEndTimes![_frame - 1])
                        {
                            // If the loop put us before the current frame (which is common)
                            // then reset back to the first frame. We will then progress
                            // forward again from there (below).
                            _frame = 0;
                        }
                    }

                    while (_frameTimer > _frameEndTimes![_frame])
                    {
                        _frame++;
                    }

                    if (_frame != oldFrame)
                    {
                        _frameDirty = true;
                        OnFrameChanged(EventArgs.Empty);
                    }
                }
            }

            /// <summary>
            /// The image this object wraps.
            /// </summary>
            internal Image Image => _image;

            /// <summary>
            /// Selects the current frame as the active frame in the image.
            /// </summary>
            internal void UpdateFrame()
            {
                if (_frameDirty)
                {
                    _image.SelectActiveFrame(FrameDimension.Time, _frame);
                    _frameDirty = false;
                }
            }

            /// <summary>
            /// Raises the FrameChanged event.
            /// </summary>
            private void OnFrameChanged(EventArgs e)
            {
                _onFrameChangedHandler?.Invoke(_image, e);
            }
        }
    }
}
