using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MemHTTPExpose.Memory
{
    public class MemoryHandler
    {
        protected MemoryScanner _mScanner;
        protected MemoryReader _mReader;
        private Process _process;

        protected static readonly IDictionary<string, Type> Types = new Dictionary<string, Type>()
        {
            { "object", typeof(object) },
            { "string", typeof(string) },
            { "bool", typeof(bool) },
            { "byte", typeof(byte) },
            { "char", typeof(char) },
            { "decimal", typeof(decimal) },
            { "double", typeof(double) },
            { "short", typeof(short) },
            { "int", typeof(int) },
            { "long", typeof(long) },
            { "sbyte", typeof(sbyte) },
            { "float", typeof(float) },
            { "ushort", typeof(ushort) },
            { "uint", typeof(uint) },
            { "ulong", typeof(ulong) },
            { "void", typeof(void) },
            { "ptr", typeof(IntPtr)}
        };
        

        public MemoryHandler(Process proc){
            this._mScanner = new(proc);
            this._mReader = new(proc);
            _process = proc;
            _mScanner.Reload();
        }

        public Process GetProcess()
        {
            return _process;
        }

        public T Read<T>(IntPtr identifier)
        {
            return Read<T>(identifier, 0);
        }
        public T Read<T>(IntPtr identifier, int offset)
        {
            return (T)_mReader.Read[typeof(T)].DynamicInvoke(identifier + offset);
        }

        public IntPtr Scan(string pattern, int offset)
        {
            return _mScanner.FindPattern(pattern, offset);
        }

        public void Reload()
        {
            _mScanner.Reload();
        }
    }

}
