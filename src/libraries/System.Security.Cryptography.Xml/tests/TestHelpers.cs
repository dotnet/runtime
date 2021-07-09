// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using System.Xml.Resolvers;

namespace System.Security.Cryptography.Xml.Tests
{
    internal static class TestHelpers
    {
        /// <summary>
        /// Convert a <see cref="Stream"/> to a <see cref="string"/> using the given <see cref="Encoding"/>.
        /// </summary>
        /// <param name="stream">
        /// The <see cref="Stream"/> to read from. This cannot be null.
        /// </param>
        /// <param name="encoding">
        /// The <see cref="Encoding"/> to use. This cannot be null.
        /// </param>
        /// <returns>
        /// The stream as a string.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// No argument can be null.
        /// </exception>
        public static string StreamToString(Stream stream, Encoding encoding)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }
            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            using (StreamReader streamReader = new StreamReader(stream, encoding))
            {
                return streamReader.ReadToEnd();
            }
        }

        /// <summary>
        /// Perform
        /// </summary>
        /// <param name="inputXml">
        /// The XML to transform. This cannot be null, empty or whitespace.
        /// </param>
        /// <param name="transform">
        /// The <see cref="Transform"/> to perform on
        /// <paramref name="inputXml"/>. This cannot be null.
        /// </param>
        /// <param name="encoding">
        /// An optional <see cref="Encoding"/> to use when serializing or
        /// deserializing <paramref name="inputXml"/>. This should match the
        /// encoding specified in <paramref name="inputXml"/>. If omitted or
        /// null, <see cref="UTF8Encoding"/> is used.
        /// </param>
        /// <param name="resolver">
        /// An optional <see cref="XmlResolver"/> to use. If omitted or null,
        /// no resolver is used.
        /// </param>
        /// <returns>
        /// The transformed <paramref name="inputXml"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="transform"/> cannot be null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="inputXml"/> cannot be null, empty or whitespace.
        /// </exception>
        /// <exception cref="XmlException">
        /// <paramref name="inputXml"/> is not valid XML.
        /// </exception>
        public static string ExecuteTransform(string inputXml, Transform transform, Encoding encoding = null, XmlResolver resolver = null)
        {
            if (string.IsNullOrWhiteSpace(inputXml))
            {
                throw new ArgumentException("Cannot be null, empty or whitespace", nameof(inputXml));
            }
            if (transform == null)
            {
                throw new ArgumentNullException(nameof(Transform));
            }

            XmlDocument doc = new XmlDocument();
            doc.XmlResolver = resolver;
            doc.PreserveWhitespace = true;
            doc.LoadXml(inputXml);

            Encoding actualEncoding = encoding ?? Encoding.UTF8;
            byte[] data = actualEncoding.GetBytes(inputXml);
            using (Stream stream = new MemoryStream(data))
            using (XmlReader reader = XmlReader.Create(stream, new XmlReaderSettings { ValidationType = ValidationType.None, DtdProcessing = DtdProcessing.Parse, XmlResolver = resolver }))
            {
                doc.Load(reader);
                transform.LoadInput(doc);
                return StreamToString((Stream)transform.GetOutput(), actualEncoding);
            }
        }

        /// <summary>
        /// Convert <paramref name="fileName"/> to a full URI for referencing
        /// in an <see cref="XmlPreloadedResolver"/>.
        /// </summary>
        /// <param name="fileName">
        /// The file name. This cannot be null, empty or whitespace.
        /// </param>
        /// <returns>
        /// The created <see cref="Uri"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="fileName"/> cannot be null, empty or whitespace.
        /// </exception>
        public static Uri ToUri(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("Cannot be null, empty or whitespace", nameof(fileName));
            }

            string path = Path.Combine(Directory.GetCurrentDirectory(), fileName);
            return new Uri("file://" + (path[0] == '/' ? path : '/' + path));
        }

