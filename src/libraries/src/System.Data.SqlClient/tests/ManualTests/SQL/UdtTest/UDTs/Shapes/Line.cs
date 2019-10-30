// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Data.SqlTypes;
using System.IO;

using Microsoft.SqlServer.Server;

[Serializable]
[SqlUserDefinedType(Format.UserDefined, IsByteOrdered = false, MaxByteSize = 20)]
public class Line : INullable, IBinarySerialize
{
    private Point start = new Point();
    private Point end = new Point();

    private bool fIsNull = false;

    public static Line Null { get { return new Line(true); } }

    public const int MaxByteSize = 20;
    public const bool IsFixedLength = true;
    public const bool IsByteOrdered = false;

    public Line()
    {
        start.X = start.Y = end.X = end.Y = 0;
    }

    public Line(bool fNull)
    {
        fIsNull = true;
    }

    public Line(Point ix, Point iy)
    {
        start.X = ix.X;
        start.Y = ix.Y;
        end.X = iy.X;
        end.Y = iy.Y;
    }

    public bool IsNull
    {
        get
        {
            return fIsNull;
        }
    }

    public void Read(BinaryReader r)
    {
        start = new Point();
        start.Read(r);
        end = new Point();
        end.Read(r);
        fIsNull = BitConverter.ToBoolean(r.ReadBytes(1), 0);
    }

    public void Write(BinaryWriter w)
    {
        start.Write(w);
        end.Write(w);
        w.Write(fIsNull);
    }

    public void FillFromBytes(SqlBytes data)
    {
        if (data.IsNull)
        {
            fIsNull = true;
            return;
        }

        if (data.Length != 16)
            throw new ArgumentException();
        byte[] value = data.Value;

        //read x1,y1,x2,y2
        start.X = BitConverter.ToInt32(value, 0);
        start.Y = BitConverter.ToInt32(value, 4);
        end.X = BitConverter.ToInt32(value, 8);
        end.Y = BitConverter.ToInt32(value, 12);
    }

    public void FillBytes(SqlBytes data)
    {
        if (fIsNull)
        {
            if (data.IsNull)
                return;
            else
            {
                data.SetNull();
                return;
            }
        }

        byte[] bigbytes = new byte[16];
        byte[] bytes = BitConverter.GetBytes(start.X);
        bytes.CopyTo(bigbytes, 0);
        bytes = BitConverter.GetBytes(start.Y);
        bytes.CopyTo(bigbytes, 4);

        bytes = BitConverter.GetBytes(end.X);
        bytes.CopyTo(bigbytes, 8);
        bytes = BitConverter.GetBytes(end.Y);
        bytes.CopyTo(bigbytes, 12);

        int i;
        for (i = 0; i < bigbytes.Length; i++)
            data[i] = bigbytes[i];
        data.SetLength(i);
    }

    //it should be x1,y1,x2,y2
    public static Line Parse(SqlString data)
    {
        string[] array = data.Value.Split(new char[] { ',' });

        if (array.Length != 4)
            throw new ArgumentException();
        Line line = new Line();
        line.start.X = int.Parse(array[0]);
        line.start.Y = int.Parse(array[1]);
        line.end.X = int.Parse(array[2]);
        line.end.Y = int.Parse(array[3]);

        return line;
    }

    public override string ToString()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append(start.ToString());
        builder.Append(",");
        builder.Append(end.ToString());

        return builder.ToString();
    }

    public double Length()
    {
        return end.DistanceFrom(start);
    }

    public Point Start
    {
        get
        {
            return start;
        }
    }

    public Point End
    {
        get
        {
            return end;
        }
    }
}