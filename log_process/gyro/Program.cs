using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpPlot;

namespace gyro
{
	class Program
	{
		static Program()
		{
			stInputDir = Environment.CurrentDirectory + "\\..\\..\\rawdata";
			stOutputDir = Environment.CurrentDirectory + "\\..\\..\\out";
		}

		private static string stInputDir;
		private static string stOutputDir;

		private static List<LogData> logdata = new List<LogData>();

		static void Main(string[] args)
		{
			List<string> filefullpaths = Directory.EnumerateFiles(stInputDir, "*.csv", SearchOption.TopDirectoryOnly).ToList();
			

			foreach (string item in filefullpaths)
			{
				logdata.Add(new LogData());
				int index = logdata.Count - 1;
				using (StreamReader sr = new StreamReader(item))
				{
					string line = sr.ReadLine();
					logdata[index].Lable = line.Substring(1, line.Length - 1);
					while (!sr.EndOfStream)
					{
						line = sr.ReadLine();
						string[] div_line = line.Split(',');
						logdata[index].time.Add(ParseDateTime(div_line[0]));
						logdata[index].x.Add(Double.Parse(div_line[1]));
						logdata[index].y.Add(Double.Parse(div_line[2]));
						logdata[index].z.Add(Double.Parse(div_line[3]));
					}
				}
				logdata[index].OffsetTime();
				WriteLogData(Path.Combine(stOutputDir, logdata[index].Lable + ".txt"), logdata[index]);
			}

			PlotLogData(Path.Combine(stOutputDir, logdata[1].Lable + ".txt"), Path.Combine(stOutputDir, logdata[0].Lable + ".txt"));
		}



		static double ParseDateTime(string arg)
		{
			string[] date_time = arg.Split(' ');
			string date = date_time[0];		// 日は取り敢えず無視
			string[] h_m_s = date_time[1].Split(':');	// 時間，分，秒

			double second = Double.Parse(h_m_s[2]) + Double.Parse(h_m_s[1]) * 60 + Double.Parse(h_m_s[0]) * 60 * 60;	// 秒単位に

			return second;
		}

		private static void WriteLogData(string path, LogData data)
		{
			if (!Directory.Exists(stOutputDir))
			{
				Directory.CreateDirectory(stOutputDir);
			}

			using (StreamWriter sw = new StreamWriter(path))
			{
				sw.WriteLine("#{0}\t{1}\t{2}\t{3}", "time[s]", "x", "y", "z");
				for (int i = 0; i < data.time.Count; i++)
				{
					sw.WriteLine("{0:f6}\t{1:e3}\t{2:e3}\t{3:e3}", data.time[i], data.x[i], data.y[i], data.z[i]); 
				}
			}
		}

		private static void PlotLogData(string path_r,string path_l)
		{
			#region gnuplot
			using (PlotStream gnuplot = new PlotStream())
			{
				gnuplot.Start();
				gnuplot.ChangeDirectory(stOutputDir);
				gnuplot.SetLineStyle(1, new LineStyle(1, 1, Color.Green));
				gnuplot.SetLineStyle(2, new LineStyle(1, 1, Color.Blue));
				gnuplot.SetLineStyle(3, new LineStyle(1, 1, Color.Red));
				gnuplot.SetLineStyle(4, new LineStyle(1, 1, Color.Purple));
				gnuplot.SetLineStyle(5, new LineStyle(1, 1, Color.Cyan));
				gnuplot.SetLineStyle(6, new LineStyle(1, 1, Color.Black));

				gnuplot.SetLegendPosition(LegendPosition.left,LegendPosition.top);
				gnuplot.SetLegendFont(24, "Helvetica");
				//gnuplot.SetLegendTitle("");

				gnuplot.SetXLabel("time [s]", 36, "Helvetica");
				gnuplot.SetYLabel("??", 36, "Helvetica");

				gnuplot.SetTicsFont(28, "Helvetica");
				gnuplot.SetXRange(0, 3200);
				gnuplot.SetXTics(0, 400, false);
				gnuplot.SetYRange(-250, 250);
				gnuplot.SetYTics(-250, 25);

				gnuplot.SetMargin(7.5, 10.5, 1, 3.5);
				gnuplot.SetGrid();

				gnuplot.PlotFromFile(path_r, 1, 2, PlottingStyle.lines, 1, PlotAxis.x1y1, "right x");
				gnuplot.ReplotFromFile(path_r, 1, 3, PlottingStyle.lines, 2, PlotAxis.x1y1, "right y");
				gnuplot.ReplotFromFile(path_r, 1, 4, PlottingStyle.lines, 3, PlotAxis.x1y1, "right z");
				gnuplot.ReplotFromFile(path_l, 1, 2, PlottingStyle.lines, 4, PlotAxis.x1y1, "left x");
				gnuplot.ReplotFromFile(path_l, 1, 3, PlottingStyle.lines, 5, PlotAxis.x1y1, "left y");
				gnuplot.ReplotFromFile(path_l, 1, 4, PlottingStyle.lines, 6, PlotAxis.x1y1, "left z");

				gnuplot.SetTerminal(Terminal.postscript, 20, 14, 28, "Helvetica");
				gnuplot.SetOutput("gyro" + ".eps");
				gnuplot.Replot();
				gnuplot.SetOutput();
				gnuplot.Stream.WriteLine("unset style line");
				gnuplot.Stream.WriteLine("set terminal wxt");

			#endregion

			
			}
		}
	}

	class LogData
	{
		public string Lable { get; set; }

		public List<double> time = new List<double>();
		public List<double> x=new List<double>();
		public List<double> y=new List<double>();
		public List<double> z=new List<double>();

		public void OffsetTime()
		{
			if (time.Count == 0)
			{
				throw new InvalidOperationException();
			}

			double offset = time[0];
			for (int i = 0; i < time.Count; i++)
			{
				time[i] -= offset;
			}
		}
	}
}
