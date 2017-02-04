using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CHIP_8
{
	public class Chip8
	{
		private const int ScreenWidth = 64;
		private const int ScreenHeight = 32;
		private const bool RequireInstructionsToBeTwoByteAligned = false;

		private readonly byte[] Memory = new byte[4096];
		private readonly byte[] V = new byte[16];
		private ushort I;
		private readonly ushort[] Stack = new ushort[16];
		private byte DelayTimer;
		private byte SoundTimer;
		private ushort ProgramCounter;
		private byte StackPointer;
		private long LastFrameTicks;
		private long LastFrequencyHitTicks;

		private Display Display = new Display();
		private Keyboard Keyboard = new Keyboard();
		private Sound Sound = new Sound();

		private readonly long InstructionIntervalTicks = Stopwatch.Frequency / 1200;
		private readonly long TimerIntervalTicks = Stopwatch.Frequency / 60;

		private readonly Random Random = new Random();

		public void Load(string filePath)
		{
			if (string.IsNullOrWhiteSpace(filePath))
				throw new ArgumentException("A valid file path must be given.");
			if (!File.Exists(filePath))
				throw new ArgumentException("File doesn't exist.");

			Buffer.BlockCopy(InternalFontData, 0, Memory, InternalFontDataAddress, InternalFontData.Length);

			byte[] rom = File.ReadAllBytes(filePath);
			if (rom.Length > 3584)
				throw new InvalidOperationException("File too large.");

			Buffer.BlockCopy(rom, 0, Memory, 0x200, rom.Length);
			ProgramCounter = 0x200;
		}

		public void Run()
		{
			WinApi.timeBeginPeriod(1);
			Display.Initialize(ScreenWidth, ScreenHeight);
			Keyboard.Initialize();
			Sound.Initialize();
			LastFrameTicks = LastFrequencyHitTicks = Stopwatch.GetTimestamp();

			while (true)
			{
				// Throttle execution speed
				long currentTimeTicks = Stopwatch.GetTimestamp();
				while (currentTimeTicks - LastFrameTicks < InstructionIntervalTicks)
				{
					System.Threading.Thread.Sleep(1);
					currentTimeTicks = Stopwatch.GetTimestamp();
				}
				LastFrameTicks = currentTimeTicks;

				// Tick down delay and sound timers
				while (currentTimeTicks - LastFrequencyHitTicks >= TimerIntervalTicks)
				{
					if (DelayTimer > 0)
						DelayTimer--;
					if (SoundTimer > 0)
					{
						SoundTimer--;
						if (SoundTimer == 0)
							Sound.StopPlaying();
					}
					LastFrequencyHitTicks += TimerIntervalTicks;
				}

				Keyboard.ProcessInputEvents();

				if (ProgramCounter > 4094)
					throw new Exception("ProgramCounter outside Memory range");
				if (RequireInstructionsToBeTwoByteAligned && ProgramCounter % 2 != 0)
					throw new Exception("ProgramCounter on odd address");

				ushort instruction = (ushort)((Memory[ProgramCounter] << 8) | Memory[ProgramCounter + 1]);
				//Debug.WriteLine("0x{0:X4}: {1:X4}  {2}", ProgramCounter, instruction, Disassembler.Decode(instruction));
				ProgramCounter += 2;
				ExecuteInstruction(instruction);
			}

			// TODO: Allow escaping from execution mode and do timeEndPeriod (and other "deinit" (display/keyboard) afterwards)
		}

		private void ExecuteInstruction(ushort instruction)
		{
			if (instruction == 0x00E0)
			{
				Display.Clear();
			}
			else if (instruction == 0x00EE)
			{
				if (StackPointer == 0)
					throw new Exception("Cannot return from subroutine - Stack Pointer is at 0");
				StackPointer--;
				ProgramCounter = Stack[StackPointer];
			}
			//else if ((instruction & 0xF000) == 0x0000)
			//{
			//	throw new Exception("0nnn is unsupported");
			//}
			else if ((instruction & 0xF000) == 0x1000)
			{
				ushort address = (ushort)(instruction & 0xFFF);
				if (address > 4094)
					throw new Exception("Cannot jump - address outside Memory range");
				if (RequireInstructionsToBeTwoByteAligned && address % 2 != 0)
					throw new Exception("Cannot jump - odd address");
				ProgramCounter = address;
			}
			else if ((instruction & 0xF000) == 0x2000)
			{
				if (StackPointer >= 16)
					throw new Exception("Cannot call subroutine - stack overflow");
				ushort address = (ushort)(instruction & 0xFFF);
				if (address > 4094)
					throw new Exception("Cannot call subroutine - address outside Memory range");
				if (RequireInstructionsToBeTwoByteAligned && address % 2 != 0)
					throw new Exception("Cannot call subroutine - odd address");
				Stack[StackPointer] = ProgramCounter;
				StackPointer++;
				ProgramCounter = address;
			}
			else if ((instruction & 0xF000) == 0x3000)
			{
				if (V[(instruction & 0x0F00) >> 8] == (instruction & 0xFF))
				{
					if ((ProgramCounter + 2) > 4094)
						throw new Exception("Cannot skip instruction - address outside Memory range");
					ProgramCounter += 2;
				}
			}
			else if ((instruction & 0xF000) == 0x4000)
			{
				if (V[(instruction & 0x0F00) >> 8] != (instruction & 0xFF))
				{
					if ((ProgramCounter + 2) > 4094)
						throw new Exception("Cannot skip instruction - address outside Memory range");
					ProgramCounter += 2;
				}
			}
			else if ((instruction & 0xF00F) == 0x5000)
			{
				if (V[(instruction & 0x0F00) >> 8] == V[(instruction & 0x00F0) >> 4])
				{
					if ((ProgramCounter + 2) > 4094)
						throw new Exception("Cannot skip instruction - address outside Memory range");
					ProgramCounter += 2;
				}
			}
			else if ((instruction & 0xF000) == 0x6000)
			{
				V[(instruction & 0x0F00) >> 8] = (byte)(instruction & 0xFF);
			}
			else if ((instruction & 0xF000) == 0x7000)
			{
				V[(instruction & 0x0F00) >> 8] += (byte)(instruction & 0xFF);
			}
			else if ((instruction & 0xF00F) == 0x8000)
			{
				V[(instruction & 0x0F00) >> 8] = V[(instruction & 0x00F0) >> 4];
			}
			else if ((instruction & 0xF00F) == 0x8001)
			{
				V[(instruction & 0x0F00) >> 8] |= V[(instruction & 0x00F0) >> 4];
			}
			else if ((instruction & 0xF00F) == 0x8002)
			{
				V[(instruction & 0x0F00) >> 8] &= V[(instruction & 0x00F0) >> 4];
			}
			else if ((instruction & 0xF00F) == 0x8003)
			{
				V[(instruction & 0x0F00) >> 8] ^= V[(instruction & 0x00F0) >> 4];
			}
			else if ((instruction & 0xF00F) == 0x8004)
			{
				if ((instruction & 0x0F00) >> 8 == 15)
					throw new Exception("Target can't be VF?");
				int sum = V[(instruction & 0x0F00) >> 8] + V[(instruction & 0x00F0) >> 4];
				V[(instruction & 0x0F00) >> 8] = (byte)(sum & 0xFF);
				V[15] = (byte)(sum >> 8);
			}
			else if ((instruction & 0xF00F) == 0x8005)
			{
				if ((instruction & 0x0F00) >> 8 == 15)
					throw new Exception("Target can't be VF?");
				bool borrow = V[(instruction & 0x0F00) >> 8] < V[(instruction & 0x00F0) >> 4];
				V[(instruction & 0x0F00) >> 8] -= V[(instruction & 0x00F0) >> 4];
				V[15] = borrow ? (byte)0 : (byte)1;
			}
			else if ((instruction & 0xF00F) == 0x8006)
			{
				// The original COSMAC-VIP behaviour of right-shifting the VY (!!) register by 1 bit then storing the result in VX is implemented here!
				// Reference: http://laurencescotford.co.uk/?p=266 and http://mattmik.com/files/chip8/mastering/chip8.html
				if ((instruction & 0x0F00) >> 8 == 15)
					throw new Exception("Target can't be VF?");
				byte shiftout = (byte)(V[(instruction & 0x00F0) >> 4] & 0x1);
				V[(instruction & 0x0F00) >> 8] = (byte)(V[(instruction & 0x00F0) >> 4] >> 1);
				V[15] = shiftout;
			}
			else if ((instruction & 0xF00F) == 0x8007)
			{
				if ((instruction & 0x0F00) >> 8 == 15)
					throw new Exception("Target can't be VF?");
				bool borrow = V[(instruction & 0x00F0) >> 4] < V[(instruction & 0x0F00) >> 8];
				V[(instruction & 0x0F00) >> 8] = (byte)(V[(instruction & 0x00F0) >> 4] - V[(instruction & 0x0F00) >> 8]);
				V[15] = borrow ? (byte)0 : (byte)1;
			}
			else if ((instruction & 0xF00F) == 0x800E)
			{
				// The original COSMAC-VIP behaviour of left-shifting the VY (!!) register by 1 bit then storing the result in VX is implemented here!
				// Reference: http://laurencescotford.co.uk/?p=266 and http://mattmik.com/files/chip8/mastering/chip8.html
				if ((instruction & 0x0F00) >> 8 == 15)
					throw new Exception("Target can't be VF?");
				byte shiftout = (byte)((V[(instruction & 0x00F0) >> 4] & 0x80) >> 7);
				V[(instruction & 0x0F00) >> 8] = (byte)(V[(instruction & 0x00F0) >> 4] << 1);
				V[15] = shiftout;
			}
			else if ((instruction & 0xF00F) == 0x9000)
			{
				if (V[(instruction & 0x0F00) >> 8] != V[(instruction & 0x00F0) >> 4])
				{
					if ((ProgramCounter + 2) > 4094)
						throw new Exception("Cannot skip instruction - address outside Memory range");
					ProgramCounter += 2;
				}
			}
			else if ((instruction & 0xF000) == 0xA000)
			{
				I = (ushort)(instruction & 0xFFF);
			}
			else if ((instruction & 0xF000) == 0xB000)
			{
				ushort address = (ushort)(instruction & 0xFFF);
				if (address + V[0] > 4094)
					throw new Exception("Cannot jump - address outside Memory range");
				if (RequireInstructionsToBeTwoByteAligned && (address + V[0]) % 2 != 0)
					throw new Exception("Cannot jump - odd address");
				ProgramCounter = (ushort)(address + V[0]);
			}
			else if ((instruction & 0xF000) == 0xC000)
			{
				V[(instruction & 0x0F00) >> 8] = (byte)(Random.Next(0xFF) & (instruction & 0xFF));
			}
			else if ((instruction & 0xF000) == 0xD000)
			{
				int numLines = instruction & 0x000F;
				if (I + numLines > 4096)
					throw new Exception("Cannot draw - sprite outside Memory range");
				int left = V[(instruction & 0x0F00) >> 8];
				int top = V[(instruction & 0x00F0) >> 4];

				byte collided = 0;
				for (int line = 0; line < numLines; line++)
				{
					int y = (top + line) % ScreenHeight; // TODO: Magic number
					byte spriteByte = Memory[I + line];
					for (int col = 0; col < 8; col++)
					{
						int x = (left + col) % ScreenWidth; // TODO: Magic number
						bool spriteBit = ((spriteByte >> (7 - col)) & 0x1) != 0;
						if (Display.DrawPixel(x, y, spriteBit))
							collided = 1;
					}
				}
				V[15] = collided;
			}
			else if ((instruction & 0xF0FF) == 0xE09E)
			{
				byte key = V[(instruction & 0x0F00) >> 8];
				if (key > 15)
					throw new Exception("Cannot check state of key - value out of range");

				if (Keyboard.IsKeyPressed(key))
				{
					if ((ProgramCounter + 2) > 4094)
						throw new Exception("Cannot skip instruction - address outside Memory range");
					ProgramCounter += 2;
				}
			}
			else if ((instruction & 0xF0FF) == 0xE0A1)
			{
				byte key = V[(instruction & 0x0F00) >> 8];
				if (key > 15)
					throw new Exception("Cannot check state of key - value out of range");

				if (!Keyboard.IsKeyPressed(key))
				{
					if ((ProgramCounter + 2) > 4094)
						throw new Exception("Cannot skip instruction - address outside Memory range");
					ProgramCounter += 2;
				}
			}
			else if ((instruction & 0xF0FF) == 0xF007)
			{
				V[(instruction & 0x0F00) >> 8] = DelayTimer;
			}
			else if ((instruction & 0xF0FF) == 0xF00A)
			{
				V[(instruction & 0x0F00) >> 8] = Keyboard.WaitForKey();
			}
			else if ((instruction & 0xF0FF) == 0xF015)
			{
				DelayTimer = V[(instruction & 0x0F00) >> 8];
			}
			else if ((instruction & 0xF0FF) == 0xF018)
			{
				SoundTimer = V[(instruction & 0x0F00) >> 8];
				if (SoundTimer > 0)
					Sound.StartPlaying();
			}
			else if ((instruction & 0xF0FF) == 0xF01E)
			{
				// TODO: Undocumented feature to set VF to 1 if I will become > 0xFFF, and 0 if not?
				I += V[(instruction & 0x0F00) >> 8];
			}
			else if ((instruction & 0xF0FF) == 0xF029)
			{
				byte character = V[(instruction & 0x0F00) >> 8];
				if (character > 15)
					throw new Exception("Cannot set I to sprite address - value out of range");
				int address = InternalFontDataAddress + (character * 5); // TODO: Magic number
				if (address + 5 > 4096)
					throw new Exception("Cannot set I to sprite address - outside Memory range");
				I = (ushort)address;
			}
			else if ((instruction & 0xF0FF) == 0xF033)
			{
				if (I + 3 > 4096)
					throw new Exception("Cannot store binary-coded decimal in memory - outside Memory range");
				byte value = V[(instruction & 0x0F00) >> 8];
				Memory[I] = (byte)(value / 100);
				Memory[I + 1] = (byte)((value / 10) % 10);
				Memory[I + 2] = (byte)(value % 10);
			}
			else if ((instruction & 0xF0FF) == 0xF055)
			{
				int numBytes = ((instruction & 0x0F00) >> 8) + 1;
				if (I + numBytes > 4096)
					throw new Exception("Cannot copy registers to memory - addresses outside Memory range");
				Buffer.BlockCopy(V, 0, Memory, I, numBytes);
			}
			else if ((instruction & 0xF0FF) == 0xF065)
			{
				int numBytes = ((instruction & 0x0F00) >> 8) + 1;
				if (I + numBytes > 4096)
					throw new Exception("Cannot copy memory to registers - addresses outside Memory range");
				Buffer.BlockCopy(Memory, I, V, 0, numBytes);
			}
			else
			{
				throw new Exception($"Invalid instruction 0x{instruction:X4}");
			}
		}

		private short InternalFontDataAddress = 0x0000;
		private byte[] InternalFontData = new byte[80]
		{
			0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
			0x20, 0x60, 0x20, 0x20, 0x70, // 1
			0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
			0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
			0x90, 0x90, 0xF0, 0x10, 0x10, // 4
			0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
			0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
			0xF0, 0x10, 0x20, 0x40, 0x40, // 7
			0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
			0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
			0xF0, 0x90, 0xF0, 0x90, 0x90, // A
			0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
			0xF0, 0x80, 0x80, 0x80, 0xF0, // C
			0xE0, 0x90, 0x90, 0x90, 0xE0, // D
			0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
			0xF0, 0x80, 0xF0, 0x80, 0x80, // F
		};
	}
}
