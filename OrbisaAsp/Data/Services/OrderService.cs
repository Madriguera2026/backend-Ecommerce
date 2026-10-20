using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using global::OrbisaAsp.Data.Models;
using OrbisaAsp.Data.Models;
using System.Text.Json;

namespace OrbisaAsp.Data.Services
{
    public class OrderService
    {
        private const string TABLE_NAME = "Order";
        private readonly IAmazonDynamoDB _dynamoDb;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public OrderService(IAmazonDynamoDB dynamoDb)
        {
            _dynamoDb = dynamoDb;
        }

        public async Task<Order> CreateAsync(Order order)
        {
            if (string.IsNullOrWhiteSpace(order.id))
                order.id = Guid.NewGuid().ToString();

            order.date = DateTime.UtcNow;

            var item = BuildItem(order);

            await _dynamoDb.PutItemAsync(new PutItemRequest
            {
                TableName = TABLE_NAME,
                Item = item,
                ConditionExpression = "attribute_not_exists(id)"
            });

            return order;
        }

        public async Task<Order?> GetByIdAsync(string id)
        {
            var response = await _dynamoDb.GetItemAsync(
                new GetItemRequest
                {
                    TableName = TABLE_NAME,
                    Key = new Dictionary<string, AttributeValue>
                    {
                        ["id"] = new AttributeValue { S = id }
                    },
                    ConsistentRead = true
                });

            if (response.Item == null || response.Item.Count == 0)
                return null;

            return ReadOrder(response.Item);
        }

        public async Task<List<Order>> GetAllAsync()
        {
            var orders = new List<Order>();
            Dictionary<string, AttributeValue>? lastKey = null;

            do
            {
                var response = await _dynamoDb.ScanAsync(
                    new ScanRequest
                    {
                        TableName = TABLE_NAME,
                        ExclusiveStartKey = lastKey
                    });

                orders.AddRange(
                    response.Items.Select(ReadOrder));

                lastKey = response.LastEvaluatedKey;
            }
            while (lastKey != null && lastKey.Count > 0);

            return orders;
        }

        public async Task<List<Order>> GetByUserIdAsync(string userId)
        {
            var orders = new List<Order>();
            Dictionary<string, AttributeValue>? lastKey = null;

            do
            {
                var response = await _dynamoDb.ScanAsync(
                    new ScanRequest
                    {
                        TableName = TABLE_NAME,
                        FilterExpression = "userId = :userId",
                        ExpressionAttributeValues =
                            new Dictionary<string, AttributeValue>
                            {
                                [":userId"] =
                                    new AttributeValue { S = userId }
                            },
                        ExclusiveStartKey = lastKey
                    });

                orders.AddRange(
                    response.Items.Select(ReadOrder));

                lastKey = response.LastEvaluatedKey;
            }
            while (lastKey != null && lastKey.Count > 0);

            return orders;
        }

        public async Task<bool> UpdateAsync(Order order)
        {
            if (string.IsNullOrWhiteSpace(order.id))
                return false;

            var existing = await GetByIdAsync(order.id);

            if (existing == null)
                return false;

            // Conserva la fecha original de creación.
            order.date = existing.date;

            var item = BuildItem(order);

            await _dynamoDb.PutItemAsync(new PutItemRequest
            {
                TableName = TABLE_NAME,
                Item = item,
                ConditionExpression = "attribute_exists(id)"
            });

            return true;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var response = await _dynamoDb.DeleteItemAsync(
                new DeleteItemRequest
                {
                    TableName = TABLE_NAME,
                    Key = new Dictionary<string, AttributeValue>
                    {
                        ["id"] = new AttributeValue { S = id }
                    },
                    ReturnValues = ReturnValue.ALL_OLD
                });

            return response.Attributes != null &&
                   response.Attributes.Count > 0;
        }

        private static Dictionary<string, AttributeValue> BuildItem(
            Order order)
        {
            var userId = order.user?.uuid;

            return new Dictionary<string, AttributeValue>
            {
                ["id"] = new AttributeValue { S = order.id },
                ["state"] = new AttributeValue
                {
                    N = ((int)order.state).ToString()
                },
                ["date"] = new AttributeValue
                {
                    S = order.date.ToUniversalTime().ToString("O")
                },
                ["userId"] = new AttributeValue
                {
                    S = userId ?? string.Empty
                },
                ["data"] = new AttributeValue
                {
                    S = JsonSerializer.Serialize(order, JsonOptions)
                }
            };
        }

        private static Order ReadOrder(
            Dictionary<string, AttributeValue> item)
        {
            if (!item.TryGetValue("data", out var data) ||
                string.IsNullOrWhiteSpace(data.S))
            {
                throw new InvalidOperationException(
                    "La orden de DynamoDB no contiene el atributo data.");
            }

            return JsonSerializer.Deserialize<Order>(
                data.S, JsonOptions)
                ?? throw new InvalidOperationException(
                    "No se pudo deserializar la orden.");
        }
    }
}
