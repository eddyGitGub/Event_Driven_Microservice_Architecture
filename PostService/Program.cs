using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PostService.Data;
using PostService.Entities;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PostService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ListenForIntegrationEvents();
            CreateHostBuilder(args).Build().Run();
        }
        private static void ListenForIntegrationEvents()
        {
            var factory = new ConnectionFactory();
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();
            var consumer = new EventingBasicConsumer(channel);

            consumer.Received += (model, ea) =>
            {
                var contextOptions = new DbContextOptionsBuilder<PostServiceContext>()
                    .UseSqlite(@"Data Source=post.db")
                    .Options;
                var dbContext = new PostServiceContext(contextOptions);

                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine(" [x] Received {0}", message);

                var data = JsonDocument.Parse(message);
                var type = ea.RoutingKey;
                if (type == "user.add")
                {
                    if (dbContext.User.Any(a => a.Id == data.RootElement.GetProperty("id").GetInt32()))
                    {
                        Console.WriteLine("Ignoring old/duplicate entity");
                    }
                    else
                    {
                        dbContext.User.Add(new User
                        {
                            Id = data.RootElement.GetProperty("id").GetInt32(),
                            Name = data.RootElement.GetProperty("name").GetString(),
                            Version = data.RootElement.GetProperty("version").GetInt32()//data["version"].Value<int>()
                        });
                        dbContext.SaveChanges();
                    }
                }
                else if (type == "user.update")
                {
                    int newVersion = data.RootElement.GetProperty("version").GetInt32();
                    var user = dbContext.User.First(a => a.Id == data.RootElement.GetProperty("version").GetInt32());
                    if (user.Version >= newVersion)
                    {
                        Console.WriteLine("Ignoring old/duplicate entity");
                    }
                    else
                    {
                        user.Name = data.RootElement.GetProperty("newname").GetString();// data["newname"].Value<string>();
                        user.Version = newVersion;
                        dbContext.SaveChanges();
                    }
                }
                channel.BasicAck(ea.DeliveryTag, false);
            };
            channel.BasicConsume(queue: "user.postservice",
                                     autoAck: false,
                                     consumer: consumer);
        }
        //private static void ListenForIntegrationEvents()
        //{
        //    var factory = new ConnectionFactory();
        //    var connection = factory.CreateConnection();
        //    var channel = connection.CreateModel();
        //    var consumer = new EventingBasicConsumer(channel);

        //    consumer.Received += (model, ea) =>
        //    {
        //        var contextOptions = new DbContextOptionsBuilder<PostServiceContext>()
        //            .UseSqlite(@"Data Source=post.db")
        //            .Options;
        //        var dbContext = new PostServiceContext(contextOptions);

        //        var body = ea.Body.ToArray();
        //        var message = Encoding.UTF8.GetString(body);
        //        Console.WriteLine(" [x] Received {0}", message);

        //        var data = JsonDocument.Parse(message);
        //        //var data = JsonSerializer.Deserialize<object>(message);
        //        int id = data.RootElement.GetProperty("id").GetInt32();
        //        var type = ea.RoutingKey;
        //        if (type == "user.add")
        //        {
        //            dbContext.User.Add(new User()
        //            {
        //                Id = data.RootElement.GetProperty("id").GetInt32(),//data.RootElement.GetProperty("Topic").GetString()//data["id"].Value<int>(),
        //                Name = data.RootElement.GetProperty("name").GetString()//data["name"].Value<string>()
        //            });
        //            dbContext.SaveChanges();
        //        }
        //        else if (type == "user.update")
        //        {
        //            var user = dbContext.User.First(a => a.Id == data.RootElement.GetProperty("id").GetInt32());
        //            user.Name = data.RootElement.GetProperty("newname").GetString();
        //            dbContext.SaveChanges();
        //        }
        //    };
        //    channel.BasicConsume(queue: "user.postservice",
        //                             autoAck: true,
        //                             consumer: consumer);
        //}
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
