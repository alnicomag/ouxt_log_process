using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace eps2pdf
{
	class Program
	{
		static void Main(string[] args)
		{
			Directory.SetCurrentDirectory(Environment.CurrentDirectory + "\\..\\..\\..\\gyro\\out");

			List<string> filefullpaths = Directory.EnumerateFiles(Environment.CurrentDirectory, "*.eps", SearchOption.TopDirectoryOnly).ToList();

			foreach (var item in filefullpaths)
			{
				Process.Start("epstopdf.pl", Path.GetFileName(@item));
			}
		}
	}
}
