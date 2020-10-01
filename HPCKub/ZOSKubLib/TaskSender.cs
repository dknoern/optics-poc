using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading;
using k8s;
using k8s.Models;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using HPCShared;

namespace ZOSKubLib
{
    public class TaskSender
    {

        IModel channel = null;

        string outputDir = null;

        string inputQueueName = "in";
        string outputQueueName = "out";

        public List<byte[]> Send(string dataDirectoryPath, List<byte[]> taskBlobs)
        {

            // TODO - need unique job number to prevent collisions
            Random rnd = new Random();  
            string jobNumber  = rnd.Next(1000, 9999).ToString();  // creates a number between 1 and 1000

     
            Console.WriteLine("TaskSender: initializing");

            Console.WriteLine("taskSender: forwarding broker port");

            V1Pod rabbit = FindFirstPod("rabbitmq");

            var config = KubernetesClientConfiguration.BuildDefaultConfig();
            IKubernetes client = new Kubernetes(config);

            PortForward(rabbit.Metadata.Name);

            Thread.Sleep(3000);

            Console.WriteLine("BatchClient: preparing remote directory structure");

            // TODO: what exactly should we put here?
            //BuildJobDir(jobNumber.ToString());
            //CopyJobDir(jobNumber.ToString());
            Thread.Sleep(10000);

            Console.WriteLine("BatchClient: publishing tasks for job " + jobNumber);

            var factory = new ConnectionFactory() { HostName = "localhost" };
            var connection = factory.CreateConnection();
            channel = connection.CreateModel();

            PublishTasks(channel, taskBlobs);

            // wait for results

                int finishedTaskCount = 0;
                int totalResult = 0;

                List<byte[]> processedResults = new List<byte[]>();

                channel.QueueDeclare(queue: outputQueueName,
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();

                    processedResults.Add(body);

                    finishedTaskCount++;

                    Console.WriteLine(finishedTaskCount + " out of " + taskBlobs.Count + " responses received");
                    
                };
                channel.BasicConsume(queue: outputQueueName,
                                     autoAck: true,
                                     consumer: consumer);

                int i = 0;
                while (finishedTaskCount < taskBlobs.Count)
                {
                    Console.WriteLine("listening for results..." + i);
                    i++;
                    Thread.Sleep(20000);
                }

                Console.WriteLine("all responses complete");
                Console.WriteLine("total result = "+ totalResult);



                return processedResults;
        }
        
        private void BuildJobDir(string jobNumber)
        {
            //TODO: fail if dir exists

            if (!Directory.Exists("jobs/"+jobNumber))
            {
                Directory.CreateDirectory("jobs/"+jobNumber);
                Directory.CreateDirectory(outputDir);
                Directory.CreateDirectory("jobs/"+jobNumber+"/err");
                System.IO.File.WriteAllText("jobs/"+jobNumber+"/config.txt", "power=2");
            }
        }

        private string GetOuputDir()
        {
            return outputDir;
        }

        private void CopyJobDir(string jobNumber)
        {
            
            V1Pod worker = FindFirstPod("worker");
            var podName = worker.Metadata.Name;

            String command = "cp jobs/"+jobNumber + "  " + podName + ":/var/lib/jobs/"+jobNumber;
            Console.WriteLine("copy command: "+ command);
            Process.Start("kubectl", command);
        }

        private void PublishTasks(IModel channel, List<byte[]> taskInputs)
        {

            channel.QueueDeclare(queue: inputQueueName,
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            channel.QueueDeclare(queue: outputQueueName,
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);
            int i = 0;

            foreach (byte[] taskInput in taskInputs)
            {
                i++;
                Console.WriteLine("publishing task " + i);

                channel.BasicPublish(exchange: "",
                                 routingKey: inputQueueName,
                                 basicProperties: null,
                                 body: taskInput);
            }
        }

        private void PodList()
        {
            var config = KubernetesClientConfiguration.BuildDefaultConfig();
            IKubernetes client = new Kubernetes(config);

            var list = client.ListNamespacedPod("default");

            foreach (var item in list.Items)
            {
                Console.WriteLine(item.Metadata.Name);
            }

            if (list.Items.Count == 0)
            {
                Console.WriteLine("Empty!");
            }
        }


        private V1Pod FindFirstPod(string prefix)
        {
            var config = KubernetesClientConfiguration.BuildDefaultConfig();
            IKubernetes client = new Kubernetes(config);
            Console.WriteLine("Starting Request!");

            var list = client.ListNamespacedPod("default");

            foreach (var item in list.Items)
            {
                if(item.Metadata.Name.StartsWith(prefix))
                {
                    return item;
                }
                Console.WriteLine(item.Metadata.Name);
            }

            return null;
        }

        private void PortForward(string PodName)
        {
            Process.Start("kubectl", "port-forward " + PodName + " 5672:5672");
        }

    }

}


