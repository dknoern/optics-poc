using System;
using System.Text;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Worker
{
    class Program
    {
        
        static void Main(string[] args)
        {

            var factory = new ConnectionFactory() { HostName = "rabbitmq-service" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {

                channel.QueueDeclare(queue: "task",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);


                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body).ToString();
                    Console.WriteLine(" [x] Received {0}", message);

                    string[] words = message.Split('|');

                    string jobNumber = words[0];
                    string taskNumber = words[1];
                    int inVal = Int16.Parse(words[2]);
                    int outVal = inVal * inVal;
                    String outString = outVal.ToString();

                    var path = "/var/lib/jobs/" + jobNumber + "/out/" + taskNumber + ".txt";
                    
                    Console.WriteLine("outpath = " + path);
                    Console.WriteLine("outVal=" + outString);

                    Thread.Sleep(5000);
                    System.IO.File.WriteAllText(path, outString);

                };
                channel.BasicConsume(queue: "task",
                                     autoAck: true,
                                     consumer: consumer);

                Console.WriteLine(" Press [enter] to exit.");
                Console.ReadLine();

                int i = 0;
                while (true)
                {
                    Console.WriteLine("listening for tasks..." + i);
                    i++;
                    Thread.Sleep(20000);
                }

            }
        }
    }
}
