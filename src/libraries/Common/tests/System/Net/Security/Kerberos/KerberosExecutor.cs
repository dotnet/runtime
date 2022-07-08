// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.RemoteExecutor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Kerberos.NET.Crypto;
using Kerberos.NET.Entities;
using Kerberos.NET.Server;

namespace System.Net.Security.Kerberos;

public class KerberosExecutor : IDisposable
{
    private ListenerOptions _options;
    private string _realm;
    private FakeKdcServer _kdcListener;
    private RemoteInvokeHandle? _invokeHandle;
    private string? _krb5Path;
    private string? _keytabPath;
    private List<string> _services;

    public static bool IsSupported { get; } = OperatingSystem.IsLinux() || OperatingSystem.IsMacOS();

    public static string FakePassword { get; } = "P@ssw0rd!";

    public KerberosExecutor(string realm)
    {
        _options = new ListenerOptions
        {
            DefaultRealm = realm,
            RealmLocator = realm => new FakeRealmService(realm)
        };

        _kdcListener = new FakeKdcServer(_options);
        _services = new List<string>();
        _realm = realm;
    }

    public void Dispose()
    {
        _invokeHandle?.Dispose();
        _kdcListener.Stop();
        File.Delete(_krb5Path);
        File.Delete(_keytabPath);
    }

    public void AddService(string name)
    {
        _services.Add(name);
    }

    public async Task Invoke(Action method)
    {
        await PrepareInvoke();
        _invokeHandle = RemoteExecutor.Invoke(method);
    }

    public async Task Invoke(Func<Task> method)
    {
        await PrepareInvoke();
        _invokeHandle = RemoteExecutor.Invoke(method);
    }

    private async Task PrepareInvoke()
    {
        // Start the KDC server
        var endpoint = await _kdcListener.Start();

        // Generate krb5.conf
        _krb5Path = Path.GetTempFileName();
        File.WriteAllText(_krb5Path,
            OperatingSystem.IsLinux() ?
            $"[realms]\n{_options.DefaultRealm} = {{\n  master_kdc = {endpoint}\n  kdc = {endpoint}\n}}\n" :
            $"[realms]\n{_options.DefaultRealm} = {{\n  kdc = tcp/{endpoint}\n}}\n");

        // Generate keytab file
        _keytabPath = Path.GetTempFileName();
        var keyTable = new KeyTable();

        var etypes = _options.Configuration.Defaults.DefaultTgsEncTypes;
        byte[] passwordBytes = FakeKerberosPrincipal.FakePassword;

        foreach (string service in _services)
        {
            foreach (var etype in etypes.Where(CryptoService.SupportsEType))
            {
                var kerbKey = new KerberosKey(
                    password: passwordBytes,
                    etype: etype,
                    principal: new PrincipalName(
                       PrincipalNameType.NT_PRINCIPAL,
                       _options.DefaultRealm,
                       new [] { service }),
                    saltType: SaltType.ActiveDirectoryUser
               );

               keyTable.Entries.Add(new KeyEntry(kerbKey));
            }
        }

        using (var fs = new FileStream(_keytabPath, FileMode.Create))
        using (var writer = new BinaryWriter(fs))
        {
            keyTable.Write(writer);
            writer.Flush();
        }

        // Set environment variables for GSSAPI
        Environment.SetEnvironmentVariable("KRB5_CONFIG", _krb5Path);
        Environment.SetEnvironmentVariable("KRB5_KTNAME", _keytabPath);
    }
}