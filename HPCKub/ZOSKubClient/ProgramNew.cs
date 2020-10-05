using HPCShared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace ZOSKubClient
{
    class ProgramNew
    {
        static void MainNew(string[] args)
        {
            DateTime tS = DateTime.UtcNow;

            const int numJobs = 100;
            const int numCores = 1;

            // TODO - implement config class!
            HPCUtilities.Init(HPCEnvironment.KubernetesAWS);

            string fileFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
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

            byte[] sharedDataBlob = HPCUtilities.Serialize(sjd);
            List<byte[]> taskBlobs = new List<byte[]>();
            foreach (var task in tasks)
                taskBlobs.Add(HPCUtilities.Serialize(task));

            // TODO - send shared data blob to cluster
            // TODO - send task blobs to cluster

            // TODO - collect results

            // TODO - where should we output the data?
            string outputFolder = @"c:\temp\"; 

            string[] resultFiles = new string[] { };
            List<ZOSResult> processedResults = new List<ZOSResult>();
            int numProcessed = 0;
            int numFail = 0;
            foreach (string resultFile in resultFiles)
            {
                var tr = HPCUtilities.Deserialize<TaskResults>(System.IO.File.ReadAllBytes(resultFile));
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
