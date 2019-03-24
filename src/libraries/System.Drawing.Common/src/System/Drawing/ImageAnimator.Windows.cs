// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing.Imaging;
    using System.Threading;

    /// <summary>
    ///     Animates one or more images that have time-based frames.
    ///     See the ImageInfo.cs file for the helper nested ImageInfo class.
    ///     
    ///     A common pattern for using this class is as follows (See PictureBox control):
    ///     1. The winform app (user's code) calls ImageAnimator.Animate() from the main thread.
    ///     2. Animate() spawns the animating (worker) thread in the background, which will update the image 
    ///        frames and raise the OnFrameChanged event, which handler will be executed in the main thread.
    ///     3. The main thread triggers a paint event (Invalidate()) from the OnFrameChanged handler.
    ///     4. From the OnPaint event, the main thread calls ImageAnimator.UpdateFrames() and then paints the 
    ///        image (updated frame).
    ///     5. The main thread calls ImageAnimator.StopAnimate() when needed. This does not kill the worker thread.
    ///     
    ///     Comment on locking the image ref:
    ///     We need to synchronize access to sections of code that modify the image(s), but we don't want to block
    ///     animation of one image when modifying a different one; for this, we use the image ref for locking the
    ///     critical section (lock(image)).
    ///     
    ///     This class is safe for multi-threading but Image is not; multithreaded applications must use a critical
    ///     section lock using the image ref the image access is not from the same thread that executes ImageAnimator
    ///     code.  If the user code locks on the image ref forever a deadlock will happen preventing the animation 
    ///     from occurring.
    /// </summary>                                
    public sealed partial class ImageAnimator
    {
        /// <summary>
        ///     A list of images to be animated.    
        /// </summary>
        private static List<ImageInfo> s_imageInfoList;

        /// <summary>
        ///     A variable to flag when an image or images need to be updated due to the selection of a new frame
        ///     in an image.  We don't need to synchronize access to this variable, in the case it is true we don't
        ///     do anything, otherwise the worse case is where a thread attempts to update the image's frame after
        ///     another one did which is harmless.
        /// </summary>
        private static bool s_anyFrameDirty;

        /// <summary>
        ///     The thread used for animating the images.
        /// </summary>
        private static Thread s_animationThread;

        /// <summary>
        ///     Lock that allows either concurrent read-access to the images list for multiple threads, or write- 
        ///     access to it for a single thread.  Observe that synchronization access to image objects are done
        ///     with critical sections (lock).
        /// </summary>
        private static ReaderWriterLock s_rwImgListLock = new ReaderWriterLock();

        /// <summary>
        ///     Flag to avoid a deadlock when waiting on a write-lock and an attempt to acquire a read-lock is 
        ///     made in the same thread. If RWLock is currently owned by another thread, the current thread is going to wait on an 
        ///     event using CoWaitForMultipleHandles while pumps message. 
        ///     The comment above refers to the COM STA message pump, not to be confused with the UI message pump.
        ///     However, the effect is the same, the COM message pump will pump messages and dispatch them to the
        ///     window while waiting on the writer lock; this has the potential of creating a re-entrancy situation 
        ///     that if during the message processing a wait on a reader lock is originated the thread will be block 
        ///     on itself.
        ///     While processing STA message, the thread may call back into managed code. We do this because 
        ///     we can not block finalizer thread.  Finalizer thread may need to release STA objects on this thread. If 
        ///     the current thread does not pump message, finalizer thread is blocked, and AD  unload is blocked while 
        ///     waiting for finalizer thread. RWLock is a fair lock. If a thread waits for a writer lock, then it needs
        ///     a reader lock while pumping message, the thread is blocked forever.
        ///     This TLS variable is used to flag the above situation and avoid the deadlock, it is ThreadStatic so each
        ///     thread calling into ImageAnimator is guarded against this problem.
        /// </summary>




        [ThreadStatic]
        private static int t_threadWriterLockWaitCount;

        /// <summary>
        ///     Prevent instantiation of this class.
        /// </summary>
        private ImageAnimator()
        {
        }

        /// <summary>
        ///     Advances the frame in the specified image. The new frame is drawn the next time the image is rendered.
        /// </summary>
        public static void UpdateFrames(Image image)
        {
            if (!s_anyFrameDirty || image == null || s_imageInfoList == null)
            {
                return;
            }

            if (t_threadWriterLockWaitCount > 0)
            {
                // Cannot acquire reader lock - frame update will be missed.
                return;
            }

            // If the current thread already has the writer lock, no reader lock is acquired. Instead, the lock count on 
            // the writer lock is incremented. It already has a reader lock, the locks ref count will be incremented
            // w/o placing the request at the end of the reader queue.

            s_rwImgListLock.AcquireReaderLock(Timeout.Infinite);

            try
            {
                bool foundDirty = false;
                bool foundImage = false;

                foreach (ImageInfo imageInfo in s_imageInfoList)
                {
                    if (imageInfo.Image == image)
                    {
                        if (imageInfo.FrameDirty)
                        {
                            // See comment in the class header about locking the image ref.
#pragma warning disable CA2002
                            lock (imageInfo.Image)
                            {
#pragma warning restore CA2002
                                imageInfo.UpdateFrame();
                            }
                        }
                        foundImage = true;
                    }

                    if (imageInfo.FrameDirty)
                    {
                        foundDirty = true;
                    }

                    if (foundDirty && foundImage)
                    {
                        break;
                    }
                }

                s_anyFrameDirty = foundDirty;
            }
            finally
            {
                s_rwImgListLock.ReleaseReaderLock();
            }
        }

        /// <summary>
        ///     Advances the frame in all images currently being animated. The new frame is drawn the next time the image is rendered.
        /// </summary>
        public static void UpdateFrames()
        {
            if (!s_anyFrameDirty || s_imageInfoList == null)
            {
                return;
            }
            if (t_threadWriterLockWaitCount > 0)
            {
                // Cannot acquire reader lock at this time, frames update will be missed.
                return;
            }

            s_rwImgListLock.AcquireReaderLock(Timeout.Infinite);

            try
            {
                foreach (ImageInfo imageInfo in s_imageInfoList)
                {
                    // See comment in the class header about locking the image ref.
#pragma warning disable CA2002
                    lock (imageInfo.Image)
                    {
#pragma warning restore CA2002
                        imageInfo.UpdateFrame();
                    }
                }
                s_anyFrameDirty = false;
            }
            finally
            {
                s_rwImgListLock.ReleaseReaderLock();
            }
        }

        /// <summary>
        ///     Adds an image to the image manager.  If the image does not support animation this method does nothing.
        ///     This method creates the image list and spawns the animation thread the first time it is called.
        /// </summary>
        public static void Animate(Image image, EventHandler onFrameChangedHandler)
        {
            if (image == null)
            {
                return;
            }

            ImageInfo imageInfo = null;

            // See comment in the class header about locking the image ref.
#pragma warning disable CA2002
            lock (image)
            {
#pragma warning restore CA2002
                // could we avoid creating an ImageInfo object if FrameCount == 1 ?
                imageInfo = new ImageInfo(image);
            }

            // If the image is already animating, stop animating it
            StopAnimate(image, onFrameChangedHandler);

            // Acquire a writer lock to modify the image info list.  If the thread has a reader lock we need to upgrade
            // it to a writer lock; acquiring a reader lock in this case would block the thread on itself.  
            // If the thread already has a writer lock its ref count will be incremented w/o placing the request in the 
            // writer queue.  See ReaderWriterLock.AcquireWriterLock method in the MSDN.

            bool readerLockHeld = s_rwImgListLock.IsReaderLockHeld;
            LockCookie lockDowngradeCookie = new LockCookie();

            t_threadWriterLockWaitCount++;

            try
            {
                if (readerLockHeld)
                {
                    lockDowngradeCookie = s_rwImgListLock.UpgradeToWriterLock(Timeout.Infinite);
                }
                else
                {
                    s_rwImgListLock.AcquireWriterLock(Timeout.Infinite);
                }
            }
            finally
            {
                t_threadWriterLockWaitCount--;
                Debug.Assert(t_threadWriterLockWaitCount >= 0, "threadWriterLockWaitCount less than zero.");
            }

            try
            {
                if (imageInfo.Animated)
                {
                    // Construct the image array
                    //                               
                    if (s_imageInfoList == null)
                    {
                        s_imageInfoList = new List<ImageInfo>();
                    }

                    // Add the new image
                    //
                    imageInfo.FrameChangedHandler = onFrameChangedHandler;
                    s_imageInfoList.Add(imageInfo);

                    // Construct a new timer thread if we haven't already
                    //
                    if (s_animationThread == null)
                    {
                        s_animationThread = new Thread(new ThreadStart(AnimateImages50ms));
                        s_animationThread.Name = typeof(ImageAnimator).Name;
                        s_animationThread.IsBackground = true;
                        s_animationThread.Start();
                    }
                }
            }
            finally
            {
                if (readerLockHeld)
                {
                    s_rwImgListLock.DowngradeFromWriterLock(ref lockDowngradeCookie);
                }
                else
                {
                    s_rwImgListLock.ReleaseWriterLock();
                }
            }
        }

        /// <summary>
        ///    Whether or not the image has multiple time-based frames.
        /// </summary>
        public static bool CanAnimate(Image image)
        {
            if (image == null)
            {
                return false;
            }

            // See comment in the class header about locking the image ref.
#pragma warning disable CA2002
            lock (image)
            {
#pragma warning restore CA2002
                Guid[] dimensions = image.FrameDimensionsList;

                foreach (Guid guid in dimensions)
                {
                    FrameDimension dimension = new FrameDimension(guid);
                    if (dimension.Equals(FrameDimension.Time))
                    {
                        return image.GetFrameCount(FrameDimension.Time) > 1;
                    }
                }
            }

            return false;
        }

        /// <summary>
        ///     Removes an image from the image manager so it is no longer animated.
        /// </summary>
        public static void StopAnimate(Image image, EventHandler onFrameChangedHandler)
        {
            // Make sure we have a list of images                       
            if (image == null || s_imageInfoList == null)
            {
                return;
            }

            // Acquire a writer lock to modify the image info list - See comments on Animate() about this locking.

            bool readerLockHeld = s_rwImgListLock.IsReaderLockHeld;
            LockCookie lockDowngradeCookie = new LockCookie();

            t_threadWriterLockWaitCount++;

            try
            {
                if (readerLockHeld)
                {
                    lockDowngradeCookie = s_rwImgListLock.UpgradeToWriterLock(Timeout.Infinite);
                }
                else
                {
                    s_rwImgListLock.AcquireWriterLock(Timeout.Infinite);
                }
            }
            finally
            {
                t_threadWriterLockWaitCount--;
                Debug.Assert(t_threadWriterLockWaitCount >= 0, "threadWriterLockWaitCount less than zero.");
            }

            try
            {
                // Find the corresponding reference and remove it
                for (int i = 0; i < s_imageInfoList.Count; i++)
                {
                    ImageInfo imageInfo = s_imageInfoList[i];

                    if (image == imageInfo.Image)
                    {
                        if ((onFrameChangedHandler == imageInfo.FrameChangedHandler) || (onFrameChangedHandler != null && onFrameChangedHandler.Equals(imageInfo.FrameChangedHandler)))
                        {
                            s_imageInfoList.Remove(imageInfo);
                        }
                        break;
                    }
                }
            }
            finally
            {
                if (readerLockHeld)
                {
                    s_rwImgListLock.DowngradeFromWriterLock(ref lockDowngradeCookie);
                }
                else
                {
                    s_rwImgListLock.ReleaseWriterLock();
                }
            }
        }


        /// <summary>
        ///     Worker thread procedure which implements the main animation loop.
        ///     NOTE: This is the ONLY code the worker thread executes, keeping it in one method helps better understand 
        ///     any synchronization issues.  
        ///     WARNING: Also, this is the only place where ImageInfo objects (not the contained image object) are modified,
        ///     so no access synchronization is required to modify them.
        /// </summary>
        private static void AnimateImages50ms()
        {
            Debug.Assert(s_imageInfoList != null, "Null images list");

            while (true)
            {
                // Acquire reader-lock to access imageInfoList, elemens in the list can be modified w/o needing a writer-lock.
                // Observe that we don't need to check if the thread is waiting or a writer lock here since the thread this 
                // method runs in never acquires a writer lock.
                s_rwImgListLock.AcquireReaderLock(Timeout.Infinite);
                try
                {
                    for (int i = 0; i < s_imageInfoList.Count; i++)
                    {
                        ImageInfo imageInfo = s_imageInfoList[i];

                        // Frame delay is measured in 1/100ths of a second. This thread
                        // sleeps for 50 ms = 5/100ths of a second between frame updates,
                        // so we increase the frame delay count 5/100ths of a second
                        // at a time.
                        //
                        imageInfo.FrameTimer += 5;
                        if (imageInfo.FrameTimer >= imageInfo.FrameDelay(imageInfo.Frame))
                        {
                            imageInfo.FrameTimer = 0;

                            if (imageInfo.Frame + 1 < imageInfo.FrameCount)
                            {
                                imageInfo.Frame++;
                            }
                            else
                            {
                                imageInfo.Frame = 0;
                            }

                            if (imageInfo.FrameDirty)
                            {
                                s_anyFrameDirty = true;
                            }
                        }
                    }
                }
                finally
                {
                    s_rwImgListLock.ReleaseReaderLock();
                }

                Thread.Sleep(50);
            }
        }
    }
}

