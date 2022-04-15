// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Serialization.Tests;
using System.Text.Json.Serialization.Tests.Schemas.OrderPayload;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json
{
    internal static partial class JsonTestHelper
    {
        public static void AssertJsonEqual(string expected, string actual)
        {
            using JsonDocument expectedDom = JsonDocument.Parse(expected);
            using JsonDocument actualDom = JsonDocument.Parse(actual);
            AssertJsonEqual(expectedDom.RootElement, actualDom.RootElement, new());
        }

        private static void AssertJsonEqual(JsonElement expected, JsonElement actual, Stack<object> path)
        {
            JsonValueKind valueKind = expected.ValueKind;
            AssertTrue(passCondition: valueKind == actual.ValueKind);

            switch (valueKind)
            {
                case JsonValueKind.Object:
                    var expectedProperties = new List<string>();
                    foreach (JsonProperty property in expected.EnumerateObject())
                    {
                        expectedProperties.Add(property.Name);
                    }

                    var actualProperties = new List<string>();
                    foreach (JsonProperty property in actual.EnumerateObject())
                    {
                        actualProperties.Add(property.Name);
                    }

                    foreach (var property in expectedProperties.Except(actualProperties))
                    {
                        AssertTrue(passCondition: false, $"Property \"{property}\" missing from actual object.");
                    }

                    foreach (var property in actualProperties.Except(expectedProperties))
                    {
                        AssertTrue(passCondition: false, $"Actual object defines additional property \"{property}\".");
                    }

                    foreach (string name in expectedProperties)
                    {
                        path.Push(name);
                        AssertJsonEqual(expected.GetProperty(name), actual.GetProperty(name), path);
                        path.Pop();
                    }
                    break;
                case JsonValueKind.Array:
                    JsonElement.ArrayEnumerator expectedEnumerator = expected.EnumerateArray();
                    JsonElement.ArrayEnumerator actualEnumerator = actual.EnumerateArray();

                    int i = 0;
                    while (expectedEnumerator.MoveNext())
                    {
                        AssertTrue(passCondition: actualEnumerator.MoveNext(), "Actual array contains fewer elements.");
                        path.Push(i++);
                        AssertJsonEqual(expectedEnumerator.Current, actualEnumerator.Current, path);
                        path.Pop();
                    }

                    AssertTrue(passCondition: !actualEnumerator.MoveNext(), "Actual array contains additional elements.");
                    break;
                case JsonValueKind.String:
                    AssertTrue(passCondition: expected.GetString() == actual.GetString());
                    break;
                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                case JsonValueKind.Null:
                    AssertTrue(passCondition: expected.GetRawText() == actual.GetRawText());
                    break;
                default:
                    Debug.Fail($"Unexpected JsonValueKind: JsonValueKind.{valueKind}.");
                    break;
            }

            void AssertTrue(bool passCondition, string? message = null)
            {
                if (!passCondition)
                {
                    message ??= "Expected JSON does not match actual value";
                    Assert.Fail($"{message}\nExpected JSON: {expected}\n  Actual JSON: {actual}\n  in JsonPath: {BuildJsonPath(path)}");
                }

                // TODO replace with JsonPath implementation for JsonElement
                // cf. https://github.com/dotnet/runtime/issues/31068
                static string BuildJsonPath(Stack<object> path)
                {
                    var sb = new StringBuilder("$");
                    foreach (object node in path.Reverse())
                    {
                        string pathNode = node is string propertyName
                            ? "." + propertyName
                            : $"[{(int)node}]";

                        sb.Append(pathNode);
                    }
                    return sb.ToString();
                }
            }
        }

        public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source)
        {
            var list = new List<T>();
            await foreach (T item in source)
            {
                list.Add(item);
            }
            return list;
        }

        private static readonly Regex s_stripWhitespace = new Regex(@"\s+", RegexOptions.Compiled);

        public static string StripWhitespace(this string value)
            => s_stripWhitespace.Replace(value, string.Empty);

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
                            Images = new List<Uri> { new Uri($"http://dotnet.test/link/images/image/{k}"), new Uri($"http://dotnet.test/link/images/image/{j}") },
                            ReviewId = i + j + k
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
}
