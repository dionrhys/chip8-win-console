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

				var instruction = new Instruction((ushort)((Memory[ProgramCounter] << 8) | Memory[ProgramCounter + 1]));
				//Debug.WriteLine("0x{0:X4}: {1:X4}  {2}", ProgramCounter, instruction, Disassembler.Decode(instruction));
				ProgramCounter += 2;
				ExecuteInstruction(instruction);
			}

			// TODO: Allow escaping from execution mode and do timeEndPeriod (and other "deinit" (display/keyboard) afterwards)
		}

		private void ExecuteInstruction(Instruction instruction)
		{
			// Switch on the first nibble to avoid lots of conditional branching (encourage a jump table)
			// (Hopefully this may also result in tail call optimization for all calls, but I haven't checked that out)
			switch ((instruction.Value & 0xF000) >> 12)
			{
				case 0x0: Exec_0_Syscall(instruction); return;
				case 0x1: Exec_1_JumpAddress(instruction); return;
				case 0x2: Exec_2_CallAddress(instruction); return;
				case 0x3: Exec_3_SkipIfXEqualConstant(instruction); return;
				case 0x4: Exec_4_SkipIfXNotEqualConstant(instruction); return;
				case 0x5: Exec_5_SkipIfXEqualY(instruction); return;
				case 0x6: Exec_6_LoadXWithConstant(instruction); return;
				case 0x7: Exec_7_AddXWithConstant(instruction); return;
				case 0x8: Exec_8_ArithmeticXY(instruction); return;
				case 0x9: Exec_9_SkipIfXNotEqualY(instruction); return;
				case 0xA: Exec_A_LoadIWithAddress(instruction); return;
				case 0xB: Exec_B_JumpAddressPlusV0(instruction); return;
				case 0xC: Exec_C_RandomWithConstantMask(instruction); return;
				case 0xD: Exec_D_DrawSprite(instruction); return;
				case 0xE: Exec_E_SkipDependingOnKeyState(instruction); return;
				case 0xF: Exec_F_MiscOperations(instruction); return;
			}
		}

		private void Exec_0_Syscall(Instruction instruction)
		{
			if (instruction.Value == 0x00E0)
			{
				Display.Clear();
			}
			else if (instruction.Value == 0x00EE)
			{
				if (StackPointer == 0)
					throw new Exception("Cannot return from subroutine - Stack Pointer is at 0");
				StackPointer--;
				ProgramCounter = Stack[StackPointer];
			}
			else
			{
				throw new Exception($"Invalid instruction 0x{instruction.Value:X4}");
			}
		}

		private void Exec_1_JumpAddress(Instruction instruction)
		{
			ushort address = instruction.NNN;
			if (address > 4094)
				throw new Exception("Cannot jump - address outside Memory range");
			if (RequireInstructionsToBeTwoByteAligned && address % 2 != 0)
				throw new Exception("Cannot jump - odd address");
			ProgramCounter = address;
		}

		private void Exec_2_CallAddress(Instruction instruction)
		{
			if (StackPointer >= 16)
				throw new Exception("Cannot call subroutine - stack overflow");
			ushort address = instruction.NNN;
			if (address > 4094)
				throw new Exception("Cannot call subroutine - address outside Memory range");
			if (RequireInstructionsToBeTwoByteAligned && address % 2 != 0)
				throw new Exception("Cannot call subroutine - odd address");
			Stack[StackPointer] = ProgramCounter;
			StackPointer++;
			ProgramCounter = address;
		}

		private void Exec_3_SkipIfXEqualConstant(Instruction instruction)
		{
			if (V[instruction.X] == instruction.KK)
			{
				if ((ProgramCounter + 2) > 4094)
					throw new Exception("Cannot skip instruction - address outside Memory range");
				ProgramCounter += 2;
			}
		}

		private void Exec_4_SkipIfXNotEqualConstant(Instruction instruction)
		{
			if (V[instruction.X] != instruction.KK)
			{
				if ((ProgramCounter + 2) > 4094)
					throw new Exception("Cannot skip instruction - address outside Memory range");
				ProgramCounter += 2;
			}
		}

		private void Exec_5_SkipIfXEqualY(Instruction instruction)
		{
			if (instruction.K != 0)
				throw new Exception($"Invalid instruction 0x{instruction.Value:X4}");
			if (V[instruction.X] == V[instruction.Y])
			{
				if ((ProgramCounter + 2) > 4094)
					throw new Exception("Cannot skip instruction - address outside Memory range");
				ProgramCounter += 2;
			}
		}

		private void Exec_6_LoadXWithConstant(Instruction instruction)
		{
			V[instruction.X] = (byte)instruction.KK;
		}

		private void Exec_7_AddXWithConstant(Instruction instruction)
		{
			V[instruction.X] += (byte)instruction.KK;
		}

		private void Exec_8_ArithmeticXY(Instruction instruction)
		{
			switch (instruction.K)
			{
				case 0x0:
				{
					V[instruction.X] = V[instruction.Y];
					break;
				}
				case 0x1:
				{
					V[instruction.X] |= V[instruction.Y];
					break;
				}
				case 0x2:
				{
					V[instruction.X] &= V[instruction.Y];
					break;
				}
				case 0x3:
				{
					V[instruction.X] ^= V[instruction.Y];
					break;
				}
				case 0x4:
				{
					if (instruction.X == 15)
						throw new Exception("Target can't be VF?");
					int sum = V[instruction.X] + V[instruction.Y];
					V[instruction.X] = (byte)(sum & 0xFF);
					V[15] = (byte)(sum >> 8);
					break;
				}
				case 0x5:
				{
					if (instruction.X == 15)
						throw new Exception("Target can't be VF?");
					bool borrow = V[instruction.X] < V[instruction.Y];
					V[instruction.X] -= V[instruction.Y];
					V[15] = borrow ? (byte)0 : (byte)1;
					break;
				}
				case 0x6:
				{
					// The original COSMAC-VIP behaviour of right-shifting the VY (!!) register by 1 bit then storing the result in VX is implemented here!
					// Reference: http://laurencescotford.co.uk/?p=266 and http://mattmik.com/files/chip8/mastering/chip8.html
					if (instruction.X == 15)
						throw new Exception("Target can't be VF?");
					byte shiftout = (byte)(V[instruction.Y] & 0x1);
					V[instruction.X] = (byte)(V[instruction.Y] >> 1);
					V[15] = shiftout;
					break;
				}
				case 0x7:
				{
					if (instruction.X == 15)
						throw new Exception("Target can't be VF?");
					bool borrow = V[instruction.Y] < V[instruction.X];
					V[instruction.X] = (byte)(V[instruction.Y] - V[instruction.X]);
					V[15] = borrow ? (byte)0 : (byte)1;
					break;
				}
				case 0xE:
				{
					// The original COSMAC-VIP behaviour of left-shifting the VY (!!) register by 1 bit then storing the result in VX is implemented here!
					// Reference: http://laurencescotford.co.uk/?p=266 and http://mattmik.com/files/chip8/mastering/chip8.html
					if (instruction.X == 15)
						throw new Exception("Target can't be VF?");
					byte shiftout = (byte)((V[instruction.Y] & 0x80) >> 7);
					V[instruction.X] = (byte)(V[instruction.Y] << 1);
					V[15] = shiftout;
					break;
				}
				default:
					throw new Exception($"Invalid instruction 0x{instruction.Value:X4}");
			}
		}

		private void Exec_9_SkipIfXNotEqualY(Instruction instruction)
		{
			if (instruction.K != 0)
				throw new Exception($"Invalid instruction 0x{instruction.Value:X4}");
			if (V[instruction.X] != V[instruction.Y])
			{
				if ((ProgramCounter + 2) > 4094)
					throw new Exception("Cannot skip instruction - address outside Memory range");
				ProgramCounter += 2;
			}
		}

		private void Exec_A_LoadIWithAddress(Instruction instruction)
		{
			I = instruction.NNN;
		}

		private void Exec_B_JumpAddressPlusV0(Instruction instruction)
		{
			ushort address = instruction.NNN;
			if (address + V[0] > 4094)
				throw new Exception("Cannot jump - address outside Memory range");
			if (RequireInstructionsToBeTwoByteAligned && (address + V[0]) % 2 != 0)
				throw new Exception("Cannot jump - odd address");
			ProgramCounter = (ushort)(address + V[0]);
		}

		private void Exec_C_RandomWithConstantMask(Instruction instruction)
		{
			V[instruction.X] = (byte)(Random.Next(0xFF) & instruction.KK);
		}

		private void Exec_D_DrawSprite(Instruction instruction)
		{
			int numLines = instruction.K;
			if (I + numLines > 4096)
				throw new Exception("Cannot draw - sprite outside Memory range");
			int left = V[instruction.X];
			int top = V[instruction.Y];

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

		private void Exec_E_SkipDependingOnKeyState(Instruction instruction)
		{
			if (instruction.KK == 0x9E)
			{
				byte key = V[instruction.X];
				if (key > 15)
					throw new Exception("Cannot check state of key - value out of range");

				if (Keyboard.IsKeyPressed(key))
				{
					if ((ProgramCounter + 2) > 4094)
						throw new Exception("Cannot skip instruction - address outside Memory range");
					ProgramCounter += 2;
				}
			}
			else if (instruction.KK == 0xA1)
			{
				byte key = V[instruction.X];
				if (key > 15)
					throw new Exception("Cannot check state of key - value out of range");

				if (!Keyboard.IsKeyPressed(key))
				{
					if ((ProgramCounter + 2) > 4094)
						throw new Exception("Cannot skip instruction - address outside Memory range");
					ProgramCounter += 2;
				}
			}
			else
			{
				throw new Exception($"Invalid instruction 0x{instruction.Value:X4}");
			}
		}

		private void Exec_F_MiscOperations(Instruction instruction)
		{
			switch (instruction.KK)
			{
				case 0x07:
				{
					V[instruction.X] = DelayTimer;
					break;
				}
				case 0x0A:
				{
					V[instruction.X] = Keyboard.WaitForKey();
					break;
				}
				case 0x15:
				{
					DelayTimer = V[instruction.X];
					break;
				}
				case 0x18:
				{
					SoundTimer = V[instruction.X];
					if (SoundTimer > 0)
						Sound.StartPlaying();
					break;
				}
				case 0x1E:
				{
					// TODO: Undocumented feature to set VF to 1 if I will become > 0xFFF, and 0 if not?
					I += V[instruction.X];
					break;
				}
				case 0x29:
				{
					byte character = V[instruction.X];
					if (character > 15)
						throw new Exception("Cannot set I to sprite address - value out of range");
					int address = InternalFontDataAddress + (character * 5); // TODO: Magic number
					if (address + 5 > 4096)
						throw new Exception("Cannot set I to sprite address - outside Memory range");
					I = (ushort)address;
					break;
				}
				case 0x33:
				{
					if (I + 3 > 4096)
						throw new Exception("Cannot store binary-coded decimal in memory - outside Memory range");
					byte value = V[instruction.X];
					Memory[I] = (byte)(value / 100);
					Memory[I + 1] = (byte)((value / 10) % 10);
					Memory[I + 2] = (byte)(value % 10);
					break;
				}
				case 0x55:
				{
					int numBytes = (instruction.X) + 1;
					if (I + numBytes > 4096)
						throw new Exception("Cannot copy registers to memory - addresses outside Memory range");
					Buffer.BlockCopy(V, 0, Memory, I, numBytes);
					break;
				}
				case 0x65:
				{
					int numBytes = (instruction.X) + 1;
					if (I + numBytes > 4096)
						throw new Exception("Cannot copy memory to registers - addresses outside Memory range");
					Buffer.BlockCopy(Memory, I, V, 0, numBytes);
					break;
				}
				default:
					throw new Exception($"Invalid instruction 0x{instruction.Value:X4}");
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
