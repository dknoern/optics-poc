using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace HPCShared
{
    public abstract class HPCConfigurationBase
    {
        public abstract void LogInfo(string info);

        public abstract string GetInputFolder(string jobId);

        public abstract string GetOutputFolder(string jobId);

        public abstract string GetProgramFolder();

        public abstract string GetRandomInputFile(string jobId);

        public abstract string GetRandomOutputFile(string jobId);

        public abstract void WriteMessage(string m);

        public abstract void CleanFolders(string jobId);
    }

    public class HPCPackConfig : HPCConfigurationBase
    {
        private const string HPCRoot = @"C:\HPC\";
        private const string LogName = "log.txt";

        public override string GetInputFolder(string jobId)
        {
            return HPCUtilities.EnsureDir(Path.Combine(HPCRoot, "Inputs", jobId));
        }

        public override string GetOutputFolder(string jobId)
        {
            return HPCUtilities.EnsureDir(Path.Combine(HPCRoot, "Outputs", jobId));
        }

        public override string GetProgramFolder()
        {
            return Path.Combine(HPCRoot, "Programs");
        }

        public override string GetRandomInputFile(string jobId)
        {
            return HPCUtilities.GetRandomFile(GetInputFolder(jobId));
        }

        public override string GetRandomOutputFile(string jobId)
        {
            return HPCUtilities.GetRandomFile(GetOutputFolder(jobId));
        }

        public override void LogInfo(string info)
        {
            try
            {
                string logDir = Path.Combine(HPCRoot, "Logs");
                HPCUtilities.EnsureDir(logDir);

                string logFile = Path.Combine(logDir, LogName);
                using (StreamWriter sw = new StreamWriter(logFile, true))
                {
                    sw.WriteLine(info);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }

        public override void WriteMessage(string m)
        {
            Console.Error?.WriteLine(m);
        }

        public override void CleanFolders(string jobId)
        {
            string[] folders = new string[]
            {
                "Inputs",
                "Outputs",
            };

            foreach (string folder in folders)
            {
                string path = Path.Combine(HPCRoot, folder);

                string[] dirs = Directory.GetDirectories(path);
                foreach (string dir in dirs)
                {
                    string dirName = dir.Substring(path.Length).Trim('\\');
                    if (System.String.Compare(dirName, jobId, true) != 0)
                    {
                        Directory.Delete(dir, true);
                    }
                }
            }
        }
    }

    public class HPCKubConfig : HPCConfigurationBase
    {
        // TODO - implement!
        public override void CleanFolders(string jobId)
        {
            throw new NotImplementedException();
        }

        public override string GetInputFolder(string jobId)
        {
            throw new NotImplementedException();
        }

        public override string GetOutputFolder(string jobId)
        {
            throw new NotImplementedException();
        }

        public override string GetProgramFolder()
        {
            throw new NotImplementedException();
        }

        public override string GetRandomInputFile(string jobId)
        {
            throw new NotImplementedException();
        }

        public override string GetRandomOutputFile(string jobId)
        {
            throw new NotImplementedException();
        }

        public override void LogInfo(string info)
        {
            throw new NotImplementedException();
        }

        public override void WriteMessage(string m)
        {
            throw new NotImplementedException();
        }
    }

    public enum HPCEnvironment
    {
        Unknown,
        HPCPack,
        KubernetesAWS,
    }

    public static class HPCUtilities
    {
        private static HPCConfigurationBase Config;
        public static HPCEnvironment Environment { get; private set; } = HPCEnvironment.Unknown;

        public static void Init(HPCEnvironment env)
        {
            switch (env)
            {
                case HPCEnvironment.HPCPack:
                    Config = new HPCPackConfig();
                    break;
                case HPCEnvironment.KubernetesAWS:
                    Config = new HPCKubConfig();
                    break;
                default:
                    throw new NotImplementedException();
            }
            Environment = env;
        }

        private const string S_Env = "ENV";
        public static void ReadEnv(string iniFile)
        {
            using (StreamReader sr = new StreamReader(iniFile))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] vals = line.Split('=');
                    if (vals.Length != 2)
                        continue;

                    string key = vals[0].ToUpper().Trim();
                    string val = vals[1].Trim();

                    if (key == (S_Env))
                    {
                        if (Enum.TryParse<HPCEnvironment>(val, out HPCEnvironment env))
                        {
                            Init(env);
                            return;
                        }
                    }
                }
            }

            throw new Exception("Unknown environment!");
        }

        public static void WriteEnv(string iniFile)
        {
            using (StreamWriter sw = new StreamWriter(iniFile))
            {
                sw.WriteLine($"ENV={Environment.ToString()}");
            }
        }

        public static void WriteMessage(string m)
        {
            Config.WriteMessage(m);
        }

        public static void LogInfo(string info)
        {
            Config.LogInfo(info);
        }

        public static string GetInputFolder(string jobId)
        {
            return Config.GetInputFolder(jobId);
        }

        public static string GetOutputFolder(string jobId)
        {
            return Config.GetOutputFolder(jobId);
        }

        public static string GetProgramFolder()
        {
            return Config.GetProgramFolder();
        }

        public static string GetRandomInputFile(string jobId)
        {
            return Config.GetRandomInputFile(jobId);
        }

        public static string GetRandomOutputFile(string jobId)
        {
            return Config.GetRandomOutputFile(jobId);
        }

        public static void CleanFolders(string jobId)
        {
            Config.CleanFolders(jobId);
        }

        public static string EnsureDir(string dir)
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
        }

        public static string RunGlobalAction(string jobId, string actionName, Func<string> a)
        {
            //const string lockFileName = "sync.lock";
            string lockFileName = actionName + ".lock";
            string lockFile = Path.Combine(GetInputFolder(jobId), lockFileName);

            string result = String.Empty;
            while (String.IsNullOrWhiteSpace(result = IsActionComplete(lockFile)))
            {
                try
                {
                    using (FileStream fs = new FileStream(lockFile, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                    {
                        if (fs.Length > 0)
                        {
                            // something else beat us to it?
                            fs.Close();
                            // check again to see if it is complete
                            continue;
                        }

                        // we have ownership, run the action!
                        try
                        {
                            result = a();
                            if (String.IsNullOrWhiteSpace(result))
                            {
                                // invalid action?!
                                return String.Empty;
                            }
                            else
                            {
                                byte[] data = System.Text.Encoding.UTF8.GetBytes(result);
                                fs.Write(data, 0, data.Length);
                                return result;
                            }
                        }
                        catch { }
                    }
                }
                catch { }
            }

            return result;
        }

        private static string IsActionComplete(string lockFile)
        {
            try
            {
                if (File.Exists(lockFile))
                {
                    byte[] data = File.ReadAllBytes(lockFile);
                    if (data == null || data.Length == 0)
                        return String.Empty;

                    return System.Text.Encoding.UTF8.GetString(data);
                }
            }
            catch
            {
                // In use by another process - wait a bit
                System.Threading.Thread.Sleep(150);
            }

            return String.Empty;
        }

        public static string GetClientName(int sessionId, int batchNumber)
        {
            return $"s{sessionId}_b{batchNumber}";
        }

        public static string GetRandomFile(string folder)
        {
            while (true)
            {
                string fileName = Path.GetRandomFileName();
                string fullFile = Path.Combine(folder, fileName);
                if (!File.Exists(fullFile))
                    return fullFile;
            }
        }

        public static string ProcessException(Exception ex)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(ex.Message);
            if (ex.InnerException != null)
            {
                sb.AppendLine("Inner");
                sb.Append("\t");
                sb.AppendLine(ProcessException(ex.InnerException));
            }
            sb.AppendLine("ST");
            sb.AppendLine("\t" + ex.StackTrace);
            return sb.ToString();
        }

        public static T Deserialize<T>(byte[] data)
        {
            if (data == null || data.Length == 0)
                return default(T);

            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream(data))
            {
                return (T)bf.Deserialize(ms);
            }
        }

        public static byte[] Serialize(object o)
        {
            if (o == null)
                return null;

            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                bf.Serialize(ms, o);
                ms.Flush();
                return ms.ToArray();
            }
        }

        public static byte[] CompressData(byte[] data)
        {
            if (data == null || data.Length == 0)
                return data;
            using (MemoryStream ims = new MemoryStream(data))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    using (GZipStream zs = new GZipStream(ms, CompressionMode.Compress))
                    {
                        ims.CopyTo(zs);
                    }
                    ms.Flush();
                    return ms.ToArray();
                }
            }
        }

        public static byte[] DecompressData(byte[] data)
        {
            if (data == null || data.Length == 0)
                return data;
            using (MemoryStream ms = new MemoryStream(data))
            {
                using (MemoryStream oms = new MemoryStream())
                {
                    using (GZipStream zs = new GZipStream(ms, CompressionMode.Decompress))
                    {
                        zs.CopyTo(oms);
                    }
                    oms.Flush();
                    return oms.ToArray();
                }
            }
        }

        public static int GetRandomSeed(byte[] fromBytes)
        {
            return GetHash(fromBytes);
        }

        private static int GetHash(IEnumerable<byte> data)
        {
            uint hash = 2166136261;
            const uint prime = 16777619;
            foreach (byte b in data)
            {
                hash = hash ^ b;
                hash = hash * prime;
            }

            return (int)hash;
        }

    }


}
