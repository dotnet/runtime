// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Mail
{
    // Enumeration of the well-known headers.
    // If you add to this enum you MUST also add the appropriate initializer in MailHeaderInfo.s_headerInfo
    internal enum MailHeaderID
    {
        Bcc = 0,
        Cc,
        Comments,
        ContentDescription,
        ContentDisposition,
        ContentID,
        ContentLocation,
        ContentTransferEncoding,
        ContentType,
        Date,
        From,
        Importance,
        InReplyTo,
        Keywords,
        Max,
        MessageID,
        MimeVersion,
        Priority,
        References,
        ReplyTo,
        ResentBcc,
        ResentCc,
        ResentDate,
        ResentFrom,
        ResentMessageID,
        ResentSender,
        ResentTo,
        Sender,
        Subject,
        To,
        XPriority,
        XReceiver,
        XSender,
        ZMaxEnumValue = XSender,  // Keep this to equal to the last "known" enum entry if you add to the end
        Unknown = -1
    }
}
