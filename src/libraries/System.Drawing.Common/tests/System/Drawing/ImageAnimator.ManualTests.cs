// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        [ConditionalTheory(nameof(ManualTestsEnabled))] // Performs GIF animation and captures frames for visual verification
        [InlineData("animated-timer-1fps-repeat-2.gif")]
        [InlineData("animated-timer-10fps-repeat-2.gif")]
        [InlineData("animated-timer-100fps-repeat-2.gif")]
        public void AnimateAndCaptureFrames(string imageName)
        {
            // This test animates the test gifs that we have and waits 30 seconds
            // for the animation to progress. As the frame change events occur, we
            // capture PNG snapshots of the current frame, essentially extracting
            // the frames from the GIF.

            // The animation should progress at the expected pace to stay synchronized
            // with the wall clock, and the animated timer images show the time duration
            // within the image itself, so this can be manually verified for accuracy.

            // The captured frames are stored in the artifacts/bin/System.Drawing.Common.Tests
            // folder for each configuration, and then under an `ImageAnimatorManualTests` folder
            // with a timestamped folder under that. Each animated gif then gets its own folder too.
            string testOutputFolder = Path.Combine(OutputFolder, Path.GetFileNameWithoutExtension(imageName));
            Directory.CreateDirectory(testOutputFolder);

            DateTime startTime = DateTime.Now;
            int frameIndex = 0;

            EventHandler frameChangedHandler = new EventHandler(new Action<object, EventArgs>((object o, EventArgs e) =>
            {
                Bitmap animation = (Bitmap)o;
                ImageAnimator.UpdateFrames(animation);

                // We save captures using jpg so that:
                // a) The images don't get saved as animated gifs again, and just a single frame is saved
                // b) Saving pngs in this test on Linux was leading to sporadic GDI+ errors; Jpeg is more reliable
                string timestamp = (DateTime.Now - startTime).TotalMilliseconds.ToString("000000");
                animation.Save(Path.Combine(testOutputFolder, $"{frameIndex++}_{timestamp}.jpg"), ImageFormat.Jpeg);
            }));

            using (Bitmap image = new(Helpers.GetTestBitmapPath(imageName)))
            {
                ImageAnimator.Animate(image, frameChangedHandler);
                Thread.Sleep(30_000);
                ImageAnimator.StopAnimate(image, frameChangedHandler);
            }
        }
    }
}
