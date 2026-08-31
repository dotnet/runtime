// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.DirectoryServices.Protocols.Tests.TestServer
{
    internal sealed partial class LdapTestServer : IDisposable
    {
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _cts = new();
        private readonly ConcurrentBag<Task> _activeTasks = new();
        private readonly object _lock = new();
        private readonly Dictionary<string, Entry> _entries = new(StringComparer.OrdinalIgnoreCase);

        private X509Certificate2 _certificate;
        private bool _useLdaps;
        private int _processedCount;

        internal int ProcessedCount => Volatile.Read(ref _processedCount);

        internal int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

        internal string BaseDn => "dc=test";

        internal LdapTestServer()
        {
            _listener = new TcpListener(IPAddress.Loopback, 0);

            _entries[BaseDn] = new Entry(BaseDn, new Dictionary<string, List<byte[]>>(StringComparer.OrdinalIgnoreCase)
            {
                ["objectClass"] = new List<byte[]> { Encoding.UTF8.GetBytes("top"), Encoding.UTF8.GetBytes("domain") },
                ["dc"] = new List<byte[]> { Encoding.UTF8.GetBytes("test") },
            });
        }

        internal int Start()
        {
            _listener.Start();
            _activeTasks.Add(AcceptLoopAsync());

            return Port;
        }

        internal int Start(X509Certificate2 startTlsCertificate)
        {
            _certificate = startTlsCertificate;
            return Start();
        }

        internal int StartLdaps(X509Certificate2 certificate)
        {
            _certificate = certificate;
            _useLdaps = true;
            return Start();
        }

        public void Dispose()
        {
            _cts.Cancel();
            _listener.Stop();

            try
            {
                Task.WhenAll(_activeTasks).Wait(TimeSpan.FromSeconds(5));
            }
            catch (AggregateException)
            {
                // Observe exceptions from tasks torn down during shutdown.
                // Presumably they will have caused test failures already, so we can ignore them here.
            }

            _cts.Dispose();
        }

        private async Task AcceptLoopAsync()
        {
            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    _activeTasks.Add(HandleConnectionAsync(client));
                }
            }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
            catch (SocketException) { }
        }

        private bool TryAddEntry(string dn, Dictionary<string, List<byte[]>> attributes)
        {
            lock (_lock)
            {
                if (_entries.ContainsKey(dn))
                {
                    return false;
                }

                // Per RFC 4511 section 4.7, RDN attributes are part of the entry.
                (string rdnAttr, string rdnValue) = ParseRdn(dn);

                if (rdnAttr.Length > 0)
                {
                    if (!attributes.TryGetValue(rdnAttr, out List<byte[]> values))
                    {
                        values = new List<byte[]>();
                        attributes[rdnAttr] = values;
                    }

                    byte[] rdnBytes = Encoding.UTF8.GetBytes(rdnValue);
                    bool alreadyPresent = false;

                    foreach (byte[] v in values)
                    {
                        if (v.AsSpan().SequenceEqual(rdnBytes))
                        {
                            alreadyPresent = true;
                            break;
                        }
                    }

                    if (!alreadyPresent)
                    {
                        values.Add(rdnBytes);
                    }
                }

                _entries[dn] = new Entry(dn, attributes);
                return true;
            }
        }

        private bool TryDeleteEntry(string dn)
        {
            lock (_lock)
            {
                return _entries.Remove(dn);
            }
        }

        private LdapResultCode ModifyEntry(string dn, List<Modification> modifications)
        {
            lock (_lock)
            {
                if (!_entries.TryGetValue(dn, out Entry entry))
                {
                    return LdapResultCode.NoSuchObject;
                }

                foreach (Modification mod in modifications)
                {
                    switch (mod.Operation)
                    {
                        case ModifyOperation.Add:
                            if (!entry.Attributes.TryGetValue(mod.AttributeName, out List<byte[]> existingAdd))
                            {
                                existingAdd = new List<byte[]>();
                                entry.Attributes[mod.AttributeName] = existingAdd;
                            }

                            foreach (byte[] val in mod.Values)
                            {
                                if (entry.HasAttributeValue(mod.AttributeName, val))
                                {
                                    return LdapResultCode.AttributeOrValueExists;
                                }

                                existingAdd.Add(val);
                            }

                            break;

                        case ModifyOperation.Delete:
                            if (mod.Values.Count == 0)
                            {
                                entry.Attributes.Remove(mod.AttributeName);
                            }
                            else
                            {
                                if (entry.Attributes.TryGetValue(mod.AttributeName, out List<byte[]> existingDel))
                                {
                                    foreach (byte[] val in mod.Values)
                                    {
                                        string valStr = Encoding.UTF8.GetString(val);

                                        for (int i = existingDel.Count - 1; i >= 0; i--)
                                        {
                                            if (string.Equals(Encoding.UTF8.GetString(existingDel[i]), valStr, StringComparison.OrdinalIgnoreCase))
                                            {
                                                existingDel.RemoveAt(i);
                                                break;
                                            }
                                        }
                                    }

                                    if (existingDel.Count == 0)
                                    {
                                        entry.Attributes.Remove(mod.AttributeName);
                                    }
                                }
                            }

                            break;

                        case ModifyOperation.Replace:
                            if (mod.Values.Count == 0)
                            {
                                entry.Attributes.Remove(mod.AttributeName);
                            }
                            else
                            {
                                entry.Attributes[mod.AttributeName] = new List<byte[]>(mod.Values);
                            }

                            break;
                    }
                }

                return LdapResultCode.Success;
            }
        }

        private LdapResultCode CompareEntryAttribute(string dn, string attrName, byte[] assertionValue)
        {
            lock (_lock)
            {
                if (!_entries.TryGetValue(dn, out Entry entry))
                {
                    return LdapResultCode.NoSuchObject;
                }

                return entry.HasAttributeValue(attrName, assertionValue)
                    ? LdapResultCode.CompareTrue
                    : LdapResultCode.CompareFalse;
            }
        }

        private LdapResultCode MoveEntry(string dn, string newRdn, string newSuperior, bool deleteOldRdn)
        {
            lock (_lock)
            {
                if (!_entries.TryGetValue(dn, out Entry entry))
                {
                    return LdapResultCode.NoSuchObject;
                }

                string parentDn = GetParentDn(dn);
                string newDn = newSuperior is not null
                    ? newRdn + "," + newSuperior
                    : newRdn + "," + parentDn;

                if (_entries.ContainsKey(newDn))
                    return LdapResultCode.EntryAlreadyExists;

                _entries.Remove(dn);

                if (deleteOldRdn)
                {
                    (string oldRdnAttr, string oldRdnValue) = ParseRdn(dn);

                    if (oldRdnAttr.Length > 0 && entry.Attributes.TryGetValue(oldRdnAttr, out List<byte[]> oldValues))
                    {
                        byte[] oldRdnBytes = Encoding.UTF8.GetBytes(oldRdnValue);

                        for (int i = oldValues.Count - 1; i >= 0; i--)
                        {
                            if (oldValues[i].AsSpan().SequenceEqual(oldRdnBytes))
                            {
                                oldValues.RemoveAt(i);
                                break;
                            }
                        }

                        if (oldValues.Count == 0)
                            entry.Attributes.Remove(oldRdnAttr);
                    }
                }

                (string newRdnAttr, string newRdnValue) = ParseRdn(newDn);

                if (newRdnAttr.Length > 0)
                {
                    if (!entry.Attributes.TryGetValue(newRdnAttr, out List<byte[]> newValues))
                    {
                        newValues = new List<byte[]>();
                        entry.Attributes[newRdnAttr] = newValues;
                    }

                    byte[] newRdnBytes = Encoding.UTF8.GetBytes(newRdnValue);
                    bool alreadyPresent = false;

                    foreach (byte[] v in newValues)
                    {
                        if (v.AsSpan().SequenceEqual(newRdnBytes))
                        {
                            alreadyPresent = true;
                            break;
                        }
                    }

                    if (!alreadyPresent)
                    {
                        newValues.Add(newRdnBytes);
                    }
                }

                _entries[newDn] = new Entry(newDn, entry.Attributes);

                return LdapResultCode.Success;
            }
        }

        private List<Entry> Search(string baseDn, LdapSearchScope scope, LdapDerefPolicy derefPolicy, Func<Entry, bool> filter)
        {
            if (derefPolicy != LdapDerefPolicy.NeverDerefAliases)
            {
                return null;
            }

            lock (_lock)
            {
                var results = new List<Entry>();

                foreach (Entry entry in _entries.Values)
                {
                    bool inScope = scope switch
                    {
                        LdapSearchScope.BaseObject => string.Equals(entry.DistinguishedName, baseDn, StringComparison.OrdinalIgnoreCase),
                        LdapSearchScope.SingleLevel => string.Equals(GetParentDn(entry.DistinguishedName), baseDn, StringComparison.OrdinalIgnoreCase),
                        LdapSearchScope.WholeSubtree => string.Equals(entry.DistinguishedName, baseDn, StringComparison.OrdinalIgnoreCase) ||
                             entry.DistinguishedName.EndsWith("," + baseDn, StringComparison.OrdinalIgnoreCase),
                        _ => false,
                    };

                    if (inScope && filter(entry))
                    {
                        results.Add(entry);
                    }
                }

                return results;
            }
        }

        private static string GetParentDn(string dn)
        {
            int idx = dn.IndexOf(',');

            return idx < 0 ? string.Empty : dn.Substring(idx + 1);
        }

        private static (string name, string value) ParseRdn(string dn)
        {
            string rdn = dn;
            int commaIdx = dn.IndexOf(',');

            if (commaIdx >= 0)
            {
                rdn = dn.Substring(0, commaIdx);
            }

            int eqIdx = rdn.IndexOf('=');

            if (eqIdx < 0)
            {
                return (string.Empty, string.Empty);
            }

            return (rdn.Substring(0, eqIdx).Trim(), rdn.Substring(eqIdx + 1).Trim());
        }

        private sealed class Entry
        {
            internal string DistinguishedName { get; }
            internal Dictionary<string, List<byte[]>> Attributes { get; }

            internal Entry(string dn, Dictionary<string, List<byte[]>> attributes)
            {
                DistinguishedName = dn;
                Attributes = new Dictionary<string, List<byte[]>>(attributes, StringComparer.OrdinalIgnoreCase);
            }

            internal bool HasAttributeValue(string name, byte[] value)
            {
                if (!Attributes.TryGetValue(name, out List<byte[]> values))
                {
                    return false;
                }

                string assertionStr = Encoding.UTF8.GetString(value);

                foreach (byte[] v in values)
                {
                    if (string.Equals(Encoding.UTF8.GetString(v), assertionStr, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }

            internal bool HasAttribute(string name) => Attributes.ContainsKey(name);
        }
    }
}
