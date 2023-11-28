// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using Xunit;

public class Product
{
    public int ProductID { get; set; }
    public string ProductName { get; set; }
    public string Category { get; set; }
    public decimal UnitPrice { get; set; }
    public int UnitsInStock { get; set; }

    private static List<Product> s_productList;

    public static List<Product> GetProductList()
    {
        if (s_productList == null)
            CreateLists();

        return s_productList;
    }

    private static void CreateLists()
    {
        s_productList =
            new List<Product> {
                    new Product { ProductID = 1, ProductName = "Chai", Category = "Beverages", UnitPrice = 18.0000M, UnitsInStock = 39 },
                    new Product { ProductID = 2, ProductName = "Chang", Category = "Beverages", UnitPrice = 19.0000M, UnitsInStock = 17 },
                    new Product { ProductID = 3, ProductName = "Aniseed Syrup", Category = "Condiments", UnitPrice = 10.0000M, UnitsInStock = 13 },
                    new Product { ProductID = 4, ProductName = "Chef Anton's Cajun Seasoning", Category = "Condiments", UnitPrice = 22.0000M, UnitsInStock = 53 },
                    new Product { ProductID = 5, ProductName = "Chef Anton's Gumbo Mix", Category = "Condiments", UnitPrice = 21.3500M, UnitsInStock = 0 },
                    new Product { ProductID = 6, ProductName = "Grandma's Boysenberry Spread", Category = "Condiments", UnitPrice = 25.0000M, UnitsInStock = 120 },
                    new Product { ProductID = 7, ProductName = "Uncle Bob's Organic Dried Pears", Category = "Produce", UnitPrice = 30.0000M, UnitsInStock = 15 },
                    new Product { ProductID = 8, ProductName = "Northwoods Cranberry Sauce", Category = "Condiments", UnitPrice = 40.0000M, UnitsInStock = 6 },
                    new Product { ProductID = 9, ProductName = "Mishi Kobe Niku", Category = "Meat/Poultry", UnitPrice = 97.0000M, UnitsInStock = 29 },
                    new Product { ProductID = 10, ProductName = "Ikura", Category = "Seafood", UnitPrice = 31.0000M, UnitsInStock = 31 },
                    new Product { ProductID = 11, ProductName = "Queso Cabrales", Category = "Dairy Products", UnitPrice = 21.0000M, UnitsInStock = 22 },
                    new Product { ProductID = 12, ProductName = "Queso Manchego La Pastora", Category = "Dairy Products", UnitPrice = 38.0000M, UnitsInStock = 86 },
                    new Product { ProductID = 13, ProductName = "Konbu", Category = "Seafood", UnitPrice = 6.0000M, UnitsInStock = 24 },
                    new Product { ProductID = 14, ProductName = "Tofu", Category = "Produce", UnitPrice = 23.2500M, UnitsInStock = 35 },
                    new Product { ProductID = 15, ProductName = "Genen Shouyu", Category = "Condiments", UnitPrice = 15.5000M, UnitsInStock = 39 },
                    new Product { ProductID = 16, ProductName = "Pavlova", Category = "Confections", UnitPrice = 17.4500M, UnitsInStock = 29 },
                    new Product { ProductID = 17, ProductName = "Alice Mutton", Category = "Meat/Poultry", UnitPrice = 39.0000M, UnitsInStock = 0 },
                    new Product { ProductID = 18, ProductName = "Carnarvon Tigers", Category = "Seafood", UnitPrice = 62.5000M, UnitsInStock = 42 },
                    new Product { ProductID = 19, ProductName = "Teatime Chocolate Biscuits", Category = "Confections", UnitPrice = 9.2000M, UnitsInStock = 25 },
                    new Product { ProductID = 20, ProductName = "Sir Rodney's Marmalade", Category = "Confections", UnitPrice = 81.0000M, UnitsInStock = 40 },
                    new Product { ProductID = 21, ProductName = "Sir Rodney's Scones", Category = "Confections", UnitPrice = 10.0000M, UnitsInStock = 3 },
                    new Product { ProductID = 22, ProductName = "Gustaf's Kn\u00E4ckebr\u00F6d", Category = "Grains/Cereals", UnitPrice = 21.0000M, UnitsInStock = 104 },
                    new Product { ProductID = 23, ProductName = "Tunnbr\u00F6d", Category = "Grains/Cereals", UnitPrice = 9.0000M, UnitsInStock = 61 },
                    new Product { ProductID = 24, ProductName = "Guaran\u00E1 Fant\u00E1stica", Category = "Beverages", UnitPrice = 4.5000M, UnitsInStock = 20 },
                    new Product { ProductID = 25, ProductName = "NuNuCa Nu\u00DF-Nougat-Creme", Category = "Confections", UnitPrice = 14.0000M, UnitsInStock = 76 },
                    new Product { ProductID = 26, ProductName = "Gumb\u00E4r Gummib\u00E4rchen", Category = "Confections", UnitPrice = 31.2300M, UnitsInStock = 15 },
                    new Product { ProductID = 27, ProductName = "Schoggi Schokolade", Category = "Confections", UnitPrice = 43.9000M, UnitsInStock = 49 },
                    new Product { ProductID = 28, ProductName = "R\u00F6ssle Sauerkraut", Category = "Produce", UnitPrice = 45.6000M, UnitsInStock = 26 },
                    new Product { ProductID = 29, ProductName = "Th\u00FCringer Rostbratwurst", Category = "Meat/Poultry", UnitPrice = 123.7900M, UnitsInStock = 0 },
                    new Product { ProductID = 30, ProductName = "Nord-Ost Matjeshering", Category = "Seafood", UnitPrice = 25.8900M, UnitsInStock = 10 },
                    new Product { ProductID = 31, ProductName = "Gorgonzola Telino", Category = "Dairy Products", UnitPrice = 12.5000M, UnitsInStock = 0 },
                    new Product { ProductID = 32, ProductName = "Mascarpone Fabioli", Category = "Dairy Products", UnitPrice = 32.0000M, UnitsInStock = 9 },
                    new Product { ProductID = 33, ProductName = "Geitost", Category = "Dairy Products", UnitPrice = 2.5000M, UnitsInStock = 112 },
                    new Product { ProductID = 34, ProductName = "Sasquatch Ale", Category = "Beverages", UnitPrice = 14.0000M, UnitsInStock = 111 },
                    new Product { ProductID = 35, ProductName = "Steeleye Stout", Category = "Beverages", UnitPrice = 18.0000M, UnitsInStock = 20 },
                    new Product { ProductID = 36, ProductName = "Inlagd Sill", Category = "Seafood", UnitPrice = 19.0000M, UnitsInStock = 112 },
                    new Product { ProductID = 37, ProductName = "Gravad lax", Category = "Seafood", UnitPrice = 26.0000M, UnitsInStock = 11 },
                    new Product { ProductID = 38, ProductName = "C\u00F4te de Blaye", Category = "Beverages", UnitPrice = 263.5000M, UnitsInStock = 17 },
                    new Product { ProductID = 39, ProductName = "Chartreuse verte", Category = "Beverages", UnitPrice = 18.0000M, UnitsInStock = 69 },
                    new Product { ProductID = 40, ProductName = "Boston Crab Meat", Category = "Seafood", UnitPrice = 18.4000M, UnitsInStock = 123 },
                    new Product { ProductID = 41, ProductName = "Jack's New England Clam Chowder", Category = "Seafood", UnitPrice = 9.6500M, UnitsInStock = 85 },
                    new Product { ProductID = 42, ProductName = "Singaporean Hokkien Fried Mee", Category = "Grains/Cereals", UnitPrice = 14.0000M, UnitsInStock = 26 },
                    new Product { ProductID = 43, ProductName = "Ipoh Coffee", Category = "Beverages", UnitPrice = 46.0000M, UnitsInStock = 17 },
                    new Product { ProductID = 44, ProductName = "Gula Malacca", Category = "Condiments", UnitPrice = 19.4500M, UnitsInStock = 27 },
                    new Product { ProductID = 45, ProductName = "Rogede sild", Category = "Seafood", UnitPrice = 9.5000M, UnitsInStock = 5 },
                    new Product { ProductID = 46, ProductName = "Spegesild", Category = "Seafood", UnitPrice = 12.0000M, UnitsInStock = 95 },
                    new Product { ProductID = 47, ProductName = "Zaanse koeken", Category = "Confections", UnitPrice = 9.5000M, UnitsInStock = 36 },
                    new Product { ProductID = 48, ProductName = "Chocolade", Category = "Confections", UnitPrice = 12.7500M, UnitsInStock = 15 },
                    new Product { ProductID = 49, ProductName = "Maxilaku", Category = "Confections", UnitPrice = 20.0000M, UnitsInStock = 10 },
                    new Product { ProductID = 50, ProductName = "Valkoinen suklaa", Category = "Confections", UnitPrice = 16.2500M, UnitsInStock = 65 },
                    new Product { ProductID = 51, ProductName = "Manjimup Dried Apples", Category = "Produce", UnitPrice = 53.0000M, UnitsInStock = 20 },
                    new Product { ProductID = 52, ProductName = "Filo Mix", Category = "Grains/Cereals", UnitPrice = 7.0000M, UnitsInStock = 38 },
                    new Product { ProductID = 53, ProductName = "Perth Pasties", Category = "Meat/Poultry", UnitPrice = 32.8000M, UnitsInStock = 0 },
                    new Product { ProductID = 54, ProductName = "Tourti\u00E8re", Category = "Meat/Poultry", UnitPrice = 7.4500M, UnitsInStock = 21 },
                    new Product { ProductID = 55, ProductName = "P\u00E2t\u00E9 chinois", Category = "Meat/Poultry", UnitPrice = 24.0000M, UnitsInStock = 115 },
                    new Product { ProductID = 56, ProductName = "Gnocchi di nonna Alice", Category = "Grains/Cereals", UnitPrice = 38.0000M, UnitsInStock = 21 },
                    new Product { ProductID = 57, ProductName = "Ravioli Angelo", Category = "Grains/Cereals", UnitPrice = 19.5000M, UnitsInStock = 36 },
                    new Product { ProductID = 58, ProductName = "Escargots de Bourgogne", Category = "Seafood", UnitPrice = 13.2500M, UnitsInStock = 62 },
                    new Product { ProductID = 59, ProductName = "Raclette Courdavault", Category = "Dairy Products", UnitPrice = 55.0000M, UnitsInStock = 79 },
                    new Product { ProductID = 60, ProductName = "Camembert Pierrot", Category = "Dairy Products", UnitPrice = 34.0000M, UnitsInStock = 19 },
                    new Product { ProductID = 61, ProductName = "Sirop d'\u00E9rable", Category = "Condiments", UnitPrice = 28.5000M, UnitsInStock = 113 },
                    new Product { ProductID = 62, ProductName = "Tarte au sucre", Category = "Confections", UnitPrice = 49.3000M, UnitsInStock = 17 },
                    new Product { ProductID = 63, ProductName = "Vegie-spread", Category = "Condiments", UnitPrice = 43.9000M, UnitsInStock = 24 },
                    new Product { ProductID = 64, ProductName = "Wimmers gute Semmelkn\u00F6del", Category = "Grains/Cereals", UnitPrice = 33.2500M, UnitsInStock = 22 },
                    new Product { ProductID = 65, ProductName = "Louisiana Fiery Hot Pepper Sauce", Category = "Condiments", UnitPrice = 21.0500M, UnitsInStock = 76 },
                    new Product { ProductID = 66, ProductName = "Louisiana Hot Spiced Okra", Category = "Condiments", UnitPrice = 17.0000M, UnitsInStock = 4 },
                    new Product { ProductID = 67, ProductName = "Laughing Lumberjack Lager", Category = "Beverages", UnitPrice = 14.0000M, UnitsInStock = 52 },
                    new Product { ProductID = 68, ProductName = "Scottish Longbreads", Category = "Confections", UnitPrice = 12.5000M, UnitsInStock = 6 },
                    new Product { ProductID = 69, ProductName = "Gudbrandsdalsost", Category = "Dairy Products", UnitPrice = 36.0000M, UnitsInStock = 26 },
                    new Product { ProductID = 70, ProductName = "Outback Lager", Category = "Beverages", UnitPrice = 15.0000M, UnitsInStock = 15 },
                    new Product { ProductID = 71, ProductName = "Flotemysost", Category = "Dairy Products", UnitPrice = 21.5000M, UnitsInStock = 26 },
                    new Product { ProductID = 72, ProductName = "Mozzarella di Giovanni", Category = "Dairy Products", UnitPrice = 34.8000M, UnitsInStock = 14 },
                    new Product { ProductID = 73, ProductName = "R\u00F6d Kaviar", Category = "Seafood", UnitPrice = 15.0000M, UnitsInStock = 101 },
                    new Product { ProductID = 74, ProductName = "Longlife Tofu", Category = "Produce", UnitPrice = 10.0000M, UnitsInStock = 4 },
                    new Product { ProductID = 75, ProductName = "Rh\u00F6nbr\u00E4u Klosterbier", Category = "Beverages", UnitPrice = 7.7500M, UnitsInStock = 125 },
                    new Product { ProductID = 76, ProductName = "Lakkalik\u00F6\u00F6ri", Category = "Beverages", UnitPrice = 18.0000M, UnitsInStock = 57 },
                    new Product { ProductID = 77, ProductName = "Original Frankfurter gr\u00FCne So\u00DFe", Category = "Condiments", UnitPrice = 13.0000M, UnitsInStock = 32 }
        };
    }
}

