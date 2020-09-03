using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeepRunning
{
    public class AppInfo
    {
        public string Name { get; set; }
        public string PathExe { get; set; }
        public string Folder { get; set; }
        public string Message { get; set; }
        public int CountNotRun { get; set; }
        public DateTime? UpdateExpiredTime { get; set; }
    }
}
