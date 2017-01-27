using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CHIP_8
{
	public static class Disassembler
	{
		public static string Decode(ushort instruction)
		{
			if (instruction == 0x00E0)
			{
				return "CLS";
			}
			else if (instruction == 0x00EE)
			{
				return "RET";
			}
			else if ((instruction & 0xF000) == 0x0000)
			{
				return $"SYS 0x{instruction & 0xFFF:X3}";
			}
			else if ((instruction & 0xF000) == 0x1000)
			{
				return $"JP 0x{instruction & 0xFFF:X3}";
			}
			else if ((instruction & 0xF000) == 0x2000)
			{
				return $"CALL 0x{instruction & 0xFFF:X3}";
			}
			else if ((instruction & 0xF000) == 0x3000)
			{
				return $"SE V{(instruction & 0xF00) >> 8:X}, 0x{instruction & 0xFF:X2}";
			}
			else if ((instruction & 0xF000) == 0x4000)
			{
				return $"SNE V{(instruction & 0xF00) >> 8:X}, 0x{instruction & 0xFF:X2}";
			}
			else if ((instruction & 0xF00F) == 0x5000)
			{
				return $"SE V{(instruction & 0xF00) >> 8:X}, V{(instruction & 0xF0) >> 4:X}";
			}
			else if ((instruction & 0xF000) == 0x6000)
			{
				return $"LD V{(instruction & 0xF00) >> 8:X}, 0x{instruction & 0xFF:X2}";
			}
			else if ((instruction & 0xF000) == 0x7000)
			{
				return $"ADD V{(instruction & 0xF00) >> 8:X}, 0x{instruction & 0xFF:X2}";
			}
			else if ((instruction & 0xF00F) == 0x8000)
			{
				return $"LD V{(instruction & 0xF00) >> 8:X}, V{(instruction & 0xF0) >> 4:X}";
			}
			else if ((instruction & 0xF00F) == 0x8001)
			{
				return $"OR V{(instruction & 0xF00) >> 8:X}, V{(instruction & 0xF0) >> 4:X}";
			}
			else if ((instruction & 0xF00F) == 0x8002)
			{
				return $"AND V{(instruction & 0xF00) >> 8:X}, V{(instruction & 0xF0) >> 4:X}";
			}
			else if ((instruction & 0xF00F) == 0x8003)
			{
				return $"XOR V{(instruction & 0xF00) >> 8:X}, V{(instruction & 0xF0) >> 4:X}";
			}
			else if ((instruction & 0xF00F) == 0x8004)
			{
				return $"ADD V{(instruction & 0xF00) >> 8:X}, V{(instruction & 0xF0) >> 4:X}";
			}
			else if ((instruction & 0xF00F) == 0x8005)
			{
				return $"SUB V{(instruction & 0xF00) >> 8:X}, V{(instruction & 0xF0) >> 4:X}";
			}
			else if ((instruction & 0xF00F) == 0x8006)
			{
				return $"SHR V{(instruction & 0xF00) >> 8:X} {{, V{(instruction & 0xF0) >> 4:X} }}";
			}
			else if ((instruction & 0xF00F) == 0x8007)
			{
				return $"SUBN V{(instruction & 0xF00) >> 8:X}, V{(instruction & 0xF0) >> 4:X}";
			}
			else if ((instruction & 0xF00F) == 0x800E)
			{
				return $"SHL V{(instruction & 0xF00) >> 8:X} {{, V{(instruction & 0xF0) >> 4:X} }}";
			}
			else if ((instruction & 0xF00F) == 0x9000)
			{
				return $"SNE V{(instruction & 0xF00) >> 8:X}, V{(instruction & 0xF0) >> 4:X}";
			}
			else if ((instruction & 0xF000) == 0xA000)
			{
				return $"LD I, 0x{instruction & 0xFFF:X3}";
			}
			else if ((instruction & 0xF000) == 0xB000)
			{
				return $"JP V0, 0x{instruction & 0xFFF:X3}";
			}
			else if ((instruction & 0xF000) == 0xC000)
			{
				return $"RND V{(instruction & 0xF00) >> 8:X}, 0x{instruction & 0xFF:X2}";
			}
			else if ((instruction & 0xF000) == 0xD000)
			{
				return $"DRW V{(instruction & 0xF00) >> 8:X}, V{(instruction & 0xF0) >> 4:X}, 0x{instruction & 0xF:X}";
			}
			else if ((instruction & 0xF0FF) == 0xE09E)
			{
				return $"SKP V{(instruction & 0xF00) >> 8:X}";
			}
			else if ((instruction & 0xF0FF) == 0xE0A1)
			{
				return $"SKNP V{(instruction & 0xF00) >> 8:X}";
			}
			else if ((instruction & 0xF0FF) == 0xF007)
			{
				return $"LD V{(instruction & 0xF00) >> 8:X}, DT";
			}
			else if ((instruction & 0xF0FF) == 0xF00A)
			{
				return $"LD V{(instruction & 0xF00) >> 8:X}, K";
			}
			else if ((instruction & 0xF0FF) == 0xF015)
			{
				return $"LD DT, V{(instruction & 0xF00) >> 8:X}";
			}
			else if ((instruction & 0xF0FF) == 0xF018)
			{
				return $"LD ST, V{(instruction & 0xF00) >> 8:X}";
			}
			else if ((instruction & 0xF0FF) == 0xF01E)
			{
				return $"ADD I, V{(instruction & 0xF00) >> 8:X}";
			}
			else if ((instruction & 0xF0FF) == 0xF029)
			{
				// find good asm for load font sprite address for character Vx into I
				return $"LD I, &F[V{(instruction & 0xF00) >> 8:X}]"; // TODO: dis make sense?
			}
			else if ((instruction & 0xF0FF) == 0xF033)
			{
				// find good asm for load BCD into Mem[I]...Mem[I+2]
				return $"LDBCD V{(instruction & 0xF00) >> 8:X}"; // TODO: mess
			}
			else if ((instruction & 0xF0FF) == 0xF055)
			{
				// find good asm for memcpy src=V0...Vx, dest=Mem[I]...Mem[I+x]
				return $"LD [I], V{(instruction & 0xF00) >> 8:X}"; // TODO: mess
			}
			else if ((instruction & 0xF0FF) == 0xF065)
			{
				// find good asm for memcpy src=Mem[I]...Mem[I+x], dest=V0...Vx
				return $"LD V{(instruction & 0xF00) >> 8:X}, [I]"; // TODO: mess
			}
			else
			{
				throw new Exception($"Invalid instruction 0x{instruction:X4}");
			}
		}
	}
}