#pragma warning disable SYSLIB0022 // Rijndael types are obsolete
        /// <summary>
        /// Get specification URL from algorithm implementation
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetEncryptionMethodName(SymmetricAlgorithm key, bool keyWrap = false)
        {
            if (key is TripleDES)
            {
                return keyWrap ? EncryptedXml.XmlEncTripleDESKeyWrapUrl : EncryptedXml.XmlEncTripleDESUrl;
            }
            else if (key is DES)
            {
                return keyWrap ? EncryptedXml.XmlEncTripleDESKeyWrapUrl : EncryptedXml.XmlEncDESUrl;
            }
            else if (key is Rijndael || key is Aes)
            {
                switch (key.KeySize)
                {
                    case 128:
                        return keyWrap ? EncryptedXml.XmlEncAES128KeyWrapUrl : EncryptedXml.XmlEncAES128Url;
                    case 192:
                        return keyWrap ? EncryptedXml.XmlEncAES192KeyWrapUrl : EncryptedXml.XmlEncAES192Url;
                    case 256:
                        return keyWrap ? EncryptedXml.XmlEncAES256KeyWrapUrl : EncryptedXml.XmlEncAES256Url;
                }
            }

            throw new ArgumentException($"The specified algorithm `{key.GetType().FullName}` is not supported for XML Encryption.");
        }
