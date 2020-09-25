using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using KubeBatch;

namespace Client
{
    class Program
    {
        /**
        Sample batch job.  Compute sum of squares
        */
        static void Main(string[] args)
        {
            // number of squares (size of job)
            int size = 10; 

            // random job number
            Random rnd = new Random();  
            int jobNumber  = rnd.Next(1000, 9999);  // creates a number between 1 and 1000

            // build list of task inputs
            var taskInputs = new List<String>();

            for(int i=1;i<size+1;i++){
                String message = i.ToString();
                taskInputs.Add(message);
            }

            // subbmit batch jobb
            BatchClient batch = new BatchClient();
            batch.Init(jobNumber, taskInputs);

            // when complete, read results and determing final value
            int sum = GetResult(batch.GetOuputDir());
            Console.WriteLine("final value: " + sum);
        }

        public static int GetResult(string outputDir)
        {
            System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(outputDir);
            int sum = 0;

            foreach (var fi in dir.GetFiles())
            {
                Console.WriteLine("output file: " + fi.FullName);

                string readText = File.ReadAllText(fi.FullName);
                sum+=Int32.Parse(readText);
            }

            return sum;
        }
    }










 
}
