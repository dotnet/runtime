// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Reflection;

namespace System.Security.Permissions.Tests
{
    public class PermissionTests
    {
        [Fact]
        public static void EnvironmentPermissionCallMethods()
        {
            EnvironmentPermission ep = new EnvironmentPermission(new Permissions.EnvironmentPermissionAccess(), "testpath");
            ep.AddPathList(new Permissions.EnvironmentPermissionAccess(), "testpath");
            string path = ep.GetPathList(new Permissions.EnvironmentPermissionAccess());
            bool isunrestricted = ep.IsUnrestricted();
            ep.SetPathList(new Permissions.EnvironmentPermissionAccess(), "testpath2");
            SecurityElement se = new SecurityElement("");
            ep.FromXml(se);
            se = ep.ToXml();
        }

        [Fact]
        public static void EnvironmentPermissionsAttributeCallMethods()
        {
            EnvironmentPermissionAttribute epa = new EnvironmentPermissionAttribute(new Permissions.SecurityAction());
            IPermission ip = epa.CreatePermission();
        }

        [Fact]
        public static void FileDialogPermissionCallMethods()
        {
            FileDialogPermission fdp = new FileDialogPermission(new Permissions.FileDialogPermissionAccess());
            IPermission ip = fdp.Copy();
            IPermission ip2 = fdp.Intersect(ip);
            bool issubset = fdp.IsSubsetOf(ip);
            bool isunrestricted = fdp.IsUnrestricted();
            IPermission ip3 = fdp.Union(ip2);
            SecurityElement se = new SecurityElement("");
            fdp.FromXml(se);
            se = fdp.ToXml();
        }

        [Fact]
        public static void FileDialogPermissionAttributeCallMethods()
        {
            FileDialogPermissionAttribute fspa = new FileDialogPermissionAttribute(new Permissions.SecurityAction());
            IPermission ip = fspa.CreatePermission();
        }

        [Fact]
        public static void FileIOPermissionCallMethods()
        {
            FileIOPermissionAccess fiopa = new Permissions.FileIOPermissionAccess();
            FileIOPermission fiop = new FileIOPermission(fiopa, "testpath");
            FileIOPermission fiop2 = new FileIOPermission(new Permissions.PermissionState());
            fiop.AddPathList(fiopa, "testpath");
            fiop.AddPathList(fiopa, new string[1] { "testpath" });
            IPermission ip = fiop.Copy();
            fiop.Equals(new object());
            int hash = fiop.GetHashCode();
            string[] pathlist = fiop.GetPathList(fiopa);
            IPermission ip2 = fiop.Intersect(ip);
            fiop.IsSubsetOf(ip);
            bool isunrestricted = fiop.IsUnrestricted();
            fiop.SetPathList(fiopa, "testpath");
            fiop.SetPathList(fiopa, new string[1] { "testpath" });
            IPermission ip3 = fiop.Union(ip);
            SecurityElement se = new SecurityElement("");
            fiop.FromXml(se);
            se = fiop.ToXml();
        }

        [Fact]
        public static void GacIdentityPermissionCallMethods()
        {
            GacIdentityPermission gip = new GacIdentityPermission();
            IPermission ip = gip.Copy();
            IPermission ip2 = gip.Intersect(ip);
            bool issubset = gip.IsSubsetOf(ip);
            IPermission ip3 = gip.Union(ip2);
            SecurityElement se = new SecurityElement("");
            gip.FromXml(se);
            se = gip.ToXml();
        }

        [Fact]
        public static void GacIdentityPermissionAttributeCallMethods()
        {
            GacIdentityPermissionAttribute gipa = new GacIdentityPermissionAttribute(new Permissions.SecurityAction());
            IPermission ip = gipa.CreatePermission();
        }

        [Fact]
        public static void PrincipalPermissionCallMethods()
        {
            PrincipalPermission pp = new PrincipalPermission(new Permissions.PermissionState());
            PrincipalPermission pp2 = new PrincipalPermission("name", "role");
            PrincipalPermission pp3 = new PrincipalPermission("name", "role", true);
            IPermission ip = pp.Copy();
            bool testbool = pp.Equals(new object());
            int testint = pp.GetHashCode();
            IPermission ip2 = pp.Intersect(ip);
            testbool = pp.IsSubsetOf(ip);
            testbool = pp.IsUnrestricted();
            string teststring = pp.ToString();
            ip2 = pp.Union(ip);
            SecurityElement se = new SecurityElement("");
            pp.FromXml(se);
            se = pp.ToXml();
        }

        [Fact]
        public static void PrincipalPermissionAttributeCallMethods()
        {
            PrincipalPermissionAttribute ppa = new PrincipalPermissionAttribute(new Permissions.SecurityAction());
            IPermission ip = ppa.CreatePermission();
        }

