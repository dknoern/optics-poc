using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace HPCShared
{
    public static class HPCUtilities
    {
        private const string HPCRoot = @"C:\HPC\";
        private const string LogName = "log.txt";

 
        public static void LogInfo(string info)
        {
            try
            { 
                string logDir = Path.Combine(HPCRoot, "Logs");
                EnsureDir(logDir);

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

        private static string EnsureDir(string dir)
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
        }

        public static string GetInputFolder(string jobId)
        {
            return EnsureDir(Path.Combine(HPCRoot, "Inputs", jobId));
        }

        public static string GetOutputFolder(string jobId)
        {
            return EnsureDir(Path.Combine(HPCRoot, "Outputs", jobId));
        }

        public static string GetProgramFolder()
        {
            return Path.Combine(HPCRoot, "Programs");
        }

        public static string GetRandomInputFile(string jobId)
        {
            return GetRandomFile(GetInputFolder(jobId));
        }

        public static string GetRandomOutputFile(string jobId)
        {
            return GetRandomFile(GetOutputFolder(jobId));
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

        public static void CleanFolders(string jobId)
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

        public static string GetClientName(int sessionId, int batchNumber)
        {
            return $"s{sessionId}_b{batchNumber}";
        }

        private static string GetRandomFile(string folder)
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
