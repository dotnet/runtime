// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Data.SqlTypes;
using System.IO;

using Microsoft.SqlServer.Server;

[Serializable]
[SqlUserDefinedType(Format.UserDefined, IsByteOrdered = false, MaxByteSize = 30)]
public class Circle : INullable, IBinarySerialize
{
    private Point1 center = new Point1();
    private int rad;

    private bool fIsNull = false;

    public static Circle Null { get { return new Circle(true); } }

    public const int MaxByteSize = 30;
    public const bool IsFixedLength = true;
    public const bool IsByteOrdered = false;


    public void Read(BinaryReader r)
    {
        center = new Point1();
        center.Read(r);
        rad = r.ReadInt32();
        fIsNull = BitConverter.ToBoolean(r.ReadBytes(1), 0);
    }

    public void Write(BinaryWriter w)
    {
        center.Write(w);
        w.Write(rad);
        w.Write(fIsNull);
    }

    public Circle()
    {
        center.X = 0;
        center.Y = 0;
        rad = 0;
        fIsNull = false;

    }

    public Circle(bool fNull)
    {
        fIsNull = true;
    }

    public bool IsNull
    {
        get
        {
            return fIsNull;
        }
    }

    public void FillFromBytes(SqlBytes data)
    {
        if (data.IsNull)
        {
            fIsNull = true;
            return;
        }

        if (data.Length != 12)
            throw new ArgumentException();
        byte[] value = data.Value;

        //read x1,y1,x2,y2
        center.X = BitConverter.ToInt32(value, 0);
        center.Y = BitConverter.ToInt32(value, 4);
        rad = BitConverter.ToInt32(value, 8);
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

        byte[] bigbytes = new byte[12];
        byte[] bytes = BitConverter.GetBytes(center.X);
        bytes.CopyTo(bigbytes, 0);
        bytes = BitConverter.GetBytes(center.Y);
        bytes.CopyTo(bigbytes, 4);

        bytes = BitConverter.GetBytes(rad);
        bytes.CopyTo(bigbytes, 8);

        int i;
        for (i = 0; i < bigbytes.Length; i++)
            data[i] = bigbytes[i];
        data.SetLength(i);

    }

    //it should be x1,y1,x2,y2
    public static Circle Parse(SqlString data)
    {
        string[] array = data.Value.Split(new char[] { ',' });

        if (array.Length != 3)
            throw new ArgumentException();
        Circle circ = new Circle();

        circ.center.X = int.Parse(array[0]);
        circ.center.Y = int.Parse(array[1]);
        circ.rad = int.Parse(array[2]);
        return circ;
    }

    public override string ToString()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append(center.ToString());
        builder.Append(",");
        builder.Append(rad.ToString());

        return builder.ToString();
    }

    public Point1 Center
    {
        get
        {
            return center;
        }
    }

    public int Radius
    {
        get
        {
            return rad;
        }
    }
}
