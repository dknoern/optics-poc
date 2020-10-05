using HPCShared;
using ZOSKubLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace ZOSKubClient
{
    class Program
    {
        static void Main(string[] args)
        {
            DateTime tS = DateTime.UtcNow;

            const int numJobs = 100;
            const int numCores = 1;

            // TODO - implement config class!
            HPCUtilities.Init(HPCEnvironment.KubernetesAWS);

            //string fileFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string fileFolder =  @"C:\tmp\zoskub\input";
            string outputFolder = @"C:\tmp\zoskub\output";

            string zarFile = Path.Combine(fileFolder, "tol_test.zar");
            string topFile = Path.Combine(fileFolder, "tol_test.top");

            JobData jd;
            SharedJobData sjd;
            List<TaskData> tasks;
            JobDataUtilities.CreateJobDataMCTol(
                24,
                zarFile,
                topFile,
                4,
                250,
                out jd,
                out sjd,
                out tasks);

            //JobDataUtilities.CreateJobDataPrimes(
            //    numJobs,
            //    numCores,
            //    out jd,
            //    out sjd,
            //    out tasks);

            string sjdFile = Path.Combine(fileFolder, jd.JobId + ".sjd");

            byte[] sharedDataBlob = HPCUtilities.Serialize(sjd);

            List<byte[]> taskBlobs = new List<byte[]>();
            foreach (var task in tasks)
                taskBlobs.Add(HPCUtilities.Serialize(task));

            File.WriteAllBytes(sjdFile,sharedDataBlob);

            // send shared data blob and task blobs to cluster, collect results
            Console.WriteLine("JobId = "+ jd.JobId);

            string dataDirectoryPath = null;
            TaskSender taskSender = new TaskSender(Orchestrator.Docker);

            taskSender.CopySharedJobData(sjdFile);
            List<byte[]> resultByteArrays = taskSender.Send(taskBlobs);

            List<ZOSResult> processedResults = new List<ZOSResult>();
            int numProcessed = 0;
            int numFail = 0;
            foreach (byte[] resultByteArray in resultByteArrays)
            {
                var tr = HPCUtilities.Deserialize<TaskResults>(resultByteArray);
                ZOSResult result;
                JobDataUtilities.ProcessZOSResult(tr, out result);
                if (result != null)
                {
                    JobDataUtilities.StoreZOSResult(jd.JobType, result, outputFolder, numProcessed);
                }
                else
                {
                    ++numFail;
                }
                ++numProcessed;
            }

            int numSucceed;
            var stats = JobDataUtilities.GetZOSStats(
                jd.JobType,
                tS,
                outputFolder,
                out numSucceed,
                ref numFail);

            foreach (var stat in stats)
            {
                Console.WriteLine(stat.ToString());
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}
