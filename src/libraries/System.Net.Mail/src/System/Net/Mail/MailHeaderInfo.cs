// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Net.Mail
{
    internal static class MailHeaderInfo
    {
        // Structure that wraps information about a single mail header
        private readonly struct HeaderInfo
        {
            public readonly string NormalizedName;
            public readonly bool IsSingleton;
            public readonly MailHeaderID ID;
            public readonly bool IsUserSettable;
            public readonly bool AllowsUnicode;

            public HeaderInfo(MailHeaderID id, string name, bool isSingleton, bool isUserSettable, bool allowsUnicode)
            {
                ID = id;
                NormalizedName = name;
                IsSingleton = isSingleton;
                IsUserSettable = isUserSettable;
                AllowsUnicode = allowsUnicode;
            }
        }

        // Table of well-known mail headers.
        // Keep the initializers in sync with the enum above.
        private static readonly HeaderInfo[] s_headerInfo = {
            //             ID                                     NormalizedString             IsSingleton      IsUserSettable      AllowsUnicode
            new HeaderInfo(MailHeaderID.Bcc,                      "Bcc",                       true,            false,              true),
            new HeaderInfo(MailHeaderID.Cc,                       "Cc",                        true,            false,              true),
            new HeaderInfo(MailHeaderID.Comments,                 "Comments",                  false,           true,               true),
            new HeaderInfo(MailHeaderID.ContentDescription,       "Content-Description",       true,            true,               true),
            new HeaderInfo(MailHeaderID.ContentDisposition,       "Content-Disposition",       true,            true,               true),
            new HeaderInfo(MailHeaderID.ContentID,                "Content-ID",                true,            false,              false),
            new HeaderInfo(MailHeaderID.ContentLocation,          "Content-Location",          true,            false,              true),
            new HeaderInfo(MailHeaderID.ContentTransferEncoding,  "Content-Transfer-Encoding", true,            false,              false),
            new HeaderInfo(MailHeaderID.ContentType,              "Content-Type",              true,            false,              false),
            new HeaderInfo(MailHeaderID.Date,                     "Date",                      true,            false,              false),
            new HeaderInfo(MailHeaderID.From,                     "From",                      true,            false,              true),
            new HeaderInfo(MailHeaderID.Importance,               "Importance",                true,            false,              false),
            new HeaderInfo(MailHeaderID.InReplyTo,                "In-Reply-To",               true,            true,               false),
            new HeaderInfo(MailHeaderID.Keywords,                 "Keywords",                  false,           true,               true),
            new HeaderInfo(MailHeaderID.Max,                      "Max",                       false,           true,               false),
            new HeaderInfo(MailHeaderID.MessageID,                "Message-ID",                true,            true,               false),
            new HeaderInfo(MailHeaderID.MimeVersion,              "MIME-Version",              true,            false,              false),
            new HeaderInfo(MailHeaderID.Priority,                 "Priority",                  true,            false,              false),
            new HeaderInfo(MailHeaderID.References,               "References",                true,            true,               false),
            new HeaderInfo(MailHeaderID.ReplyTo,                  "Reply-To",                  true,            false,              true),
            new HeaderInfo(MailHeaderID.ResentBcc,                "Resent-Bcc",                false,           true,               true),
            new HeaderInfo(MailHeaderID.ResentCc,                 "Resent-Cc",                 false,           true,               true),
            new HeaderInfo(MailHeaderID.ResentDate,               "Resent-Date",               false,           true,               false),
            new HeaderInfo(MailHeaderID.ResentFrom,               "Resent-From",               false,           true,               true),
            new HeaderInfo(MailHeaderID.ResentMessageID,          "Resent-Message-ID",         false,           true,               false),
            new HeaderInfo(MailHeaderID.ResentSender,             "Resent-Sender",             false,           true,               true),
            new HeaderInfo(MailHeaderID.ResentTo,                 "Resent-To",                 false,           true,               true),
            new HeaderInfo(MailHeaderID.Sender,                   "Sender",                    true,            false,              true),
            new HeaderInfo(MailHeaderID.Subject,                  "Subject",                   true,            false,              true),
            new HeaderInfo(MailHeaderID.To,                       "To",                        true,            false,              true),
            new HeaderInfo(MailHeaderID.XPriority,                "X-Priority",                true,            false,              false),
            new HeaderInfo(MailHeaderID.XReceiver,                "X-Receiver",                false,           true,               true),
            new HeaderInfo(MailHeaderID.XSender,                  "X-Sender",                  true,            true,               true)
        };

        private static readonly Dictionary<string, int> s_headerDictionary;

        static MailHeaderInfo()
        {
#if DEBUG
            // Check that enum and header info array are in sync
            for (int i = 0; i < s_headerInfo.Length; i++)
            {
                if ((int)s_headerInfo[i].ID != i)
                {
                    throw new Exception("Header info data structures are not in sync");
                }
            }
#endif

            // Create dictionary for string-to-enum lookup.  Ordinal and IgnoreCase are intentional.
            s_headerDictionary = new Dictionary<string, int>((int)MailHeaderID.ZMaxEnumValue + 1, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < s_headerInfo.Length; i++)
            {
                s_headerDictionary.Add(s_headerInfo[i].NormalizedName, i);
            }
        }

        internal static string GetString(MailHeaderID id)
        {
            switch (id)
            {
                case MailHeaderID.Unknown:
                case MailHeaderID.ZMaxEnumValue + 1:
                    return null;
                default:
                    return s_headerInfo[(int)id].NormalizedName;
            }
        }

        internal static MailHeaderID GetID(string name)
        {
            int id;
            return s_headerDictionary.TryGetValue(name, out id) ? (MailHeaderID)id : MailHeaderID.Unknown;
        }

        internal static bool IsUserSettable(string name)
        {
            //values not in the list of well-known headers are always user-settable
            int index;
            return !s_headerDictionary.TryGetValue(name, out index) || s_headerInfo[index].IsUserSettable;
        }

        internal static bool IsSingleton(string name)
        {
            int index;
            return s_headerDictionary.TryGetValue(name, out index) && s_headerInfo[index].IsSingleton;
        }

        internal static string NormalizeCase(string name)
        {
            int index;
            return s_headerDictionary.TryGetValue(name, out index) ? s_headerInfo[index].NormalizedName : name;
        }

        internal static bool AllowsUnicode(string name)
        {
            int index;
            return !s_headerDictionary.TryGetValue(name, out index) || s_headerInfo[index].AllowsUnicode;
        }
    }
}
