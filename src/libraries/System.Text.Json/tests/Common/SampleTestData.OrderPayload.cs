// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Text.Json.Serialization.Tests.Schemas.OrderPayload
{ 
    public partial class Order
    {
        public long OrderNumber { get; set; }
        public User Customer { get; set; }
        public IEnumerable<Product> Products { get; set; }
        public IEnumerable<ShippingInfo> ShippingInfo { get; set; }
        public bool OneTime { get; set; }
        public bool Cancelled { get; set; }
        public bool IsGift { get; set; }
        public bool IsGPickUp { get; set; }
        public Address ShippingAddress { get; set; }
        public Address PickupAddress { get; set; }
        public SampleEnumInt64 Coupon { get; set; }
        public IEnumerable<Comment> UserInteractions { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
        public DateTime Confirmed { get; set; }
        public DateTime ShippingDate { get; set; }
        public DateTime EstimatedDelivery { get; set; }
        public IEnumerable<Order> RelatedOrder { get; set; }
        public User ReviewedBy { get; set; }
    }
    
    public class Product
    {
        public Guid ProductId { get; set; }
        public string SKU { get; set; }
        public TestClassWithInitializedProperties Brand { get; set; }
        public SimpleTestClassWithNonGenericCollectionWrappers ProductCategory { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Title { get; set; }
        public Price Price { get; set; }
        public bool BestChoice { get; set; }
        public float AverageStars { get; set; }  
        public bool Featured { get; set; }
        public TestClassWithInitializedProperties ProductRestrictions { get; set; }
        public SimpleTestClassWithGenericCollectionWrappers SalesInfo { get; set; }
        public IEnumerable<Review> Reviews { get; set; }
        public SampleEnum Origin { get; set; }
        public BasicCompany Manufacturer { get; set; }
        public bool Fragile { get; set; }
        public Uri DetailsUrl { get; set; }
        public decimal NetWeight { get; set; }
        public decimal GrossWeight { get; set; }
        public int Length { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public FeaturedImage FeaturedImage { get; set; }
        public PreviewImage PreviewImage { get; set; }
        public IEnumerable<string> KeyWords;
        public IEnumerable<Image> RelatedImages { get; set; }
        public Uri RelatedVideo { get; set; }
        public DateTime DeletedAt { get; set; }
        public DateTime GuaranteeStartsAt { get; set; }
        public DateTime GuaranteeEndsAt { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
        public bool IsActive { get; set; }
        public IEnumerable<Product> SimilarProducts { get; set; }
        public IEnumerable<Product> RelatedProducts { get; set; }
    }

    public class Review
    {
        public long ReviewId { get; set; } 
        public User Customer { get; set; }
        public string ProductSku { get; set; }
        public string CustomerName { get; set; }
        public int Stars { get; set; }
        public string Title { get; set; }
        public string Comment { get; set; }
        public IEnumerable<Uri> Images { get; set; }
    }

    public class Comment
    {
        public long Id { get; set; }
        public long OrderNumber { get; set; }
        public User Customer { get; set; }
        public User Employee { get; set; }
        public IEnumerable<Comment> Responses { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
    }

    public class ShippingInfo
    {
        public long OrderNumber { get; set; }
        public User Employee { get; set; }
        public string CarrierId { get; set; }
        public string ShippingType { get; set; }
        public DateTime EstimatedDelivery { get; set; }
        public Uri Tracking { get; set; }
        public string CarrierName { get; set; }
        public string HandlingInstruction { get; set; }
        public string CurrentStatus { get; set; }
        public bool IsDangerous { get; set; }
    }

    public class Price
    {
        public Product Product { get; set; }
        public bool AllowDiscount { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal RecommendedPrice { get; set; }
        public decimal FinalPrice { get; set; }
        public SampleEnumInt16 DiscountType { get; set; }
    }

    public class PreviewImage
    {
        public string Id { get; set; }
        public string Filter { get; set; }
        public string Size { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class FeaturedImage
    {
        public string Id { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string PhotoId { get; set; }
    }

    public class Image
    {
        public string Id { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class User
    {
        public BasicPerson PersonalInfo { get; set; }
        public string UserId { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string ImageId { get; set; }
        public string TwitterId { get; set; }
        public string FacebookId { get; set; }
        public int SubscriptionType { get; set; }
        public bool IsNew { get; set; }
        public bool IsEmployee { get; set; }
        public UserType UserType { get; set; }
    }

    public enum UserType
    {
        Customer = 1,
        Employee = 2,
        Supplier = 3
    }

    public partial class Order
    {
        public static List<Order> PopulateLargeObject(int size)
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
