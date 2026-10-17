using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using OrbisaAsp.Data.Models;

namespace OrbisaAsp.Data.Services
{
    public class UserService
    {
        private const string TABLE_NAME = "User";
        private const string COGNITO_SUB_INDEX = "username-index";

        private readonly IAmazonDynamoDB _dynamoDb;

        public UserService(IAmazonDynamoDB dynamoDb)
        {
            _dynamoDb = dynamoDb;
        }

        public async Task<bool> CreateUser(User user)
        {
            try
            {
                var request = new PutItemRequest
                {
                    TableName = TABLE_NAME,
                    Item = new Dictionary<string, AttributeValue>
                    {
                        ["uuid"] = new AttributeValue { S = Guid.NewGuid().ToString() },
                        ["username"] = new AttributeValue { S = user.username },
                        ["name"] = new AttributeValue { S = user.name },
                        ["whatsapp"] = new AttributeValue { S = user.whatsapp },
                    }
                };
                await _dynamoDb.PutItemAsync(request);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }            
        }

        public async Task<List<User>> GetUsers()
        {
            var request = new ScanRequest
            {
                TableName = TABLE_NAME
            };

            var response = await _dynamoDb.ScanAsync(request);

            var users = new List<User>();

            foreach (var item in response.Items)
            {
                users.Add(new User(
                    item["uuid"].S,
                    item["username"].S,
                    item["name"].S,
                    item["whatsapp"].S
                ));
            }

            return users;
        }

        public async Task<User?> GetById(string uuid)
        {
            var request = new GetItemRequest
            {
                TableName = "User",

                Key = new Dictionary<string, AttributeValue>
                {
                    ["uuid"] = new AttributeValue
                    {
                        S = uuid
                    }
                }
            };

            var response = await _dynamoDb.GetItemAsync(request);
            if (response.Item == null || response.Item.Count == 0)
                return null;

            return new User(
                response.Item["uuid"].S,
                response.Item["username"].S,
                response.Item["name"].S,
                response.Item["whatsapp"].S
            );
        }

        public async Task<User?> GetByUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return null;

            var request = new QueryRequest
            {
                TableName = TABLE_NAME,
                IndexName = COGNITO_SUB_INDEX,

                KeyConditionExpression = "#sub = :sub",

                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    ["#sub"] = "username"
                },

                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":sub"] = new AttributeValue
                    {
                        S = username
                    }
                },

                Limit = 1
            };

            var response = await _dynamoDb.QueryAsync(request);

            if (response.Items.Count == 0)
                return null;

            var item = response.Items[0];

            return new User
            {
                uuid = item.TryGetValue("uuid", out var uuid)
                    ? uuid.S
                    : string.Empty,

                username = item.TryGetValue("username", out var sub)
                    ? sub.S
                    : string.Empty,

                name = item.TryGetValue("name", out var name)
                    ? name.S
                    : string.Empty,

                whatsapp = item.TryGetValue("whatsapp", out var whatsapp)
                    ? whatsapp.S
                    : string.Empty
            };
        }

        public async Task<bool> UpdateUser(string uuid, User user)
        {
            try
            {
                var request = new UpdateItemRequest
                {
                    TableName = TABLE_NAME,

                    Key = new Dictionary<string, AttributeValue>
                    {
                        ["uuid"] = new AttributeValue
                        {
                            S = uuid
                        }
                    },

                    UpdateExpression = "SET #name = :name, whatsapp = :whatsapp",

                    ExpressionAttributeNames = new Dictionary<string, string>
                    {
                        ["#name"] = "name"
                    },

                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        [":name"] = new AttributeValue
                        {
                            S = user.name
                        },

                        [":whatsapp"] = new AttributeValue
                        {
                            S = user.whatsapp
                        }
                    },

                    ReturnValues = "ALL_NEW"
                };

                await _dynamoDb.UpdateItemAsync(request);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> DeleteUser(string uuid)
        {
            try
            {
                var request = new DeleteItemRequest
                {
                    TableName = TABLE_NAME,

                    Key = new Dictionary<string, AttributeValue>
                    {
                        ["uuid"] = new AttributeValue
                        {
                            S = uuid
                        }
                    }
                };

                await _dynamoDb.DeleteItemAsync(request);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
