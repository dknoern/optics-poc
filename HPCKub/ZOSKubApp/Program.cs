using HPCShared;
using System;
using System.Collections.Generic;
using System.IO;

namespace ZOSKubApp
{
    public class Program
    {
        static void Main(string[] args)
        {
            // arguments
            // job id
            // shared data file
            // job data file
            // output file

            DateTime tS = DateTime.Now;

            // TODO - implement config class!
            HPCUtilities.Init(HPCEnvironment.KubernetesAWS);

            int numArgs = args.Length;
            if (numArgs < 4)
                throw new Exception("Invalid number of arguments");

            string jobId = args[numArgs - 4];
            string sharedDataFile = args[numArgs - 3];
            string taskDataFile = args[numArgs - 2];
            string outFile = args[numArgs - 1];

            // TODO - shared data isn't needed for Prime factoring
            // For other jobs, it needs to be pre-processed once per node

            TaskData td = HPCUtilities.Deserialize<TaskData>(File.ReadAllBytes(taskDataFile));

            Func<SharedJobData> getSJD = () =>
            {
                return HPCUtilities.Deserialize<SharedJobData>(File.ReadAllBytes(sharedDataFile));
            };
            JobDataUtilities.SetSharedJobData(td.Job, getSJD);

            ZOSTaskData taskSettings = HPCUtilities.Deserialize<ZOSTaskData>(td.Data[0].Data);

            byte[] resultData = JobDataUtilities.RunZOSJob(
                td,
                tS);

            File.WriteAllBytes(outFile, resultData);

            //RunPrime1(td.Job, td.TaskNumber, taskSettings.NumberToFactor, outFile);
        }

        //public static void RunPrime1(
        //    JobData jobData,
        //    int taskNumber,
        //    int numberToFactor,
        //    string outFile)
        //{
        //    DateTime tS = DateTime.Now;
        //    List<DataEntry> results = new List<DataEntry>();

        //    try
        //    {
        //        List<int> factors = null;
        //        for (int i = 0; i < 200000; ++i)
        //            factors = GetFactors(numberToFactor, new List<int>());

        //        results.Add(new DataEntry()
        //        {
        //            ID = "numbertofactor",
        //            DataType = DataTypes.ValueInt,
        //            Data = BitConverter.GetBytes(numberToFactor),
        //        });
        //        results.Add(new DataEntry()
        //        {
        //            ID = "numberoffactors",
        //            DataType = DataTypes.ValueInt,
        //            Data = BitConverter.GetBytes(factors.Count),
        //        });
        //        foreach (int factor in factors)
        //        {
        //            results.Add(new DataEntry()
        //            {
        //                ID = "factor",
        //                DataType = DataTypes.ValueInt,
        //                Data = BitConverter.GetBytes(factor),
        //            });
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        Console.Error.WriteLine("RunPrime1 - " + ex.Message);
        //    }

        //    TimeSpan elapsed = (DateTime.Now - tS);
        //    results.Add(new DataEntry()
        //    {
        //        ID = "elapsedTime",
        //        Name = Environment.MachineName,
        //        DataType = DataTypes.Misc,
        //        Data = BitConverter.GetBytes(elapsed.Ticks),
        //    });

        //    TaskResults ret = new TaskResults()
        //    {
        //        Job = jobData,
        //        TaskNumber = taskNumber,
        //        Results = results.ToArray(),
        //    };

        //    byte[] retData = HPCUtilities.Serialize(ret);
        //    File.WriteAllBytes(outFile, retData);
        //}


        //private static List<int> GetFactors(int n, List<int> primes)
        //{
        //    List<int> factors = new List<int>();

        //    for (int i = 0; i < primes.Count;)
        //    {
        //        if (n % primes[i] == 0)
        //        {
        //            factors.Add(-primes[i]);
        //            n /= primes[i];
        //        }
        //        else
        //        {
        //            i++;
        //        }
        //    }

        //    for (int i = 2; n > 1;)
        //    {
        //        if (i * i > n)
        //        {
        //            factors.Add(n);
        //            break;
        //        }
        //        if (n % i == 0)
        //        {
        //            factors.Add(i);
        //            n /= i;
        //        }
        //        else
        //        {
        //            i++;
        //        }
        //    }

        //    return factors;
        //}

    }
}
