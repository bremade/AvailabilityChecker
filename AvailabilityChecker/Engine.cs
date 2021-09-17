using System;
using System.Runtime.InteropServices;
using Common;

namespace AvailabilityChecker
{

    static class Engine
    {
        static public bool findProcessInSystray()
        {
            IntPtr systemTrayHandle = GetSystemTrayHandle();

            UInt32 count = User32.SendMessage(systemTrayHandle, TB.BUTTONCOUNT, 0, 0);

            for (int i = 0; i < count; i++)
            {
                TBBUTTON tbButton = new TBBUTTON();
                string text = String.Empty;
                IntPtr ipWindowHandle = IntPtr.Zero;

                GetTBButton(systemTrayHandle, i, ref tbButton, ref text, ref ipWindowHandle);

                // Workaround to check if microphone is in use
                if (text.Contains("using your microphone"))
                {
                    return true;
                }
            }
            return false;
        }

        static IntPtr GetSystemTrayHandle()
        {
            IntPtr hWndTray = User32.FindWindow("Shell_TrayWnd", null);
            if (hWndTray != IntPtr.Zero)
            {
                hWndTray = User32.FindWindowEx(hWndTray, IntPtr.Zero, "TrayNotifyWnd", null);
                if (hWndTray != IntPtr.Zero)
                {
                    hWndTray = User32.FindWindowEx(hWndTray, IntPtr.Zero, "SysPager", null);
                    if (hWndTray != IntPtr.Zero)
                    {
                        hWndTray = User32.FindWindowEx(hWndTray, IntPtr.Zero, "ToolbarWindow32", null);
                        return hWndTray;
                    }
                }
            }

            return IntPtr.Zero;
        }


        public static unsafe void GetTBButton(IntPtr hToolbar, int i, ref TBBUTTON tbButton, ref string text, ref IntPtr ipWindowHandle)
        {
            // One page
            const int BUFFER_SIZE = 0x1000;

            byte[] localBuffer = new byte[BUFFER_SIZE];

            UInt32 processId = 0;
            UInt32 threadId = User32.GetWindowThreadProcessId(hToolbar, out processId);

            IntPtr hProcess = Kernel32.OpenProcess(ProcessRights.ALL_ACCESS, false, processId);

            IntPtr ipRemoteBuffer = Kernel32.VirtualAllocEx(
                hProcess,
                IntPtr.Zero,
                new UIntPtr(BUFFER_SIZE),
                MemAllocationType.COMMIT,
                MemoryProtection.PAGE_READWRITE);

            // Check TButton
            fixed (TBBUTTON* pTBButton = &tbButton)
            {
                IntPtr ipTBButton = new IntPtr(pTBButton);

                int b = (int)User32.SendMessage(hToolbar, TB.GETBUTTON, (IntPtr)i, ipRemoteBuffer);

                // this is fixed
                Int32 dwBytesRead = 0;
                IntPtr ipBytesRead = new IntPtr(&dwBytesRead);

                bool b2 = Kernel32.ReadProcessMemory(
                    hProcess,
                    ipRemoteBuffer,
                    ipTBButton,
                    new UIntPtr((uint)sizeof(TBBUTTON)),
                    ipBytesRead);

            }

            // Retrieve Text
            fixed (byte* pLocalBuffer = localBuffer)
            {
                IntPtr ipLocalBuffer = new IntPtr(pLocalBuffer);

                int chars = (int)User32.SendMessage(hToolbar, TB.GETBUTTONTEXTW, (IntPtr)tbButton.idCommand, ipRemoteBuffer);

                Int32 dwBytesRead = 0;
                IntPtr ipBytesRead = new IntPtr(&dwBytesRead);

                bool b4 = Kernel32.ReadProcessMemory(
                    hProcess,
                    ipRemoteBuffer,
                    ipLocalBuffer,
                    new UIntPtr(BUFFER_SIZE),
                    ipBytesRead);


                text = Marshal.PtrToStringUni(ipLocalBuffer, chars);

                if (text == " ") text = String.Empty;
            }

            Kernel32.VirtualFreeEx(
                hProcess,
                ipRemoteBuffer,
                UIntPtr.Zero,
                MemAllocationType.RELEASE);


            Kernel32.CloseHandle(hProcess);
        }

    }
}