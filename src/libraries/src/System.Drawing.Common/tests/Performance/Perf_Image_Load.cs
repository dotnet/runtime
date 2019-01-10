// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using Microsoft.Xunit.Performance;
using Microsoft.DotNet.XUnitExtensions;

namespace System.Drawing.Tests
{
    public class Perf_Image_Load : RemoteExecutorTestBase
    {
        [Benchmark(InnerIterationCount = 100)]
        [ConditionalBenchmark(typeof(Helpers), nameof(Helpers.GetIsDrawingSupported))]
        public void Bitmap_FromStream()
        {
            var files = CreateTestImageFiles();

            using (FileStream bmpStream = new FileStream(files.BitmapPath, FileMode.Open, FileAccess.Read))
            using (FileStream jpgStream = new FileStream(files.JpegPath, FileMode.Open, FileAccess.Read))
            using (FileStream pngStream = new FileStream(files.PngPath, FileMode.Open, FileAccess.Read))
            {
                foreach (var iteration in Benchmark.Iterations)
                {
                    using (iteration.StartMeasurement())
                    {
                        for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        {
                            using (new Bitmap(bmpStream))
                            using (new Bitmap(jpgStream))
                            using (new Bitmap(pngStream))
                            {
                            }
                        }
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 100)]
        [ConditionalBenchmark(typeof(Helpers), nameof(Helpers.GetIsDrawingSupported))]
        public void Image_FromStream()
        {
            var files = CreateTestImageFiles();

            using (FileStream bmpStream = new FileStream(files.BitmapPath, FileMode.Open, FileAccess.Read))
            using (FileStream jpgStream = new FileStream(files.JpegPath, FileMode.Open, FileAccess.Read))
            using (FileStream pngStream = new FileStream(files.PngPath, FileMode.Open, FileAccess.Read))
            {
                foreach (var iteration in Benchmark.Iterations)
                {
                    using (iteration.StartMeasurement())
                    {
                        for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        {
                            using (Image.FromStream(bmpStream))
                            using (Image.FromStream(jpgStream))
                            using (Image.FromStream(pngStream))
                            {
                            }
                        }
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 100)]
        [ConditionalBenchmark(typeof(Helpers), nameof(Helpers.GetIsDrawingSupported))]
        public void Image_FromStream_NoValidation()
        {
            var files = CreateTestImageFiles();

            using (FileStream bmpStream = new FileStream(files.BitmapPath, FileMode.Open, FileAccess.Read))
            using (FileStream jpgStream = new FileStream(files.JpegPath, FileMode.Open, FileAccess.Read))
            using (FileStream pngStream = new FileStream(files.PngPath, FileMode.Open, FileAccess.Read))
            {
                foreach (var iteration in Benchmark.Iterations)
                {
                    using (iteration.StartMeasurement())
                    {
                        for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        {
                            using (Image.FromStream(bmpStream, false, false))
                            using (Image.FromStream(jpgStream, false, false))
                            using (Image.FromStream(pngStream, false, false))
                            {
                            }
                        }
                    }
                }
            }
        }

        [Benchmark(InnerIterationCount = 100)]
        [ConditionalBenchmark(typeof(Helpers), nameof(Helpers.GetIsDrawingSupported))]
        public void Image_FromStream_NoValidation_Gif()
        {
            // GIF has extra logic looking for animated GIFs
            string gifPath = CreateTestImageFile(ImageFormat.Gif);

            using (FileStream gifStream = new FileStream(gifPath, FileMode.Open, FileAccess.Read))
            {
                foreach (var iteration in Benchmark.Iterations)
                {
                    using (iteration.StartMeasurement())
                    {
                        for (int i = 0; i < Benchmark.InnerIterationCount; i++)
                        {
                            using (Image.FromStream(gifStream, false, false))
                            {
                            }
                        }
                    }
                }
            }
        }

        private (string BitmapPath, string JpegPath, string PngPath) CreateTestImageFiles()
        {
            return (CreateTestImageFile(ImageFormat.Bmp), CreateTestImageFile(ImageFormat.Bmp), CreateTestImageFile(ImageFormat.Bmp));
        }

        private string CreateTestImageFile(ImageFormat format)
        {
            string path = GetTestFilePath();
            path = $"{path}.{format.ToString().ToLowerInvariant()}";

            Random r = new Random(1066);

            const int Size = 1000;
            Point RandomPoint() => new Point(r.Next(Size), r.Next(Size));

            using (Bitmap bitmap = new Bitmap(Size, Size))
            using (Pen pen = new Pen(Color.Blue))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                for (int i = 0; i < 100; i++)
                {
                    graphics.DrawBezier(pen, RandomPoint(), RandomPoint(), RandomPoint(), RandomPoint());
                }

                bitmap.Save(path, format);
            }

            return path;
        }
    }
}
