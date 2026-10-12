using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using OrbisaAsp.Data.Models;

namespace OrbisaAsp.Data.Services
{
    public class ProductService
    {
        private const string TABLE_NAME = "Product";

        private readonly IAmazonDynamoDB _dynamoDb;
        private readonly IImageService _imageService;

        public ProductService(IAmazonDynamoDB dynamoDb, IImageService imageService)
        {
            _dynamoDb = dynamoDb;
            _imageService = imageService;
        }

        public async Task<Product> CreateProduct(Product product, List<IFormFile>? images)
        {
            try
            {
                //product.id = Guid.NewGuid().ToString();

                product.images = new List<string>();

                if (images != null && images.Count > 0)
                {
                    product.images =
                        await _imageService.UploadImagesAsync(images, product.id);
                }

                var request = new PutItemRequest
                {
                    TableName = TABLE_NAME,

                    Item = new Dictionary<string, AttributeValue>
                    {
                        ["id"] = new AttributeValue { S = product.id },

                        ["images"] = new AttributeValue
                        {
                            L = product.images
                                .Select(image => new AttributeValue
                                {
                                    S = image
                                })
                                .ToList()
                        },

                        ["name"] = new AttributeValue { S = product.name },
                        ["size"] = new AttributeValue { N = ((int)product.size).ToString() },
                        ["color"] = new AttributeValue { S = product.color },
                        ["gender"] = new AttributeValue { N = ((int)product.gender).ToString() },
                        ["description"] = new AttributeValue { S = product.description },
                        ["tag"] = new AttributeValue { S = product.tag },
                        ["price"] = new AttributeValue { N = ((double)product.price).ToString() },
                        ["isAbled"] = new AttributeValue { BOOL = product.isAbled }
                    }
                };

                await _dynamoDb.PutItemAsync(request);

                return product;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<List<Product>> GetProducts()
        {
            var request = new ScanRequest
            {
                TableName = TABLE_NAME
            };

            var response =
                await _dynamoDb.ScanAsync(request);

            var products = new List<Product>();

            foreach (var item in response.Items)
            {
                products.Add(MapProduct(item));
            }

            return products;
        }

        //TODO: Logica de filtrar por ID
        public async Task<Product> GetProductById(string id)
        {
            var request = new GetItemRequest
            {
                TableName = TABLE_NAME,

                Key = new Dictionary<string, AttributeValue>
                {
                    ["id"] = new AttributeValue
                    {
                        S = id
                    }
                }
            };

            var response = await _dynamoDb.GetItemAsync(request);

            if (response.Item == null || response.Item.Count == 0)
                return null;

            return MapProduct(response.Item);
        }

        public async Task<Product?> UpdateProduct(
    Product product,
    List<IFormFile>? images)
        {
            // Primero verificamos que el producto exista
            var existingProduct = await GetProductById(product.id);

            if (existingProduct == null)
            {
                return null;
            }

            // Si se enviaron nuevas imágenes, las subimos a Cloudinary
            if (images != null && images.Count > 0)
            {
                product.images =
                    await _imageService.UploadImagesAsync(images, existingProduct.id);
            }
            else
            {
                // Si no se enviaron imágenes, conservamos las actuales
                product.images = existingProduct.images;
            }

            var request = new PutItemRequest
            {
                TableName = TABLE_NAME,

                Item = new Dictionary<string, AttributeValue>
                {
                    ["id"] = new AttributeValue { S = product.id },
                    ["images"] = new AttributeValue {
                        L = product.images
                            .Select(image => new AttributeValue
                            {
                                S = image
                            })
                            .ToList()
                    },
                    ["name"] = new AttributeValue { S = product.name },
                    ["size"] = new AttributeValue { N = ((int)product.size).ToString() },
                    ["color"] = new AttributeValue { S = product.color },
                    ["gender"] = new AttributeValue { N = ((int)product.gender).ToString() },
                    ["description"] = new AttributeValue { S = product.description },
                    ["tag"] = new AttributeValue { S = product.tag },
                    ["price"] = new AttributeValue { N = ((double)product.price).ToString() },
                    ["isAbled"] = new AttributeValue { BOOL = product.isAbled }
                }
            };

            await _dynamoDb.PutItemAsync(request);

            return product;
        }

        public async Task<bool> DeleteProduct(string id)
        {
            // Verificamos que exista
            var existingProduct = await GetProductById(id);

            if (existingProduct == null)
            {
                return false;
            }

            var request = new DeleteItemRequest
            {
                TableName = TABLE_NAME,

                Key = new Dictionary<string, AttributeValue>
                {
                    ["id"] = new AttributeValue
                    {
                        S = id
                    }
                }
            };

            await _dynamoDb.DeleteItemAsync(request);

            return true;
        }

        private Product MapProduct(
            Dictionary<string, AttributeValue> item)
        {
            var images = new List<string>();

            if (item.ContainsKey("images") &&
                item["images"].L != null)
            {
                images = item["images"].L
                    .Where(x => x.S != null)
                    .Select(x => x.S)
                    .ToList();
            }

            return new Product
            {
                id = item["id"].S,
                images = images,
                name = item["name"].S,
                size = (SizeType)int.Parse(item["size"].N),
                color = item["color"].S,
                gender = (GenderType)int.Parse(item["gender"].N),
                description = item["description"].S,
                tag = item["tag"].S,
                price = double.Parse(item["price"].N),
                isAbled = (bool)item["isAbled"].BOOL
            };
        }
    }
}
