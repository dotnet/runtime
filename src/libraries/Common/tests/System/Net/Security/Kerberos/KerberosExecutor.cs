// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.RemoteExecutor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kerberos.NET.Configuration;
using Kerberos.NET.Crypto;
using Kerberos.NET.Server;
using Kerberos.NET.Logging;
using Xunit.Abstractions;

namespace System.Net.Security.Kerberos;

public class KerberosExecutor : IDisposable
{
    private readonly ListenerOptions _options;
    private readonly string _realm;
    private readonly FakePrincipalService _principalService;
    private readonly FakeKdcServer _kdcListener;
    private RemoteInvokeHandle? _invokeHandle;
    private string? _krb5Path;
    private string? _keytabPath;
    private string? _tracePath;
    private readonly List<FakeKerberosPrincipal> _servicePrincipals;
    private readonly ITestOutputHelper _testOutputHelper;

    public static bool IsSupported { get; } =
        RemoteExecutor.IsSupported && (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS());
    public const string DefaultAdminPassword = "PLACEHOLDERadmin.";

    public const string DefaultUserPassword = "PLACEHOLDERcorrect20";

    public KerberosExecutor(ITestOutputHelper testOutputHelper, string realm)
    {
        var krb5Config = Krb5Config.Default();
        krb5Config.KdcDefaults.RegisterDefaultPkInitPreAuthHandler = false;

        var logger = new KerberosDelegateLogger(
            (level, categoryName, eventId, scopeState, logState, exception, log) =>
                testOutputHelper.WriteLine($"[{level}] [{categoryName}] {log}")
        );

        _principalService = new FakePrincipalService(realm);

        byte[] krbtgtPassword = new byte[16];

        var krbtgt = new FakeKerberosPrincipal(PrincipalType.Service, "krbtgt", realm, krbtgtPassword);
        _principalService.Add("krbtgt", krbtgt);
        _principalService.Add($"krbtgt/{realm}", krbtgt);

        _options = new ListenerOptions
        {
            Configuration = krb5Config,
            DefaultRealm = realm,
            RealmLocator = realm => new FakeRealmService(realm, krb5Config, _principalService),
            Log = logger,
            IsDebug = true,
        };

        _kdcListener = new FakeKdcServer(_options);
        _realm = realm;
        _servicePrincipals = new List<FakeKerberosPrincipal>();
        _testOutputHelper = testOutputHelper;
    }

    public void Dispose()
    {
        try
        {
            _invokeHandle?.Dispose();
        }
        catch (Exception)
        {
            try
            {
                _testOutputHelper.WriteLine("GSSAPI trace:");
                _testOutputHelper.WriteLine(File.ReadAllText(_tracePath));
            }
            catch (IOException)
            {
            }
            throw;
        }
        finally
        {
            _kdcListener.Stop();
            File.Delete(_tracePath);
            File.Delete(_krb5Path);
            File.Delete(_keytabPath);
        }
    }

    public void AddService(string name, string password = DefaultAdminPassword)
    {
        var principal = new FakeKerberosPrincipal(PrincipalType.Service, name, _realm, Encoding.Unicode.GetBytes(password));
        _principalService.Add(name, principal);
        _servicePrincipals.Add(principal);
    }
 
    public void AddUser(string name, string password = DefaultUserPassword)
    {
        var principal = new FakeKerberosPrincipal(PrincipalType.User, name, _realm, Encoding.Unicode.GetBytes(password));
        _principalService.Add(name, principal);
        _principalService.Add($"{name}@{_realm}", principal);
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
        //byte[] passwordBytes = FakeKerberosPrincipal.FakePassword;

        foreach (var servicePrincipal in _servicePrincipals)
        {
            foreach (var etype in etypes.Where(CryptoService.SupportsEType))
            {
                var kerbKey = servicePrincipal.RetrieveLongTermCredential(etype);
                keyTable.Entries.Add(new KeyEntry(kerbKey));
            }
        }

        using (var fs = new FileStream(_keytabPath, FileMode.Create))
        using (var writer = new BinaryWriter(fs))
        {
            keyTable.Write(writer);
            writer.Flush();
        }

        _tracePath = Path.GetTempFileName();

        // Set environment variables for GSSAPI
        Environment.SetEnvironmentVariable("KRB5_CONFIG", _krb5Path);
        Environment.SetEnvironmentVariable("KRB5_KTNAME", _keytabPath);
        Environment.SetEnvironmentVariable("KRB5_TRACE", _tracePath);
    }
}