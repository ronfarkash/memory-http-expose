using MemHTTPExpose.Memory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MemHTTPExpose.ExposedProc
{
    public class ExposedProcess : MemoryHandler
    {
        private static ExposedProcess _instance = null;
        private static object _mutex = new();

        public static ExposedProcess Get(int procId)
        {
            if(_instance == null)
            {
                lock (_mutex)
                {
                    if(_instance == null)
                    {
                        _instance = new(procId);
                    }
                }
            }
            return _instance;
        }
        public static ExposedProcess Get(string procName)
        {
            if (_instance == null)
            {
                lock (_mutex)
                {
                    if (_instance == null)
                    {
                        _instance = new(procName);
                    }
                }
            }
            return _instance;
        }

        static ExposedProcess() { }

        private ExposedProcess(string procName) : base(Process.GetProcessesByName(procName).First()){}
        private ExposedProcess(int procId): base(Process.GetProcessById(procId)){}

        
    }
}