        [Fact]
        public static void PublisherIdentityPermissionCallMethods()
        {
            PublisherIdentityPermission pip = new PublisherIdentityPermission(new System.Security.Cryptography.X509Certificates.X509Certificate());
            PublisherIdentityPermission pip2 = new PublisherIdentityPermission(new Permissions.PermissionState());
            IPermission ip = pip.Copy();
            IPermission ip2 = pip.Intersect(ip);
            bool testbool = pip.IsSubsetOf(ip);
            ip2 = pip.Union(ip);
            SecurityElement se = new SecurityElement("");
            pip.FromXml(se);
            se = pip.ToXml();
        }

        [Fact]
        public static void PublisherIdentityPermissionAttributeCallMethods()
        {
            PublisherIdentityPermissionAttribute pipa = new PublisherIdentityPermissionAttribute(new Permissions.SecurityAction());
            IPermission ip = pipa.CreatePermission();
        }

        [Fact]
        public static void ReflectionPermissionCallMethods()
        {
            ReflectionPermission rp = new ReflectionPermission(new Permissions.PermissionState());
            ReflectionPermission rp2 = new ReflectionPermission(new Permissions.ReflectionPermissionFlag());
            IPermission ip = rp.Copy();
            IPermission ip2 = rp.Intersect(ip);
            bool testbool = rp.IsSubsetOf(ip);
            testbool = rp.IsUnrestricted();
            ip2 = rp.Union(ip);
            SecurityElement se = new SecurityElement("");
            rp.FromXml(se);
            se = rp.ToXml();
        }

        [Fact]
        public static void ReflectionPermissionAttributeCallMethods()
        {
            ReflectionPermissionAttribute rpa = new ReflectionPermissionAttribute(new Permissions.SecurityAction());
            IPermission ip = rpa.CreatePermission();
        }

        [Fact]
        public static void RegistryPermissionCallMethods()
        {
            Permissions.RegistryPermissionAccess rpa = new Permissions.RegistryPermissionAccess();
            RegistryPermission rp = new RegistryPermission(new Permissions.PermissionState());
            RegistryPermission rp2 = new RegistryPermission(rpa, new System.Security.AccessControl.AccessControlActions(), "testpath");
            RegistryPermission rp3 = new RegistryPermission(rpa, "testpath");
            rp.AddPathList(rpa, "testpath");
            IPermission ip = rp.Copy();
            string path = rp.GetPathList(rpa);
            IPermission ip2 = rp.Intersect(ip);
            bool testbool = rp.IsSubsetOf(ip);
            testbool = rp.IsUnrestricted();
            rp.SetPathList(rpa, "testpath");
            ip2 = rp.Union(ip);
            SecurityElement se = new SecurityElement("");
            rp.FromXml(se);
            se = rp.ToXml();
        }

        [Fact]
        public static void RegistryPermissionAttributeCallMethods()
        {
            RegistryPermissionAttribute rpa = new RegistryPermissionAttribute(new Permissions.SecurityAction());
            IPermission ip = rpa.CreatePermission();
        }

        [Fact]
        public static void SecurityPermissionCallMethods()
        {
            SecurityPermission sp = new SecurityPermission(new Permissions.PermissionState());
            SecurityPermission sp2 = new SecurityPermission(new Permissions.SecurityPermissionFlag());
            IPermission ip = sp.Copy();
            IPermission ip2 = sp.Intersect(ip);
            bool testbool = sp.IsSubsetOf(ip);
            testbool = sp.IsUnrestricted();
            ip2 = sp.Union(ip);
            SecurityElement se = new SecurityElement("");
            sp.FromXml(se);
            se = sp.ToXml();
        }

        [Fact]
        public static void SecurityPermissionAttributeCallMethods()
        {
            SecurityPermissionAttribute spa = new SecurityPermissionAttribute(new Permissions.SecurityAction());
            IPermission ip = spa.CreatePermission();
        }

        [Fact]
        public static void SiteIdentityPermissionCallMethods()
        {
            SiteIdentityPermission sip = new SiteIdentityPermission(new Permissions.PermissionState());
            SiteIdentityPermission sip2 = new SiteIdentityPermission("testsite");
            IPermission ip = sip.Copy();
            IPermission ip2 = sip.Intersect(ip);
            bool testbool = sip.IsSubsetOf(ip);
            ip2 = sip.Union(ip);
            SecurityElement se = new SecurityElement("");
            sip.FromXml(se);
            se = sip.ToXml();
        }

        [Fact]
        public static void SiteIdentityPermissionAttributeCallMethods()
        {
            SiteIdentityPermissionAttribute sipa = new SiteIdentityPermissionAttribute(new Permissions.SecurityAction());
            IPermission ip = sipa.CreatePermission();
        }

