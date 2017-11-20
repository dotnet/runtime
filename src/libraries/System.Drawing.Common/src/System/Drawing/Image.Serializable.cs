// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace System.Drawing
{
    [Serializable]
    partial class Image
    {
#pragma warning disable CA2229
        internal Image(SerializationInfo info, StreamingContext context)
        {
#pragma warning restore CA2229
            SerializationInfoEnumerator sie = info.GetEnumerator();
            if (sie == null)
            {
                return;
            }
            for (; sie.MoveNext();)
            {
                if (string.Equals(sie.Name, "Data", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        byte[] dat = (byte[])sie.Value;
                        if (dat != null)
                        {
                            InitializeFromStream(new MemoryStream(dat));
                        }
                    }
                    catch (ExternalException e)
                    {
                        Debug.Fail("failure: " + e.ToString());
                    }
                    catch (ArgumentException e)
                    {
                        Debug.Fail("failure: " + e.ToString());
                    }
                    catch (OutOfMemoryException e)
                    {
                        Debug.Fail("failure: " + e.ToString());
                    }
                    catch (InvalidOperationException e)
                    {
                        Debug.Fail("failure: " + e.ToString());
                    }
                    catch (NotImplementedException e)
                    {
                        Debug.Fail("failure: " + e.ToString());
                    }
                    catch (FileNotFoundException e)
                    {
                        Debug.Fail("failure: " + e.ToString());
                    }
                }
            }
        }

        void ISerializable.GetObjectData(SerializationInfo si, StreamingContext context)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                Save(stream);
                si.AddValue("Data", stream.ToArray(), typeof(byte[]));
            }
        }
    }
}

