using HPCShared;
using ZOSKubLib;
using System;
using System.Collections.Generic;

namespace TestClient
{
    class Program
    {
        static void Main(string[] args)
        {

            string dataDirectoryPath = @"C:\\Data\Some Data Files";

            // prepair input data (serialized array of integers)
            int size = 10;
            
            List<byte[]> taskBlobs = new List<byte[]>();

            for(int i=1;i<size+1;i++){
                taskBlobs.Add(i.GetBytes());
            }
 
            // send input file and task blobs to cluster, collect results
            TaskSender taskSender = new TaskSender();
            List<byte[]> results = taskSender.Send(dataDirectoryPath, taskBlobs);

            Console.WriteLine("processing complete");

            int total = 0;

            foreach( var result in results)
            {
                int square = BitConverter.ToInt32(result, 0);
                total += square;
            }

            Console.WriteLine("sum of squares of first "+ size + " integers is "+ total);

            Console.WriteLine();
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }

    }
}
