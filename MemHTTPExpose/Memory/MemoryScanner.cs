using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Runtime.InteropServices;
using static MemHTTPExpose.Memory.Kernel32;

// Taken from: https://github.com/OsuSync/OsuRTDataProvider
// Modified by me

namespace MemHTTPExpose.Memory
{
    public class MemoryScanner
    {
        private class MemoryRegion
        {
            public IntPtr AllocationBase { get; set; }
            public IntPtr BaseAddress { get; set; }
            public ulong RegionSize { get; set; }
            public byte[] DumpedRegion { get; set; }
        }

        private Process _process;

        public MemoryScanner(Process proc)
        {
            this._process = proc;
        }

        public void Reload()
        {
            ClearMemory();
            InitMemoryRegionInfo();
        }

        private List<MemoryRegion> m_memoryRegionList = new List<MemoryRegion>();
        private const int PROCESS_QUERY_INFORMATION = 0x0400;
        private const int MEM_COMMIT = 0x00001000;
        private const int PAGE_READWRITE = 0x04;
        private const int PROCESS_WM_READ = 0x0010;

        private unsafe void InitMemoryRegionInfo()
        {
            SYSTEM_INFO sys_info;
            //Get the maximum and minimum addresses of the process. 
            GetSystemInfo(out sys_info);
            IntPtr proc_min_address = sys_info.minimumApplicationAddress;
            IntPtr proc_max_address = sys_info.maximumApplicationAddress;

            byte* current_address = (byte*)proc_min_address.ToPointer();
            byte* lproc_max_address = (byte*)proc_max_address.ToPointer();

            IntPtr handle = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_WM_READ, false, _process.Id);

            if (handle == IntPtr.Zero)
            {
                Console.WriteLine($"Error Code:0x{Marshal.GetLastWin32Error():X8}");
                return;
            }

            MemoryBasicInformation mem_basic_info = new MemoryBasicInformation();

            while (current_address < lproc_max_address)
            {
                //Query the current memory page information.
                bool ok = QueryMemoryBasicInformation(handle, (IntPtr)current_address, ref mem_basic_info);

                if (!ok)
                {
                    Console.WriteLine($"Error Code:0x{Marshal.GetLastWin32Error():X8}");
                    break;
                }

                //Dump JIT code
                if ((mem_basic_info.Protect & AllocationProtect.PAGE_EXECUTE_READWRITE) > 0 && mem_basic_info.State == MEM_COMMIT)
                {
                    var region = new MemoryRegion()
                    {
                        BaseAddress = mem_basic_info.BaseAddress,
                        AllocationBase = mem_basic_info.AllocationBase,
                        RegionSize = mem_basic_info.RegionSize
                    };
                    m_memoryRegionList.Add(region);
                }

                //if (Setting.DebugMode)
                //{
                //    LogHelper.EncryptLog($"BaseAddress: 0x{mem_basic_info.BaseAddress:X8} RegionSize: 0x{mem_basic_info.RegionSize:X8} AllocationBase: 0x{mem_basic_info.AllocationBase:X8} Protect: {mem_basic_info.Protect} Commit: {mem_basic_info.State==MEM_COMMIT}(0x{mem_basic_info.State:X8})");
                //}

                current_address += mem_basic_info.RegionSize;
            }

            CloseHandle(handle);

            if (m_memoryRegionList.Count == 0)
            {
                Console.WriteLine($"Error:List is Empty");
            }
        }

