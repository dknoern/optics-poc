using ZOSKubLib;
using HPCShared;
using System.Collections.Generic;
using System;

namespace ZOSKubApp
{
    public class ZOSTaskWorker : ITaskWorker
    {

        public byte[] OnTask(byte[] input)
        {

            TaskData td = HPCUtilities.Deserialize<TaskData>(input);

            ZOSTaskData taskSettings = HPCUtilities.Deserialize<ZOSTaskData>(td.Data[0].Data);

            Console.WriteLine("ZOSTaskWorker: taskNumber " + taskSettings.TaskNumber + ", factoring " + taskSettings.NumberToFactor);

            TaskResults taskResults = RunPrime1(td.Job, td.TaskNumber, taskSettings.NumberToFactor);

            return HPCUtilities.Serialize(taskResults);
        }

        public static TaskResults RunPrime1(
            JobData jobData,
            int taskNumber,
            int numberToFactor)
        {
            DateTime tS = DateTime.Now;
            List<DataEntry> results = new List<DataEntry>();

            try
            {
                List<int> factors = null;
                for (int i = 0; i < 200000; ++i)
                    factors = GetFactors(numberToFactor, new List<int>());

                results.Add(new DataEntry()
                {
                    ID = "numbertofactor",
                    DataType = DataTypes.ValueInt,
                    Data = BitConverter.GetBytes(numberToFactor),
                });
                results.Add(new DataEntry()
                {
                    ID = "numberoffactors",
                    DataType = DataTypes.ValueInt,
                    Data = BitConverter.GetBytes(factors.Count),
                });
                foreach (int factor in factors)
                {
                    results.Add(new DataEntry()
                    {
                        ID = "factor",
                        DataType = DataTypes.ValueInt,
                        Data = BitConverter.GetBytes(factor),
                    });
                }

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("RunPrime1 - " + ex.Message);
            }

            TimeSpan elapsed = (DateTime.Now - tS);
            results.Add(new DataEntry()
            {
                ID = "elapsedTime",
                Name = Environment.MachineName,
                DataType = DataTypes.Misc,
                Data = BitConverter.GetBytes(elapsed.Ticks),
            });

            TaskResults ret = new TaskResults()
            {
                Job = jobData,
                TaskNumber = taskNumber,
                Results = results.ToArray(),
            };

            return ret;

        }


        private static List<int> GetFactors(int n, List<int> primes)
        {
            List<int> factors = new List<int>();

            for (int i = 0; i < primes.Count;)
            {
                if (n % primes[i] == 0)
                {
                    factors.Add(-primes[i]);
                    n /= primes[i];
                }
                else
                {
                    i++;
                }
            }

            for (int i = 2; n > 1;)
            {
                if (i * i > n)
                {
                    factors.Add(n);
                    break;
                }
                if (n % i == 0)
                {
                    factors.Add(i);
                    n /= i;
                }
                else
                {
                    i++;
                }
            }

            return factors;
        }

    }
}