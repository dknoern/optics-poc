using System;
using RabbitMQ.Client;
using System.Text;
using System.Threading;
using k8s;
using k8s.Models;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
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


        public void Init(int jobNumber, List<string> tasks)
        {
            outputDir = "jobs/"+jobNumber+"/out";

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

            Boolean done = false;

            while(!done)
            {
                Thread.Sleep(10000);
                CopyJobOutputDir(jobNumber.ToString());
                
                String dirName = "jobs/"+jobNumber+"/out";
                System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(dirName);
                int count = dir.GetFiles().Length;

                Console.WriteLine("output file count: "+ count);

                if(count == tasks.Count){
                    Console.WriteLine("all tasks complete");
                    done = true;
                }
            }
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

       static void CopyJobDir(string jobNumber)
        {
            
            V1Pod worker = FindFirstPod("worker");
            var podName = worker.Metadata.Name;

            String command = "cp jobs/"+jobNumber + "  " + podName + ":/var/lib/jobs/"+jobNumber;
            Console.WriteLine("copy command: "+ command);
            Process.Start("kubectl", command);
        }

       static void CopyJobOutputDir(string jobNumber)
        {
            
            V1Pod worker = FindFirstPod("worker");
            var podName = worker.Metadata.Name;

            String command = "cp " + podName + ":/var/lib/jobs/"+jobNumber+"/out jobs/"+jobNumber + "/out";

            Process process = new Process();
            process.StartInfo.FileName = "kubectl";
            process.StartInfo.Arguments = command;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();
            process.WaitForExit();

            Process.Start("kubectl", command);
        }

        static void PublishTasks( IModel channel, int jobNumber, List<String> taskInputs)
        {
            string queueName = "task";
            {
                channel.QueueDeclare(queue: queueName,
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);
                int i = 0;

                foreach (string taskInput in taskInputs)
                {
                    i++;
                    Console.WriteLine("publishing job " + jobNumber + ", " + queueName + ", " + taskInput);
                    
                    String message = jobNumber.ToString() + "|" + i.ToString() + "|" + taskInput;

                    var body = Encoding.UTF8.GetBytes(message);
                    
                    channel.BasicPublish(exchange: "",
                                     routingKey: queueName,
                                     basicProperties: null,
                                     body: body);
                }   
            }
        }

        private static void PodList()
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


        private static V1Pod FindFirstPod(string prefix)
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


        public static void PortForward(string PodName)
        {
            Process.Start("kubectl", "port-forward " + PodName + " 5672:5672");
        }

    }

}


