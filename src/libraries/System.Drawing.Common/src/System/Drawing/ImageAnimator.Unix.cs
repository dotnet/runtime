// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// System.Drawing.ImageAnimator.cs
//
// Authors:
//    Dennis Hayes (dennish@Raytek.com)
//    Sanjay Gupta (gsanjay@novell.com)
//    Sebastien Pouliot  <sebastien@ximian.com>
//
// (C) 2002 Ximian, Inc
// Copyright (C) 2004,2006-2007 Novell, Inc (http://www.novell.com)
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

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Imaging;
using System.Threading;

namespace System.Drawing
{

    internal sealed class AnimateEventArgs : EventArgs
    {

        private int frameCount;
        private int activeFrame;
        private Thread? thread;

        public AnimateEventArgs(Image image)
        {
            frameCount = image.GetFrameCount(FrameDimension.Time);
        }

        public Thread? RunThread
        {
            get { return thread; }
            set { thread = value; }
        }

        public int GetNextFrame()
        {
            if (activeFrame < frameCount - 1)
                activeFrame++;
            else
                activeFrame = 0;

            return activeFrame;
        }
    }

    public sealed class ImageAnimator
    {

        private static Hashtable ht = Hashtable.Synchronized(new Hashtable());

        private ImageAnimator()
        {
        }

        public static void Animate(Image image, EventHandler onFrameChangedHandler)
        {
            // must be non-null and contain animation time frames
            if (!CanAnimate(image))
                return;

            // is animation already in progress ?
            if (ht.ContainsKey(image))
                return;

            PropertyItem item = image.GetPropertyItem(0x5100)!; // FrameDelay in libgdiplus
            byte[] value = item.Value!;
            int[] delay = new int[(value.Length >> 2)];
            for (int i = 0, n = 0; i < value.Length; i += 4, n++)
            {
                int d = BitConverter.ToInt32(value, i) * 10;
                // follow worse case (Opera) see http://news.deviantart.com/article/27613/
                delay[n] = d < 100 ? 100 : d;
            }

            AnimateEventArgs aea = new AnimateEventArgs(image);
            WorkerThread wt = new WorkerThread(onFrameChangedHandler, aea, delay);
            Thread thread = new Thread(new ThreadStart(wt.LoopHandler));
            thread.IsBackground = true;
            aea.RunThread = thread;
            ht.Add(image, aea);
            thread.Start();
        }

        public static bool CanAnimate([NotNullWhen(true)] Image? image)
        {
            if (image == null)
                return false;

            int n = image.FrameDimensionsList.Length;
            if (n < 1)
                return false;

            for (int i = 0; i < n; i++)
            {
                if (image.FrameDimensionsList[i].Equals(FrameDimension.Time.Guid))
                {
                    return (image.GetFrameCount(FrameDimension.Time) > 1);
                }
            }
            return false;
        }

        public static void StopAnimate(Image image, EventHandler onFrameChangedHandler)
        {
            if (image == null)
                return;

            if (ht.ContainsKey(image))
            {
                AnimateEventArgs evtArgs = (AnimateEventArgs)ht[image]!;
#pragma warning disable SYSLIB0006 // https://github.com/dotnet/runtime/issues/39405
                evtArgs.RunThread!.Abort();
#pragma warning restore SYSLIB0006
                ht.Remove(image);
            }
        }

        public static void UpdateFrames()
        {
            foreach (Image? image in ht.Keys)
                UpdateImageFrame(image!);
        }


        public static void UpdateFrames(Image image)
        {
            if (image == null)
                return;

            if (ht.ContainsKey(image))
                UpdateImageFrame(image);
        }

        // this method avoid checks that aren't requied for UpdateFrames()
        private static void UpdateImageFrame(Image image)
        {
            AnimateEventArgs aea = (AnimateEventArgs)ht[image]!;
            image.SelectActiveFrame(FrameDimension.Time, aea.GetNextFrame());
        }
    }

    internal sealed class WorkerThread
    {

        private EventHandler frameChangeHandler;
        private AnimateEventArgs animateEventArgs;
        private int[] delay;

        public WorkerThread(EventHandler frmChgHandler, AnimateEventArgs aniEvtArgs, int[] delay)
        {
            frameChangeHandler = frmChgHandler;
            animateEventArgs = aniEvtArgs;
            this.delay = delay;
        }

        public void LoopHandler()
        {
            int n = 0;
            while (true)
            {
                Thread.Sleep(delay[n++]);
                frameChangeHandler(null, animateEventArgs);
                if (n == delay.Length)
                    n = 0;
            }
        }
    }
}
