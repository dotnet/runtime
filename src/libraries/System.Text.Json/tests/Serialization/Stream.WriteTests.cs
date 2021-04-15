// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization.Tests.Schemas.OrderPayload;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class StreamTests
    {
        [Fact]
        public static async Task WriteNullArgumentFail()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await JsonSerializer.SerializeAsync((Stream)null, 1));
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await JsonSerializer.SerializeAsync((Stream)null, 1, typeof(int)));
        }

        [Fact]
        public static async Task VerifyValueFail()
        {
            MemoryStream stream = new MemoryStream();
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await JsonSerializer.SerializeAsync(stream, "", (Type)null));
        }

        [Fact]
        public static async Task VerifyTypeFail()
        {
            MemoryStream stream = new MemoryStream();
            await Assert.ThrowsAsync<ArgumentException>(async () => await JsonSerializer.SerializeAsync(stream, 1, typeof(string)));
        }

        [Fact]
        public static async Task NullObjectValue()
        {
            MemoryStream stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, (object)null);

            stream.Seek(0, SeekOrigin.Begin);

            byte[] readBuffer = new byte[4];
            int bytesRead = stream.Read(readBuffer, 0, 4);

            Assert.Equal(4, bytesRead);
            string value = Encoding.UTF8.GetString(readBuffer);
            Assert.Equal("null", value);
        }

        [Fact]
        [SkipOnCoreClr("https://github.com/dotnet/runtime/issues/45464", RuntimeConfiguration.Checked)]
        public static async Task RoundTripAsync()
        {
            byte[] buffer;

            using (TestStream stream = new TestStream(1))
            {
                await WriteAsync(stream);

                // Make a copy
                buffer = stream.ToArray();
            }

            using (TestStream stream = new TestStream(buffer))
            {
                await ReadAsync(stream);
            }
        }

        [Fact]
        public static async Task RoundTripLargeJsonViaJsonElementAsync()
        {
            // Generating tailored json
            int i = 0;
            StringBuilder json = new StringBuilder();
            json.Append("{");
            while (true)
            {
                if (json.Length >= 14757)
                {
                    break;
                }
                json.AppendFormat(@"""Key_{0}"":""{0}"",", i);
                i++;
            }
            json.Remove(json.Length - 1, 1).Append("}");

            JsonElement root = JsonSerializer.Deserialize<JsonElement>(json.ToString());
            var ms = new MemoryStream();
            await JsonSerializer.SerializeAsync(ms, root, root.GetType());
        }

        [Fact]
        public static async Task RoundTripLargeJsonViaPocoAsync()
        {
            byte[] array = JsonSerializer.Deserialize<byte[]>(JsonSerializer.Serialize(new byte[11056]));
            var ms = new MemoryStream();
            await JsonSerializer.SerializeAsync(ms, array, array.GetType());
        }

        private static async Task WriteAsync(TestStream stream)
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                // Will likely default to 4K due to buffer pooling.
                DefaultBufferSize = 1
            };

            {
                LargeDataTestClass obj = new LargeDataTestClass();
                obj.Initialize();
                obj.Verify();

                await JsonSerializer.SerializeAsync(stream, obj, options: options);
            }

            // Must be changed if the test classes change:
            Assert.Equal(551_368, stream.TestWriteBytesCount);

            // We should have more than one write called due to the large byte count.
            Assert.InRange(stream.TestWriteCount, 1, int.MaxValue);

            // We don't auto-flush.
            Assert.Equal(0, stream.TestFlushCount);
        }

        private static async Task ReadAsync(TestStream stream)
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                // Will likely default to 4K due to buffer pooling.
                DefaultBufferSize = 1
            };

            LargeDataTestClass obj = await JsonSerializer.DeserializeAsync<LargeDataTestClass>(stream, options);
            // Must be changed if the test classes change; may be > since last read may not have filled buffer.
            Assert.InRange(stream.TestRequestedReadBytesCount, 551368, int.MaxValue);

            // We should have more than one read called due to the large byte count.
            Assert.InRange(stream.TestReadCount, 1, int.MaxValue);

            // We don't auto-flush.
            Assert.Equal(0, stream.TestFlushCount);

            obj.Verify();
        }

        [Fact]
        public static async Task WritePrimitivesAsync()
        {
            MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(@"1"));
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                DefaultBufferSize = 1
            };

            int i = await JsonSerializer.DeserializeAsync<int>(stream, options);
            Assert.Equal(1, i);
        }

        private class Session
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public virtual string Abstract { get; set; }
            public virtual DateTimeOffset? StartTime { get; set; }
            public virtual DateTimeOffset? EndTime { get; set; }
            public TimeSpan Duration => EndTime?.Subtract(StartTime ?? EndTime ?? DateTimeOffset.MinValue) ?? TimeSpan.Zero;
            public int? TrackId { get; set; }
        }

        private class SessionResponse : Session
        {
            public Track Track { get; set; }
            public List<Speaker> Speakers { get; set; } = new List<Speaker>();
        }

        private class Track
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        private class Speaker
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Bio { get; set; }
            public virtual string WebSite { get; set; }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(1000)]
        [InlineData(4000)]
        [InlineData(8000)]
        [InlineData(16000)]
        [InlineData(32000)]
        [InlineData(64000)]
        public static async Task LargeJsonFile(int bufferSize)
        {
            const int SessionResponseCount = 100;

            // Build up a large list to serialize.
            var list = new List<SessionResponse>();
            for (int i = 0; i < SessionResponseCount; i++)
            {
                SessionResponse response = new SessionResponse
                {
                    Id = i,
                    Abstract = new string('A', i * 2),
                    Title = new string('T', i),
                    StartTime = new DateTime(i, DateTimeKind.Utc),
                    EndTime = new DateTime(i * 10000, DateTimeKind.Utc),
                    TrackId = i,
                    Track = new Track()
                    {
                        Id = i,
                        Name = new string('N', i),
                    },
                };

                for (int j = 0; j < 5; j++)
                {
                    response.Speakers.Add(new Speaker()
                    {
                        Bio = new string('B', 50),
                        Id = j,
                        Name = new string('N', i),
                        WebSite = new string('W', 20),
                    });
                }

                list.Add(response);
            }

            // Adjust buffer length to encourage buffer flusing at several levels.
            JsonSerializerOptions options = new JsonSerializerOptions();
            if (bufferSize != 0)
            {
                options.DefaultBufferSize = bufferSize;
            }

            string json = JsonSerializer.Serialize(list, options);
            Assert.True(json.Length > 100_000); // Verify data is large and will cause buffer flushing.
            Assert.True(json.Length < 200_000); // But not too large for memory considerations.

            // Sync case.
            {
                List<SessionResponse> deserializedList = JsonSerializer.Deserialize<List<SessionResponse>>(json, options);
                Assert.Equal(SessionResponseCount, deserializedList.Count);

                string jsonSerialized = JsonSerializer.Serialize(deserializedList, options);
                Assert.Equal(json, jsonSerialized);
            }

            // Async case.
            using (var memoryStream = new MemoryStream())
            {
                await JsonSerializer.SerializeAsync(memoryStream, list, options);
                string jsonSerialized = Encoding.UTF8.GetString(memoryStream.ToArray());
                Assert.Equal(json, jsonSerialized);

                memoryStream.Position = 0;
                List<SessionResponse> deserializedList = await JsonSerializer.DeserializeAsync<List<SessionResponse>>(memoryStream, options);
                Assert.Equal(SessionResponseCount, deserializedList.Count);
            }
        }

        [Theory]
        [InlineData(1, true, true)]
        [InlineData(1, true, false)]
        [InlineData(1, false, true)]
        [InlineData(1, false, false)]
        [InlineData(10, true, false)]
        [InlineData(10, false, false)]
        [InlineData(100, false, false)]
        [InlineData(1000, false, false)]
        public static async Task VeryLargeJsonFileTest(int payloadSize, bool ignoreNull, bool writeIndented)
        {
            List<Order> list = PopulateLargeObject(payloadSize);

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                IgnoreNullValues = ignoreNull,
                WriteIndented = writeIndented
            };

            string json = JsonSerializer.Serialize(list, options);

            // Sync case.
            {
                List<Order> deserializedList = JsonSerializer.Deserialize<List<Order>>(json, options);
                Assert.Equal(payloadSize, deserializedList.Count);

                string jsonSerialized = JsonSerializer.Serialize(deserializedList, options);
                Assert.Equal(json, jsonSerialized);
            }

            // Async case.
            using (var memoryStream = new MemoryStream())
            {
                await JsonSerializer.SerializeAsync(memoryStream, list, options);
                string jsonSerialized = Encoding.UTF8.GetString(memoryStream.ToArray());
                Assert.Equal(json, jsonSerialized);

                memoryStream.Position = 0;
                List<Order> deserializedList = await JsonSerializer.DeserializeAsync<List<Order>>(memoryStream, options);
                Assert.Equal(payloadSize, deserializedList.Count);
            }
        }

        [Theory]
        [InlineData(1, true, true)]
        [InlineData(1, true, false)]
        [InlineData(1, false, true)]
        [InlineData(1, false, false)]
        [InlineData(2, true, false)]
        [InlineData(2, false, false)]
        [InlineData(4, false, false)]
        [InlineData(8, false, false)]
        [InlineData(16, false, false)] // This results a reader\writer depth of 324 which currently works on all test platforms.
        public static async Task DeepNestedJsonFileTest(int depthFactor, bool ignoreNull, bool writeIndented)
        {
            const int ListLength = 10;

            int length = ListLength * depthFactor;
            List<Order>[] orders = new List<Order>[length];
            orders[0] = PopulateLargeObject(1);
            for (int i = 1; i < length; i++ )
            {
                orders[i] = PopulateLargeObject(1);
                orders[i - 1][0].RelatedOrder = orders[i];
            }

            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                MaxDepth = (ListLength * depthFactor * 2) + 4, // Order-to-RelatedOrder has a depth of 2.
                IgnoreNullValues = ignoreNull,
                WriteIndented = writeIndented
            };
            string json = JsonSerializer.Serialize(orders[0], options);

            // Sync case.
            {
                List<Order> deserializedList = JsonSerializer.Deserialize<List<Order>>(json, options);

                string jsonSerialized = JsonSerializer.Serialize(deserializedList, options);
                Assert.Equal(json, jsonSerialized);
            }

            // Async case.
            using (var memoryStream = new MemoryStream())
            {
                await JsonSerializer.SerializeAsync(memoryStream, orders[0], options);
                string jsonSerialized = Encoding.UTF8.GetString(memoryStream.ToArray());
                Assert.Equal(json, jsonSerialized);

                memoryStream.Position = 0;
                List<Order> deserializedList = await JsonSerializer.DeserializeAsync<List<Order>>(memoryStream, options);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(4)]
        public static async Task NestedJsonFileCircularDependencyTest(int depthFactor)
        {
            const int ListLength = 2;

            int length = ListLength * depthFactor;
            List<Order>[] orders = new List<Order>[length];
            orders[0] = PopulateLargeObject(1000);
            for (int i = 1; i < length; i++)
            {
                orders[i] = PopulateLargeObject(1);
                orders[i - 1][0].RelatedOrder = orders[i];
            }

            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                IgnoreNullValues = true
            };

            // Ensure no exception for default settings (MaxDepth=64) and no cycle.
            JsonSerializer.Serialize(orders[0], options);

            // Create a cycle.
            orders[length - 1][0].RelatedOrder = orders[0];

            Assert.Throws<JsonException> (() => JsonSerializer.Serialize(orders[0], options));

            using (var memoryStream = new MemoryStream())
            {
                await Assert.ThrowsAsync<JsonException>(async () => await JsonSerializer.SerializeAsync(memoryStream, orders[0], options));
            }
        }

        [Theory]
        [InlineData(128)]
        [InlineData(1024)]
        [InlineData(4096)]
        [InlineData(8192)]
        [InlineData(16384)]
        [InlineData(65536)]
        public static async Task FlushThresholdTest(int bufferSize)
        {
            // bufferSize * 0.9 is the threshold size from codebase, subtract 2 for [" characters, then create a 
            // string containing (threshold - 2) amount of char 'a' which when written into output buffer produces buffer 
            // which size equal to or very close to threshold size, then adding the string to the list, then adding a big 
            // object to the list which changes depth of written json and should cause buffer flush
            int thresholdSize = (int)(bufferSize * 0.9 - 2);
            FlushThresholdTestClass serializeObject = new FlushThresholdTestClass(GenerateListOfSize(bufferSize));
            List<object> list = new List<object>();
            string stringOfThresholdSize = new string('a', thresholdSize);
            list.Add(stringOfThresholdSize);
            serializeObject.StringProperty = stringOfThresholdSize;
            list.Add(serializeObject);
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.DefaultBufferSize = bufferSize;

            string json = JsonSerializer.Serialize(list);

            using (var memoryStream = new MemoryStream())
            {
                await JsonSerializer.SerializeAsync(memoryStream, list, options);
                string jsonSerialized = Encoding.UTF8.GetString(memoryStream.ToArray());
                Assert.Equal(json, jsonSerialized);

                List<object> deserializedList = JsonSerializer.Deserialize<List<object>>(json, options);
                Assert.Equal(stringOfThresholdSize, ((JsonElement)deserializedList[0]).GetString());
                JsonElement obj = (JsonElement)deserializedList[1];             
                Assert.Equal(stringOfThresholdSize, obj.GetProperty("StringProperty").GetString());
            }
        }

        private class FlushThresholdTestClass 
        {
            public string StringProperty { get; set; }
            public List<int> ListOfInts { get; set; }
            public FlushThresholdTestClass(List<int> list)
            {
                ListOfInts = list;
            }
        }

        private static List<int> GenerateListOfSize(int size)
        {
            List<int> list = new List<int>();
            for (int i = 0; i < size; i++)
            {
                list.Add(1);
            }
            return list;
        }

        internal static List<Order> PopulateLargeObject(int size)
        {
            List<Order> orders = new List<Order>(size);
            for (int i = 0; i < size; i++)
            {
                Order order = new Order
                {
                    OrderNumber = i,
                    Customer = new User
                    {
                        UserId = "222ffbbb888kkk",
                        Name = "John Doe",
                        Username = "johndoe",
                        CreatedAt = new DateTime(),
                        ImageId = string.Empty,
                        UserType = UserType.Customer,
                        UpdatedAt = new DateTime(),
                        TwitterId = string.Empty,
                        FacebookId = "9988998877662222111",
                        SubscriptionType = 2,
                        IsNew = true,
                        IsEmployee = false
                    },
                    ShippingInfo = new List<ShippingInfo>
                    {
                        new ShippingInfo()
                        {
                            OrderNumber = i,
                            Employee = new User
                            {
                                UserId = "222ffbbb888" + i,
                                Name = "Shipping Coordinator",
                                Username = "coordinator" + i,
                                CreatedAt = new DateTime(),
                                ImageId = string.Empty,
                                UserType = UserType.Employee,
                                UpdatedAt = new DateTime(),
                                TwitterId = string.Empty,
                                SubscriptionType = 0,
                                IsEmployee = true
                            },
                            CarrierId = "TTT123999MMM",
                            ShippingType = "Ground",
                            EstimatedDelivery = new DateTime(),
                            Tracking = new Uri("http://TestShipCompany.test/track/123" + i),
                            CarrierName = "TestShipCompany",
                            HandlingInstruction = "Do cats eat bats? Do cats eat bats. Do cats eat bats? Do cats eat bats. Do cats eat bats? Do cats eat bats. Do cats eat bats? Do cats eat bats",
                            CurrentStatus = "Out for delivery",
                            IsDangerous = false
                        }
                    },
                    OneTime = true,
                    Cancelled = false,
                    IsGift = i % 2 == 0,
                    IsGPickUp = i % 5 == 0,
                    ShippingAddress = new Address()
                    {
                        City = "Redmond"
                    },
                    PickupAddress = new Address
                    {
                        City = "Bellevue"
                    },
                    Coupon = SampleEnumInt64.Max,
                    UserInteractions = new List<Comment>
                    {
                        new Comment
                        {
                            Id = 200 + i,
                            OrderNumber = i,
                            Customer = new User
                            {
                                UserId = "222ffbbb888kkk",
                                Name = "John Doe",
                                Username = "johndoe",
                                CreatedAt = new DateTime(),
                                ImageId = string.Empty,
                                UserType = UserType.Customer,
                                UpdatedAt = new DateTime(),
                                TwitterId = "twitterId" + i,
                                FacebookId = "9988998877662222111",
                                SubscriptionType = 2,
                                IsNew = true,
                                IsEmployee = false
                            },
                            Title = "Green Field",
                            Message = "Down, down, down. Would the fall never come to an end! 'I wonder how many miles I've fallen by this time. I think-' (for, you see, Alice had learnt several things of this sort in her lessons in the schoolroom, and though this was not a very good opportunity for showing off her knowledge, as there was no one to listen to her, still it was good practice to say it over) '-yes, that's about the right distance-but then I wonder what Latitude or Longitude I've got to",
                            Responses = new List<Comment>()
                        }
                    },
                    Created = new DateTime(2019, 11, 10),
                    Confirmed = new DateTime(2019, 11, 11),
                    ShippingDate = new DateTime(2019, 11, 12),
                    EstimatedDelivery = new DateTime(2019, 11, 15),
                    ReviewedBy = new User()
                    {
                        UserId = "222ffbbb888" + i,
                        Name = "Shipping Coordinator",
                        Username = "coordinator" + i,
                        CreatedAt = new DateTime(),
                        ImageId = string.Empty,
                        UserType = UserType.Employee,
                        UpdatedAt = new DateTime(),
                        TwitterId = string.Empty,
                        SubscriptionType = 0,
                        IsEmployee = true
                    }
                };
                List<Product> products = new List<Product>();
                for (int j = 0; j < i % 4; j++)
                {
                    Product product = new Product()
                    {
                        ProductId = Guid.NewGuid(),
                        Name = "Surface Pro",
                        SKU = "LL123" + j,
                        Brand = new TestClassWithInitializedProperties(),
                        ProductCategory = new SimpleTestClassWithNonGenericCollectionWrappers(),
                        Description = "Down, down, down. Would the fall never come to an end! 'I wonder how many miles I've fallen by this time. I think-' (for, you see, Alice had learnt several things of this sort in her lessons in the schoolroom, and though this was not a very good opportunity for showing off her knowledge, as there was no one to listen to her, still it was good practice to say it over) '-yes, that's about the right distance-but then I wonder what Latitude or Longitude I've got to",
                        Created = new DateTime(2000, 10, 12),
                        Title = "Surface Pro 6 for Business - 512GB",
                        Price = new Price(),
                        BestChoice = true,
                        AverageStars = 4.8f,
                        Featured = true,
                        ProductRestrictions = new TestClassWithInitializedProperties(),
                        SalesInfo = new SimpleTestClassWithGenericCollectionWrappers(),
                        Origin = SampleEnum.One,
                        Manufacturer = new BasicCompany(),
                        Fragile = true,
                        DetailsUrl = new Uri("http://dotnet.test/link/entries/entry/1"),
                        NetWeight = 2.7m,
                        GrossWeight = 3.3m,
                        Length = i,
                        Height = i + 1,
                        Width = i + 2,
                        FeaturedImage = new FeaturedImage(),
                        PreviewImage = new PreviewImage(),
                        KeyWords = new List<string> { "surface", "pro", "laptop" },
                        RelatedImages = new List<Image>(),
                        RelatedVideo = new Uri("http://dotnet.test/link/entries/entry/2"),
                        GuaranteeStartsAt = new DateTime(),
                        GuaranteeEndsAt = new DateTime(),
                        IsActive = true,
                        RelatedProducts = new List<Product>()
                    };
                    product.SalesInfo.Initialize();
                    List<Review> reviews = new List<Review>();
                    for (int k = 0; k < i % 3; k++)
                    {

                        Review review = new Review
                        {
                            Customer = new User
                            {
                                UserId = "333344445555",
                                Name = "Customer" + i + k,
                                Username = "cust" + i + k,
                                CreatedAt = new DateTime(),
                                ImageId = string.Empty,
                                UserType = UserType.Customer,
                                SubscriptionType = k
                            },
                            ProductSku = product.SKU,
                            CustomerName = "Customer" + i + k,
                            Stars = j + k,
                            Title = $"Title {i}{j}{k}",
                            Comment = "",
                            Images = new List<Uri>{ new Uri($"http://dotnet.test/link/images/image/{k}"), new Uri($"http://dotnet.test/link/images/image/{j}")},
                            ReviewId = i + j +k
                        };
                        reviews.Add(review);
                    }
                    product.Reviews = reviews;
                    products.Add(product);
                }
                order.Products = products;
                orders.Add(order);
            }
            return orders;
        }
    }

    public sealed class TestStream : Stream
    {
        private readonly MemoryStream _stream;

        public TestStream(int capacity) { _stream = new MemoryStream(capacity); }

        public TestStream(byte[] buffer) { _stream = new MemoryStream(buffer); }

        public int TestFlushCount { get; private set; }

        public int TestWriteCount { get; private set; }
        public int TestWriteBytesCount { get; private set; }
        public int TestReadCount { get; private set; }
        public int TestRequestedReadBytesCount { get; private set; }

        public byte[] ToArray() => _stream.ToArray();

        public override void Flush()
        {
            TestFlushCount++;
            _stream.Flush();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            TestWriteCount++;
            TestWriteBytesCount += (count - offset);
            _stream.Write(buffer, offset, count);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            TestReadCount++;
            TestRequestedReadBytesCount += count;
            return _stream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);
        public override void SetLength(long value) => _stream.SetLength(value);
        public override bool CanRead => _stream.CanRead;
        public override bool CanSeek => _stream.CanSeek;
        public override bool CanWrite => _stream.CanWrite;
        public override long Length => _stream.Length;
        public override long Position { get => _stream.Position; set => _stream.Position = value; }
    }
}
