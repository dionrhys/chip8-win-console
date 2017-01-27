using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CHIP_8
{
	class Program
	{
		static void Main(string[] args)
		{
			try
			{
				Console.Write(">");
				var gamePath = Console.ReadLine();

				var chip8 = new Chip8();
				chip8.Load(gamePath);
				chip8.Run();
			}
			catch (Exception ex)
			{
				Console.BackgroundColor = ConsoleColor.Black;
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(ex);
				Console.WriteLine("Press any key to exit . . .");
				Console.ReadKey(true);
			}
		}
	}
}
