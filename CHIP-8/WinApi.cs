using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CHIP_8
{
	internal static class WinApi
	{
		public const uint GENERIC_READ  = 0x80000000;
		public const uint GENERIC_WRITE = 0x40000000;

		public const uint FILE_SHARE_READ  = 0x00000001;
		public const uint FILE_SHARE_WRITE = 0x00000002;

		public const uint OPEN_EXISTING = 3;

		public const uint KEY_EVENT = 1;

		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern SafeFileHandle CreateFile(
			string lpFileName,
			uint dwFileAccess,
			uint dwFileShare,
			IntPtr lpSecurityAttributes,
			uint dwCreationDisposition,
			uint dwFlagsAndAttributes,
			IntPtr hTemplateFile);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool WriteConsoleOutput(
		  SafeFileHandle hConsoleOutput,
		  CHAR_INFO[] lpBuffer,
		  COORD dwBufferSize,
		  COORD dwBufferCoord,
		  ref SMALL_RECT lpWriteRegion);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool FlushConsoleInputBuffer(SafeFileHandle hConsoleInput);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool GetNumberOfConsoleInputEvents(SafeFileHandle hConsoleInput, out uint lpcNumberOfEvents);

		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern bool ReadConsoleInput(
			SafeFileHandle hConsoleInput,
			[Out] INPUT_RECORD[] lpBuffer,
			uint nLength,
			out uint lpNumberOfEventsRead);

		[DllImport("winmm.dll")]
		public static extern uint timeBeginPeriod(uint uPeriod);

		[DllImport("winmm.dll")]
		public static extern uint timeEndPeriod(uint uPeriod);

		[StructLayout(LayoutKind.Explicit)]
		public struct CHAR_INFO
		{
			[FieldOffset(0)]
			public CharUnion Char;

			[FieldOffset(2)]
			public ushort Attributes;
		}

		// TODO: Not sure what to do with this, is it needed with the CharSet.Auto behaviour and stuff?
		[StructLayout(LayoutKind.Explicit)]
		public struct CharUnion
		{
			[FieldOffset(0)]
			public char UnicodeChar;
			[FieldOffset(0)]
			public byte AsciiChar;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct COORD
		{
			public short X;
			public short Y;

			public COORD(short X, short Y)
			{
				this.X = X;
				this.Y = Y;
			}
		};

		[StructLayout(LayoutKind.Explicit, Size = 20)]
		public struct INPUT_RECORD
		{
			[FieldOffset(0)]
			public ushort EventType;

			[FieldOffset(4)]
			public KEY_EVENT_RECORD KeyEvent;

			// Don't care about MOUSE_EVENT_RECORD, WINDOW_BUFFER_SIZE_RECORD, MENU_EVENT_RECORD or FOCUS_EVENT_RECORD.
			// The StructLayout size set above ensures that the correct size will still be allocated without these other members.
		}

		[StructLayout(LayoutKind.Explicit, Size = 16, CharSet = CharSet.Auto)]
		public struct KEY_EVENT_RECORD
		{
			[FieldOffset(0)]
			public bool bKeyDown;

			[FieldOffset(4)]
			public ushort wRepeatCount;

			[FieldOffset(6)]
			public ushort wVirtualKeyCode;

			[FieldOffset(8)]
			public ushort wVirtualScanCode;

			[FieldOffset(10)]
			public char Char; // Native has a union of WCHAR and CHAR but I think this is enough if CharSet is Auto?

			[FieldOffset(12)]
			public uint dwControlKeyState;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct SMALL_RECT
		{
			public short Left;
			public short Top;
			public short Right;
			public short Bottom;
		}
	}
}
