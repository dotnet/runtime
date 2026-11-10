// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Formats.Asn1;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.DirectoryServices.Protocols.Tests.TestServer
{
    // This implementation of LDAP is not hardened and is only intended for testing purposes.
    // It supports a limited subset of LDAP operations and features, and should not be used in production or exposed to untrusted clients.
    internal sealed partial class LdapTestServer
    {
        // LDAPv3 protocol operation APPLICATION tags (RFC 4511 section 4.2)
        private enum LdapOperation
        {
            BindRequest = 0,
            BindResponse = 1,
            UnbindRequest = 2,
            SearchRequest = 3,
            SearchResultEntry = 4,
            SearchResultDone = 5,
            ModifyRequest = 6,
            ModifyResponse = 7,
            AddRequest = 8,
            AddResponse = 9,
            DelRequest = 10,
            DelResponse = 11,
            ModifyDNRequest = 12,
            ModifyDNResponse = 13,
            CompareRequest = 14,
            CompareResponse = 15,
            ExtendedRequest = 23,
            ExtendedResponse = 24,
        }

        // LDAPv3 result codes (RFC 4511 appendix A)
        private enum LdapResultCode
        {
            Success = 0,
            CompareFalse = 5,
            CompareTrue = 6,
            UnavailableCriticalExtension = 12,
            AttributeOrValueExists = 20,
            NoSuchObject = 32,
            InvalidDNSyntax = 34,
            UnwillingToPerform = 53,
            EntryAlreadyExists = 68,
        }

        // SearchRequest.scope (RFC 4511 section 4.5.1.2)
        private enum LdapSearchScope
        {
            BaseObject = 0,
            SingleLevel = 1,
            WholeSubtree = 2,
        }

        // SearchRequest.derefAliases (RFC 4511 section 4.5.1.3)
        private enum LdapDerefPolicy
        {
            NeverDerefAliases = 0,
            DerefInSearching = 1,
            DerefFindingBaseObj = 2,
            DerefAlways = 3,
        }

        private enum ModifyOperation
        {
            Add = 0,
            Delete = 1,
            Replace = 2,
        }

        private readonly struct Modification
        {
            internal ModifyOperation Operation { get; }
            internal string AttributeName { get; }
            internal List<byte[]> Values { get; }

            public Modification(ModifyOperation operation, string attributeName, List<byte[]> values)
            {
                Operation = operation;
                AttributeName = attributeName;
                Values = values;
            }
        }

        private const string PagedResultsOid = "1.2.840.113556.1.4.319";
        private const string SortRequestOid = "1.2.840.113556.1.4.473";
        private const string SortResponseOid = "1.2.840.113556.1.4.474";
        private const string DomainScopeOid = "1.2.840.113556.1.4.1339";
        private const string SearchOptionsOid = "1.2.840.113556.1.4.1340";
        private const string StartTlsOid = "1.3.6.1.4.1.1466.20037";

        private sealed class RequestControl
        {
            internal string Oid { get; }
            internal bool Criticality { get; }
            internal byte[] Value { get; }

            internal RequestControl(string oid, bool criticality, byte[] value)
            {
                Oid = oid;
                Criticality = criticality;
                Value = value;
            }
        }

        private async Task HandleConnectionAsync(TcpClient client)
        {
            using (client)
            {
                Stream stream = client.GetStream();
                SslStream sslStream = null;

                try
                {
                    if (_useLdaps && _certificate is not null)
                    {
                        sslStream = new SslStream(stream, leaveInnerStreamOpen: false);
                        await sslStream.AuthenticateAsServerAsync(_certificate);
                        stream = sslStream;
                    }

                    while (!_cts.IsCancellationRequested)
                    {
                        byte[] message = await ReadLdapMessageAsync(stream);

                        if (message is null)
                        {
                            break;
                        }

                        byte[][] responses = ProcessMessage(message, out bool upgradeToTls);

                        if (responses is null)
                        {
                            break;
                        }

                        foreach (byte[] response in responses)
                        {
                            await stream.WriteAsync(response, 0, response.Length);
                        }

                        if (upgradeToTls && _certificate is not null)
                        {
                            sslStream = new SslStream(stream, leaveInnerStreamOpen: true);
                            await sslStream.AuthenticateAsServerAsync(_certificate);
                            stream = sslStream;
                        }
                    }
                }
                catch
                {
                }
                finally
                {
                    if (sslStream is not null)
                    {
#if NET
                        await sslStream.DisposeAsync();
#else
                        sslStream.Dispose();
#endif
                    }
                }
            }
        }

        private async Task<byte[]> ReadLdapMessageAsync(Stream stream)
        {
            // Read the tag byte; a zero-length read means connection closed.
            byte[] buf = new byte[1];
            int bytesRead = await stream.ReadAsync(buf, 0, 1);

            if (bytesRead == 0)
            {
                return null;
            }

            byte tagByte = buf[0];

            // Read the first length byte.
            await ReadExactlyAsync(stream, buf, 0, 1);
            byte firstLenByte = buf[0];

            byte[] extraLenBytes = null;
            int contentLength;

            if (firstLenByte < 0x80)
            {
                contentLength = firstLenByte;
            }
            else
            {
                int numLenBytes = firstLenByte & 0x7F;

                if (numLenBytes == 0)
                {
                    // RFC 4511 Section 5.1: "Only the definite form of length encoding is used."
                    return null;
                }

                extraLenBytes = new byte[numLenBytes];
                await ReadExactlyAsync(stream, extraLenBytes, 0, numLenBytes);

                contentLength = 0;

                for (int i = 0; i < numLenBytes; i++)
                {
                    contentLength = (contentLength << 8) | extraLenBytes[i];
                }
            }

            int headerSize = 2 + (extraLenBytes?.Length ?? 0);
            byte[] result = new byte[headerSize + contentLength];
            result[0] = tagByte;
            result[1] = firstLenByte;

            if (extraLenBytes is not null)
            {
                Buffer.BlockCopy(extraLenBytes, 0, result, 2, extraLenBytes.Length);
            }

            if (contentLength > 0)
            {
                await ReadExactlyAsync(stream, result, headerSize, contentLength);
            }

            return result;
        }

        private static async Task ReadExactlyAsync(Stream stream, byte[] buffer, int offset, int count)
        {
            while (count > 0)
            {
                int bytesRead = await stream.ReadAsync(buffer, offset, count);

                if (bytesRead == 0)
                {
                    throw new EndOfStreamException();
                }

                offset += bytesRead;
                count -= bytesRead;
            }
        }

        private byte[][] ProcessMessage(byte[] messageBytes, out bool upgradeToTls)
        {
            Interlocked.Increment(ref _processedCount);
            upgradeToTls = false;
            AsnReader reader = new AsnReader(messageBytes, AsnEncodingRules.BER);
            AsnReader seqReader = reader.ReadSequence();

            if (!seqReader.TryReadInt32(out int messageId))
            {
                return Array.Empty<byte[]>();
            }

            Asn1Tag tag = seqReader.PeekTag();

            // All LDAP protocol operations must be APPLICATION class tags
            if (tag.TagClass != TagClass.Application)
            {
                return Array.Empty<byte[]>();
            }

            LdapOperation requestOp = (LdapOperation)tag.TagValue;

            // UnbindRequest [APPLICATION 2] PRIMITIVE
            if (requestOp == LdapOperation.UnbindRequest && !tag.IsConstructed)
            {
                return null;
            }

            ReadOnlyMemory<byte> protocolOp = seqReader.ReadEncodedValue();

            List<RequestControl> controls = null;

            if (seqReader.HasData)
            {
                Asn1Tag controlsTag = new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true);

                if (seqReader.PeekTag() == controlsTag)
                {
                    controls = new List<RequestControl>();
                    AsnReader controlsReader = seqReader.ReadSequence(controlsTag);

                    while (controlsReader.HasData)
                    {
                        AsnReader controlReader = controlsReader.ReadSequence();
                        string oid = Encoding.UTF8.GetString(controlReader.ReadOctetString());
                        bool criticality = false;
                        byte[] value = null;

                        if (controlReader.HasData && controlReader.PeekTag() == Asn1Tag.Boolean)
                        {
                            criticality = controlReader.ReadBoolean();
                        }

                        if (controlReader.HasData)
                        {
                            value = controlReader.ReadOctetString();
                        }

                        controls.Add(new RequestControl(oid, criticality, value));
                    }
                }
            }

            LdapOperation? responseOp = GetResponseOperation(requestOp);

            if (controls is not null && responseOp.HasValue)
            {
                foreach (RequestControl control in controls)
                {
                    if (control.Criticality &&
                        control.Oid != PagedResultsOid &&
                        control.Oid != SortRequestOid &&
                        control.Oid != DomainScopeOid &&
                        control.Oid != SearchOptionsOid)
                    {
                        return new[] { WriteLdapResultMessage(messageId, responseOp.Value, LdapResultCode.UnavailableCriticalExtension) };
                    }
                }
            }

            return requestOp switch
            {
                LdapOperation.BindRequest => new[] { HandleBindRequest(messageId) },
                LdapOperation.SearchRequest => HandleSearchRequest(messageId, protocolOp, controls),
                LdapOperation.ModifyRequest => new[] { HandleModifyRequest(messageId, protocolOp) },
                LdapOperation.AddRequest => new[] { HandleAddRequest(messageId, protocolOp) },
                LdapOperation.DelRequest => new[] { HandleDeleteRequest(messageId, protocolOp) },
                LdapOperation.ModifyDNRequest => new[] { HandleModifyDNRequest(messageId, protocolOp) },
                LdapOperation.CompareRequest => new[] { HandleCompareRequest(messageId, protocolOp) },
                LdapOperation.ExtendedRequest => HandleExtendedRequest(messageId, protocolOp, out upgradeToTls),
                _ => Array.Empty<byte[]>(),
            };
        }

        private static LdapOperation? GetResponseOperation(LdapOperation requestOp)
        {
            return requestOp switch
            {
                LdapOperation.BindRequest => LdapOperation.BindResponse,
                LdapOperation.SearchRequest => LdapOperation.SearchResultDone,
                LdapOperation.ModifyRequest => LdapOperation.ModifyResponse,
                LdapOperation.AddRequest => LdapOperation.AddResponse,
                LdapOperation.DelRequest => LdapOperation.DelResponse,
                LdapOperation.ModifyDNRequest => LdapOperation.ModifyDNResponse,
                LdapOperation.CompareRequest => LdapOperation.CompareResponse,
                LdapOperation.ExtendedRequest => LdapOperation.ExtendedResponse,
                _ => null
            };
        }

        private static byte[] HandleBindRequest(int messageId)
        {
            return WriteLdapResultMessage(messageId, LdapOperation.BindResponse, LdapResultCode.Success);
        }

        private byte[][] HandleExtendedRequest(int messageId, ReadOnlyMemory<byte> protocolOp, out bool upgradeToTls)
        {
            upgradeToTls = false;

            AsnReader opReader = new AsnReader(protocolOp, AsnEncodingRules.BER);
            AsnReader extReader = opReader.ReadSequence(new Asn1Tag(TagClass.Application, (int)LdapOperation.ExtendedRequest, isConstructed: true));

            // requestName [0] LDAPOID
            string requestOid = Encoding.UTF8.GetString(
                extReader.ReadOctetString(new Asn1Tag(TagClass.ContextSpecific, 0)));

            if (requestOid == StartTlsOid && _certificate is not null)
            {
                upgradeToTls = true;
                return new[] { WriteExtendedResponseMessage(messageId, LdapResultCode.Success, StartTlsOid) };
            }

            // Unsupported extended operation
            return new[] { WriteExtendedResponseMessage(messageId, LdapResultCode.UnavailableCriticalExtension) };
        }

        private static byte[] WriteExtendedResponseMessage(
            int messageId,
            LdapResultCode resultCode,
            string responseName = null)
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.BER);

            using (writer.PushSequence())
            {
                writer.WriteInteger(messageId);

                using (writer.PushSequence(new Asn1Tag(TagClass.Application, (int)LdapOperation.ExtendedResponse)))
                {
                    writer.WriteEnumeratedValue(resultCode);
                    writer.WriteOctetString(Array.Empty<byte>()); // matchedDN
                    writer.WriteOctetString(Array.Empty<byte>()); // diagnosticMessage

                    if (responseName is not null)
                    {
                        // responseName [10] LDAPOID
                        writer.WriteOctetString(
                            Encoding.UTF8.GetBytes(responseName),
                            new Asn1Tag(TagClass.ContextSpecific, 10));
                    }
                }
            }

            return writer.Encode();
        }

        private byte[] HandleAddRequest(int messageId, ReadOnlyMemory<byte> protocolOp)
        {
            AsnReader opReader = new AsnReader(protocolOp, AsnEncodingRules.BER);
            AsnReader addReader = opReader.ReadSequence(new Asn1Tag(TagClass.Application, (int)LdapOperation.AddRequest, isConstructed: true));

            string entryDn = Encoding.UTF8.GetString(addReader.ReadOctetString());
            AsnReader attrsReader = addReader.ReadSequence();

            var attributes = new Dictionary<string, List<byte[]>>(StringComparer.OrdinalIgnoreCase);

            while (attrsReader.HasData)
            {
                AsnReader attrReader = attrsReader.ReadSequence();
                string attrType = Encoding.UTF8.GetString(attrReader.ReadOctetString());
                AsnReader valsReader = attrReader.ReadSetOf();
                var values = new List<byte[]>();

                while (valsReader.HasData)
                {
                    values.Add(valsReader.ReadOctetString());
                }

                attributes[attrType] = values;
            }

            LdapResultCode result = TryAddEntry(entryDn, attributes)
                ? LdapResultCode.Success
                : LdapResultCode.EntryAlreadyExists;

            return WriteLdapResultMessage(messageId, LdapOperation.AddResponse, result);
        }

        private byte[] HandleDeleteRequest(int messageId, ReadOnlyMemory<byte> protocolOp)
        {
            AsnReader opReader = new AsnReader(protocolOp, AsnEncodingRules.BER);
            string dn = Encoding.UTF8.GetString(
                opReader.ReadOctetString(new Asn1Tag(TagClass.Application, (int)LdapOperation.DelRequest, isConstructed: false)));

            LdapResultCode result = TryDeleteEntry(dn)
                ? LdapResultCode.Success
                : LdapResultCode.NoSuchObject;

            return WriteLdapResultMessage(messageId, LdapOperation.DelResponse, result);
        }

        private byte[] HandleModifyRequest(int messageId, ReadOnlyMemory<byte> protocolOp)
        {
            AsnReader opReader = new AsnReader(protocolOp, AsnEncodingRules.BER);
            AsnReader modifyReader = opReader.ReadSequence(new Asn1Tag(TagClass.Application, (int)LdapOperation.ModifyRequest, isConstructed: true));

            string dn = Encoding.UTF8.GetString(modifyReader.ReadOctetString());
            AsnReader changesReader = modifyReader.ReadSequence();

            var modifications = new List<Modification>();

            while (changesReader.HasData)
            {
                AsnReader changeReader = changesReader.ReadSequence();
                ModifyOperation operation = changeReader.ReadEnumeratedValue<ModifyOperation>();
                AsnReader modificationReader = changeReader.ReadSequence();
                string attrType = Encoding.UTF8.GetString(modificationReader.ReadOctetString());
                AsnReader valsReader = modificationReader.ReadSetOf();
                var values = new List<byte[]>();

                while (valsReader.HasData)
                {
                    values.Add(valsReader.ReadOctetString());
                }

                modifications.Add(new Modification(operation, attrType, values));
            }

            LdapResultCode result = ModifyEntry(dn, modifications);

            return WriteLdapResultMessage(messageId, LdapOperation.ModifyResponse, result);
        }

        private byte[] HandleCompareRequest(int messageId, ReadOnlyMemory<byte> protocolOp)
        {
            AsnReader opReader = new AsnReader(protocolOp, AsnEncodingRules.BER);
            AsnReader compareReader = opReader.ReadSequence(new Asn1Tag(TagClass.Application, (int)LdapOperation.CompareRequest, isConstructed: true));

            string dn = Encoding.UTF8.GetString(compareReader.ReadOctetString());
            AsnReader avaReader = compareReader.ReadSequence();
            string attrDesc = Encoding.UTF8.GetString(avaReader.ReadOctetString());
            byte[] assertionValue = avaReader.ReadOctetString();

            LdapResultCode result = CompareEntryAttribute(dn, attrDesc, assertionValue);

            return WriteLdapResultMessage(messageId, LdapOperation.CompareResponse, result);
        }

        private byte[] HandleModifyDNRequest(int messageId, ReadOnlyMemory<byte> protocolOp)
        {
            AsnReader opReader = new AsnReader(protocolOp, AsnEncodingRules.BER);
            AsnReader modDnReader = opReader.ReadSequence(new Asn1Tag(TagClass.Application, (int)LdapOperation.ModifyDNRequest, isConstructed: true));

            string dn = Encoding.UTF8.GetString(modDnReader.ReadOctetString());
            string newRdn = Encoding.UTF8.GetString(modDnReader.ReadOctetString());
            bool deleteOldRdn = modDnReader.ReadBoolean();
            string newSuperior = null;

            Asn1Tag newSuperiorTag = new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: false);

            if (modDnReader.HasData && modDnReader.PeekTag() == newSuperiorTag)
            {
                newSuperior = Encoding.UTF8.GetString(modDnReader.ReadOctetString(newSuperiorTag));
            }

            LdapResultCode result = MoveEntry(dn, newRdn, newSuperior, deleteOldRdn);

            return WriteLdapResultMessage(messageId, LdapOperation.ModifyDNResponse, result);
        }

        private byte[][] HandleSearchRequest(int messageId, ReadOnlyMemory<byte> protocolOp, List<RequestControl> controls)
        {
            AsnReader opReader = new AsnReader(protocolOp, AsnEncodingRules.BER);
            AsnReader searchReader = opReader.ReadSequence(
                new Asn1Tag(TagClass.Application, (int)LdapOperation.SearchRequest, isConstructed: true));

            string baseDn = Encoding.UTF8.GetString(searchReader.ReadOctetString());

            if (!IsValidDn(baseDn))
            {
                return new[] { WriteLdapResultMessage(messageId, LdapOperation.SearchResultDone, LdapResultCode.InvalidDNSyntax) };
            }

            LdapSearchScope scope = searchReader.ReadEnumeratedValue<LdapSearchScope>();
            LdapDerefPolicy derefPolicy = searchReader.ReadEnumeratedValue<LdapDerefPolicy>();
            searchReader.TryReadInt32(out _);
            searchReader.TryReadInt32(out _);
            searchReader.ReadBoolean();

            ReadOnlyMemory<byte> filterBytes = searchReader.ReadEncodedValue();

            List<Entry> results;

            try
            {
                results = Search(baseDn, scope, derefPolicy, entry => EvaluateFilter(filterBytes, entry));
            }
            catch (NotSupportedException)
            {
                return new[] { WriteLdapResultMessage(messageId, LdapOperation.SearchResultDone, LdapResultCode.UnwillingToPerform) };
            }

            if (results is null)
            {
                return new[] { WriteLdapResultMessage(messageId, LdapOperation.SearchResultDone, LdapResultCode.UnwillingToPerform) };
            }

            // Sort
            string sortAttr = null;
            bool sortReverse = false;
            bool hasSortControl = false;

            if (controls is not null)
            {
                RequestControl sortControl = controls.Find(c => c.Oid == SortRequestOid);

                if (sortControl is not null)
                {
                    hasSortControl = true;

                    if (sortControl.Value is not null)
                    {
                        AsnReader sortReader = new AsnReader(sortControl.Value, AsnEncodingRules.BER);
                        AsnReader sortKeysReader = sortReader.ReadSequence();

                        if (sortKeysReader.HasData)
                        {
                            AsnReader sortKeyReader = sortKeysReader.ReadSequence();
                            sortAttr = Encoding.UTF8.GetString(sortKeyReader.ReadOctetString());

                            // orderingRule [0] OPTIONAL
                            Asn1Tag orderingRuleTag = new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: false);

                            if (sortKeyReader.HasData && sortKeyReader.PeekTag() == orderingRuleTag)
                            {
                                sortKeyReader.ReadOctetString(orderingRuleTag);
                            }

                            // reverseOrder [1] BOOLEAN DEFAULT FALSE
                            Asn1Tag reverseTag = new Asn1Tag(TagClass.ContextSpecific, 1, isConstructed: false);

                            if (sortKeyReader.HasData && sortKeyReader.PeekTag() == reverseTag)
                            {
                                sortReverse = sortKeyReader.ReadBoolean(reverseTag);
                            }
                        }
                    }
                }
            }

            if (sortAttr is not null)
            {
                results.Sort((a, b) =>
                {
                    string aVal = GetFirstStringValue(a, sortAttr);
                    string bVal = GetFirstStringValue(b, sortAttr);
                    int cmp = string.Compare(aVal, bVal, StringComparison.OrdinalIgnoreCase);

                    return sortReverse ? -cmp : cmp;
                });
            }

            // Paging: uses a simple byte-offset cookie, which assumes the dataset is stable
            // across paged requests. The test suite does not test paged search concurrent with modification.
            int pageSize = 0;
            int pageOffset = 0;
            bool hasPagingControl = false;

            if (controls is not null)
            {
                RequestControl pagingControl = controls.Find(c => c.Oid == PagedResultsOid);

                if (pagingControl is not null)
                {
                    hasPagingControl = true;

                    if (pagingControl.Value is not null)
                    {
                        AsnReader pagingReader = new AsnReader(pagingControl.Value, AsnEncodingRules.BER);
                        AsnReader pagingSeq = pagingReader.ReadSequence();
                        pagingSeq.TryReadInt32(out pageSize);
                        byte[] cookie = pagingSeq.ReadOctetString();

                        if (cookie.Length >= 4)
                        {
                            pageOffset = (cookie[0] << 24) | (cookie[1] << 16) | (cookie[2] << 8) | cookie[3];
                        }
                    }
                }
            }

            List<Entry> pageResults;

            if (hasPagingControl && pageSize > 0)
            {
                if (pageOffset >= results.Count)
                {
                    pageResults = new List<Entry>();
                }
                else
                {
                    int count = Math.Min(pageSize, results.Count - pageOffset);
                    pageResults = results.GetRange(pageOffset, count);
                }
            }
            else
            {
                pageResults = results;
            }

            var responses = new List<byte[]>(pageResults.Count + 1);

            foreach (Entry entry in pageResults)
            {
                responses.Add(WriteSearchResultEntry(messageId, entry));
            }

            // Build response controls
            List<(string oid, byte[] value)> responseControls = null;

            if (hasPagingControl)
            {
                int nextOffset = hasPagingControl && pageSize > 0 ? pageOffset + pageResults.Count : 0;
                bool moreResults = nextOffset < results.Count;

                AsnWriter cookieWriter = new AsnWriter(AsnEncodingRules.BER);

                using (cookieWriter.PushSequence())
                {
                    cookieWriter.WriteInteger(results.Count);

                    if (moreResults)
                    {
                        byte[] nextCookie = new byte[4];
                        nextCookie[0] = (byte)(nextOffset >> 24);
                        nextCookie[1] = (byte)(nextOffset >> 16);
                        nextCookie[2] = (byte)(nextOffset >> 8);
                        nextCookie[3] = (byte)nextOffset;
                        cookieWriter.WriteOctetString(nextCookie);
                    }
                    else
                    {
                        cookieWriter.WriteOctetString(Array.Empty<byte>());
                    }
                }

                responseControls = new List<(string oid, byte[] value)>();
                responseControls.Add((PagedResultsOid, cookieWriter.Encode()));
            }

            if (hasSortControl)
            {
                AsnWriter sortRespWriter = new AsnWriter(AsnEncodingRules.BER);

                using (sortRespWriter.PushSequence())
                {
                    sortRespWriter.WriteEnumeratedValue(LdapResultCode.Success);
                }

                if (responseControls is null)
                {
                    responseControls = new List<(string oid, byte[] value)>();
                }

                responseControls.Add((SortResponseOid, sortRespWriter.Encode()));
            }

            responses.Add(WriteLdapResultMessage(messageId, LdapOperation.SearchResultDone, LdapResultCode.Success, responseControls: responseControls));

            return responses.ToArray();
        }

        private static string GetFirstStringValue(Entry entry, string attrName)
        {
            if (entry.Attributes.TryGetValue(attrName, out List<byte[]> values) && values.Count > 0)
            {
                return Encoding.UTF8.GetString(values[0]);
            }

            return string.Empty;
        }

        private static bool IsValidDn(string dn)
        {
            if (dn.Length == 0)
            {
                return true;
            }

            if (dn.Length > 0 && dn[0] == '=')
            {
                return false;
            }

            int eqIdx = dn.IndexOf('=');

            if (eqIdx < 1)
            {
                return false;
            }

            for (int i = 0; i < eqIdx; i++)
            {
                char c = dn[i];

                if (!char.IsLetterOrDigit(c) && c != '-' && c != '.')
                {
                    return false;
                }
            }

            return true;
        }

        private static bool EvaluateFilter(ReadOnlyMemory<byte> filterBytes, Entry entry)
        {
            AsnReader reader = new AsnReader(filterBytes, AsnEncodingRules.BER);
            Asn1Tag tag = reader.PeekTag();

            // AND [0] CONSTRUCTED — RFC 4511 section 4.5.1.7
            if (tag == new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true))
            {
                AsnReader andReader = reader.ReadSequence(tag);

                while (andReader.HasData)
                {
                    if (!EvaluateFilter(andReader.ReadEncodedValue(), entry))
                    {
                        return false;
                    }
                }

                return true;
            }

            // OR [1] CONSTRUCTED
            if (tag == new Asn1Tag(TagClass.ContextSpecific, 1, isConstructed: true))
            {
                AsnReader orReader = reader.ReadSequence(tag);

                while (orReader.HasData)
                {
                    if (EvaluateFilter(orReader.ReadEncodedValue(), entry))
                    {
                        return true;
                    }
                }

                return false;
            }

            // NOT [2] CONSTRUCTED
            if (tag == new Asn1Tag(TagClass.ContextSpecific, 2, isConstructed: true))
            {
                AsnReader notReader = reader.ReadSequence(tag);

                return !EvaluateFilter(notReader.ReadEncodedValue(), entry);
            }

            // equalityMatch [3] CONSTRUCTED (implicit SEQUENCE)
            if (tag == new Asn1Tag(TagClass.ContextSpecific, 3, isConstructed: true))
            {
                AsnReader eqReader = reader.ReadSequence(tag);
                string attrDesc = Encoding.UTF8.GetString(eqReader.ReadOctetString());
                byte[] assertionValue = eqReader.ReadOctetString();

                return entry.HasAttributeValue(attrDesc, assertionValue);
            }

            // present [7] PRIMITIVE
            if (tag == new Asn1Tag(TagClass.ContextSpecific, 7, isConstructed: false))
            {
                string attrDesc = Encoding.UTF8.GetString(reader.ReadOctetString(tag));

                return entry.HasAttribute(attrDesc);
            }

            throw new NotSupportedException($"Unsupported LDAP filter tag: {tag}.");
        }

        private static byte[] WriteLdapResultMessage(
            int messageId,
            LdapOperation operation,
            LdapResultCode resultCode,
            string matchedDn = "",
            string diagnosticMessage = "",
            List<(string oid, byte[] value)> responseControls = null)
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.BER);

            using (writer.PushSequence())
            {
                writer.WriteInteger(messageId);

                using (writer.PushSequence(new Asn1Tag(TagClass.Application, (int)operation)))
                {
                    writer.WriteEnumeratedValue(resultCode);
                    writer.WriteOctetString(Encoding.UTF8.GetBytes(matchedDn));
                    writer.WriteOctetString(Encoding.UTF8.GetBytes(diagnosticMessage));
                }

                if (responseControls is not null && responseControls.Count > 0)
                {
                    using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true)))
                    {
                        foreach ((string oid, byte[] value) in responseControls)
                        {
                            using (writer.PushSequence())
                            {
                                writer.WriteOctetString(Encoding.UTF8.GetBytes(oid));

                                if (value is not null)
                                {
                                    writer.WriteOctetString(value);
                                }
                            }
                        }
                    }
                }
            }

            return writer.Encode();
        }

        private static byte[] WriteSearchResultEntry(int messageId, Entry entry)
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.BER);

            using (writer.PushSequence())
            {
                writer.WriteInteger(messageId);

                using (writer.PushSequence(new Asn1Tag(TagClass.Application, (int)LdapOperation.SearchResultEntry)))
                {
                    writer.WriteOctetString(Encoding.UTF8.GetBytes(entry.DistinguishedName));

                    using (writer.PushSequence())
                    {
                        foreach (KeyValuePair<string, List<byte[]>> attr in entry.Attributes)
                        {
                            using (writer.PushSequence())
                            {
                                writer.WriteOctetString(Encoding.UTF8.GetBytes(attr.Key));

                                using (writer.PushSetOf())
                                {
                                    foreach (byte[] val in attr.Value)
                                    {
                                        writer.WriteOctetString(val);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return writer.Encode();
        }
    }
}
