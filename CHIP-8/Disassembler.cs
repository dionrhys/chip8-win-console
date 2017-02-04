using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CHIP_8
{
	public static class Disassembler
	{
		public static string Decode(Instruction instruction)
		{
			switch ((instruction.Value & 0xF000) >> 12)
			{
				case 0x0: return Decode_0(instruction);
				case 0x1: return Decode_1(instruction);
				case 0x2: return Decode_2(instruction);
				case 0x3: return Decode_3(instruction);
				case 0x4: return Decode_4(instruction);
				case 0x5: return Decode_5(instruction);
				case 0x6: return Decode_6(instruction);
				case 0x7: return Decode_7(instruction);
				case 0x8: return Decode_8(instruction);
				case 0x9: return Decode_9(instruction);
				case 0xA: return Decode_A(instruction);
				case 0xB: return Decode_B(instruction);
				case 0xC: return Decode_C(instruction);
				case 0xD: return Decode_D(instruction);
				case 0xE: return Decode_E(instruction);
				case 0xF: return Decode_F(instruction);
				// A mathematically-impossible default case to pacify the compiler
				 default: throw new Exception($"Invalid instruction 0x{instruction.Value:X4}");
			}
		}

		private static string Decode_0(Instruction instruction)
		{
			switch (instruction.Value)
			{
				case 0x00E0: return $"CLS";
				case 0x00EE: return $"RET";
				    default: return $"SYS 0x{instruction.NNN:X3}";
			}
		}

		private static string Decode_1(Instruction instruction)
		{
			return $"JMP 0x{instruction.NNN:X3}";
		}

		private static string Decode_2(Instruction instruction)
		{
			return $"CALL 0x{instruction.NNN:X3}";
		}

		private static string Decode_3(Instruction instruction)
		{
			return $"SE V{instruction.X:X}, 0x{instruction.KK:X2}";
		}

		private static string Decode_4(Instruction instruction)
		{
			return $"SNE V{instruction.X:X}, 0x{instruction.KK:X2}";
		}

		private static string Decode_5(Instruction instruction)
		{
			if (instruction.K != 0)
				throw new Exception($"Invalid instruction 0x{instruction.Value:X4}");
			return $"SE V{instruction.X:X}, V{instruction.Y:X}";
		}

		private static string Decode_6(Instruction instruction)
		{
			return $"MOV V{instruction.X:X}, 0x{instruction.KK:X2}";
		}

		private static string Decode_7(Instruction instruction)
		{
			return $"ADD V{instruction.X:X}, 0x{instruction.KK:X2}";
		}

		private static string Decode_8(Instruction instruction)
		{
			switch (instruction.K)
			{
				case 0x0: return $"MOV V{instruction.X:X}, V{instruction.Y:X}";
				case 0x1: return $"OR V{instruction.X:X}, V{instruction.Y:X}";
				case 0x2: return $"AND V{instruction.X:X}, V{instruction.Y:X}";
				case 0x3: return $"XOR V{instruction.X:X}, V{instruction.Y:X}";
				case 0x4: return $"ADD V{instruction.X:X}, V{instruction.Y:X}";
				case 0x5: return $"SUB V{instruction.X:X}, V{instruction.Y:X}";
				case 0x6: return $"SHR V{instruction.X:X}, V{instruction.Y:X}";
				case 0x7: return $"SUBR V{instruction.X:X}, V{instruction.Y:X}";
				case 0xE: return $"SHL V{instruction.X:X}, V{instruction.Y:X}";
				 default: throw new Exception($"Invalid instruction 0x{instruction.Value:X4}");
			}
		}

		private static string Decode_9(Instruction instruction)
		{
			if (instruction.K != 0)
				throw new Exception($"Invalid instruction 0x{instruction.Value:X4}");
			return $"SNE V{instruction.X:X}, V{instruction.Y:X}";
		}

		private static string Decode_A(Instruction instruction)
		{
			return $"MOV I, 0x{instruction.NNN:X3}";
		}

		private static string Decode_B(Instruction instruction)
		{
			return $"JV0 0x{instruction.NNN:X3}";
		}

		private static string Decode_C(Instruction instruction)
		{
			return $"RND V{instruction.X:X}, 0x{instruction.KK:X2}";
		}

		private static string Decode_D(Instruction instruction)
		{
			return $"DRAW V{instruction.X:X}, V{instruction.Y:X}, 0x{instruction.K:X}";
		}

		private static string Decode_E(Instruction instruction)
		{
			switch (instruction.KK)
			{
				case 0x9E: return $"SK V{instruction.X:X}";
				case 0xA1: return $"SNK V{instruction.X:X}";
				  default: throw new Exception($"Invalid instruction 0x{instruction.Value:X4}");
			}
		}

		private static string Decode_F(Instruction instruction)
		{
			switch (instruction.KK)
			{
				case 0x07: return $"MOV V{instruction.X:X}, T";
				case 0x0A: return $"MOV V{instruction.X:X}, K";
				case 0x15: return $"MOV T, V{instruction.X:X}";
				case 0x18: return $"MOV S, V{instruction.X:X}";
				case 0x1E: return $"ADD I, V{instruction.X:X}";
				case 0x29: return $"MOV I, F{instruction.X:X}";
				case 0x33: return $"BCD V{instruction.X:X}";
				case 0x55: return $"STM 0x{instruction.X:X}";
				case 0x65: return $"LDM 0x{instruction.X:X}";
				default: throw new Exception($"Invalid instruction 0x{instruction.Value:X4}");
			}
		}
	}
}
