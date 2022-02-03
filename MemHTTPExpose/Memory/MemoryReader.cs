using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemHTTPExpose.Memory
{
    public class MemoryReader
    {
        private Process _process;

        public readonly IDictionary<Type, Delegate> Read;

        public MemoryReader(Process process)
        {
            _process = process;
            Read = new Dictionary<Type, Delegate>()
            {
                {typeof(IntPtr) , new Func<IntPtr, IntPtr>(ReadIntPtr)},
                {typeof(int) , new Func<IntPtr, int>(ReadInt32)},
                {typeof(short) , new Func<IntPtr, short>(ReadInt16)},
                {typeof(double) , new Func<IntPtr, double>(ReadDouble)},
                {typeof(string) , new Func<IntPtr, string>(ReadString)},
            };
        }

        public IntPtr ReadIntPtr(IntPtr addr)
        {
            byte[] buffer = new byte[sizeof(int)];
            int bytesRead = 0;
            Kernel32.ReadProcessMemory(_process.Handle, addr, buffer, sizeof(int), out bytesRead);
            return (IntPtr)BitConverter.ToInt32(buffer, 0);
        }
        public int ReadInt32(IntPtr addr)
        {
            byte[] buffer = new byte[sizeof(int)];
            int bytesRead = 0;
            Kernel32.ReadProcessMemory(_process.Handle, addr, buffer, sizeof(int), out bytesRead);
            return BitConverter.ToInt32(buffer, 0);
        }
        public short ReadInt16(IntPtr addr)
        {

            byte[] buffer = new byte[sizeof(short)];
            int bytesRead = 0;
            Kernel32.ReadProcessMemory(_process.Handle, addr, buffer, sizeof(short), out bytesRead);
            return BitConverter.ToInt16(buffer, 0);
        }
        public double ReadDouble(IntPtr addr)
        {
            byte[] buffer = new byte[sizeof(double)];
            int bytesRead = 0;
            Kernel32.ReadProcessMemory(_process.Handle, addr, buffer, sizeof(double), out bytesRead);
            return BitConverter.ToDouble(buffer, 0);
        }
        public string ReadString(IntPtr addr)
        {
            int length = ReadInt32(addr);
            Console.WriteLine(length);
            // max length 4096?
            if (length <= 0 || length > 4096)
                return "";
            byte[] buffer = new byte[length];
            int bytesRead = 0;
            Kernel32.ReadProcessMemory(_process.Handle, addr, buffer, (uint)length, out bytesRead);
            return Encoding.Unicode.GetString(buffer);
        }
    }
}
