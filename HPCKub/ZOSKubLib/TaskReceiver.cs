using System;
using System.Text;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ZOSKubLib
{
    public class TaskReceiver
    {

        ITaskWorker taskWorker = null;

        public TaskReceiver(ITaskWorker taskWorker)
        {
            this.taskWorker = taskWorker;
        }

        public void Receive()
        {

            string inputQueueName = "in";
            string outputQueueName = "out";

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
                    var inBody = ea.Body.ToArray();

                    byte[] outBody = taskWorker.OnTask(inBody);

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
                channel.BasicConsume(queue: inputQueueName,
                                     autoAck: true,
                                     consumer: consumer);

                int i = 0;
                while (true)
                {
                    Console.WriteLine("listening for tasks..." + i);
                    i++;
                    Thread.Sleep(5000);
                }

            
        }

    }
}