        [Fact]
        public static void StrongNameIdentityPermissionCallMethods()
        {
            StrongNameIdentityPermission snip = new StrongNameIdentityPermission(new Permissions.PermissionState());
            StrongNameIdentityPermission snip2 = new StrongNameIdentityPermission(new Permissions.StrongNamePublicKeyBlob(new byte[1]), "test", new Version(1, 2));
            IPermission ip = snip.Copy();
            IPermission ip2 = snip.Intersect(ip);
            bool testbool = snip.IsSubsetOf(ip);
            ip2 = snip.Union(ip);
            SecurityElement se = new SecurityElement("");
            snip.FromXml(se);
            se = snip.ToXml();
        }

        [Fact]
        public static void StrongNameIdentityPermissionAttributeCallMethods()
        {
            StrongNameIdentityPermissionAttribute snipa = new StrongNameIdentityPermissionAttribute(new Permissions.SecurityAction());
            IPermission ip = snipa.CreatePermission();
        }

        [Fact]
        public static void StrongNamePublicKeyBlobTests()
        {
            StrongNamePublicKeyBlob snpkb = new StrongNamePublicKeyBlob(new byte[1]);
            bool testbool = snpkb.Equals(new object());
            int hash = snpkb.GetHashCode();
            string teststring = snpkb.ToString();
        }

        [Fact]
        public static void TypeDescriptorPermissionCallMethods()
        {
            TypeDescriptorPermission tdp = new TypeDescriptorPermission(new PermissionState());
            TypeDescriptorPermission tdp2 = new TypeDescriptorPermission(new TypeDescriptorPermissionFlags());
            IPermission ip = tdp.Copy();
            IPermission ip2 = tdp.Intersect(ip);
            bool testbool = tdp.IsSubsetOf(ip);
            testbool = tdp.IsUnrestricted();
            ip2 = tdp.Union(ip);
            SecurityElement se = new SecurityElement("");
            tdp.FromXml(se);
            se = tdp.ToXml();
        }

        [Fact]
        public static void UIPermissionCallMethods()
        {
            UIPermission uip = new UIPermission(new PermissionState());
            UIPermission uip2 = new UIPermission(new UIPermissionClipboard());
            UIPermission uip3 = new UIPermission(new UIPermissionWindow());
            UIPermission uip4 = new UIPermission(new UIPermissionWindow(), new UIPermissionClipboard());
            IPermission ip = uip.Copy();
            IPermission ip2 = uip.Intersect(ip);
            bool testbool = uip.IsSubsetOf(ip);
            testbool = uip.IsUnrestricted();
            ip2 = uip.Union(ip);
            SecurityElement se = new SecurityElement("");
            uip.FromXml(se);
            se = uip.ToXml();
        }

        [Fact]
        public static void UIPermissionAttributeCallMethods()
        {
            UIPermissionAttribute uipa = new UIPermissionAttribute(new Permissions.SecurityAction());
            IPermission ip = uipa.CreatePermission();
        }

        [Fact]
        public static void UrlIdentityPermissionCallMethods()
        {
            UrlIdentityPermission uip = new UrlIdentityPermission(new PermissionState());
            UrlIdentityPermission uip2 = new UrlIdentityPermission("testsite");
            IPermission ip = uip.Copy();
            IPermission ip2 = uip.Intersect(ip);
            bool testbool = uip.IsSubsetOf(ip);
            ip2 = uip.Union(ip);
            SecurityElement se = new SecurityElement("");
            uip.FromXml(se);
            se = uip.ToXml();
        }

        [Fact]
        public static void UrlIdentityPermissionAttributeCallMethods()
        {
            UrlIdentityPermissionAttribute uipa = new UrlIdentityPermissionAttribute(new Permissions.SecurityAction());
            IPermission ip = uipa.CreatePermission();
        }

        [Fact]
        public static void ZoneIdentityPermissionCallMethods()
        {
            ZoneIdentityPermission zip = new ZoneIdentityPermission(new PermissionState());
            ZoneIdentityPermission zip2 = new ZoneIdentityPermission(new SecurityZone());
            IPermission ip = zip.Copy();
            IPermission ip2 = zip.Intersect(ip);
            bool testbool = zip.IsSubsetOf(ip);
            SecurityElement se = new SecurityElement("");
            zip.FromXml(se);
            se = zip.ToXml();
        }

        [Fact]
        public static void ZoneIdentityPermissionAttributeCallMethods()
        {
            ZoneIdentityPermissionAttribute zipa = new ZoneIdentityPermissionAttribute(new Permissions.SecurityAction());
            IPermission ip = zipa.CreatePermission();
        }
    }
}
