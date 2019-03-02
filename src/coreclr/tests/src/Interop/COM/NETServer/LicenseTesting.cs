// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

[ComVisible(true)]
[Guid(Server.Contract.Guids.LicenseTesting)]
[LicenseProvider(typeof(MockLicenseProvider))]
public class LicenseTesting : Server.Contract.ILicenseTesting
{
    public LicenseTesting()
    {
        LicenseManager.Validate(typeof(LicenseTesting), this);
    }

    public string LicenseUsed { get; set; }

    void Server.Contract.ILicenseTesting.SetNextDenyLicense(bool denyLicense)
    {
        MockLicenseProvider.DenyLicense = denyLicense;
    }

    void Server.Contract.ILicenseTesting.SetNextLicense(string lic)
    {
        MockLicenseProvider.License = lic;
    }

    string Server.Contract.ILicenseTesting.GetLicense()
    {
        return LicenseUsed;
    }
}

public class MockLicenseProvider : LicenseProvider
{
    public static bool DenyLicense { get; set; }
    public static string License { get; set; }

    public override License GetLicense(LicenseContext context, Type type, object instance, bool allowExceptions)
    {
        if (DenyLicense)
        {
            if (allowExceptions)
            {
                throw new LicenseException(type);
            }
            else
            {
                return null;
            }
        }

        if (type != typeof(LicenseTesting))
        {
            throw new Exception();
        }

        var lic = new MockLicense();

        if (instance != null)
        {
            ((LicenseTesting)instance).LicenseUsed = lic.LicenseKey;
        }

        return lic;
    }

    private class MockLicense : License
    {
        public override string LicenseKey => MockLicenseProvider.License ?? "__MOCK_LICENSE_KEY__";

        public override void Dispose () { }
    }
}
