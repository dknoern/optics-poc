using System;

namespace HPCShared
{
    public enum JobTypes
    {
        PrimeTest1,
        ZOS_GlobalOptimization,
        ZOS_HammerOptimization,
        ZOS_MCTolerancing,
        ZOS_NSCRayTrace,
    }

    [Serializable]
    public class JobData
    {
        public JobTypes JobType { get; set; }
        public string JobId { get; set; }
    }

    public enum DataTypes
    {
        LookupData,
        FactorData,
        FactorizeData,
        ZOSTaskData,
        File = 1000,
        ValueDouble,
        ValueInt,
        ValueLong,
        TotalOptSystems = 2000,
        FinalOptMF,
        Misc = 100000,
    }

    [Serializable]
    public class ZOSTaskData
    {
        // Common
        public int TotalTasks;
        public int TaskNumber;
        public int NumCores = 4;
        public int NumParallel = 0;
        public bool UseACIS = true;

        // MC Tolerancing
        public int NumMC;

        // Optimization
        public int TaskTime;
        public bool UseDLS;

        // NSC RT
        public bool UsePolarization = true;
        public bool SplitRays = false;
        public bool ScatterRays = false;
        public bool IgnoreErrors = true;
        public double RaysMult = 1.0;

        // Prime number / stub test
        public int NumberToFactor;
    }

    [Serializable]
    public class DataEntry
    {
        public DataTypes DataType { get; set; }
        public string ID { get; set; }
        public string Name { get; set; }
        public byte[] Data { get; set; }
    }

    [Serializable]
    public class SharedJobData
    {
        public string JobId { get; set; }
        public DataEntry[] Data { get; set; }
    }
}