#pragma warning restore SYSLIB0022

        /// <summary>
        /// Lists functions creating symmetric algorithms
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<SymmetricAlgorithmFactory> GetSymmetricAlgorithms(bool skipDes = false)
        {
            if (!skipDes)
            {
                yield return new SymmetricAlgorithmFactory("DES", () => DES.Create());
            }

            yield return new SymmetricAlgorithmFactory("TripleDES", () => TripleDES.Create());

            foreach (var keySize in new[] { 128, 192, 256 })
            {
                yield return new SymmetricAlgorithmFactory($"AES{keySize}", () =>
                {
                    Aes aes = Aes.Create();
                    aes.KeySize = keySize;
                    return aes;
                });
            }
        }

        private static readonly byte[] SamplePfx = Convert.FromBase64String(
    // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Suppression approved. Unit test dummy certificate.")]
    @"MIIFpQIBAzCCBV8GCSqGSIb3DQEHAaCCBVAEggVMMIIFSDCCAl8GCSqGSIb3DQEHBqCCAlAwggJMAgEAMIICRQYJKoZIhvcNAQcBMBwGCiqGSIb3DQEMAQMwDgQIGTfVa4+vR1UCAgfQgIICGJuFE9alFWJFkaoeewKDIEnVwRxXfMsi8dcySYnp7jljEUQBfW/GIbOf7Lg2nHd0qxvxYI2YL4Zs+d0jWbqfNHamGFCMPe1dK957Z2PsKXR183vMSgnmlLAHktsIN+Gor7q1GbQ4ljfZkGqZ/rkgUsgsSYZSnJevP/uH0VnvxemljVJ7N7gKMYO0aqrca4qJ0O4YxBYyaerPFUOYunQlvk6DOF3SQXza5oFKcPGrSpE/9eQrnmm64BtbdnUE6qqEjfZfNa6MOD3vOnapLUBsel2TtVCu8tEl7I8FGxozTLXVTXOBkL3k7xLRS52ZtpbcU2JIhlDGpxeFXmjKYzdzHoL20iJubfdkUYtHwB0XjBKKLcI7jfgGgjNauaTLAx8FF+5O9s7Zbj2+SKWv56kqAwdX+iH21VgjAN9EByIXHb3p2ZOvy4ONDXTmfSn7jbuPLZTi+u6bxn2JOLf/gjEA8FiCuQDL9gF247bnUq08Z1uzuAUeaPL13U8mxwEuvCOXx5NEQIuf3cusnaH4+7uIhPk5tnfA5XOaABySetRjZhVN5dC5/g3KTwmaDamlW3Y7Az/NzAC4uKa2ny5jwYKBgHviEKOyJfLDKr5fOMRToOfgxvAdXZohQQTE1+TcBjp+eeV5koDfB1ReCKIRHugPZu5j9SCVcYanwFeJ5M4cEHZ9U1Ytsmzjh0fwV17D/hxQ4aS4VwVpOMypMIIC4QYJKoZIhvcNAQcBoIIC0gSCAs4wggLKMIICxgYLKoZIhvcNAQwKAQKgggKeMIICmjAcBgoqhkiG9w0BDAEDMA4ECBRdKqx022cfAgIH0ASCAnjZx9fvPCHizdH6apVzWWmfy/84HvDPjFOUV1TPehTnDPkNpF/uK/ya4jlbl4Kw0Zfknt5Xydl89SMXIWa2q+nWmxyG3XyfGqOAeBfJBSdCF5K3qkZZnzEfraKZZ5Hh8IEmK+ey45O6sltua6Xl5MRBmKLiwma7vX4ihXQTMfb0WlWDYCXZi85OeF0OlUjRWAwz4PeeiBK4nmI/vNmF1EzDVdZGkrrE8mot3Y4z6bvwqip2tUUbHuMnC+/1ikAcJzCOw4NpnEWCRtIJxgJ9es8E8CUfHESnWKe4nh6tJVJ15B8/7oF7N6j7oq4Oj346JthKoWWkzifNaH79A60/uFh08Rv7zrtJf6kedY6Ve2bR5lhWn0cv9Q6IaoqTmKKTmKJnjdQO9lKRCR6iI2OsYtXBropD8xhNNqsyfpNmP0G6wFiEZZxZjWOkZEJLUzFbH+Su+7l2l4FN9sM7k211/l3/3YF1QJHwZsgL98DZL4qE+nkuZQcdtOUx8QTyTOcVb3IzgCAwZm0rgdXQpJ9yRBgOC/6MnqaCPI0jJuavXF/a28GJWWGlazx7SWTrbzNVJ83ZhQ+pfPEPtMi3t0YVLLvapu3otgpiMkv4ew/ssXwYbg6xBWfotK+NG1cPwVFy9/V9+H5dpdvRI/le2QG0F5xCfCeKh/3AuNiMPEGoVUR5kj5cwFK6eskvt/+74ZenxfNPZ2Uttiw8DsqtTx1gxhcSZeU5YWpO7O78RaYE4Ll4kPbbvIaR18Napb6NKP846z02zvaw+feXARLe0HUY58TlmUjSX3MZRK4PEdyMIQ/URyPimj4rImaDfFrKPAHIjqT3EKv+KuNs8TEVMBMGCSqGSIb3DQEJFTEGBAQBAAAAMD0wITAJBgUrDgMCGgUABBRZOo132cuo2zNyy+SH2c+pN4OGmQQU2nQao3je7DTj2G6Gge8pooPf2ncCAgfQ");

        private static readonly byte[] SampleDsaPfx = Convert.FromBase64String(
            // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Suppression approved. Unit test dummy certificate.")]
            "MIIF+QIBAzCCBb8GCSqGSIb3DQEHAaCCBbAEggWsMIIFqDCCA9cGCSqGSIb3DQEH" +
            "BqCCA8gwggPEAgEAMIIDvQYJKoZIhvcNAQcBMBwGCiqGSIb3DQEMAQYwDgQI8liC" +
            "7ThjN10CAggAgIIDkFQEBVf6+23WfPd7Rn38z2zvzNEeq5ddwJVQQVctbFTaemHH" +
            "15ygd126N+BVw5+2RX8SzDb9qwzHnBUgQQ9WThW3i2JDB9wpE94KJOKYbFTy5hOO" +
            "eDw1rf+dbS89jRtWkG3kOxQpgr2I8B/MX4IUgb8SFn/Uqd3V/FcYAPBiBiZxPdHX" +
            "QXruCNKXjJDeO5lyvXpB2mN4cf6uXrF44tGbKzmdS2+iFg5xmx7E3KB9CGkH6FmK" +
            "O9vG35Hqe+3ZmuAR9qnJAOk9ja3Rx5XuQ248a68MZuPSo2GxMLUXh0BH1aimTHH2" +
            "YQz6RzD3jBVIujZ3fhecY6BDCHOZKSjkQEnJAl/K0nzeeKaKN/biK+PouO8qlSnR" +
            "lwjBng4XeolCwEE6S3EiingOZRq/IYRkwM6YUfCCsHNRnCALprxuYWRj/5vTiYD8" +
            "uIIGLTimOynTTh5tOmJiCHzoEmpq0nGtM0fekbgc+xVsslsT10TtdjjRRK26aw/M" +
            "BwkZ7MiPjyTKxJ21zTl0Jc6DcoZK1CKOBy4mG1VE2hZP8oGvNmy1KUZGKeFHS9dA" +
            "EjMqhkqPTSOL8wZYenPhaD3dWEWX28mFib9WSy/o6n5U37bwRgI/SxeNt5Q3qbuu" +
            "KNo9bC8XGnRl+AaoeUsl62353kZ7SXGTovWSSQ482CK7QMrfKU4MID0IhFxggkNP" +
            "GkSYZsIGXC6fVcYjsyzdo/q4NwY3+ysCBVi0294ECvd8cAppXSRXFAjGDwtnBPQC" +
            "XTN831Op9gEzId1NTFhhOvYl6S+yXJcmJ52aFj7mB58dNa1qXHmVLug70PNp4Z01" +
            "ELYQ5psdEK8r2/pNM8X+e9SZETCzt5jBd5l5kQ+FB5byO2v2nVJ1hnASG3ckGDS0" +
            "dl5mW1Y+qa91tu4M5AwDe0ECGirROr3qsP1w2RtPPBgYvGD1SiKp7yZ+2GVUaJEs" +
            "pGlmNt90wA6ddjXkO/av3nYWhWSjI2qkNPHFcqOzFsNXsoilgbnUUImfth7U81d4" +
            "hHJ6cOgDNfN+sxv6pL3nt12eK24lW2dhetyedr3lN8q6bNkC5l+Oq89CTyz0MlZk" +
            "dT/eS5pHsXMFXRDF+ZIljoea13J4jRdVZeZJsqIoql3qdY1SvnyUSyRJR/VfBGWD" +
            "aIJjmwG/z5VVjKPklpB72SkqVWyzLF/Vra/rqy1TlFYAZ91eYAQJ3faezB251bKl" +
            "80rAXicKdsLMHEhsdjCCAckGCSqGSIb3DQEHAaCCAboEggG2MIIBsjCCAa4GCyqG" +
            "SIb3DQEMCgECoIIBdjCCAXIwHAYKKoZIhvcNAQwBAzAOBAh2G75W3/WqEAICCAAE" +
            "ggFQ/bprf5JYknV8tp8mcxXphVVPmWW6PxeB9YzLngbSKO7KTd8pLxG8m6eDrCbf" +
            "fkgffdT+peGSUWYAu9/RgG2x1AdNTd4s9BgQ8y4nDbB7NV1BW7Cm7VOcYR6/+H+2" +
            "CfNU4gPWKBwSFiZfC55/cdIkg4fh9UeoS+ImEApDQu7Thzjc+pVUEiCgnDA83wEs" +
            "h7oP63eZhaH5WtPrsnOzpYW3tjLdC59LHe4W1goG8wp552MHxnOiHXZXbgNHdmwx" +
            "QexumEv5CHbjW6f7NYFMBycYSPkCH22A8DCwIrid+hcvMPlsTR4Z2wL8PTk9zmi0" +
            "R5daKIKRQts6hjPQH8VIXuFeb545i8U1lDw3MiF17Dp2z8vAr3TRZswN979mLVfB" +
            "jyuLJ/nIyLLEvpc3iIbSsoGGa5+VxQhk0Wal0PLGdzZ/Y2QhBhixeq94JB0w2fo7" +
            "Ljj3MSUwIwYJKoZIhvcNAQkVMRYEFDdYxa4ZJsCeAQZDzHLZ+cdpC2z5MDEwITAJ" +
            "BgUrDgMCGgUABBQrvSbTT08JKqS+yVmoU8tj2gzcogQIGnaqGGnaa+ACAggA");

        public static X509Certificate2 GetSampleX509Certificate()
        {
            return new X509Certificate2(SamplePfx, "mono");
        }

        public static X509Certificate2 GetSampleDSAX509Certificate()
        {
            return new X509Certificate2(SampleDsaPfx, "velleity");
        }

        public static Stream LoadResourceStream(string resourceName)
        {
            return typeof(TestHelpers).Assembly.GetManifestResourceStream(resourceName);
        }

        public static byte[] LoadResource(string resourceName)
        {
            using (Stream stream = typeof(TestHelpers).Assembly.GetManifestResourceStream(resourceName))
            {
                long length = stream.Length;
                byte[] buffer = new byte[length];
                stream.Read(buffer, 0, (int)length);
                return buffer;
            }
        }
    }
}
