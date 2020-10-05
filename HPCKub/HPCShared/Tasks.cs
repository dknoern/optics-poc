using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HPCShared
{
    [Serializable]
    public class TaskData
    {
        public JobData Job { get; set; }
        public int TaskNumber { get; set; }
        public int TotalTasks { get; set; }
        public DataEntry[] Data { get; set; }
    }

    [Serializable]
    public class TaskResults
    {
        public JobData Job { get; set; }
        public int TaskNumber { get; set; }
        public DataEntry[] Results { get; set; }
    }
}
