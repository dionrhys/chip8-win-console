using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CHIP_8
{
	public class Display
	{
		private bool[,] FrameBuffer;
		private SafeFileHandle ConsoleOutputHandle;

		public void Initialize(int width, int height)
		{
			Console.SetWindowSize(width, height);
			Console.SetBufferSize(width, height);
			Console.BackgroundColor = ConsoleColor.Black;
			Console.ForegroundColor = ConsoleColor.White;
			Console.CursorVisible = false;
			Console.Clear();

			FrameBuffer = new bool[height, width];

			ConsoleOutputHandle = WinApi.CreateFile(
				"CONOUT$",
				WinApi.GENERIC_WRITE,
				WinApi.FILE_SHARE_WRITE,
				IntPtr.Zero,
				WinApi.OPEN_EXISTING,
				0,
				IntPtr.Zero);
			if (ConsoleOutputHandle.IsInvalid)
				throw new Exception($"Invalid CONOUT$ handle: {new Win32Exception(Marshal.GetLastWin32Error()).Message}");
		}

		public void Clear()
		{
			Array.Clear(FrameBuffer, 0, FrameBuffer.Length);
			Console.Clear();
		}

		public bool DrawPixel(int x, int y, bool spriteBit)
		{
			if (y >= FrameBuffer.GetLength(0) || x >= FrameBuffer.GetLength(1))
				throw new ArgumentException("Cannot draw - pixel outside display dimensions");

			bool displayBit = FrameBuffer[y, x];
			FrameBuffer[y, x] ^= spriteBit;

			var sizeCoord = new WinApi.COORD(1, 1);
			var offsetCoord = new WinApi.COORD(0, 0);
			ushort charAttribs = FrameBuffer[y, x] ? (ushort)0xF0 /* White Background */ : (ushort)0x00 /* Black Background */;
			var charInfo = new WinApi.CHAR_INFO[1]
			{
				new WinApi.CHAR_INFO { Char = new WinApi.CharUnion { UnicodeChar = ' ' }, Attributes = charAttribs }
			};
			var rect = new WinApi.SMALL_RECT { Left = (short)x, Top = (short)y, Right = (short)x, Bottom = (short)y };
			bool success = WinApi.WriteConsoleOutput(ConsoleOutputHandle, charInfo, sizeCoord, offsetCoord, ref rect);
			if (!success)
				throw new Exception($"Cannot draw - WriteConsoleOutput failed: {new Win32Exception(Marshal.GetLastWin32Error()).Message}");

			return spriteBit && displayBit;
		}
	}
}
