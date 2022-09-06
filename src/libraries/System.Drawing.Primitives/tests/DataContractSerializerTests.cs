// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using System.Runtime.Serialization.Tests;
using System.Drawing;

namespace System.Drawing.Primitives.Tests
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBuiltWithAggressiveTrimming))]
    public class DataContractSerializerTests
    {
        [Fact]
        public static void DCS_Point()
        {
            var objs = new Point[]
            {
            new Point(0,0),
            new Point(1,2),
            new Point(new Size(1,2))
            };
            var serializedStrings = new string[]
            {
            @"<Point xmlns=""http://schemas.datacontract.org/2004/07/System.Drawing"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><x>0</x><y>0</y></Point>",
            @"<Point xmlns=""http://schemas.datacontract.org/2004/07/System.Drawing"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><x>1</x><y>2</y></Point>",
            @"<Point xmlns=""http://schemas.datacontract.org/2004/07/System.Drawing"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><x>1</x><y>2</y></Point>"
            };
            for (int i = 0; i < objs.Length; i++)
            {
                Assert.StrictEqual(DataContractSerializerHelper.SerializeAndDeserialize<Point>(objs[i], serializedStrings[i]), objs[i]);
            }
        }

        [Fact]
        public static void DCS_PointF()
        {
            var objs = new PointF[]
            {
            new PointF(0, 0),
            new PointF(1,2),
            new PointF(1.5000f,-1.5000f)
            };
            var serializedStrings = new string[]
            {
            @"<PointF xmlns=""http://schemas.datacontract.org/2004/07/System.Drawing"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><x>0</x><y>0</y></PointF>",
            @"<PointF xmlns=""http://schemas.datacontract.org/2004/07/System.Drawing"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><x>1</x><y>2</y></PointF>",
            @"<PointF xmlns=""http://schemas.datacontract.org/2004/07/System.Drawing"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><x>1.5</x><y>-1.5</y></PointF>"
            };
            for (int i = 0; i < objs.Length; i++)
            {
                Assert.StrictEqual(DataContractSerializerHelper.SerializeAndDeserialize<PointF>(objs[i], serializedStrings[i]), objs[i]);
            }
        }

        [Fact]
        public static void DCS_Rectangle()
        {
            var objs = new Rectangle[]
            {
            new Rectangle(0, 0, 0, 0),
            new Rectangle(1, 2, 1, 2),
            new Rectangle(new Point(1,2), new Size(1,2)),
            new Rectangle(1, -2, 1, -2)
            };
            var serializedStrings = new string[]
            {
            @"<Rectangle xmlns=""http://schemas.datacontract.org/2004/07/System.Drawing"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><height>0</height><width>0</width><x>0</x><y>0</y></Rectangle>",
            @"<Rectangle xmlns=""http://schemas.datacontract.org/2004/07/System.Drawing"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><height>2</height><width>1</width><x>1</x><y>2</y></Rectangle>",
            @"<Rectangle xmlns=""http://schemas.datacontract.org/2004/07/System.Drawing"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><height>2</height><width>1</width><x>1</x><y>2</y></Rectangle>",
            @"<Rectangle xmlns=""http://schemas.datacontract.org/2004/07/System.Drawing"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><height>-2</height><width>1</width><x>1</x><y>-2</y></Rectangle>"
            };
            for (int i = 0; i < objs.Length; i++)
            {
                Assert.StrictEqual(DataContractSerializerHelper.SerializeAndDeserialize<Rectangle>(objs[i], serializedStrings[i]), objs[i]);
            }
        }

        [Fact]
        public static void DCS_RectangleF()
        {
            var objs = new RectangleF[]
            {
            new RectangleF(0, 0, 0, 0),
            new RectangleF(new PointF(1.5000f,2.5000f), new SizeF(1.5000f,2.5000f)),
            new RectangleF(1.50001f, -2.5000f, 1.5000f, -2.5000f)
            };
            var serializedStrings = new string[]
            {
            @"<RectangleF xmlns=""http://schemas.datacontract.org/2004/07/System.Drawing"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><height>0</height><width>0</width><x>0</x><y>0</y></RectangleF>",
            @"<RectangleF xmlns=""http://schemas.datacontract.org/2004/07/System.Drawing"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><height>2.5</height><width>1.5</width><x>1.5</x><y>2.5</y></RectangleF>",
            @"<RectangleF xmlns=""http://schemas.datacontract.org/2004/07/System.Drawing"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><height>-2.5</height><width>1.5</width><x>1.50001</x><y>-2.5</y></RectangleF>"
            };
            for (int i = 0; i < objs.Length; i++)
            {
                Assert.StrictEqual(DataContractSerializerHelper.SerializeAndDeserialize<RectangleF>(objs[i], serializedStrings[i]), objs[i]);
            }
        }

        [Fact]
        public static void DCS_Size()
        {
            var objs = new Size[]
            {
            new Size(0,0),
            new Size(new Point(1,2)),
            new Size(1,2)
            };
            var serializedStrings = new string[]
            {
            @"<Size xmlns=""http://schemas.datacontract.org/2004/07/System.Drawing"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><height>0</height><width>0</width></Size>",
            @"<Size xmlns=""http://schemas.datacontract.org/2004/07/System.Drawing"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><height>2</height><width>1</width></Size>",
            @"<Size xmlns=""http://schemas.datacontract.org/2004/07/System.Drawing"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><height>2</height><width>1</width></Size>"
            };
            for (int i = 0; i < objs.Length; i++)
            {
                Assert.StrictEqual(DataContractSerializerHelper.SerializeAndDeserialize<Size>(objs[i], serializedStrings[i]), objs[i]);
            }
        }

        [Fact]
        public static void DCS_SizeF()
        {
            var objs = new SizeF[]
            {
            new SizeF(0,0),
            new SizeF(new PointF(1.5000f,-2.5000f)),
            new SizeF(1.5000f,-2.5000f)
            };
            var serializedStrings = new string[]
            {
            @"<SizeF xmlns=""http://schemas.datacontract.org/2004/07/System.Drawing"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><height>0</height><width>0</width></SizeF>",
            @"<SizeF xmlns=""http://schemas.datacontract.org/2004/07/System.Drawing"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><height>-2.5</height><width>1.5</width></SizeF>",
            @"<SizeF xmlns=""http://schemas.datacontract.org/2004/07/System.Drawing"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><height>-2.5</height><width>1.5</width></SizeF>"
            };
            for (int i = 0; i < objs.Length; i++)
            {
                Assert.StrictEqual(DataContractSerializerHelper.SerializeAndDeserialize<SizeF>(objs[i], serializedStrings[i]), objs[i]);
            }
        }
    }
}
