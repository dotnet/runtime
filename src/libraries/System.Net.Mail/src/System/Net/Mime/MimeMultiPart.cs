// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mime
{
    internal sealed class MimeMultiPart : MimeBasePart
    {
        private static int s_boundary;

        internal MimeMultiPart(MimeMultiPartType type)
        {
            MimeMultiPartType = type;
        }

        internal MimeMultiPartType MimeMultiPartType
        {
            set
            {
                if (value > MimeMultiPartType.Related || value < MimeMultiPartType.Mixed)
                {
                    throw new NotSupportedException(value.ToString());
                }
                SetType(value);
            }
        }

        private void SetType(MimeMultiPartType type)
        {
            ContentType.MediaType = "multipart/" + type.ToString().ToLowerInvariant();
            ContentType.Boundary = GetNextBoundary();
        }

        internal Collection<MimeBasePart> Parts => field ??= new Collection<MimeBasePart>();

        internal override async Task SendAsync<TIOAdapter>(BaseWriter writer, bool allowUnicode, CancellationToken cancellationToken = default)
        {
            PrepareHeaders(allowUnicode);
            writer.WriteHeaders(Headers, allowUnicode);
            Stream outputStream = writer.GetContentStream();
            MimeWriter mimeWriter = new MimeWriter(outputStream, ContentType.Boundary!);

            foreach (MimeBasePart part in Parts)
            {
                await part.SendAsync<TIOAdapter>(mimeWriter, allowUnicode, cancellationToken).ConfigureAwait(false);
            }

            mimeWriter.Close();
            outputStream.Close();
        }

        internal static string GetNextBoundary()
        {
            int b = Interlocked.Increment(ref s_boundary) - 1;
            return $"--boundary_{(uint)b}_{Guid.NewGuid()}";
        }
    }
}
