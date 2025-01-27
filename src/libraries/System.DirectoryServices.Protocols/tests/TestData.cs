// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace System.DirectoryServices.Protocols.Tests
{
    internal class TestData
    {
        // pem file created using:
        // 'openssl genpkey -algorithm RSA -out server.key'
        // 'openssl req -x509 -new -key server.key -out server.crt -days 36500 -subj "/CN=ldap.example.com"'
        public static readonly byte[] CertificatePemBytes = AsciiBytes(
    @"-----BEGIN CERTIFICATE-----
MIIDGTCCAgGgAwIBAgIUYySC2OnLrVK6F5H4piG4RqYShDUwDQYJKoZIhvcNAQEL
BQAwGzEZMBcGA1UEAwwQbGRhcC5leGFtcGxlLmNvbTAgFw0yNTAxMjQxNjM3NDZa
GA8yMTI0MTIzMTE2Mzc0NlowGzEZMBcGA1UEAwwQbGRhcC5leGFtcGxlLmNvbTCC
ASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBAKtRfM0vwQqEefYV3pNKKjrO
SCk1fdxBmRVMNytZI/BRxx3B+uD8CNNNgEnOvxaY+AZpZrkRzdJ4HxlSCgon80z+
om0nG8z1uGMbhkjk4zqGAYr0wSH4CiqMBJ7YwkDtfJk+cQzrmc0A6E+VYL9Na/sF
+qQZK+Am644ZeBPQO8FEdg4mkqFWGK2mq+wRUFiz54DCjiNw62CeDFKsYXh8biw5
NDEVeZ/NbTZTp1+soPT9SPMrqvkJNGIZZuhRwdZsimzo8WnuPo7u8FKeSC4yJyeq
7Bi8kBdAcfW0ij7vnXh5vJybt4AkK7X13nSoi7MaOBlKewhu9vFsBplfs/RZlFkC
AwEAAaNTMFEwHQYDVR0OBBYEFGzUsJalFAxIGjaDYzgP6vuIwfBNMB8GA1UdIwQY
MBaAFGzUsJalFAxIGjaDYzgP6vuIwfBNMA8GA1UdEwEB/wQFMAMBAf8wDQYJKoZI
hvcNAQELBQADggEBAEXUL0aDx7kI3ub2jqTm1ObdnHP7KMezT/vBg6QcLuPO0gIc
51ngMPDvJNXjZSuXVu0LM3vvBTGhuenZhQrBusNYEGrHgxKeF+/KELMOV15QYSfa
cVs8igjgcQYAWM35Us7Sh0JKtbnx0Ync7jKzZCJYgY7MKS+3SLu8qCvL8QrxMOII
FHiTsfF4IGCLwfitvSl7u6DhPOk7OBloV7y4CN1EaPRapKnjnAzbpEIeF6XQk47Q
pZfbXZ61yLY9YRR6EmBPymss5pa2OWp3m9HXyJ7G0Pj4JvaJnKn8yTDTXyHNcv7X
8yX0goL/GhNNPYUUGEQ36vylB9oSkh8krUX+HWI=
-----END CERTIFICATE-----
");

        internal static byte[] AsciiBytes(string s)
        {
            byte[] bytes = new byte[s.Length];

            for (int i = 0; i < s.Length; i++)
            {
                bytes[i] = (byte)s[i];
            }

            return bytes;
        }
    }
}
