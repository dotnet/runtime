// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Data.SqlClient.Tests
{
    public static class SqlCredentialTest
    {
        [Fact]
        public static void Test_SqlCredential_Password_Requirements()
        {
            var userId = "user";

            // Create password with value longer than max allowed length
            var longPassword = new SecureString();

            var genPassword = string.Empty.PadLeft(129, '0');
            genPassword.ToCharArray().ToList().ForEach(c => longPassword.AppendChar(c));
            longPassword.MakeReadOnly();

            // Verify non-null password requirement
            Assert.Throws<ArgumentNullException>(() => new SqlCredential(userId, null));

            // Verify max length requirement
            Assert.Throws<ArgumentException>(() => new SqlCredential(userId, longPassword));

            // Verify read only password requirement
            Assert.Throws<ArgumentException>(() => new SqlCredential(userId, new SecureString()));

        }

        [Fact]
        public static void Test_SqlCredential_UserId_Requirements()
        {
            var password = new SecureString();
            password.MakeReadOnly();

            // Create userId longer than max allowed length
            var userId = string.Empty.PadLeft(129, '0');

            // Verify max length requirement
            Assert.Throws<ArgumentException>(() => new SqlCredential(userId, password));

            // Verify non-null userId requirement
            Assert.Throws<ArgumentNullException>(() => new SqlCredential(null, password));

        }

        [Fact]
        public static void Test_SqlCredential_Properties()
        {
            var userId = "user";
            var password = new SecureString();
            password.AppendChar('0');
            password.MakeReadOnly();

            var credential = new SqlCredential(userId, password);

            Assert.Equal(userId, credential.UserId);
            Assert.Equal(password, credential.Password);

        }

    }
}
