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

            string inputQueueName = "task";

            var factory = new ConnectionFactory() { HostName = "rabbitmq-service" };
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();
            var outChannel = connection.CreateModel();

                channel.QueueDeclare(queue: inputQueueName,
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

                    // fake extra time for calc
                    Thread.Sleep(3000);

                    int outVal = inVal * inVal;
                    String outString = outVal.ToString();

                    string outputQueueName = "out-"+jobNumber;


                    Console.WriteLine("publishing response " + jobNumber + ", " + outputQueueName + ", " + outString);

                    String outMessage = jobNumber + "|" + taskNumber + "|" + outString;

                    var outBody = Encoding.UTF8.GetBytes(outMessage);

                    outChannel.QueueDeclare(queue: outputQueueName,
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                    outChannel.BasicPublish(exchange: "",
                                     routingKey: outputQueueName,
                                     basicProperties: null,
                                     body: outBody);
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