public class LinqBenchmarks
{
#if DEBUG
    public const int IterationsWhere00 = 1;
    public const int IterationsWhere01 = 1;
    public const int IterationsCount00 = 1;
    public const int IterationsOrder00 = 1;
#else
    public const int IterationsWhere00 = 1000000;
    public const int IterationsWhere01 = 250000;
    public const int IterationsCount00 = 1000000;
    public const int IterationsOrder00 = 25000;
#endif

    private static volatile object s_volatileObject;

    private static void Escape(object obj)
    {
        s_volatileObject = obj;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool Bench()
    {
        bool result = true;
        result &= Where00();
        result &= Where01();
        result &= Count00();
        result &= Order00();
        return result;
    }

    #region Where00
    private bool Where00()
    {
        bool result = true;
        result &= Where00For();
        result &= Where00LinqMethod();
        result &= Where00LinqQuery();
        return result;
    }

    private bool Where00LinqQuery()
    {
        List<Product> products = Product.GetProductList();
        int count = 0;
        for (int i = 0; i < IterationsWhere00; i++)
        {
            var soldOutProducts =
                    from prod in products
                    where prod.UnitsInStock == 0
                    select prod;

            foreach (var product in soldOutProducts)
            {
                count++;
            }
        }

        return (count == 5 * IterationsWhere00);
    }

    private bool Where00LinqMethod()
    {
        List<Product> products = Product.GetProductList();
        int count = 0;
        for (int i = 0; i < IterationsWhere00; i++)
        {
            var soldOutProducts = products.Where(p => p.UnitsInStock == 0);

            foreach (var product in soldOutProducts)
            {
                count++;
            }
        }

        return (count == 5 * IterationsWhere00);
    }

    private bool Where00For()
    {
        List<Product> products = Product.GetProductList();
        int count = 0;
        for (int i = 0; i < IterationsWhere00; i++)
        {
            List<Product> soldOutProducts = new List<Product>();

            foreach (Product p in products)
            {
                if (p.UnitsInStock == 0)
                {
                    soldOutProducts.Add(p);
                }
            }

            foreach (var product in soldOutProducts)
            {
                count++;
            }
        }

        return (count == 5 * IterationsWhere00);
    }
    #endregion

    #region Where01
    private bool Where01()
    {
        bool result = true;
        result &= Where01For();
        result &= Where01LinqMethod();
        result &= Where01LinqQuery();
        return result;
    }

    private bool Where01LinqQuery()
    {
        List<Product> products = Product.GetProductList();
        int count = 0;
        for (int i = 0; i < IterationsWhere01; i++)
        {
            var expensiveInStockProducts =
                    from prod in products
                    where prod.UnitsInStock > 0 && prod.UnitPrice > 60.00M
                    select prod;

            foreach (var product in expensiveInStockProducts)
            {
                count++;
            }
        }

        return (count == 4 * IterationsWhere01);
    }

    private bool Where01LinqMethod()
    {
        List<Product> products = Product.GetProductList();
        int count = 0;
        for (int i = 0; i < IterationsWhere01; i++)
        {
            var soldOutProducts = products.Where(p => p.UnitsInStock > 0 && p.UnitPrice > 60.00M);

            foreach (var product in soldOutProducts)
            {
                count++;
            }
        }

        return (count == 4 * IterationsWhere01);
    }

    private bool Where01LinqMethodNested()
    {
        List<Product> products = Product.GetProductList();
        int count = 0;
        for (int i = 0; i < IterationsWhere01; i++)
        {
            var soldOutProducts = products.Where(p => p.UnitsInStock > 0).Where(p => p.UnitPrice > 60.00M);

            foreach (var product in soldOutProducts)
            {
                count++;
            }
        }

        return (count == 4 * IterationsWhere01);
    }

    private bool Where01For()
    {
        List<Product> products = Product.GetProductList();
        int count = 0;
        for (int i = 0; i < IterationsWhere01; i++)
        {
            List<Product> soldOutProducts = new List<Product>();

            foreach (Product p in products)
            {
                if (p.UnitsInStock > 0 && p.UnitPrice > 60.00M)
                {
                    soldOutProducts.Add(p);
                }
            }

            foreach (var product in soldOutProducts)
            {
                count++;
            }
        }

        return (count == 4 * IterationsWhere01);
    }
    #endregion

    #region Count00
    private bool Count00()
    {
        bool result = true;
        result &= Count00For();
        result &= Count00LinqMethod();
        return result;
    }

    private bool Count00LinqMethod()
    {
        List<Product> products = Product.GetProductList();
        int count = 0;
        for (int i = 0; i < IterationsCount00; i++)
        {
            count += products.Count(p => p.UnitsInStock == 0);
        }

        return (count == 5 * IterationsCount00);
    }

    private bool Count00For()
    {
        List<Product> products = Product.GetProductList();
        int count = 0;
        for (int i = 0; i < IterationsCount00; i++)
        {
            foreach (Product p in products)
            {
                if (p.UnitsInStock == 0)
                {
                    count++;
                }
            }
        }

        return (count == 5 * IterationsCount00);
    }
    #endregion

    #region Order00
    private bool Order00()
    {
        bool result = true;
        result &= Order00Manual();
        result &= Order00LinqMethod();
        result &= Order00LinqQuery();
        return result;
    }

    private bool Order00LinqQuery()
    {
        List<Product> products = Product.GetProductList();
        Product medianPricedProduct = null;
        for (int i = 0; i < IterationsOrder00; i++)
        {
            var productsInPriceOrder = from prod in products orderby prod.UnitPrice descending select prod;
            int count = productsInPriceOrder.Count();
            medianPricedProduct = productsInPriceOrder.ElementAt<Product>(count / 2);
            Escape(medianPricedProduct);
        }

        return (medianPricedProduct.ProductID == 57);
    }

    private bool Order00LinqMethod()
    {
        List<Product> products = Product.GetProductList();
        Product medianPricedProduct = null;
        for (int i = 0; i < IterationsOrder00; i++)
        {
            var productsInPriceOrder = products.OrderByDescending(p => p.UnitPrice);
            int count = productsInPriceOrder.Count();
            medianPricedProduct = productsInPriceOrder.ElementAt<Product>(count / 2);
            Escape(medianPricedProduct);
        }

        return (medianPricedProduct.ProductID == 57);
    }

    private bool Order00Manual()
    {
        List<Product> products = Product.GetProductList();
        Product medianPricedProduct = null;
        for (int i = 0; i < IterationsOrder00; i++)
        {
            Product[] productsInPriceOrder = products.ToArray<Product>();
            Array.Sort<Product>(productsInPriceOrder, delegate (Product x, Product y) { return -x.UnitPrice.CompareTo(y.UnitPrice); });
            int count = productsInPriceOrder.Count();
            medianPricedProduct = productsInPriceOrder[count / 2];
            Escape(medianPricedProduct);
        }

        return (medianPricedProduct.ProductID == 57);
    }
    #endregion

    [Fact]
    public static int TestEntryPoint()
    {
        var tests = new LinqBenchmarks();
        bool result = tests.Bench();
        return result ? 100 : -1;
    }
}
