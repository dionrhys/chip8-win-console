using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CHIP_8
{
	public struct Instruction
	{
		private readonly ushort instruction;

		public Instruction(ushort instruction)
		{
			this.instruction = instruction;
		}

		public ushort Value => instruction;
		public ushort NNN => (ushort)(instruction & 0xFFF);
		public byte X => (byte)((instruction & 0xF00) >> 8);
		public byte Y => (byte)((instruction & 0xF0) >> 4);
		public byte KK => (byte)(instruction & 0xFF);
		public byte K => (byte)(instruction & 0xF);
	}
}
