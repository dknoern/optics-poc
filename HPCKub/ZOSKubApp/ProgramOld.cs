using HPCShared;
using ZOSKubLib;
using System;
using System.Collections.Generic;
using System.IO;

namespace ZOSKubApp
{
    public class ProgramOld
    {
        static void MainOld(string[] args)
        {
            Console.WriteLine("worker process started....");


            ITaskWorker taskWorker = new ZOSTaskWorker();

            TaskReceiver taskReceiver = new TaskReceiver(taskWorker);

            taskReceiver.Receive();


            Console.WriteLine("...worker process ended (should not happen).");
        }
    }
}