using HPCShared;
using ZOSKubLib;
using System;
using System.Collections.Generic;

namespace ZOSKubClient
{
    class ProgramOld
    {
        static void MainOld(string[] args)
        {
            DateTime tS = DateTime.UtcNow;

            const int numJobs = 3;
            const int numCores = 1;

            JobData jd;
            SharedJobData sjd;
            List<TaskData> tasks;
            JobDataUtilities.CreateJobDataPrimes(
                numJobs,
                numCores,
                out jd,
                out sjd,
                out tasks);

            byte[] sharedDataBlob = HPCUtilities.Serialize(sjd);
            List<byte[]> taskBlobs = new List<byte[]>();
            foreach (var task in tasks)
                taskBlobs.Add(HPCUtilities.Serialize(task));

            string dataDirectoryPath = @"C:\\Data\Some Data Files";

            // TODO - where (if at all) should we output the data?
            // string outputFolder = @"c:\temp\"; 


            // send input file and task blobs to cluster, collect results
            TaskSender taskSender = new TaskSender();
            List<byte[]> results = taskSender.Send(dataDirectoryPath, taskBlobs);

            // DK - temp output results
            Console.WriteLine("processing complete");

            foreach( var result in results)
            {

                TaskResults taskResults = HPCUtilities.Deserialize<TaskResults>(result);

                DataEntry[] data = taskResults.Results;


                Console.Write("number: " + BitConverter.ToInt32(data[0].Data) +", factors: ");
                for(int i=1;i<data.Length-1;i++){
                    Console.Write(BitConverter.ToInt32(data[i].Data) + " ");
                }
                Console.WriteLine();
            }


            // DK- following code not quite hooked up yet with k8s
            /*

            string[] resultFiles = new string[] { };
            // List<ZOSResult> processedResults = new List<ZOSResult>();
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

            */

            Console.WriteLine();
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }

    }
}
