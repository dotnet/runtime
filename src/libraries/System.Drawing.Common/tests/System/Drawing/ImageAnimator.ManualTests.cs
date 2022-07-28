// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using Xunit;

namespace System.Drawing.Tests
{
    public class ImageAnimatorManualTests
    {
        public static bool ManualTestsEnabled => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MANUAL_TESTS"));
        public static string OutputFolder = Path.Combine(Environment.CurrentDirectory, "ImageAnimatorManualTests", DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"));

        // To run these tests, change the working directory to src/libraries/System.Drawing.Common,
        // set the `MANUAL_TESTS` environment variable to any non-empty value, and run
        // `dotnet test --filter "ImageAnimatorManualTests"

        [ConditionalFact(Helpers.IsDrawingSupported, nameof(ManualTestsEnabled), Timeout = 75_000)]
        public void AnimateAndCaptureFrames()
        {
            // This test animates the test gifs that we have and waits 60 seconds
            // for the animations to progress. As the frame change events occur, we
            // capture snapshots of the current frame, essentially extracting the
            // frames from the GIF.

            // The animation should progress at the expected pace to stay synchronized
            // with the wall clock, and the animated timer images show the time duration
            // within the image itself, so this can be manually verified for accuracy.

            // The captured frames are stored in the `artifacts/bin/System.Drawing.Common.Tests`
            // folder for each configuration, and then under an `ImageAnimatorManualTests` folder
            // with a timestamped folder under that. Each animation image gets its own folder too.

            string[] images = new string[]
            {
                "animated-timer-1fps-repeat-2.gif",
                "animated-timer-1fps-repeat-infinite.gif",
                "animated-timer-10fps-repeat-2.gif",
                "animated-timer-10fps-repeat-infinite.gif",
                "animated-timer-100fps-repeat-2.gif",
                "animated-timer-100fps-repeat-infinite.gif",
                "animated-timer-0-delay-all-frames.gif",
            };

            Dictionary<string, EventHandler> handlers = new();
            Dictionary<string, int> frameIndexes = new();
            Dictionary<string, Bitmap> bitmaps = new();

            Stopwatch stopwatch = new();

            foreach (var imageName in images)
            {
                string testOutputFolder = Path.Combine(OutputFolder, Path.GetFileNameWithoutExtension(imageName));
                Directory.CreateDirectory(testOutputFolder);
                frameIndexes[imageName] = 0;

                handlers[imageName] = new EventHandler(new Action<object, EventArgs>((object o, EventArgs e) =>
                {
                    Bitmap animation = (Bitmap)o;
                    ImageAnimator.UpdateFrames(animation);

                    // We save captures using jpg so that:
                    // a) The images don't get saved as animated gifs again, and just a single frame is saved
                    // b) Saving pngs in this test on Linux was leading to sporadic GDI+ errors; Jpeg is more reliable
                    string timestamp = stopwatch.ElapsedMilliseconds.ToString("000000");
                    animation.Save(Path.Combine(testOutputFolder, $"{++frameIndexes[imageName]}_{timestamp}.jpg"), ImageFormat.Jpeg);
                }));

                bitmaps[imageName] = new Bitmap(Helpers.GetTestBitmapPath(imageName));
                ImageAnimator.Animate(bitmaps[imageName], handlers[imageName]);
            }

            stopwatch.Start();
            Thread.Sleep(60_000);

            foreach (var imageName in images)
            {
                ImageAnimator.StopAnimate(bitmaps[imageName], handlers[imageName]);
                bitmaps[imageName].Dispose();
            }
        }
    }
}
