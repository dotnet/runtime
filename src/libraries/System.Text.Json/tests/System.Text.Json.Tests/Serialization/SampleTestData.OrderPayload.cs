// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Text.Json.Serialization.Tests.Schemas.OrderPayload
{ 
    public class Order
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
}
