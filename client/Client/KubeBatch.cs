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

namespace KubeBatch
{
    class BatchClient
    {

        IModel channel = null;
        int jobNumber = -1;

        string outputDir = null;

        string inputQueueName = "task";
        string outputQueueName = null;


        public void Init(int jobNumber, List<string> tasks)
        {
            outputDir = "jobs/"+jobNumber+"/out";

            outputQueueName = "out-"+jobNumber;

            Console.WriteLine("BatchClient: initializing");

            Console.WriteLine("BatchClient: forwarding broker port");

            V1Pod rabbit = FindFirstPod("rabbitmq");

            var config = KubernetesClientConfiguration.BuildDefaultConfig();
            IKubernetes client = new Kubernetes(config);

            PortForward(rabbit.Metadata.Name);

            Thread.Sleep(3000);

            Console.WriteLine("BatchClient: preparing remote directory structure");

            BuildJobDir(jobNumber.ToString());
            CopyJobDir(jobNumber.ToString());
            Thread.Sleep(10000);

            Console.WriteLine("BatchClient: publishing tasks for job " + jobNumber);

            var factory = new ConnectionFactory() { HostName = "localhost" };
            var connection = factory.CreateConnection();
            channel = connection.CreateModel();

            PublishTasks(channel, jobNumber, tasks);

            // wait for results

                int finishedTaskCount = 0;

                int totalResult = 0;
            
                channel.QueueDeclare(queue: outputQueueName,
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

                    finishedTaskCount++;

                    Console.WriteLine(finishedTaskCount + " out of " + tasks.Count + " responses received");

                    string[] words = message.Split('|');

                    string jobNumber = words[0];
                    string taskNumber = words[1];
                    int outVal = Int16.Parse(words[2]);

                    // TODO: plug in function here
                    totalResult += outVal;
                    // TODO: end

                    Console.WriteLine("outVal=" + outVal);

                };
                channel.BasicConsume(queue: outputQueueName,
                                     autoAck: true,
                                     consumer: consumer);



                int i = 0;
                while (finishedTaskCount < tasks.Count)
                {
                    Console.WriteLine("listening for results..." + i);
                    i++;
                    Thread.Sleep(20000);
                }

                Console.WriteLine("all responses complete");
                Console.WriteLine("total result = "+ totalResult);
            

        }
        
        void BuildJobDir(string jobNumber)
        {
            // TODO fail if dir exists

            if (!Directory.Exists("jobs/"+jobNumber))
            {
                Directory.CreateDirectory("jobs/"+jobNumber);
                Directory.CreateDirectory(outputDir);
                Directory.CreateDirectory("jobs/"+jobNumber+"/err");
                System.IO.File.WriteAllText("jobs/"+jobNumber+"/config.txt", "power=2");
            }
        }

        public string GetOuputDir()
        {
            return outputDir;
        }

       void CopyJobDir(string jobNumber)
        {
            
            V1Pod worker = FindFirstPod("worker");
            var podName = worker.Metadata.Name;

            String command = "cp jobs/"+jobNumber + "  " + podName + ":/var/lib/jobs/"+jobNumber;
            Console.WriteLine("copy command: "+ command);
            Process.Start("kubectl", command);
        }

         void PublishTasks(IModel channel, int jobNumber, List<String> taskInputs)
        {

            channel.QueueDeclare(queue: inputQueueName,
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            string outputQueueName = "out-" + jobNumber.ToString();

            channel.QueueDeclare(queue: outputQueueName,
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);
            int i = 0;

            foreach (string taskInput in taskInputs)
            {
                i++;
                Console.WriteLine("publishing job " + jobNumber + ", " + inputQueueName + ", " + taskInput);

                String message = jobNumber.ToString() + "|" + i.ToString() + "|" + taskInput;

                var body = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish(exchange: "",
                                 routingKey: inputQueueName,
                                 basicProperties: null,
                                 body: body);
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


        public void PortForward(string PodName)
        {
            Process.Start("kubectl", "port-forward " + PodName + " 5672:5672");
        }

    }

}


