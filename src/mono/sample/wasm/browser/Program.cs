// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using SkiaSharp;
//using System.Runtime.InteropServices.JavaScript;

namespace Sample
{
    public class Test
    {
        public static async Task<int> Main(string[] args)
        {
            await Task.Delay(1);
            Console.WriteLine("Hello World - jsut !");
            //Console.WriteLine ($"from pinvoke: {Pkg.Test.print_line("Foo Bar")}");
            var data = CreateImage("Hello world");
            Console.WriteLine ($"back from CreateImage");
            return args.Length;
        }

        public static int TestMeaning()
        {
            var data = CreateImage("Hello world");
            Console.WriteLine ($"back from CreateImage");
            using var ms = new MemoryStream();
            data.SaveTo(ms);
            var bytes = ms.ToArray();
            string b64 = Convert.ToBase64String(bytes);
            Console.WriteLine (b64);

            return 42;
        }


        private static SKData CreateImage(string text)
        {
            // create a surface
            var info = new SKImageInfo(256, 256);
            using (var surface = SKSurface.Create(info))
            {
                // the the canvas and properties
                var canvas = surface.Canvas;

                // make sure the canvas is blank
                canvas.Clear(SKColors.White);

                // draw some text
                var paint = new SKPaint
                {
                    Color = SKColors.Black,
                          IsAntialias = true,
                          Style = SKPaintStyle.Fill,
                          TextAlign = SKTextAlign.Center,
                          TextSize = 24
                };
                var coord = new SKPoint(info.Width / 2, (info.Height + paint.TextSize) / 2);
                canvas.DrawText(text, coord, paint);

                // retrieve the encoded image
                using (var image = surface.Snapshot())
                {
                    return image.Encode(SKEncodedImageFormat.Png, 100);
                }
            }

        }
    }
}
