// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace System.Net.Security.Tests.AndroidPlatformTrust;

public sealed class ExtractRootCA : Task
{
    [Required]
    public string P7bPath { get; set; } = string.Empty;

    [Required]
    public string OutputPath { get; set; } = string.Empty;

    public override bool Execute()
    {
        SignedCms cms = new SignedCms();
        cms.Decode(File.ReadAllBytes(P7bPath));

        byte[]? rootRawData = null;
        X509Certificate2Collection collection = cms.Certificates;
        try
        {
            foreach (X509Certificate2 cert in collection)
            {
                if (cert.Subject == cert.Issuer)
                {
                    rootRawData = cert.Export(X509ContentType.Cert);
                    break;
                }
            }
        }
        finally
        {
            foreach (X509Certificate2 cert in collection)
            {
                cert.Dispose();
            }
        }

        if (rootRawData is null)
        {
            Log.LogError($"Root CA not found in {P7bPath}");
            return false;
        }

        File.WriteAllBytes(OutputPath, rootRawData);
        return true;
    }
}
