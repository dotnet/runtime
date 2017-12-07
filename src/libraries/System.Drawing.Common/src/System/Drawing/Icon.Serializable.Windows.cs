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
            _iconData = (byte[])info.GetValue("IconData", typeof(byte[]));
            _iconSize = (Size)info.GetValue("IconSize", typeof(Size));

            if (_iconSize.IsEmpty)
            {
                Initialize(0, 0);
            }
            else
            {
                Initialize(_iconSize.Width, _iconSize.Height);
            }
        }

        void ISerializable.GetObjectData(SerializationInfo si, StreamingContext context)
        {
            if (_iconData != null)
            {
                si.AddValue("IconData", _iconData, typeof(byte[]));
            }
            else
            {
                MemoryStream stream = new MemoryStream();
                Save(stream);
                si.AddValue("IconData", stream.ToArray(), typeof(byte[]));
            }
            si.AddValue("IconSize", _iconSize, typeof(Size));
        }
    }
}
