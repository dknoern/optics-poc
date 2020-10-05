using ZOSKubLib;
using HPCShared;
using System.Collections.Generic;
using System;
using System.IO;

namespace ZOSKubApp
{
    public class ZOSTaskWorker : ITaskWorker
    {
        public byte[] OnTask(byte[] input)
        {
            // TODO: get shared data file name here
            string sharedDataFile = "some filename";
            
            DateTime tS = DateTime.Now;

            // TODO - implement config class!
            HPCUtilities.Init(HPCEnvironment.KubernetesAWS);

            TaskData td = HPCUtilities.Deserialize<TaskData>(input);

            Func<SharedJobData> getSJD = () =>
            {
                return HPCUtilities.Deserialize<SharedJobData>(File.ReadAllBytes(sharedDataFile));
            };
            JobDataUtilities.SetSharedJobData(td.Job, getSJD);

            ZOSTaskData taskSettings = HPCUtilities.Deserialize<ZOSTaskData>(td.Data[0].Data);

            byte[] resultData = JobDataUtilities.RunZOSJob(
                td,
                tS);

            return resultData;
        }
    }
}