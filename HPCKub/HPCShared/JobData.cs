using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace HPCShared
{

    public enum ZOSJobTypes
    {
        NotSet,
        GlobalOpt,
        HammerOpt,
        MCTol,
        NSCRT,

        PrimeFactor = 10000,
    }

    public class ZOSJobData
    {
        public ZOSJobTypes JobType = ZOSJobTypes.NotSet;
        public ZOSTaskData Settings = new ZOSTaskData();
        public string ZarFile;
        public string TopFile;
        public string Nodes;
    }


    [Serializable]
    public class FileResult
    {
        public string FileName { get; set; }
        public byte[] FileData { get; set; }
    }

    [Serializable]
    public class GlobalOptResult
    {
        public double MeritFunction { get; set; }
        public FileResult BestFile { get; set; }
        public long Systems { get; set; }
    }

    [Serializable]
    public class NSCRTResult
    {
        public double LostThresholds { get; set; }
        public double LostErrors { get; set; }
        public long TotalRays { get; set; }
        public FileResult[] DetectorData { get; set; }
    }

    [Serializable]
    public class MCTResult
    {
        // 0 = best, 1 = worst, 2 = ztd
        public FileResult[] Files { get; set; }
    }

    [Serializable]
    public class PrimeResult
    {
        public int NumberToFactor { get; set; }
        public int[] Factors { get; set; }
    }

    [Serializable]
    public class ZOSResult
    {
        public string Machine { get; set; }
        public long RunTimeTicks { get; set; }

        public GlobalOptResult Optimization { get; set; }
        public NSCRTResult NSCRayTrace { get; set; }
        public MCTResult MonteCarloTolerancing { get; set; }
        public PrimeResult PrimeFactoring { get; set; }
    }

    public class GlobalOptStats
    {
        public long TotalSystems { get; set; }
        public GlobalOptResult[] Results { get; set; }

        public string GetSummary(double clockTimeS, double cpuTimeS)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var result in Results)
            {
                sb.AppendLine($"{result.BestFile?.FileName ?? String.Empty}: {result.MeritFunction.ToString("e5")}");
            }

            double bestMF = Double.MaxValue;
            if (Results != null)
            {
                foreach (var result in Results)
                {
                    if (result.MeritFunction < bestMF)
                        bestMF = result.MeritFunction;
                }
            }

            sb.AppendLine($"Systems evaluated: {TotalSystems}");
            sb.AppendLine($"Best MF: {bestMF.ToString("g5")}");
            sb.AppendLine($"Clock systems/s: {(TotalSystems / clockTimeS).ToString("f2")}");
            sb.AppendLine($"Task systems/s: {(TotalSystems / cpuTimeS).ToString("f2")}");

            return sb.ToString();
        }
    }

    public class NSCRTStats
    {
        public double TotalLostThresholds { get; set; }
        public double TotalLostErrors { get; set; }
        public long TotalRays { get; set; }
        public NSCRTResult[] Results { get; set; }

        public string GetSummary(double clockTimeS, double cpuTimeS)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var result in Results)
            {
                foreach (var dr in result.DetectorData)
                {
                    sb.AppendLine(dr.FileName);
                }
            }

            sb.AppendLine($"Total rays: {TotalRays}");
            sb.AppendLine($"Clock rays/s: {(TotalRays / clockTimeS).ToString("e3")}");
            sb.AppendLine($"Task rays/s: {(TotalRays / cpuTimeS).ToString("e3")}");
            sb.AppendLine($"Lost energy (thresholds): {TotalLostThresholds.ToString("e5")}");
            sb.AppendLine($"Lost energy (errors): {TotalLostErrors.ToString("e5")}");

            return sb.ToString();
        }
    }

    public class MCTStats
    {
        public MCTResult[] Results { get; set; }

        public string GetSummary(double clockTimeS, double cpuTimeS)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var result in Results)
            {
                foreach (var dr in result.Files)
                {
                    sb.AppendLine(dr.FileName);
                }
            }

            return sb.ToString();
        }
    }

    public class PrimeStats
    {
        public PrimeResult[] Results { get; set; }
        public string GetSummary(double clockTimeS, double cpuTimeS)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var result in Results)
            {
                sb.Append(result.NumberToFactor);
                sb.Append("=");
                if (result.Factors != null)
                {
                    if (result.Factors.Length == 1)
                        sb.Append("Prime");
                    else
                        sb.Append(String.Join(",", result.Factors));
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }

    public class ZOSStats
    {
        public string MachineName { get; set; }
        public bool IsMaster { get { return String.IsNullOrWhiteSpace(MachineName); } }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public double TotalRunTimeS { get; set; }
        public int NumTasks { get; set; }
        public ZOSResult[] Results { get; set; }

        public GlobalOptStats Optimization { get; set; }
        public NSCRTStats NSCRayTrace { get; set; }
        public MCTStats MonteCarloTolerancing { get; set; }
        public PrimeStats PrimeFactorization { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            string statsSource;
            if (String.IsNullOrWhiteSpace(MachineName))
                statsSource = "All Machines";
            else
                statsSource = MachineName;
            sb.AppendLine($"**** Data for {statsSource} ****");

            double clockTimeS = (EndTime - StartTime).TotalSeconds;
            double cpuTimeS = TotalRunTimeS;

            sb.AppendLine("--- results ---");
            if (Optimization != null)
                sb.AppendLine(Optimization.GetSummary(clockTimeS, cpuTimeS));
            if (NSCRayTrace != null)
                sb.AppendLine(NSCRayTrace.GetSummary(clockTimeS, cpuTimeS));
            if (MonteCarloTolerancing != null)
                sb.AppendLine(MonteCarloTolerancing.GetSummary(clockTimeS, cpuTimeS));
            if (PrimeFactorization != null)
                sb.AppendLine(PrimeFactorization.GetSummary(clockTimeS, cpuTimeS));

            sb.AppendLine();
            sb.AppendLine("--- stats ---");

            long totalTicks = 0;
            long minTicks = Int64.MaxValue;
            long maxTicks = Int64.MinValue;
            int numResults = 0;

            Dictionary<string, int> machineCounts = new Dictionary<string, int>();
            if (Results != null)
            {
                foreach (var result in Results)
                {
                    ++numResults;
                    totalTicks += result.RunTimeTicks;
                    minTicks = Math.Min(minTicks, result.RunTimeTicks);
                    maxTicks = Math.Max(maxTicks, result.RunTimeTicks);

                    if (!machineCounts.ContainsKey(result.Machine))
                        machineCounts.Add(result.Machine, 1);
                    else
                        ++machineCounts[result.Machine];
                }
            }
            SortedSet<string> machines = new SortedSet<string>();
            foreach (var mc in machineCounts)
            {
                machines.Add($"{mc.Key} ({mc.Value})");
            }

            double meanTicks = (double)totalTicks / numResults;
            TimeSpan tsMean = TimeSpan.FromTicks((long)meanTicks);
            TimeSpan tsMax = TimeSpan.FromTicks(maxTicks);
            TimeSpan tsMin = TimeSpan.FromTicks(minTicks);

            sb.AppendLine($"# of tasks: {NumTasks}");
            sb.AppendLine($"Clock time: {clockTimeS.ToString("f2")} s");
            sb.AppendLine($"Task time: {cpuTimeS.ToString("f2")} s");
            sb.AppendLine($"Machines: {String.Join(", ", machines)}");
            sb.AppendLine($"Mean task run time: {tsMean.TotalSeconds.ToString("f2")}s");
            sb.AppendLine($"Max task run time: {tsMax.TotalSeconds.ToString("f2")}s");
            sb.AppendLine($"Min task run time: {tsMin.TotalSeconds.ToString("f2")}s");

            return sb.ToString();
        }

    }

    public static class JobDataUtilities
    {
        public static void CreateZOSJobData(
            ZOSJobData jobData,
            out JobData jobId,
            out SharedJobData sjd,
            out List<TaskData> tasks)
        {
            ZOSTaskData settings = jobData.Settings;
            switch (jobData.JobType)
            {
                case ZOSJobTypes.GlobalOpt:
                case ZOSJobTypes.HammerOpt:
                    CreateJobDataGlobalOpt(
                        settings.TotalTasks,
                        jobData.ZarFile,
                        settings.TaskTime,
                        settings.NumCores,
                        settings.UseDLS,
                        (jobData.JobType == ZOSJobTypes.HammerOpt),
                        out jobId, out sjd, out tasks);
                    break;
                case ZOSJobTypes.MCTol:
                    CreateJobDataMCTol(
                        settings.TotalTasks,
                        jobData.ZarFile,
                        jobData.TopFile,
                        settings.NumCores,
                        settings.NumMC,
                        out jobId, out sjd, out tasks);
                    break;
                case ZOSJobTypes.NSCRT:
                    CreateJobDataRT(
                        settings.TotalTasks,
                        jobData.ZarFile,
                        settings.NumCores,
                        settings.UsePolarization,
                        settings.SplitRays,
                        settings.ScatterRays,
                        settings.IgnoreErrors,
                        settings.RaysMult,
                        out jobId, out sjd, out tasks);
                    break;
                case ZOSJobTypes.PrimeFactor:
                    CreateJobDataPrimes(
                        settings.TotalTasks,
                        settings.NumCores,
                        out jobId, out sjd, out tasks);
                    break;
                default:
                    throw new ArgumentException();
            }
        }

        public static void CreateJobDataGlobalOpt(
            int numJobs,
            string zarFile,
            int timeS,
            int numCores,
            bool useDls,
            bool hammer,
            out JobData jobId,
            out SharedJobData sjd,
            out List<TaskData> tasks)
        {
            jobId = new HPCShared.JobData()
            {
                JobId = Guid.NewGuid().ToString(),
                JobType = hammer ? JobTypes.ZOS_HammerOptimization : JobTypes.ZOS_GlobalOptimization,
            };

            sjd = new SharedJobData()
            {
                JobId = jobId.JobId,
                Data = new HPCShared.DataEntry[]
                {
                    new HPCShared.DataEntry()
                    {
                        ID = "zarfile",
                        Name = System.IO.Path.GetFileName(zarFile),
                        DataType = HPCShared.DataTypes.File,
                        Data = System.IO.File.ReadAllBytes(zarFile),
                    }
                }
            };

            byte bDLS = (byte)(useDls ? 1 : 0);
            tasks = new List<TaskData>();
            for (int i = 0; i < numJobs; i++)
            {
                ZOSTaskData ztd = new ZOSTaskData()
                {
                    TotalTasks = numJobs,
                    TaskNumber = i,
                    NumCores = numCores,
                    TaskTime = timeS,
                    UseDLS = useDls,
                };

                var td = new TaskData()
                {
                    Job = jobId,
                    TaskNumber = i,
                    TotalTasks = numJobs,
                    Data = new HPCShared.DataEntry[]
                    {
                        new DataEntry
                        {
                            ID = "taskdata",
                            DataType = HPCShared.DataTypes.ZOSTaskData,
                            Data = HPCUtilities.Serialize(ztd),
                        },
                    }
                };

                tasks.Add(td);
            }
        }

        public static void CreateJobDataMCTol(
            int numJobs,
            string zarFile,
            string topFile,
            int numCores,
            int numMC,
            out JobData jobId,
            out SharedJobData sjd,
            out List<TaskData> tasks)
        {
            jobId = new HPCShared.JobData()
            {
                JobId = Guid.NewGuid().ToString(),
                JobType = JobTypes.ZOS_MCTolerancing,
            };

            sjd = new SharedJobData()
            {
                JobId = jobId.JobId,
                Data = new HPCShared.DataEntry[]
                {
                    new HPCShared.DataEntry()
                    {
                        ID = "zarfile",
                        Name = System.IO.Path.GetFileName(zarFile),
                        DataType = HPCShared.DataTypes.File,
                        Data = System.IO.File.ReadAllBytes(zarFile),
                    },
                    new HPCShared.DataEntry()
                    {
                        ID = "topfile",
                        Name = System.IO.Path.GetFileName(topFile),
                        DataType = DataTypes.File,
                        Data = System.IO.File.ReadAllBytes(topFile),
                    },
                }
            };

            tasks = new List<TaskData>();
            for (int i = 0; i < numJobs; i++)
            {
                ZOSTaskData ztd = new ZOSTaskData()
                {
                    TotalTasks = numJobs,
                    TaskNumber = i,
                    NumCores = numCores,
                    NumMC = numMC,
                };

                var td = new TaskData()
                {
                    Job = jobId,
                    TaskNumber = i,
                    TotalTasks = numJobs,
                    Data = new HPCShared.DataEntry[]
                    {
                        new DataEntry
                        {
                            ID = "taskdata",
                            DataType = HPCShared.DataTypes.ZOSTaskData,
                            Data = HPCUtilities.Serialize(ztd),
                        },
                    }
                };

                tasks.Add(td);
            }
        }

        public static void CreateJobDataRT(
             int numJobs,
             string zarFile,
             int numCores,
             bool usePol, bool split, bool scatter, bool ignoreErr,
             double raysMult,
             out JobData jobId,
             out SharedJobData sjd,
             out List<TaskData> tasks)
        {
            jobId = new HPCShared.JobData()
            {
                JobId = Guid.NewGuid().ToString(),
                JobType = JobTypes.ZOS_NSCRayTrace,
            };

            sjd = new SharedJobData()
            {
                JobId = jobId.JobId,
                Data = new HPCShared.DataEntry[]
                {
                    new HPCShared.DataEntry()
                    {
                        ID = "zarfile",
                        Name = System.IO.Path.GetFileName(zarFile),
                        DataType = HPCShared.DataTypes.File,
                        Data = System.IO.File.ReadAllBytes(zarFile),
                    },
                }
            };

            tasks = new List<TaskData>();
            for (int i = 0; i < numJobs; i++)
            {
                ZOSTaskData ztd = new ZOSTaskData()
                {
                    TotalTasks = numJobs,
                    TaskNumber = i,
                    NumCores = numCores,
                    UsePolarization = usePol,
                    SplitRays = split,
                    ScatterRays = scatter,
                    IgnoreErrors = ignoreErr,
                    RaysMult = raysMult,
                };

                var td = new TaskData()
                {
                    Job = jobId,
                    TaskNumber = i,
                    TotalTasks = numJobs,
                    Data = new HPCShared.DataEntry[]
                    {
                        new DataEntry
                        {
                            ID = "taskdata",
                            DataType = HPCShared.DataTypes.ZOSTaskData,
                            Data = HPCUtilities.Serialize(ztd),
                        },
                    }
                };

                tasks.Add(td);
            }
        }

        public static void CreateJobDataPrimes(
             int numJobs,
             int numCores,
             out JobData jobId,
             out SharedJobData sjd,
             out List<TaskData> tasks)
        {
            jobId = new HPCShared.JobData()
            {
                JobId = Guid.NewGuid().ToString(),
                JobType = JobTypes.PrimeTest1,
            };

            sjd = new SharedJobData()
            {
                JobId = jobId.JobId,
                Data = new HPCShared.DataEntry[]
                {
                }
            };

            tasks = new List<TaskData>();
            Random r = new Random();
            for (int i = 0; i < numJobs; i++)
            {
                int numToFactor;
                do
                {
                    numToFactor = Math.Abs(r.Next());
                }
                while (numToFactor < 1000);

                ZOSTaskData ztd = new ZOSTaskData()
                {
                    TotalTasks = numJobs,
                    TaskNumber = i,
                    NumCores = numCores,
                    NumberToFactor = numToFactor,
                };

                var td = new TaskData()
                {
                    Job = jobId,
                    TaskNumber = i,
                    TotalTasks = numJobs,
                    Data = new HPCShared.DataEntry[]
                    {
                        new DataEntry
                        {
                            ID = "taskdata",
                            DataType = HPCShared.DataTypes.ZOSTaskData,
                            Data = HPCUtilities.Serialize(ztd),
                        },
                    }
                };

                tasks.Add(td);
            }
        }


        public static List<ZOSStats> GetZOSStats(
            JobTypes jt,
            DateTime tS,
            string outputFolder,
            out int numSucceed,
            ref int numFail)
        {
            Dictionary<string, List<ZOSResult>> rbm = new Dictionary<string, List<ZOSResult>>();
            Action<string, ZOSResult> addResult = (string m, ZOSResult r) =>
            {
                List<ZOSResult> rm;
                if (!rbm.TryGetValue(m, out rm))
                {
                    rm = new List<ZOSResult>();
                    rbm.Add(m, rm);
                }

                rm.Add(r);
            };

            string[] resultFiles = Directory.GetFiles(outputFolder, "result_*.dat");
            numSucceed = 0;
            foreach (string resultFile in resultFiles)
            {
                byte[] resultData = File.ReadAllBytes(resultFile);
                if (resultData == null || resultData.Length == 0)
                {
                    ++numFail;
                    continue;
                }

                var optResult = HPCUtilities.Deserialize<ZOSResult>(resultData);
                if (optResult == null)
                {
                    ++numFail;
                    continue;
                }
                else
                {
                    ++numSucceed;

                    addResult(String.Empty, optResult);
                    addResult(optResult.Machine, optResult);
                }
            }
            DateTime tE = DateTime.UtcNow;

            List<ZOSStats> allStats = new List<ZOSStats>();
            foreach (var machineResults in rbm)
            {
                var results = machineResults.Value;

                long calcTicks = 0;
                foreach (var optResult in results)
                    calcTicks += optResult.RunTimeTicks;

                TimeSpan cpuTime = TimeSpan.FromTicks(calcTicks);

                ZOSStats stats = new ZOSStats();
                stats.MachineName = machineResults.Key;
                stats.StartTime = tS;
                stats.EndTime = tE;
                stats.NumTasks = stats.IsMaster ? (numSucceed + numFail) : results.Count;
                stats.Results = results.ToArray();
                stats.TotalRunTimeS = cpuTime.TotalSeconds;
                stats.Results = results.ToArray();

                switch (jt)
                {
                    case JobTypes.ZOS_GlobalOptimization:
                    case JobTypes.ZOS_HammerOptimization:
                        {
                            GetOptStats(
                                stats,
                                outputFolder,
                                ref numSucceed,
                                ref numFail);
                        }
                        break;

                    case JobTypes.ZOS_MCTolerancing:
                        {
                            GetMCTStats(
                                stats,
                                outputFolder,
                                ref numSucceed,
                                ref numFail);
                        }
                        break;

                    case JobTypes.ZOS_NSCRayTrace:
                        {
                            GetNSCRTStats(
                                stats,
                                outputFolder,
                                ref numSucceed,
                                ref numFail);
                        }
                        break;

                    case JobTypes.PrimeTest1:
                        {
                            GetPrimesStats(
                                stats,
                                outputFolder,
                                ref numSucceed,
                                ref numFail);
                        }
                        break;
                }

                allStats.Add(stats);
            }

            try
            {
                foreach (string resultFile in resultFiles)
                {
                    File.Delete(resultFile);
                }
            }
            catch { }

            return allStats;
        }

        public static void GetOptStats(
            ZOSStats stats,
            string outputFolder,
            ref int numSucceed,
            ref int numFail)
        {
            bool isMaster = stats.IsMaster;
            long totalSys = 0;
            List<GlobalOptResult> results = new List<GlobalOptResult>();
            foreach (var result in stats.Results)
            {
                var optResult = result.Optimization;
                if (optResult == null || optResult.BestFile == null || String.IsNullOrWhiteSpace(optResult.BestFile.FileName))
                {
                    if (isMaster)
                    {
                        ++numFail;
                        --numSucceed;
                    }
                    continue;
                }
                else
                {
                    results.Add(optResult);
                }

                totalSys += optResult.Systems;
            }
            results.Sort(CompareOptMF);

            stats.Optimization = new GlobalOptStats();
            stats.Optimization.Results = results.ToArray();
            stats.Optimization.TotalSystems = totalSys;

            if (isMaster)
            {
                int fileNum = 0;
                foreach (var optResult in results)
                {
                    string inFile = Path.Combine(outputFolder, optResult.BestFile.FileName);
                    string file = "GOPT_" + (++fileNum).ToString("####") + ".zmx";
                    optResult.BestFile.FileName = file;
                    Console.WriteLine($"{file} - MF: {optResult.MeritFunction.ToString("e5")}");

                    file = Path.Combine(outputFolder, file);
                    File.Move(inFile, file);
                }
            }
        }

        public static void GetNSCRTStats(
            ZOSStats stats,
            string outputFolder,
            ref int numSucceed,
            ref int numFail)
        {
            bool isMaster = stats.IsMaster;
            stats.NSCRayTrace = new NSCRTStats();

            List<NSCRTResult> results = new List<NSCRTResult>();
            double totalThresh = 0.0;
            double totalError = 0.0;
            long totalRays = 0;
            foreach (var result in stats.Results)
            {
                if (result.NSCRayTrace == null)
                {
                    if (isMaster)
                    {
                        ++numFail;
                        --numSucceed;
                    }
                    continue;
                }
                else
                {
                    if (isMaster)
                    {
                        ++numSucceed;
                    }
                    results.Add(result.NSCRayTrace);
                }

                totalThresh += result.NSCRayTrace.LostThresholds;
                totalError += result.NSCRayTrace.LostErrors;
                totalRays += result.NSCRayTrace.TotalRays;
            }

            // TODO - combine detector data?
            // For now just leave detector data files in output folder

            stats.NSCRayTrace.Results = results.ToArray();
            stats.NSCRayTrace.TotalLostErrors = totalError;
            stats.NSCRayTrace.TotalLostThresholds = totalThresh;
            stats.NSCRayTrace.TotalRays = totalRays;
        }

        public static void GetMCTStats(
            ZOSStats stats,
            string outputFolder,
            ref int numSucceed,
            ref int numFail)
        {
            bool isMaster = stats.IsMaster;
            stats.MonteCarloTolerancing = new MCTStats();

            List<MCTResult> results = new List<MCTResult>();
            foreach (var result in stats.Results)
            {
                var mcResult = result.MonteCarloTolerancing;
                if (mcResult == null || mcResult.Files == null || mcResult.Files.Length != 3)
                {
                    if (isMaster)
                    {
                        ++numFail;
                        --numSucceed;
                    }
                    continue;
                }
                else
                {
                    if (isMaster)
                    {
                        ++numSucceed;
                    }
                    results.Add(mcResult);
                }
            }

            // TODO - combine ztd files?  Organize best / works?
            // For now just leave data files in output folder

            stats.MonteCarloTolerancing.Results = results.ToArray();
        }

        public static void GetPrimesStats(
            ZOSStats stats,
            string outputFolder,
            ref int numSucceed,
            ref int numFail)
        {
            bool isMaster = stats.IsMaster;
            stats.PrimeFactorization = new PrimeStats();

            List<PrimeResult> results = new List<PrimeResult>();
            foreach (var result in stats.Results)
            {
                var pResult = result.PrimeFactoring;
                if (pResult == null || pResult.Factors == null || pResult.Factors.Length == 0)
                {
                    if (isMaster)
                    {
                        ++numFail;
                        --numSucceed;
                    }
                    continue;
                }
                else
                {
                    if (isMaster)
                    {
                        ++numSucceed;
                    }
                    results.Add(pResult);
                }
            }

            // TODO - combine ztd files?  Organize best / works?
            // For now just leave data files in output folder

            stats.PrimeFactorization.Results = results.ToArray();
        }

        private static int CompareOptMF(GlobalOptResult lhs, GlobalOptResult rhs)
        {
            return lhs.MeritFunction.CompareTo(rhs.MeritFunction);
        }

        public static void ProcessZOSResult(
            TaskResults result,
            out ZOSResult resultData)
        {
            switch (result.Job.JobType)
            {
                case JobTypes.ZOS_GlobalOptimization:
                case JobTypes.ZOS_HammerOptimization:
                    ProcessResultGlobalOpt(result, out resultData);
                    break;
                case JobTypes.ZOS_MCTolerancing:
                    ProcessResultMCT(result, out resultData);
                    break;
                case JobTypes.ZOS_NSCRayTrace:
                    ProcessResultNSCRT(result, out resultData);
                    break;
                case JobTypes.PrimeTest1:
                    ProcessResultPrimes(result, out resultData);
                    break;
                default:
                    throw new ArgumentException("Unknown job type: " + result.Job.JobType);
            }
        }

        public static void StoreZOSResult(
            JobTypes jt,
            ZOSResult result,
            string outputFolder,
            int numProcessed)
        {
            switch (jt)
            {
                case JobTypes.ZOS_GlobalOptimization:
                case JobTypes.ZOS_HammerOptimization:
                    {
                        StoreResultGlobalOpt(result, outputFolder, numProcessed);
                    }
                    break;
                case JobTypes.ZOS_MCTolerancing:
                    {
                        StoreResultMCT(result, outputFolder, numProcessed);
                    }
                    break;
                case JobTypes.ZOS_NSCRayTrace:
                    {
                        StoreResultNSCRT(result, outputFolder, numProcessed);
                    }
                    break;
                case JobTypes.PrimeTest1:
                    {
                        StoreResultPrimes(result, outputFolder, numProcessed);
                    }
                    break;
            }
        }

        public static void StoreResultGlobalOpt(
            ZOSResult optResult,
            string outputFolder,
            int numProcessed)
        {
            if (optResult.Optimization.BestFile != null)
            {
                string tempZmx = $"task_{numProcessed}.zmx";
                optResult.Optimization.BestFile.FileName = tempZmx;
                tempZmx = Path.Combine(outputFolder, tempZmx);
                File.WriteAllBytes(tempZmx, optResult.Optimization.BestFile.FileData);
            }
            else
            {
                optResult.Optimization.BestFile.FileName = String.Empty;
            }
            optResult.Optimization.BestFile.FileData = null;

            byte[] resultBytes = HPCUtilities.Serialize(optResult);

            string resFile = $"result_{numProcessed}.dat";
            resFile = Path.Combine(outputFolder, resFile);
            File.WriteAllBytes(resFile, resultBytes);
        }

        public static void ProcessResultGlobalOpt(
            TaskResults result,
            out ZOSResult resultData)
        {
            resultData = new ZOSResult();
            resultData.Optimization = new GlobalOptResult();
            resultData.Optimization.BestFile = new FileResult();
            foreach (var entry in result.Results)
            {
                switch (entry.ID)
                {
                    case "systems":
                        Debug.Assert(entry.DataType == DataTypes.TotalOptSystems);
                        resultData.Optimization.Systems = BitConverter.ToInt64(entry.Data, 0);
                        break;
                    case "mf":
                        Debug.Assert(entry.DataType == DataTypes.FinalOptMF);
                        resultData.Optimization.MeritFunction = BitConverter.ToDouble(entry.Data, 0);
                        break;
                    case "zmx":
                        Debug.Assert(entry.DataType == DataTypes.File);
                        resultData.Optimization.BestFile.FileName = entry.Name;
                        resultData.Optimization.BestFile.FileData = HPCUtilities.DecompressData(entry.Data);
                        break;
                    case "elapsedTime":
                        Debug.Assert(entry.DataType == DataTypes.Misc);
                        resultData.Machine = entry.Name;
                        resultData.RunTimeTicks = BitConverter.ToInt64(entry.Data, 0);
                        break;
                }
            }
        }

        public static void StoreResultNSCRT(
            ZOSResult rtResult,
            string outputFolder,
            int numProcessed)
        {
            foreach (var dr in rtResult.NSCRayTrace.DetectorData)
            {
                if (dr.FileData != null)
                {
                    string tmpFile = $"t{numProcessed}_{dr.FileName}";
                    dr.FileName = tmpFile;
                    tmpFile = Path.Combine(outputFolder, tmpFile);
                    File.WriteAllBytes(tmpFile, dr.FileData);
                }
                else
                {
                    dr.FileName = String.Empty;
                }
                dr.FileData = null;
            }

            byte[] resultBytes = HPCUtilities.Serialize(rtResult);
            string resFile = $"result_{numProcessed}.dat";
            resFile = Path.Combine(outputFolder, resFile);
            File.WriteAllBytes(resFile, resultBytes);
        }

        public static void ProcessResultNSCRT(
            TaskResults result,
            out ZOSResult resultData)
        {
            resultData = new ZOSResult();
            resultData.NSCRayTrace = new NSCRTResult();

            var rtData = resultData.NSCRayTrace;
            List<FileResult> detResults = new List<FileResult>();
            foreach (var entry in result.Results)
            {
                switch (entry.ID)
                {
                    case "lostthresholds":
                        Debug.Assert(entry.DataType == DataTypes.ValueDouble);
                        rtData.LostThresholds = BitConverter.ToDouble(entry.Data, 0);
                        break;
                    case "losterrors":
                        Debug.Assert(entry.DataType == DataTypes.ValueDouble);
                        rtData.LostErrors = BitConverter.ToDouble(entry.Data, 0);
                        break;
                    case "totalrays":
                        Debug.Assert(entry.DataType == DataTypes.ValueLong);
                        rtData.TotalRays = BitConverter.ToInt64(entry.Data, 0);
                        break;
                    case "detdata":
                        Debug.Assert(entry.DataType == DataTypes.File);
                        FileResult dr = new FileResult()
                        {
                            FileName = entry.Name,
                            FileData = HPCUtilities.DecompressData(entry.Data),
                        };
                        detResults.Add(dr);
                        break;
                    case "elapsedTime":
                        Debug.Assert(entry.DataType == DataTypes.Misc);
                        resultData.Machine = entry.Name;
                        resultData.RunTimeTicks = BitConverter.ToInt64(entry.Data, 0);
                        break;
                }
            }

            rtData.DetectorData = detResults.ToArray();
        }

        public static void StoreResultMCT(
            ZOSResult mcResult,
            string outputFolder,
            int numProcessed)
        {
            foreach (var dr in mcResult.MonteCarloTolerancing.Files)
            {
                if (dr.FileData != null)
                {
                    string tmpFile = $"t{numProcessed}_{dr.FileName}";
                    dr.FileName = tmpFile;
                    tmpFile = Path.Combine(outputFolder, tmpFile);
                    File.WriteAllBytes(tmpFile, dr.FileData);
                }
                else
                {
                    dr.FileName = String.Empty;
                }
                dr.FileData = null;
            }

            byte[] resultBytes = HPCUtilities.Serialize(mcResult);
            string resFile = $"result_{numProcessed}.dat";
            resFile = Path.Combine(outputFolder, resFile);
            File.WriteAllBytes(resFile, resultBytes);
        }

        public static void ProcessResultMCT(
            TaskResults result,
            out ZOSResult resultData)
        {
            resultData = new ZOSResult();
            resultData.MonteCarloTolerancing = new MCTResult();

            var mcData = resultData.MonteCarloTolerancing;
            List<FileResult> mcFiles = new List<FileResult>();
            foreach (var entry in result.Results)
            {
                switch (entry.ID)
                {
                    case "bestzmx":
                    case "worstzmx":
                    case "ztd":
                        Debug.Assert(entry.DataType == DataTypes.File);
                        FileResult mdFile = new FileResult()
                        {
                            FileName = entry.Name,
                            FileData = HPCUtilities.DecompressData(entry.Data),
                        };
                        mcFiles.Add(mdFile);
                        break;
                    case "elapsedTime":
                        Debug.Assert(entry.DataType == DataTypes.Misc);
                        resultData.Machine = entry.Name;
                        resultData.RunTimeTicks = BitConverter.ToInt64(entry.Data, 0);
                        break;
                }
            }

            mcData.Files = mcFiles.ToArray();
        }

        public static void StoreResultPrimes(
            ZOSResult primeResult,
            string outputFolder,
            int numProcessed)
        {
            byte[] resultBytes = HPCUtilities.Serialize(primeResult);
            string resFile = $"result_{numProcessed}.dat";
            resFile = Path.Combine(outputFolder, resFile);
            File.WriteAllBytes(resFile, resultBytes);
        }

        public static void ProcessResultPrimes(
            TaskResults result,
            out ZOSResult resultData)
        {
            resultData = new ZOSResult();
            resultData.PrimeFactoring = new PrimeResult();

            var pData = resultData.PrimeFactoring;
            List<int> factors = new List<int>();
            int numFactors = 0;
            foreach (var entry in result.Results)
            {
                switch (entry.ID)
                {
                    case "numbertofactor":
                        pData.NumberToFactor = BitConverter.ToInt32(entry.Data, 0);
                        break;
                    case "numberoffactors":
                        numFactors = BitConverter.ToInt32(entry.Data, 0);
                        break;
                    case "factor":
                        factors.Add(BitConverter.ToInt32(entry.Data, 0));
                        break;
                    case "elapsedTime":
                        Debug.Assert(entry.DataType == DataTypes.Misc);
                        resultData.Machine = entry.Name;
                        resultData.RunTimeTicks = BitConverter.ToInt64(entry.Data, 0);
                        break;
                }
            }

            pData.Factors = factors.ToArray();
            Debug.Assert(numFactors == factors.Count);
        }

    }
}
