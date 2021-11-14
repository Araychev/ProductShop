using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ProductShop.Data;
using ProductShop.Models;

namespace ProductShop
{
    public class StartUp
    {
        public static void Main(string[] args)
        {
            var context = new ProductShopContext();
            // context.Database.EnsureDeleted();
            //  context.Database.EnsureCreated();


            var json = File.ReadAllText("../../../Datasets/categories-products.json");
            // var jsonCategories = File.ReadAllText("../../../Datasets/categories.json");
            //  var jsonUsers = File.ReadAllText("../../../Datasets/users.json");
            //  var jsonProducrs = File.ReadAllText("../../../Datasets/products.json");

            //  ImportUsers(context, jsonUsers);
            //  ImportProducts(context, jsonProducrs);
            // ImportCategories(context, jsonCategories);

            string result = GetCategoriesByProductsCount(context);

            Console.WriteLine(result);
        }

        public static string ImportUsers(ProductShopContext context, string inputJson)
        {
            IEnumerable<User> users = JsonConvert.DeserializeObject<IEnumerable<User>>(inputJson);

            context.Users.AddRange(users);
            context.SaveChanges();

            return $"Successfully imported {users.Count()}";
        }


        public static string ImportProducts(ProductShopContext context, string inputJson)
        {

            var products = JsonConvert.DeserializeObject<Product[]>(inputJson);
            context.Products.AddRange(products);
            context.SaveChanges();

            return $"Successfully imported {products.Count()}";
        }


        public static string ImportCategories(ProductShopContext context, string inputJson)
        {
            var categories = JsonConvert.DeserializeObject<Category[]>(inputJson).Where(x => x.Name != null).ToArray();

            context.Categories.AddRange(categories);
            context.SaveChanges();

            return $"Successfully imported {categories.Count()}";

        }

        public static string ImportCategoryProducts(ProductShopContext context, string inputJson)
        {
            var categoryProducts = JsonConvert.DeserializeObject<CategoryProduct[]>(inputJson);

            context.CategoryProducts.AddRange(categoryProducts);
            context.SaveChanges();

            return $"Successfully imported {categoryProducts.Count()}";

        }


        public static string GetProductsInRange(ProductShopContext context)
        {
            var products = context
                .Products
                .Where(x => x.Price >= 500 && x.Price <= 1000)
                .Select(x => new
                {
                    x.Name,
                    x.Price,
                    Seller = x.Seller.FirstName + " " + x.Seller.LastName
                })
                .OrderBy(x => x.Price)
                .ToArray();


            var resolver = new DefaultContractResolver()
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };


            var json = JsonConvert.SerializeObject(products, new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ContractResolver = resolver
            });

            return json;
        }


        public static string GetSoldProducts(ProductShopContext context)
        {
            var soldProducts = context
                .Users
                .Include(x => x.ProductsSold)
                .ThenInclude(x => x.Buyer)
                .Where(x => x.ProductsSold.Count > 0 && x.ProductsSold.Any(b => b.Buyer != null))
                .Select(x => new
                {
                    x.FirstName,
                    x.LastName,
                    SoldProducts = x.ProductsSold.Select(product => new
                    {
                        product.Name,
                        product.Price,
                        BuyerFirstName = product.Buyer.FirstName,
                        BuyerLastName = product.Buyer.LastName
                    })
                })
                .OrderBy(x => x.LastName)
                .ThenBy(x => x.FirstName)
                .ToArray();

            var resolver = new DefaultContractResolver()
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };

            var json = JsonConvert.SerializeObject(soldProducts,
                new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    ContractResolver = resolver
                });

            return json;
        }

        public static string GetCategoriesByProductsCount(ProductShopContext context)
        {
            var users = context.Categories
                .Select(x => new
                {
                    Category = x.Name,
                    ProductsCount = x.CategoryProducts.Count,
                    AveragePrice = x.CategoryProducts.Average(p => p.Product.Price).ToString("F2"),
                    TotalRevenue = x.CategoryProducts.Sum(p => p.Product.Price).ToString("F2")
                })
                .OrderByDescending(x => x.ProductsCount);

            var resolver = new DefaultContractResolver()
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };

            var json = JsonConvert.SerializeObject(users,
                new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    ContractResolver = resolver
                });

            return json;
        }

        public static string GetUsersWithProducts(ProductShopContext context)
        {
            var users = context
                .Users
                .AsEnumerable()
                .Where(u => u.ProductsSold.Any(ps => ps.Buyer != null))
                .OrderByDescending(u => u.ProductsSold.Count(ps => ps.Buyer != null))
                .Select(u => new
                {
                    u.FirstName,
                    u.LastName,
                    u.Age,
                    SoldProducts = new
                    {
                        Count = u.ProductsSold.Count(ps => ps.Buyer != null),
                        Products = u.ProductsSold
                            .Where(p => p.Buyer != null)
                            .Select(p => new
                            {
                                p.Name,
                                p.Price
                            })
                    }
                })
                .ToList();

            var output = new
            {
                UsersCount = users.Count,
                Users = users
            };

            var resolver = new DefaultContractResolver()
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };

            var json = JsonConvert.SerializeObject(output,
                new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    ContractResolver = resolver,
                    NullValueHandling = NullValueHandling.Ignore
                });

            return json;
        }
    }
}