        /// <returns>Boolean based on RPM results and valid properties.</returns>
        private bool DumpMemory()
        {
            try
            {
                // Checks to ensure we have valid data.
                if (this._process == null)
                    return false;
                if (this._process.HasExited == true)
                    return false;

                // Create the region space to dump into.
                foreach (var region in m_memoryRegionList)
                {
                    if (region.DumpedRegion != null) continue;

                    region.DumpedRegion = new byte[region.RegionSize];

                    bool bReturn = false;
                    int nBytesRead = 0;

                    // Dump the memory.
                    bReturn = ReadProcessMemory(
                        this._process.Handle, region.BaseAddress, region.DumpedRegion, (uint)region.RegionSize, out nBytesRead
                        );

                    // Validation checks.
                    if (bReturn == false || nBytesRead != (int)region.RegionSize)
                        return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($":{ex.Message}");
                return false;
            }
        }

        public static byte[] HexToByteArray(string hex)
        {
            hex = string.Join("", hex.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
            if (hex.Length % 2 == 1)
                throw new Exception("Hex must be even length.");

            byte[] arr = new byte[hex.Length >> 1];

            for (int i = 0; i < hex.Length >> 1; ++i)
            {
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
            }

            return arr;
        }

        public static int GetHexVal(char hex)
        {
            int val = hex;
            return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }

        private bool MaskCheck(MemoryRegion region, int offset, string pattern)
        {
            string[] hexString = pattern.Split(' ');
            byte[] hexBytes = HexToByteArray(pattern);
            for (int i = 0; i < hexString.Length && (ulong)(i + offset) < region.RegionSize; i++)
            {
                // Wildcard
                if (hexString[i].Equals("??"))
                    continue;

                if (hexBytes[i] != region.DumpedRegion[offset + i])
                    return false;
            }
            return true;
        }

        public IntPtr FindPattern(string pattern, int offset)
        {
            try
            {
                if (!DumpMemory())
                    return IntPtr.Zero;

                foreach (var region in m_memoryRegionList)
                {
                    for (int x = 0; x < region.DumpedRegion.Length; x++)
                    {
                        if (MaskCheck(region, x, pattern))
                        {
                            return new IntPtr((int)region.BaseAddress + (x + offset));
                        }
                    }
                }
                // Pattern was not found.
                return IntPtr.Zero;
            }
            catch (Exception ex)
            {
                Console.WriteLine($":{ex.Message}");
                return IntPtr.Zero;
            }
        }

        public void ClearMemory()
        {
            m_memoryRegionList.Clear();
        }

        private bool isX64()
        {
            return IntPtr.Size == 8;
        }

        private bool QueryMemoryBasicInformation(IntPtr handle, IntPtr current_address, ref MemoryBasicInformation memoryBasicInformation)
        {
            if (isX64())
            {
                MEMORY_BASIC_INFORMATION_X64 mem_basic_info = new MEMORY_BASIC_INFORMATION_X64();
                int mem_info_size = Marshal.SizeOf<MEMORY_BASIC_INFORMATION_X64>();
                int size = VirtualQueryEx_X64(handle, current_address, out mem_basic_info, (uint)mem_info_size);

                if (size != mem_info_size)
                {
                    Console.WriteLine($"(X64)Error Code:0x{Marshal.GetLastWin32Error():X8}");
                    return false;
                }

                memoryBasicInformation.RegionSize = mem_basic_info.RegionSize;
                memoryBasicInformation.BaseAddress = mem_basic_info.BaseAddress;
                memoryBasicInformation.AllocationProtect = mem_basic_info.AllocationProtect;
                memoryBasicInformation.AllocationBase = mem_basic_info.AllocationBase;
                memoryBasicInformation.Type = mem_basic_info.Type;
                memoryBasicInformation.State = mem_basic_info.State;
                memoryBasicInformation.Protect = mem_basic_info.Protect;
                return true;
            }
            else
            {
                MEMORY_BASIC_INFORMATION_X86 mem_basic_info = new MEMORY_BASIC_INFORMATION_X86();
                int mem_info_size = Marshal.SizeOf<MEMORY_BASIC_INFORMATION_X86>();
                int size = VirtualQueryEx_X86(handle, current_address, out mem_basic_info, (uint)mem_info_size);

                if (size != mem_info_size)
                {
                    Console.WriteLine($"(X86)Error Code:0x{Marshal.GetLastWin32Error():X8}");
                    return false;
                }

                memoryBasicInformation.RegionSize = mem_basic_info.RegionSize;
                memoryBasicInformation.BaseAddress = mem_basic_info.BaseAddress;
                memoryBasicInformation.AllocationProtect = mem_basic_info.AllocationProtect;
                memoryBasicInformation.AllocationBase = mem_basic_info.AllocationBase;
                memoryBasicInformation.Type = mem_basic_info.Type;
                memoryBasicInformation.State = mem_basic_info.State;
                memoryBasicInformation.Protect = mem_basic_info.Protect;
                return true;
            }
        }

        public Process Process
        {
            get { return this._process; }
            set { this._process = value; }
        }
     

    }
}