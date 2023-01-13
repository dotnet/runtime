using System;
using System.Net.Http;
using System.Threading;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Moq;

using Xunit;

namespace HttpClientHandlerDependencyInjection
{
    public class HttpClientHandlerFactoryDependencyInjectionTests
    {
        string nextFunctionInput = "throw";

        [Fact]
        public void Demonstrate_That_ExceptionsInHttpFactoriesPersist_Incorrectly()
        {
            var myException = new Exception("The one and only exception");
            var mockHandlerFactory = new Mock<IHandlerFactory>();
            mockHandlerFactory
                .Setup(mock => mock.Create("throw"))
                .Callback(() => nextFunctionInput = "don't throw")
                .Throws(myException);

            mockHandlerFactory
                .Setup(mock => mock.Create("don't throw"))
                .Returns(new HttpClientHandler());

            var services = GetServiceCollection(mockHandlerFactory.Object);
            var a = services.GetService<IHttpClientFactory>();

            // Should throw on the first call
            var act = () => a.CreateClient("myClient");
            var exFromFirstCall = act.Should().Throw<Exception>().Which;

            mockHandlerFactory.Verify(mock => mock.Create("throw"), Times.Once);

            // Wait 1 second -- It's the minimum handler lifetime possible
            Thread.Sleep(1001);

            // The following code fails, but should not.
            // The client that is created is a part of a lazy initialization
            // The ActiveHandlerTrackingEntry is incorrectly caching the exception indefinitely (https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Http/src/DefaultHttpClientFactory.cs#L118)
            // The entry timer is not being started due to the exception (https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Http/src/DefaultHttpClientFactory.cs#L120)
            ////var client = a.CreateClient("myClient");
            ////mockHandlerFactory.Verify(mock => mock.Create("don't throw"), Times.Once);
            ////client.Should().BeOfType<HttpClientHandler>();

            // The following code succeeds, but should not
            var act2 = () => a.CreateClient("myClient");
            var exception = act2.Should().Throw<Exception>("The one and only exception").Which;
            exception.Should().Be(exFromFirstCall);
        }

        private IServiceProvider GetServiceCollection(IHandlerFactory handlerFactory)
        {
            var services = new ServiceCollection();
            services.AddHttpClient("myClient")
           .ConfigurePrimaryHttpMessageHandler((p) =>
           {
               return handlerFactory.Create(nextFunctionInput);
           })
           .SetHandlerLifetime(TimeSpan.FromSeconds(1)); // Minimum timespan is 1 second

            return services.BuildServiceProvider();
        }

        public interface IHandlerFactory
        {
            HttpClientHandler Create(string toDo);
        }
    }
}
