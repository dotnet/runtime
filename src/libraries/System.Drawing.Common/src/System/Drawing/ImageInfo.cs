// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private class ImageInfo
        {
            private const int PropertyTagFrameDelay = 0x5100;

            private Image _image;
            private int _frame;
            private int _frameCount;
            private bool _frameDirty;
            private bool _animated;
            private EventHandler _onFrameChangedHandler;
            private int[] _frameDelay;
            private int _frameTimer;

            public ImageInfo(Image image)
            {
                _image = image;
                _animated = ImageAnimator.CanAnimate(image);

                if (_animated)
                {
                    _frameCount = image.GetFrameCount(FrameDimension.Time);

                    PropertyItem frameDelayItem = image.GetPropertyItem(PropertyTagFrameDelay);

                    // If the image does not have a frame delay, we just return 0.                                     
                    //
                    if (frameDelayItem != null)
                    {
                        // Convert the frame delay from byte[] to int
                        //
                        byte[] values = frameDelayItem.Value;
                        Debug.Assert(values.Length == 4 * FrameCount, "PropertyItem has invalid value byte array");
                        _frameDelay = new int[FrameCount];
                        for (int i = 0; i < FrameCount; ++i)
                        {
                            _frameDelay[i] = values[i * 4] + 256 * values[i * 4 + 1] + 256 * 256 * values[i * 4 + 2] + 256 * 256 * 256 * values[i * 4 + 3];
                        }
                    }
                }
                else
                {
                    _frameCount = 1;
                }
                if (_frameDelay == null)
                {
                    _frameDelay = new int[FrameCount];
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
            public int Frame
            {
                get
                {
                    return _frame;
                }
                set
                {
                    if (_frame != value)
                    {
                        if (value < 0 || value >= FrameCount)
                        {
                            throw new ArgumentException(SR.InvalidFrame, nameof(value));
                        }

                        if (Animated)
                        {
                            _frame = value;
                            _frameDirty = true;

                            OnFrameChanged(EventArgs.Empty);
                        }
                    }
                }
            }

            /// <summary>
            /// The current frame has not been updated.
            /// </summary>
            public bool FrameDirty
            {
                get
                {
                    return _frameDirty;
                }
            }

            public EventHandler FrameChangedHandler
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
            public int FrameCount
            {
                get
                {
                    return _frameCount;
                }
            }

            /// <summary>
            /// The delay associated with the frame at the specified index.
            /// </summary>
            public int FrameDelay(int frame)
            {
                return _frameDelay[frame];
            }

            internal int FrameTimer
            {
                get
                {
                    return _frameTimer;
                }
                set
                {
                    _frameTimer = value;
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
            protected void OnFrameChanged(EventArgs e)
            {
                if (_onFrameChangedHandler != null)
                {
                    _onFrameChangedHandler(_image, e);
                }
            }
        }
    }
}
