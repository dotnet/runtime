// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.PrivateUri.Tests
{
    public class UriBuilderRefreshTest
    {
        private static Uri s_starterUri = new Uri("http://user:psw@host:9090/path/file.txt?query#fragment");

        [Fact]
        public void UriBuilder_ChangeScheme_Refreshed()
        {
            UriBuilder builder = new UriBuilder(s_starterUri);
            Assert.Equal<String>(s_starterUri.Scheme, builder.Scheme);
            Assert.Equal<String>(s_starterUri.Scheme, builder.Uri.Scheme);
            string newValue = "newvalue";
            builder.Scheme = newValue;
            Assert.Equal<String>(newValue, builder.Scheme);
            Assert.Equal<String>(newValue, builder.Uri.Scheme);
        }

        [Fact]
        public void UriBuilder_ChangeUser_Refreshed()
        {
            UriBuilder builder = new UriBuilder(s_starterUri);
            Assert.Equal<String>(s_starterUri.UserInfo, builder.UserName + ":" + builder.Password);
            Assert.Equal<String>(s_starterUri.UserInfo, builder.Uri.UserInfo);
            string newValue = "newvalue";
            builder.UserName = newValue;
            Assert.Equal<String>(newValue, builder.UserName);
            Assert.Equal<String>(newValue + ":" + builder.Password, builder.Uri.UserInfo);
        }

        [Fact]
        public void UriBuilder_ChangePassword_Refreshed()
        {
            UriBuilder builder = new UriBuilder(s_starterUri);
            Assert.Equal<String>(s_starterUri.UserInfo, builder.UserName + ":" + builder.Password);
            Assert.Equal<String>(s_starterUri.UserInfo, builder.Uri.UserInfo);
            string newValue = "newvalue";
            builder.Password = newValue;
            Assert.Equal<String>(newValue, builder.Password);
            Assert.Equal<String>(builder.UserName + ":" + newValue, builder.Uri.UserInfo);
        }

        [Fact]
        public void UriBuilder_ChangeHost_Refreshed()
        {
            UriBuilder builder = new UriBuilder(s_starterUri);
            Assert.Equal<String>(s_starterUri.Host, builder.Host);
            Assert.Equal<String>(s_starterUri.Host, builder.Uri.Host);
            string newValue = "newvalue";
            builder.Host = newValue;
            Assert.Equal<String>(newValue, builder.Host);
            Assert.Equal<String>(newValue, builder.Uri.Host);
        }

        [Fact]
        public void UriBuilder_ChangePort_Refreshed()
        {
            UriBuilder builder = new UriBuilder(s_starterUri);
            Assert.Equal<int>(s_starterUri.Port, builder.Port);
            Assert.Equal<int>(s_starterUri.Port, builder.Uri.Port);
            int newValue = 1010;
            builder.Port = newValue;
            Assert.Equal<int>(newValue, builder.Port);
            Assert.Equal<int>(newValue, builder.Uri.Port);
        }

        [Fact]
        public void UriBuilder_ChangePath_Refreshed()
        {
            UriBuilder builder = new UriBuilder(s_starterUri);
            Assert.Equal<String>(s_starterUri.AbsolutePath, builder.Path);
            Assert.Equal<String>(s_starterUri.AbsolutePath, builder.Uri.AbsolutePath);
            string newValue = "/newvalue";
            builder.Path = newValue;
            Assert.Equal<String>(newValue, builder.Path);
            Assert.Equal<String>(newValue, builder.Uri.AbsolutePath);
        }

        [Fact]
        public void UriBuilder_ChangeQuery_Refreshed()
        {
            UriBuilder builder = new UriBuilder(s_starterUri);
            Assert.Equal<String>(s_starterUri.Query, builder.Query);
            Assert.Equal<String>(s_starterUri.Query, builder.Uri.Query);
            string newValue = "newvalue";
            builder.Query = newValue;
            Assert.Equal<String>("?" + newValue, builder.Query);
            Assert.Equal<String>("?" + newValue, builder.Uri.Query);
        }

        [Fact]
        public void UriBuilder_ChangeFragment_Refreshed()
        {
            UriBuilder builder = new UriBuilder(s_starterUri);
            Assert.Equal<String>(s_starterUri.Fragment, builder.Fragment);
            Assert.Equal<String>(s_starterUri.Fragment, builder.Uri.Fragment);
            string newValue = "newvalue";
            builder.Fragment = newValue;
            Assert.Equal<String>("#" + newValue, builder.Fragment);
            Assert.Equal<String>("#" + newValue, builder.Uri.Fragment);
        }
    }
}
