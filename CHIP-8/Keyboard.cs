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
	public class Keyboard
	{
		private readonly bool[] KeyPressStates = new bool[16]; // TODO: (Re-)Initialization?

		private SafeFileHandle ConsoleInputHandle;

		public void Initialize()
		{
			ConsoleInputHandle = WinApi.CreateFile(
				"CONIN$",
				WinApi.GENERIC_WRITE | WinApi.GENERIC_READ,
				WinApi.FILE_SHARE_READ,
				IntPtr.Zero,
				WinApi.OPEN_EXISTING,
				0,
				IntPtr.Zero);
			if (ConsoleInputHandle.IsInvalid)
				throw new Exception("Invalid CONIN$ handle");
		}

		/// <summary>
		/// Process console input events to record which CHIP-8 keys are held down or not.
		/// </summary>
		public void ProcessInputEvents()
		{
			bool success;
			var inputRecords = new WinApi.INPUT_RECORD[1];
			while (true)
			{
				uint numEvents;
				success = WinApi.GetNumberOfConsoleInputEvents(ConsoleInputHandle, out numEvents);
				if (!success)
					throw new Exception($"Cannot process key input events - GetNumberOfConsoleInputEvents failed: {new Win32Exception(Marshal.GetLastWin32Error()).Message}");
				if (numEvents == 0)
					break;

				uint numEventsRead;
				success = WinApi.ReadConsoleInput(ConsoleInputHandle, inputRecords, 1, out numEventsRead);
				if (!success || numEventsRead != 1)
					throw new Exception($"Cannot process key input events - ReadConsoleInput failed: {new Win32Exception(Marshal.GetLastWin32Error()).Message}");

				if (inputRecords[0].EventType == WinApi.KEY_EVENT)
				{
					var keyEvent = inputRecords[0].KeyEvent;
					// Only care about events for the 16 Chip-8 (COSMAC-VIP) Keys, ignore everything else
					if (WindowsVirtualScanCodeIsChip8Key(keyEvent.wVirtualScanCode))
					{
						var chip8Key = WindowsVirtualScanCodeToChip8Key(keyEvent.wVirtualScanCode);
						KeyPressStates[chip8Key] = keyEvent.bKeyDown;
					}
				}
			}
		}

		public bool IsKeyPressed(byte key)
		{
			if (key > 15)
				throw new Exception("Cannot get key state - value out of range");

			return KeyPressStates[key];
		}

		public byte WaitForKey()
		{
			bool success;
			success = WinApi.FlushConsoleInputBuffer(ConsoleInputHandle);
			if (!success)
				throw new Exception($"Cannot wait on key - FlushConsoleInputBuffer failed: {new Win32Exception(Marshal.GetLastWin32Error()).Message}");

			var inputRecords = new WinApi.INPUT_RECORD[1];
			while (true)
			{
				uint numEventsRead;
				success = WinApi.ReadConsoleInput(ConsoleInputHandle, inputRecords, 1, out numEventsRead);
				if (!success || numEventsRead != 1)
					throw new Exception($"Cannot wait on key - ReadConsoleInput failed: {new Win32Exception(Marshal.GetLastWin32Error()).Message}");

				if (inputRecords[0].EventType == WinApi.KEY_EVENT)
				{
					var keyEvent = inputRecords[0].KeyEvent;
					// Only care about events for the 16 Chip-8 (COSMAC-VIP) Keys, ignore everything else
					// Also filter out keys that are repeated (already being held down), we want first hits only
					// TODO: The filtering out repeated of already-pressed keys isn't working, Windows just sends key down events
					// with a repeat count of 1 while holding down a key :(
					if (keyEvent.bKeyDown && keyEvent.wRepeatCount == 1 && WindowsVirtualScanCodeIsChip8Key(keyEvent.wVirtualScanCode))
					{
						return WindowsVirtualScanCodeToChip8Key(keyEvent.wVirtualScanCode);
					}
				}
			}
		}

		/// <summary>
		/// Checks if the given virtual keyboard scan code corresponds to a CHIP-8 key.
		/// These scan codes are based on the physical key positions, not any localized keyboard layout.
		/// https://web.archive.org/web/20130218144111/http://altdevblogaday.com/2011/10/02/i-never-managed-to-go-left-on-first-try/
		/// </summary>
		/// <param name="vScanCode">Virtual Scan Code of the key to check.</param>
		/// <returns>True if the virtual scan code maps to a CHIP-8 key; false otherwise.</returns>
		private bool WindowsVirtualScanCodeIsChip8Key(int vScanCode)
		{
			switch (vScanCode)
			{
				case 0x2D: return true; // X
				case 0x02: return true; // 1
				case 0x03: return true; // 2
				case 0x04: return true; // 3
				case 0x10: return true; // Q
				case 0x11: return true; // W
				case 0x12: return true; // E
				case 0x1E: return true; // A
				case 0x1F: return true; // S
				case 0x20: return true; // D
				case 0x2C: return true; // Z
				case 0x2E: return true; // C
				case 0x05: return true; // 4
				case 0x13: return true; // R
				case 0x21: return true; // F
				case 0x2F: return true; // V
				default: return false;
			}
		}

		/// <summary>
		/// Maps virtual keyboard scan codes to a CHIP-8 key if possible.
		/// These scan codes are based on the physical key positions, not any localized keyboard layout.
		/// https://web.archive.org/web/20130218144111/http://altdevblogaday.com/2011/10/02/i-never-managed-to-go-left-on-first-try/
		/// </summary>
		/// <param name="vScanCode">Virtual Scan Code of the key to map.</param>
		/// <returns>CHIP-8 key between 0x0 and 0xF if successfully mapped; throws an exception otherwise.</returns>
		private byte WindowsVirtualScanCodeToChip8Key(int vScanCode)
		{
			switch (vScanCode)
			{
				case 0x2D: return 0x0; // X
				case 0x02: return 0x1; // 1
				case 0x03: return 0x2; // 2
				case 0x04: return 0x3; // 3
				case 0x10: return 0x4; // Q
				case 0x11: return 0x5; // W
				case 0x12: return 0x6; // E
				case 0x1E: return 0x7; // A
				case 0x1F: return 0x8; // S
				case 0x20: return 0x9; // D
				case 0x2C: return 0xA; // Z
				case 0x2E: return 0xB; // C
				case 0x05: return 0xC; // 4
				case 0x13: return 0xD; // R
				case 0x21: return 0xE; // F
				case 0x2F: return 0xF; // V
				default: throw new Exception("Cannot convert COSMAC-VIP key to Windows Virtual-Key - value out of range");
			}
		}
	}
}
