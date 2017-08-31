// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace System.Drawing
{
    [Serializable]
    partial class Icon
    {
        private Icon(SerializationInfo info, StreamingContext context)
        {
            MemoryStream dataStream = null;
            int width = 0;
            int height = 0;
            foreach (SerializationEntry serEnum in info)
            {
                if (String.Compare(serEnum.Name, "IconData", true) == 0)
                {
                    dataStream = new MemoryStream((byte[])serEnum.Value);
                }
                if (String.Compare(serEnum.Name, "IconSize", true) == 0)
                {
                    Size iconSize = (Size)serEnum.Value;
                    width = iconSize.Width;
                    height = iconSize.Height;
                }
            }
            if (dataStream != null)
            {
                dataStream.Seek(0, SeekOrigin.Begin);
                InitFromStreamWithSize(dataStream, width, height);
            }
        }

        void ISerializable.GetObjectData(SerializationInfo si, StreamingContext context)
        {
            MemoryStream ms = new MemoryStream();
            Save(ms);
            si.AddValue("IconSize", this.Size, typeof(Size));
            si.AddValue("IconData", ms.ToArray());
        }
    }
}
